<#
.SYNOPSIS
Valida que remover a associação readers revoga vault:read pelo BFF.

.DESCRIPTION
Remove temporariamente um usuário do grupo /altivy/readers, executa o cenário
E2E de negação para o vault informado e restaura a associação no bloco finally.
O script usa somente variáveis locais em
%APPDATA%\Microsoft\UserSecrets\Altivy.Notes\.env e não imprime secrets.

.PARAMETER BaseUrl
URL local da Admin REST do Keycloak. Padrão: https://localhost:7443.

.PARAMETER Realm
Realm dedicado. Padrão: altivy.

.PARAMETER Username
Usuário que perderá acesso temporariamente. Padrão: lzocateli.

.PARAMETER VaultId
Vault autorizado antes da revogação. Padrão: lab-0001.

.PARAMETER VerifyOnly
Verifica somente se o usuário pertence ao grupo /altivy/readers, sem alterar o Keycloak.

.PARAMETER RestoreOnly
Restaura a associação do usuário ao grupo /altivy/readers sem executar E2E.

.EXAMPLE
.\tools\scripts\test-keycloak-revocation.ps1

.NOTES
Requer PowerShell 7.4+, Docker Desktop, Keycloak e o Compose E2E saudáveis.
Consulte deploy/e2e/README.md.

.LINK
../../deploy/e2e/README.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^https?://')]
    [string] $BaseUrl = 'https://localhost:7443',

    [ValidatePattern('^[a-z0-9-]+$')]
    [string] $Realm = 'altivy',

    [ValidatePattern('^[a-z0-9._-]+$')]
    [string] $Username = 'lzocateli',

    [ValidatePattern('^[a-z0-9-]+$')]
    [string] $VaultId = 'lab-0001',

    [switch] $VerifyOnly,

    [switch] $RestoreOnly,

    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}

if ($RemainingArguments.Count -gt 0) {
    throw "Argumento desconhecido: $($RemainingArguments -join ' '). Use --help."
}

$BaseUrl = $BaseUrl.TrimEnd('/')
$environmentPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/.env'
$environmentPath = (Resolve-Path $environmentPath).Path

foreach ($line in Get-Content -LiteralPath $environmentPath) {
    if ($line -match '^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)=(?<value>.*)$') {
        [Environment]::SetEnvironmentVariable($Matches.name, $Matches.value.Trim(), 'Process')
    }
}

if (-not $env:ALTIVY_KEYCLOAK_ADMIN_PASSWORD) {
    throw "ALTIVY_KEYCLOAK_ADMIN_PASSWORD não está definido em $environmentPath."
}

$tokenResponse = Invoke-RestMethod -Method Post `
    -Uri "$BaseUrl/realms/master/protocol/openid-connect/token" `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{ grant_type = 'password'; client_id = 'admin-cli'; username = $env:ALTIVY_KEYCLOAK_ADMIN_USERNAME; password = $env:ALTIVY_KEYCLOAK_ADMIN_PASSWORD } `
    -NoProxy
$headers = @{ Authorization = "Bearer $($tokenResponse.access_token)" }
$encodedUsername = [Uri]::EscapeDataString($Username)
$users = @(Invoke-RestMethod -Uri "$BaseUrl/admin/realms/$Realm/users?username=$encodedUsername&exact=true" -Headers $headers -NoProxy)
if ($users.Count -eq 0) {
    $users = @(Invoke-RestMethod -Uri "$BaseUrl/admin/realms/$Realm/users?search=$encodedUsername" -Headers $headers -NoProxy)
}

$user = $users | Where-Object { $_.username -eq $Username } | Select-Object -First 1
if (-not $user) {
    throw "Usuário $Username não foi encontrado no realm $Realm."
}

$altivyGroups = @(Invoke-RestMethod -Uri "$BaseUrl/admin/realms/$Realm/groups?search=altivy&exact=true" -Headers $headers -NoProxy)
$altivyGroup = $altivyGroups | Where-Object { $_.path -eq '/altivy' } | Select-Object -First 1
if (-not $altivyGroup) {
    throw 'Grupo /altivy não foi encontrado.'
}

$readerGroups = @(Invoke-RestMethod -Uri "$BaseUrl/admin/realms/$Realm/groups/$($altivyGroup.id)/children?search=readers&exact=true" -Headers $headers -NoProxy)
$readerGroup = $readerGroups | Where-Object { $_.path -eq '/altivy/readers' } | Select-Object -First 1
if (-not $readerGroup) {
    throw 'Grupo /altivy/readers não foi encontrado.'
}

$membershipPath = "$BaseUrl/admin/realms/$Realm/users/$($user.id)/groups/$($readerGroup.id)"
$memberGroups = @(Invoke-RestMethod -Uri "$BaseUrl/admin/realms/$Realm/users/$($user.id)/groups" -Headers $headers -NoProxy)
$wasMember = $memberGroups.path -contains '/altivy/readers'
if ($RestoreOnly) {
    if (-not $wasMember) {
        Invoke-RestMethod -Method Put -Uri $membershipPath -Headers $headers -NoProxy
        Write-Output "Associação /altivy/readers restaurada para $Username."
    }
    else {
        Write-Output "Associação /altivy/readers já está ativa para $Username."
    }

    exit 0
}

if ($VerifyOnly) {
    Write-Output "Associação /altivy/readers ativa para ${Username}: $wasMember"
    exit 0
}

if (-not $wasMember) {
    throw "Usuário $Username não pertence ao grupo /altivy/readers; a revogação não pode ser comprovada."
}

try {
    Invoke-RestMethod -Method Delete -Uri $membershipPath -Headers $headers -NoProxy
    Write-Output "Associação /altivy/readers removida temporariamente para $Username."

    & 'C:\Program Files\Docker\Docker\resources\bin\docker.exe' run --rm `
        --network host `
        --volume "${PWD}/frontend/tests:/app/tests:ro" `
        --env-file "$environmentPath" `
        --env ALTIVY_E2E_BASE_URL=https://localhost:7443 `
        --env ALTIVY_E2E_DENIED_VAULT_ID=$VaultId `
        lzocateli/playwright-e2e:0.1.0 `
        --browser chromium -k denied_vault
    if ($LASTEXITCODE -ne 0) {
        throw "O cenário E2E de revogação falhou com código $LASTEXITCODE."
    }
}
finally {
    if ($wasMember) {
        $restoreToken = Invoke-RestMethod -Method Post `
            -Uri "$BaseUrl/realms/master/protocol/openid-connect/token" `
            -ContentType 'application/x-www-form-urlencoded' `
            -Body @{ grant_type = 'password'; client_id = 'admin-cli'; username = $env:ALTIVY_KEYCLOAK_ADMIN_USERNAME; password = $env:ALTIVY_KEYCLOAK_ADMIN_PASSWORD } `
            -NoProxy
        $restoreHeaders = @{ Authorization = "Bearer $($restoreToken.access_token)" }
        Invoke-RestMethod -Method Put -Uri $membershipPath -Headers $restoreHeaders -NoProxy
        Write-Output "Associação /altivy/readers restaurada para $Username."
    }
}
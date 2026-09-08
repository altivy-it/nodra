<#
.SYNOPSIS
Provisiona autorização UMA de proprietário para um vault específico.

.DESCRIPTION
Cria ou reutiliza o recurso vault-{VaultId}, uma policy de usuário para o
proprietário e permissions de vault:read, vault:write, vault:manage e
vault:delete no Authorization Services do client altivy-api. Deve ser executado
por administrador do Keycloak; o runtime do Altivy permanece com uma_protection.

.PARAMETER VaultId
Identificador opaco e estável do vault, com letras minúsculas, números e hífen.

.PARAMETER OwnerUsername
Username exato do proprietário já existente no realm Altivy.

.PARAMETER BaseUrl
URL do Keycloak. Padrão: http://localhost:8080.

.PARAMETER DryRun
Exibe o plano sem acessar a rede, solicitar credenciais ou alterar o Keycloak.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./tools/scripts/provision-vault-owner-authorization.ps1 --help

.EXAMPLE
./tools/scripts/provision-vault-owner-authorization.ps1 `
  -VaultId vault-prod-001 `
  -OwnerUsername alice

.EXAMPLE
./tools/scripts/provision-vault-owner-authorization.ps1 `
  -VaultId lab-0001 `
  -OwnerUsername lzocateli `
  -BaseUrl https://localhost:7443 `
  -DryRun

.INPUTS
Nenhum.

.OUTPUTS
Resumo sanitizado dos recursos criados ou reutilizados.

.NOTES
Requer PowerShell 7.4 ou superior e Keycloak 26.7.0. O fluxo funcional requer
uma conta administrativa do realm e ALTIVY_KEYCLOAK_ADMIN_PASSWORD no ambiente
ou em %APPDATA%\Microsoft\UserSecrets\Altivy.Notes\.env. Não use este script
para conceder acesso a usuários não autorizados; eventos administrativos do
Keycloak são a trilha de auditoria.

.LINK
../../docs/configuracao-keycloak.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^[a-z0-9-]{1,64}$')]
    [string] $VaultId,

    [ValidatePattern('^[^\s]+$')]
    [string] $OwnerUsername,

    [ValidatePattern('^https?://')]
    [string] $BaseUrl = 'http://localhost:8080',

    [switch] $DryRun,

    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}

if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumentos não reconhecidos: $($RemainingArguments -join ', '). Use --help.")
    exit 2
}

if ([string]::IsNullOrWhiteSpace($VaultId) -or [string]::IsNullOrWhiteSpace($OwnerUsername)) {
    [Console]::Error.WriteLine('VaultId e OwnerUsername são obrigatórios. Use --help.')
    exit 2
}

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$useNoProxy = ([Uri]$BaseUrl).IsLoopback
$resourceName = "vault-$VaultId"
$scopes = @('vault:read', 'vault:write', 'vault:manage', 'vault:delete')

if ($DryRun) {
    @(
        "realm: altivy",
        "client: altivy-api",
        "recurso: $resourceName",
        "policy de usuário: $resourceName-owner-$OwnerUsername",
        "permissions: $($scopes -join ', ')"
    ) | ForEach-Object { Write-Output "- $_" }
    exit 0
}

function Import-LocalEnvironment {
    $environmentPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/.env'
    if (-not (Test-Path -LiteralPath $environmentPath -PathType Leaf)) { return }

    foreach ($line in Get-Content -LiteralPath $environmentPath) {
        if ($line -match '^\s*(?:export\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.*)\s*$') {
            if (-not [Environment]::GetEnvironmentVariable($Matches.name, 'Process')) {
                $value = $Matches.value.Trim().Trim('"').Trim("'")
                [Environment]::SetEnvironmentVariable($Matches.name, $value, 'Process')
            }
        }
    }
}

function Find-One {
    param([Parameter(Mandatory)][object[]] $Items, [Parameter(Mandatory)][string] $Description)
    if ($Items.Count -ne 1) { throw "$Description deve resolver exatamente um resultado." }
    return $Items[0]
}

Import-LocalEnvironment
$adminUsername = $env:ALTIVY_KEYCLOAK_ADMIN_USERNAME ?? 'admin'
$adminPassword = $env:ALTIVY_KEYCLOAK_ADMIN_PASSWORD
if ([string]::IsNullOrWhiteSpace($adminPassword)) {
    throw 'Defina ALTIVY_KEYCLOAK_ADMIN_PASSWORD em ambiente seguro. Use --help para detalhes.'
}

try {
    $tokenResponse = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post `
        -Uri "$BaseUrl/realms/master/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' -Body @{
        grant_type = 'password'; client_id = 'admin-cli'; username = $adminUsername; password = $adminPassword
    }
    if (-not $tokenResponse.access_token) { throw 'Não foi possível obter token administrativo.' }
    $headers = @{ Authorization = "Bearer $($tokenResponse.access_token)" }

    $apiClient = Find-One @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$BaseUrl/admin/realms/altivy/clients?clientId=altivy-api" -Headers $headers) 'Client altivy-api'
    $owner = Find-One @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$BaseUrl/admin/realms/altivy/users?username=$([Uri]::EscapeDataString($OwnerUsername))&exact=true" -Headers $headers |
        Where-Object { $_.username -eq $OwnerUsername }) "Usuário '$OwnerUsername'"
    $authzBase = "$BaseUrl/admin/realms/altivy/clients/$($apiClient.id)/authz/resource-server"

    $scopeRecords = @{}
    foreach ($scopeName in $scopes) {
        $scopeRecords[$scopeName] = Find-One @(Invoke-RestMethod -NoProxy:$useNoProxy `
                -Uri "$authzBase/scope?name=$([Uri]::EscapeDataString($scopeName))&exact=true" -Headers $headers) "Escopo '$scopeName'"
    }

    $resource = @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$authzBase/resource?name=$([Uri]::EscapeDataString($resourceName))&exactName=true" -Headers $headers |
        Where-Object { $_.name -eq $resourceName })
    if ($resource.Count -eq 0) {
        $resource = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post -Uri "$authzBase/resource" -Headers $headers `
            -ContentType 'application/json' -Body (@{
                name               = $resourceName
                uris               = @("/api/v1/vaults/$VaultId")
                scopes             = @($scopes | ForEach-Object { @{ id = $scopeRecords[$_].id; name = $_ } })
                ownerManagedAccess = $false
            } | ConvertTo-Json -Depth 5 -Compress)
        Write-Output "Recurso '$resourceName' criado."
    }
    else {
        $resource = Find-One $resource "Recurso '$resourceName'"
        Write-Output "Recurso '$resourceName' já existe."
    }

    $policyName = "$resourceName-owner-$OwnerUsername"
    $policy = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$authzBase/policy?name=$([Uri]::EscapeDataString($policyName))" -Headers $headers |
        Where-Object { $_.name -eq $policyName })
    if ($policy.Count -eq 0) {
        $policy = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post -Uri "$authzBase/policy/user" -Headers $headers `
            -ContentType 'application/json' -Body (@{
                name = $policyName; type = 'user'; logic = 'POSITIVE'; decisionStrategy = 'UNANIMOUS'; users = @([string]$owner.id)
            } | ConvertTo-Json -Depth 5 -Compress)
        Write-Output "Policy '$policyName' criada."
    }
    else {
        $policy = Find-One $policy "Policy '$policyName'"
        Write-Output "Policy '$policyName' já existe."
    }

    foreach ($scopeName in $scopes) {
        $permissionName = "$resourceName-$($scopeName.Replace(':', '-'))"
        $permission = @(Invoke-RestMethod -NoProxy:$useNoProxy `
                -Uri "$authzBase/permission?name=$([Uri]::EscapeDataString($permissionName))" -Headers $headers |
            Where-Object { $_.name -eq $permissionName })
        if ($permission.Count -gt 0) {
            $null = Find-One $permission "Permission '$permissionName'"
            Write-Output "Permission '$permissionName' já existe."
            continue
        }

        $null = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post -Uri "$authzBase/permission/resource" -Headers $headers `
            -ContentType 'application/json' -Body (@{
                name = $permissionName; type = 'resource'; logic = 'POSITIVE'; decisionStrategy = 'UNANIMOUS'
                resources = @([string]$resource._id); scopes = @([string]$scopeRecords[$scopeName].id); policies = @([string]$policy.id)
            } | ConvertTo-Json -Depth 5 -Compress)
        Write-Output "Permission '$permissionName' criada."
    }
}
catch {
    if ($_.Exception.Message -match 'HTTP 401') {
        throw 'Falha de autenticação. Verifique ALTIVY_KEYCLOAK_ADMIN_PASSWORD.'
    }
    throw
}
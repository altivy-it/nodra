<#
.SYNOPSIS
Provisiona a leitura do vault de exemplo para todos os usuários do realm.

.DESCRIPTION
Cria ou reutiliza o recurso vault-nodra-exemplo, uma policy de role baseada no
papel padrão do realm (default-roles-altivy) e a permission de vault:read no
Authorization Services do client altivy-api. Não concede vault:write,
vault:manage nem vault:delete: a API recusa essas operações no vault de
exemplo de qualquer forma. Deve ser executado por administrador do Keycloak; o
runtime do Nodra não cria policies.

.PARAMETER BaseUrl
URL do Keycloak. Padrão: http://localhost:8080.

.PARAMETER Realm
Realm do Nodra. Padrão: altivy.

.PARAMETER DryRun
Exibe o plano sem acessar a rede, solicitar credenciais ou alterar o Keycloak.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./tools/scripts/provision-sample-vault-authorization.ps1 --help

.EXAMPLE
./tools/scripts/provision-sample-vault-authorization.ps1 -BaseUrl https://localhost:7443 -DryRun

.EXAMPLE
./tools/scripts/provision-sample-vault-authorization.ps1 -BaseUrl https://localhost:7443

.INPUTS
Nenhum.

.OUTPUTS
Resumo sanitizado dos recursos criados ou reutilizados.

.NOTES
Requer PowerShell 7.4 ou superior e Keycloak 26.7.0. O fluxo funcional requer
o client restrito altivy-provisioner e ALTIVY_PROVISIONER_CLIENT_SECRET no
ambiente ou em %APPDATA%\Microsoft\UserSecrets\Altivy.Notes\.env.

.LINK
../../docs/configuracao-keycloak.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^https?://')]
    [string] $BaseUrl = 'http://localhost:8080',

    [ValidatePattern('^[A-Za-z0-9_-]{1,64}$')]
    [string] $Realm = 'altivy',

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

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$useNoProxy = ([Uri]$BaseUrl).IsLoopback
$vaultId = 'nodra-exemplo'
$resourceName = "vault-$vaultId"
$scopeName = 'vault:read'
$defaultRole = "default-roles-$($Realm.ToLowerInvariant())"
$policyName = "$resourceName-all-users"
$permissionName = "$resourceName-vault-read"

if ($DryRun) {
    @(
        "realm: $Realm",
        'client: altivy-api',
        "recurso: $resourceName (somente $scopeName)",
        "policy de role: $policyName ($defaultRole)",
        "permission: $permissionName"
    ) | ForEach-Object { Write-Output "- $_" }
    exit 0
}

function Import-LocalEnvironment {
    if ([string]::IsNullOrWhiteSpace($env:APPDATA)) { return }
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
$provisionerSecret = $env:ALTIVY_PROVISIONER_CLIENT_SECRET
if ([string]::IsNullOrWhiteSpace($provisionerSecret)) {
    throw 'Defina ALTIVY_PROVISIONER_CLIENT_SECRET em ambiente seguro. Use --help para detalhes.'
}

try {
    $tokenResponse = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post `
        -Uri "$BaseUrl/realms/$Realm/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' -Body @{
        grant_type = 'client_credentials'; client_id = 'altivy-provisioner'; client_secret = $provisionerSecret
    }
    if (-not $tokenResponse.access_token) { throw 'Não foi possível obter token administrativo.' }
    $headers = @{ Authorization = "Bearer $($tokenResponse.access_token)" }

    $apiClient = Find-One @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$BaseUrl/admin/realms/$Realm/clients?clientId=altivy-api" -Headers $headers) 'Client altivy-api'
    $authzBase = "$BaseUrl/admin/realms/$Realm/clients/$($apiClient.id)/authz/resource-server"

    $scope = Find-One @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$authzBase/scope?name=$([Uri]::EscapeDataString($scopeName))&exact=true" -Headers $headers) "Escopo '$scopeName'"

    $resource = @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$authzBase/resource?name=$([Uri]::EscapeDataString($resourceName))&exactName=true" -Headers $headers |
        Where-Object { $_.name -eq $resourceName })
    if ($resource.Count -eq 0) {
        $resource = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post -Uri "$authzBase/resource" -Headers $headers `
            -ContentType 'application/json' -Body (@{
                name               = $resourceName
                uris               = @("/api/v1/vaults/$vaultId")
                scopes             = @(@{ id = $scope.id; name = $scopeName })
                ownerManagedAccess = $false
            } | ConvertTo-Json -Depth 5 -Compress)
        Write-Output "Recurso '$resourceName' criado."
    }
    else {
        $resource = Find-One $resource "Recurso '$resourceName'"
        Write-Output "Recurso '$resourceName' já existe."
    }

    $policy = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$authzBase/policy?name=$([Uri]::EscapeDataString($policyName))" -Headers $headers |
        Where-Object { $_.name -eq $policyName })
    if ($policy.Count -eq 0) {
        # O Keycloak resolve a role pelo nome; fetchRoles consulta o usuário e não depende das roles presentes no token.
        $policy = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post -Uri "$authzBase/policy/role" -Headers $headers `
            -ContentType 'application/json' -Body (@{
                name = $policyName; type = 'role'; logic = 'POSITIVE'; decisionStrategy = 'UNANIMOUS'; fetchRoles = $true
                roles = @(@{ id = $defaultRole; required = $false })
            } | ConvertTo-Json -Depth 5 -Compress)
        Write-Output "Policy '$policyName' criada."
    }
    else {
        $policy = Find-One $policy "Policy '$policyName'"
        Write-Output "Policy '$policyName' já existe."
    }

    $permission = @(Invoke-RestMethod -NoProxy:$useNoProxy `
            -Uri "$authzBase/permission?name=$([Uri]::EscapeDataString($permissionName))" -Headers $headers |
        Where-Object { $_.name -eq $permissionName })
    if ($permission.Count -gt 0) {
        $null = Find-One $permission "Permission '$permissionName'"
        Write-Output "Permission '$permissionName' já existe."
    }
    else {
        $null = Invoke-RestMethod -NoProxy:$useNoProxy -Method Post -Uri "$authzBase/permission/resource" -Headers $headers `
            -ContentType 'application/json' -Body (@{
                name = $permissionName; type = 'resource'; logic = 'POSITIVE'; decisionStrategy = 'UNANIMOUS'
                resources = @([string]$resource._id); scopes = @([string]$scope.id); policies = @([string]$policy.id)
            } | ConvertTo-Json -Depth 5 -Compress)
        Write-Output "Permission '$permissionName' criada."
    }
}
catch {
    if ($_.Exception.Message -match 'HTTP 401') {
        throw 'Falha de autenticação. Verifique ALTIVY_PROVISIONER_CLIENT_SECRET.'
    }
    throw
}

<#
.SYNOPSIS
Cria uma política UMA de concessão baseada em grupo para vault:read.

.DESCRIPTION
Cria uma política de grupo no Authorization Services do altivy-api que
concede vault:read para membros de /altivy/readers.

.PARAMETER BaseUrl
URL do Keycloak. Padrão: http://localhost:8080.

.PARAMETER DryRun
Exibe o plano sem acessar a rede.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./create-authorization-policy.ps1 --help

.EXAMPLE
./create-authorization-policy.ps1 -DryRun

.EXAMPLE
./create-authorization-policy.ps1

.INPUTS
Nenhum.

.OUTPUTS
Resumo sanitizado dos recursos criados ou reutilizados.

.NOTES
Requer PowerShell 7.4 ou superior e Keycloak 26.7.0.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^https?://')]
    [string] $BaseUrl = 'http://localhost:8080',

    [switch] $DryRun,

    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'

if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}

if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumento desconhecido: $($RemainingArguments -join ' '). Use --help.")
    exit 2
}

$BaseUrl = $BaseUrl.TrimEnd('/')
$useNoProxy = ([Uri]$BaseUrl).IsLoopback

function Import-LocalEnvironment {
    if ([string]::IsNullOrWhiteSpace($env:APPDATA)) {
        return
    }

    $environmentPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/.env'
    if (-not (Test-Path -LiteralPath $environmentPath -PathType Leaf)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $environmentPath) {
        if ($line -match '^\s*(?:export\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.*)\s*$') {
            $name = $Matches.name
            if (-not [Environment]::GetEnvironmentVariable($name, 'Process')) {
                $value = $Matches.value.Trim()
                if ($value.Length -ge 2 -and $value[0] -eq '"' -and $value[$value.Length - 1] -eq '"') {
                    $value = $value.Substring(1, $value.Length - 2)
                }
                elseif ($value.Length -ge 2 -and $value[0] -eq "'" -and $value[$value.Length - 1] -eq "'") {
                    $value = $value.Substring(1, $value.Length - 2)
                }
                [Environment]::SetEnvironmentVariable($name, $value, 'Process')
            }
        }
    }
}

Import-LocalEnvironment

$provisionerSecret = $env:ALTIVY_PROVISIONER_CLIENT_SECRET

if ($DryRun) {
    $plan = @(
        "realm: altivy",
        "client: altivy-api",
        "política: readers-vault-read (tipo: group, recurso: vault-lab-0001, escopo: vault:read, grupo: /altivy/readers)",
        "permissão: vault-lab-0001:vault:read permite readers-vault-read"
    )
    $plan | ForEach-Object { Write-Output "- $_" }
    exit 0
}
if (-not $provisionerSecret) {
    throw 'Defina ALTIVY_PROVISIONER_CLIENT_SECRET. Use --help para detalhes.'
}

try {
    $tokenResponse = Invoke-RestMethod `
        -NoProxy:$useNoProxy `
        -Method Post `
        -Uri "$BaseUrl/realms/altivy/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body @{
            grant_type    = 'client_credentials'
            client_id     = 'altivy-provisioner'
            client_secret = $provisionerSecret
        }
    
    if (-not $tokenResponse.access_token) {
        throw 'Não foi possível obter token administrativo.'
    }
    
    $accessToken = $tokenResponse.access_token
    $headers = @{ Authorization = "Bearer $accessToken" }
    
    # Obter cliente altivy-api
    $apiClients = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$BaseUrl/admin/realms/altivy/clients?clientId=altivy-api" -Headers $headers)
    if ($apiClients.Count -eq 0) {
        throw 'Client altivy-api não encontrado.'
    }
    $apiClient = $apiClients[0]
    $rsId = $apiClient.id
    
    # Grupos aninhados precisam ser resolvidos por children no Admin API.
    $parentGroups = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$BaseUrl/admin/realms/altivy/groups?search=altivy&exact=true" -Headers $headers | Where-Object { $_.path -eq '/altivy' })
    if ($parentGroups.Count -eq 0) {
        throw 'Grupo /altivy não encontrado.'
    }
    $readerGroups = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$BaseUrl/admin/realms/altivy/groups/$($parentGroups[0].id)/children?search=readers&exact=true" -Headers $headers | Where-Object { $_.path -eq '/altivy/readers' })
    if ($readerGroups.Count -eq 0) {
        throw 'Grupo /altivy/readers não encontrado.'
    }
    $readerGroup = $readerGroups[0]
    
    $authzBase = "$BaseUrl/admin/realms/altivy/clients/$rsId/authz/resource-server"
    $policyName = 'readers-vault-read'
    $existingPolicies = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$authzBase/policy?name=$policyName" -Headers $headers | Where-Object { $_.name -eq $policyName })

    if ($existingPolicies.Count -gt 0) {
        $createdPolicy = $existingPolicies[0]
        Write-Output "Política '$policyName' já existe."
    }
    else {
        $policyBody = @{
            name              = $policyName
            type              = 'group'
            logic             = 'POSITIVE'
            decisionStrategy  = 'UNANIMOUS'
            groupsClaim       = 'groups'
            groups            = @(@{ id = $readerGroup.id; path = $readerGroup.path })
        }

        $createdPolicy = Invoke-RestMethod `
            -NoProxy:$useNoProxy `
            -Method Post `
            -Uri "$authzBase/policy/group" `
            -Headers $headers `
            -ContentType 'application/json' `
            -Body ($policyBody | ConvertTo-Json -Depth 5 -Compress)

        Write-Output "Criada política '$policyName' com ID $($createdPolicy.id)."
    }

    $resourceCandidates = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$authzBase/resource?name=vault-lab-0001&exactName=true" -Headers $headers)
    if ($resourceCandidates.Count -eq 0) {
        throw 'Recurso vault-lab-0001 não encontrado. Execute o spike com -ReconcileOnly antes de provisionar a policy.'
    }
    $resource = $resourceCandidates[0]

    $scopeCandidates = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$authzBase/scope?name=vault:read&exact=true" -Headers $headers)
    if ($scopeCandidates.Count -eq 0) {
        throw 'Escopo vault:read não encontrado no Authorization Services.'
    }
    $scope = $scopeCandidates[0]

    $permissionName = 'vault-lab-0001-vault-read'
    $existingPermissions = @(Invoke-RestMethod -NoProxy:$useNoProxy -Uri "$authzBase/permission?name=$permissionName" -Headers $headers | Where-Object { $_.name -eq $permissionName })
    if ($existingPermissions.Count -eq 0) {
        $permissionBody = @{
            name             = $permissionName
            type             = 'resource'
            logic            = 'POSITIVE'
            decisionStrategy = 'UNANIMOUS'
            resources        = @([string]$resource._id)
            scopes           = @([string]$scope.id)
            policies         = @([string]$createdPolicy.id)
        }

        $createdPermission = Invoke-RestMethod `
            -NoProxy:$useNoProxy `
            -Method Post `
            -Uri "$authzBase/permission/resource" `
            -Headers $headers `
            -ContentType 'application/json' `
            -Body ($permissionBody | ConvertTo-Json -Depth 5 -Compress)

        Write-Output "Criada permissão '$permissionName' com ID $($createdPermission.id)."
    }
    else {
        Write-Output "Permissão '$permissionName' já existe."
    }
    
}
catch {
    if ($_.Exception.Message -match 'HTTP 401') {
        throw 'Falha de autenticação. Verifique ALTIVY_PROVISIONER_CLIENT_SECRET.'
    }
    throw $_
}
finally {
    $adminPasswordPlain = $null
}

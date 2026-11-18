<#
.SYNOPSIS
Provisiona as roles e grants do Nodra no PostgreSQL principal do laboratório.

.DESCRIPTION
Lê as connection strings locais de integração, valida que apontam para
localhost:5432 e executa o bootstrap SQL idempotente no container PostgreSQL
principal. Senhas são usadas somente em memória e nunca são exibidas.

.PARAMETER ContainerName
Nome do container PostgreSQL principal. O padrão é altivy-postresql.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./tools/scripts/bootstrap-altivy-postgres.ps1 --help

.EXAMPLE
./tools/scripts/bootstrap-altivy-postgres.ps1

.INPUTS
Nenhum.

.OUTPUTS
Saída do psql, sem connection strings ou senhas.

.NOTES
Requer PowerShell 7+, Docker Desktop em execução e psql na imagem do container.
O arquivo em %APPDATA%\Microsoft\UserSecrets\Altivy.Notes\.env deve conter
ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING e
ALTIVY_INTEGRATION_RUNTIME_CONNECTION_STRING para localhost:5432/keycloak.

.LINK
../../backend/libs/infrastructure/data/README.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^[a-zA-Z0-9][a-zA-Z0-9_.-]*$')]
    [string] $ContainerName = 'altivy-postresql',

    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
$environmentPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/.env'

if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}

if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumento desconhecido: $($RemainingArguments -join ' '). Use --help.")
    exit 2
}

function Get-EnvironmentValue {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Path
    )

    $value = $null
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match "^\s*$([regex]::Escape($Name))\s*=\s*(?<value>.*)$") {
            $value = $Matches.value.Trim()
            break
        }
    }

    if ($null -eq $value) {
        throw "Defina $Name em $Path. Use --help para detalhes."
    }

    if ($value.Length -ge 2 -and $value[0] -eq '"' -and $value[$value.Length - 1] -eq '"') {
        return $value.Substring(1, $value.Length - 2)
    }

    if ($value.Length -ge 2 -and $value[0] -eq "'" -and $value[$value.Length - 1] -eq "'") {
        return $value.Substring(1, $value.Length - 2)
    }

    return $value
}

function Get-ValidatedConnectionString {
    param(
        [Parameter(Mandatory)]
        [string] $VariableName,

        [Parameter(Mandatory)]
        [string] $ExpectedUsername,

        [Parameter(Mandatory)]
        [string] $Path
    )

    $connectionString = Get-EnvironmentValue -Name $VariableName -Path $Path
    $databaseHost = [regex]::Match($connectionString, '(?:^|;)Host=(?<value>[^;]+)', 'IgnoreCase').Groups['value'].Value
    $portText = [regex]::Match($connectionString, '(?:^|;)Port=(?<value>[^;]+)', 'IgnoreCase').Groups['value'].Value
    $port = 0
    [void] [int]::TryParse($portText, [ref] $port)
    $database = [regex]::Match($connectionString, '(?:^|;)Database=(?<value>[^;]+)', 'IgnoreCase').Groups['value'].Value
    $username = [regex]::Match($connectionString, '(?:^|;)Username=(?<value>[^;]+)', 'IgnoreCase').Groups['value'].Value
    $password = [regex]::Match($connectionString, '(?:^|;)Password=(?<value>[^;]+)', 'IgnoreCase').Groups['value'].Value
    if ($databaseHost -notin @('localhost', '127.0.0.1') -or $port -ne 5432 -or
        $database -ne 'keycloak' -or $username -ne $ExpectedUsername -or
        [string]::IsNullOrWhiteSpace($password)) {
        throw "$VariableName deve apontar para localhost:5432/keycloak com o usuário $ExpectedUsername."
    }

    return $password
}

if (-not (Test-Path -LiteralPath $environmentPath -PathType Leaf)) {
    throw "Arquivo de ambiente não encontrado: $environmentPath. Use --help para detalhes."
}

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sqlPath = Join-Path $repositoryRoot 'backend/libs/infrastructure/data/sql/001-bootstrap-altivy-roles.sql'
if (-not (Test-Path -LiteralPath $sqlPath -PathType Leaf)) {
    throw "Bootstrap SQL não encontrado: $sqlPath."
}

if ($null -eq (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker não está disponível no PATH.'
}

$migratorPassword = Get-ValidatedConnectionString `
    -VariableName 'ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING' `
    -ExpectedUsername 'altivy_migrator' `
    -Path $environmentPath
$runtimePassword = Get-ValidatedConnectionString `
    -VariableName 'ALTIVY_INTEGRATION_RUNTIME_CONNECTION_STRING' `
    -ExpectedUsername 'altivy_runtime' `
    -Path $environmentPath

Get-Content -Raw -LiteralPath $sqlPath |
    & docker exec -i $ContainerName psql -U keycloak -d keycloak `
        -v ON_ERROR_STOP=1 `
        -v "altivy_migrator_password=$migratorPassword" `
        -v "altivy_runtime_password=$runtimePassword"
if ($LASTEXITCODE -ne 0) {
    throw "O bootstrap PostgreSQL falhou no container $ContainerName."
}
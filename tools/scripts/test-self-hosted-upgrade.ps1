<#
.SYNOPSIS
Testa upgrade de schema e rollback de API em Compose efemero e isolado.
.DESCRIPTION
Cria bundle de migrations da revisao anterior, migra um PostgreSQL descartavel
ate a fronteira escolhida, aplica o bundle atual e executa liveness do API antigo
somente pelo NGINX. Nenhum dado, secret ou container Production e reutilizado.
.PARAMETER PreviousCommit
Commit Git local da versao de migrations anterior, com seus sources completos.
.PARAMETER PreviousMigration
Migration final que representa o schema de origem.
.PARAMETER PreviousApiImage
Imagem API anterior ja presente localmente e fixada por digest OCI.
.PARAMETER CurrentMigrationImage
Imagem do bundle atual ja presente localmente. Padrao: nodra-migrations:0.1.0.
.PARAMETER PostgresImage
Imagem PostgreSQL ja presente localmente. Padrao: imagem PostgreSQL 18 do plano.
.PARAMETER ProjectName
Nome do projeto Compose efemero. Padrao: ID aleatorio altivy-p04-*.
.PARAMETER RemainingArguments
Aceita literalmente --help.
.EXAMPLE
./tools/scripts/test-self-hosted-upgrade.ps1 -PreviousCommit 69431f03b72703983c3e2ad0df49cf1a9b5385c1 -PreviousMigration 20260928234000_AddTrashAuditEvents -PreviousApiImage lzocateli/altivy-api@sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef
.INPUTS
Somente fontes Git e imagens locais. Gera senha sintetica em memoria para o banco descartavel.
.OUTPUTS
Contagens de migrations e resultado do smoke. Nao imprime dados das tabelas ou logs de conteudo.
.NOTES
Requer PowerShell 7.4+, Docker Compose v2, imagens de teste locais e acesso ao commit anterior no object database Git.
Todas as aplicacoes executam em containers com --network none; HTTP do smoke passa pelo NGINX efemero.
.LINK
../../docs/runbooks/recuperacao-self-hosted.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^[a-fA-F0-9]{7,40}$')][string] $PreviousCommit,
    [ValidatePattern('^[0-9]{14}_[A-Za-z0-9_]+$')][string] $PreviousMigration,
    [ValidatePattern('^[A-Za-z0-9./_-]+@sha256:[a-f0-9]{64}$')][string] $PreviousApiImage,
    [string] $CurrentMigrationImage = 'nodra-migrations:0.1.0',
    [string] $PostgresImage = 'lzocateli/postgresql:18.4-pgvector0.8.6-bookworm',
    [ValidatePattern('^[a-z0-9][a-z0-9_-]{0,48}$')][string] $ProjectName,
    [Parameter(ValueFromRemainingArguments)][string[]] $RemainingArguments
)
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; return }
if ($RemainingArguments.Count -gt 0) { throw 'Argumento desconhecido. Use --help.' }
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot)
Push-Location $repository
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('altivy-p04-upgrade-' + [Guid]::NewGuid().ToString('N'))
$previousMigrationImage = $null
$compose = @()
$savedEnvironment = @{}

function Set-TestEnvironment {
    param([string] $Name, [string] $Value)
    if (-not $savedEnvironment.ContainsKey($Name)) {
        $savedEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name,'Process')
    }
    [Environment]::SetEnvironmentVariable($Name,$Value,'Process')
}

function Invoke-Docker {
    param([string[]] $Arguments, [switch] $Capture, [switch] $Quiet)
    $localPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $result = & docker @Arguments 2>&1
        $nativeExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $localPreference }
    if ($nativeExit -ne 0) { throw 'Upgrade isolado falhou; nenhum dado Production foi acessado. Use --help.' }
    if ($Capture) { return ($result | Out-String).Trim() }
    if (-not $Quiet) { foreach ($line in $result) { Write-Host $line } }
}

function Get-ImageId {
    param([string] $Image)
    $imageId = Invoke-Docker -Arguments @('image','inspect',$Image,'--format','{{.Id}}') -Capture
    if ($imageId -notmatch '^sha256:[a-f0-9]{64}$') { throw 'Imagem de teste nao esta presente no Docker local.' }
    return $imageId
}

try {
    if (-not $PreviousCommit -or -not $PreviousMigration -or -not $PreviousApiImage) {
        throw 'Informe -PreviousCommit, -PreviousMigration e -PreviousApiImage. Use --help.'
    }
    $previousCommitHash = (& git rev-parse "$PreviousCommit^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or $previousCommitHash -notmatch '^[a-f0-9]{40}$') {
        throw 'Commit anterior nao existe no object database Git local.'
    }
    $previousApiId = Invoke-Docker -Arguments @('image','inspect',$PreviousApiImage,'--format','{{.Id}}') -Capture
    if ($previousApiId -notmatch '^sha256:[a-f0-9]{64}$') { throw 'Imagem API anterior nao esta presente no Docker local.' }
    $currentMigrationId = Get-ImageId $CurrentMigrationImage
    $postgresImageId = Get-ImageId $PostgresImage
    $migrationSource = "backend/libs/infrastructure/data/Migrations/$PreviousMigration.cs"
    $null = & git cat-file -e "${previousCommitHash}:$migrationSource" 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Migration baseline nao existe no commit anterior.' }

    New-Item -ItemType Directory -Path $testRoot | Out-Null
    $sourceArchive = Join-Path $testRoot 'previous-source.tar'
    $previousSource = Join-Path $testRoot 'previous-source'
    New-Item -ItemType Directory -Path $previousSource | Out-Null
    & git archive --format=tar --output=$sourceArchive $previousCommitHash
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar archive do commit anterior.' }
    & tar -xf $sourceArchive -C $previousSource
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao extrair o commit anterior em diretorio temporario.' }

    $previousMigrationImage = 'nodra-migrations:p04-' + $previousCommitHash.Substring(0,7) + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
    $dockerfile = Join-Path $repository 'deploy/production/migrations.Dockerfile'
    Invoke-Docker -Arguments @(
        'build','--file',$dockerfile,'--build-arg',"IMAGE_VERSION=0.0.0-p04.$($previousCommitHash.Substring(0,7))",
        '--tag',$previousMigrationImage,$previousSource
    )

    if (-not $ProjectName) { $ProjectName = 'altivy-p04-' + [Guid]::NewGuid().ToString('N').Substring(0,8) }
    $compose = @('compose','-p',$ProjectName,'-f','deploy/e2e/compose-upgrade-test.yaml')
    $syntheticPassword = [Guid]::NewGuid().ToString('N')
    Set-TestEnvironment 'P04_TEST_USER' 'restore_operator'
    Set-TestEnvironment 'P04_TEST_PASSWORD' $syntheticPassword
    Set-TestEnvironment 'P04_POSTGRES_IMAGE' $PostgresImage
    Set-TestEnvironment 'P04_CURRENT_MIGRATIONS_IMAGE' $CurrentMigrationImage
    Set-TestEnvironment 'P04_PREVIOUS_MIGRATIONS_IMAGE' $previousMigrationImage
    Set-TestEnvironment 'P04_ROLLBACK_API_IMAGE' $PreviousApiImage

    Invoke-Docker -Arguments ($compose + @('config','--quiet')) -Quiet
    Invoke-Docker -Arguments ($compose + @('up','-d','--wait','postgres'))
    Invoke-Docker -Arguments ($compose + @('--profile','upgrade','run','--no-deps','--rm','previous-migrator',$PreviousMigration))

    $historyBefore = Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-XAt','-U','restore_operator','-d','altivy',
        '-c','SELECT count(*) FROM altivy."__EFMigrationsHistory"'
    )) -Capture
    $schemaQuery = "SELECT string_agg(table_name||'.'||column_name||':'||udt_name, '|' ORDER BY table_name,ordinal_position) FROM information_schema.columns WHERE table_schema='altivy'"
    $schemaBefore = Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-XAt','-U','restore_operator','-d','altivy','-c',$schemaQuery
    )) -Capture
    $fixtureId = 'lab-p04-' + [Guid]::NewGuid().ToString('N')
    $insert = "INSERT INTO altivy.`"vaults`" (`"Id`", `"Name`", state) VALUES ('$fixtureId', 'Synthetic P04', 2)"
    Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-Xq','-U','restore_operator','-d','altivy','-v','ON_ERROR_STOP=1','-c',$insert
    )) -Quiet
    Invoke-Docker -Arguments ($compose + @('--profile','upgrade','run','--no-deps','--rm','migrator'))

    $historyAfter = Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-XAt','-U','restore_operator','-d','altivy',
        '-c','SELECT count(*) FROM altivy."__EFMigrationsHistory"'
    )) -Capture
    $newestMigration = Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-XAt','-U','restore_operator','-d','altivy',
        '-c','SELECT "MigrationId" FROM altivy."__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1'
    )) -Capture
    $vaultCount = Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-XAt','-U','restore_operator','-d','altivy',
        '-c',"SELECT count(*) FROM altivy.`"vaults`" WHERE `"Id`"='$fixtureId'"
    )) -Capture
    $schemaAfter = Invoke-Docker -Arguments ($compose + @(
        'exec','-T','postgres','psql','-XAt','-U','restore_operator','-d','altivy','-c',$schemaQuery
    )) -Capture
    if ([int]$historyAfter -le [int]$historyBefore -or $newestMigration -le $PreviousMigration -or
        $vaultCount -ne '1' -or $schemaBefore -cne $schemaAfter) {
        throw 'Migração atual alterou schema inesperadamente ou não preservou o fixture.'
    }
    Write-Host "Upgrade isolado passou: $historyBefore -> $historyAfter migrations; fixture e schema preservados."
    Write-Host "PostgreSQL $postgresImageId; bundle atual $currentMigrationId; API anterior $PreviousApiImage."

    Invoke-Docker -Arguments ($compose + @('up','-d','--wait','rollback-api','gateway'))
    Invoke-Docker -Arguments ($compose + @('--profile','upgrade','run','--rm','--no-deps','smoke'))
    Write-Host 'API anterior saudável após upgrade e acessível somente por /health/live via NGINX.'
} finally {
    if ($compose.Count -gt 0) {
        try { Invoke-Docker -Arguments ($compose + @('down','--remove-orphans')) -Quiet } catch { Write-Warning 'Teardown do Compose de teste requer verificação manual.' }
    }
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name,$savedEnvironment[$name],'Process') }
    if ($previousMigrationImage) {
        try { Invoke-Docker -Arguments @('image','rm',$previousMigrationImage) -Quiet } catch { Write-Warning 'A imagem temporária do migrator antigo permaneceu no cache local.' }
    }
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
    Pop-Location
}
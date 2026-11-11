<#
.SYNOPSIS
Constroi imagens Nodra localmente, gera SBOM e verifica vulnerabilidades.
.DESCRIPTION
Orquestra Docker/BuildKit e Trivy fixado por digest, sem publicar imagens e sem
montar socket Docker no scanner. Usa archives temporarios, removidos ao terminar.
.PARAMETER Image
api, bff, worker, web, migrations ou all (padrao).
.PARAMETER Tag
Tag local de validacao; padrao ci.
.PARAMETER DryRun
Mostra comandos sem executar Docker, rede ou criar arquivos.
.PARAMETER ReleaseEntriesPath
Diretorio das cinco entradas JSON do publisher canonico; escaneia os digests remotos sem rebuild.
.PARAMETER RemainingArguments
Aceita somente --help.
.EXAMPLE
./tools/scripts/validate-release-images.ps1 -Image all -Tag ci
.EXAMPLE
./tools/scripts/validate-release-images.ps1 -Image api -DryRun
.INPUTS
Dockerfiles e contexto do repositorio, sem credenciais.
.OUTPUTS
Relatorios em artifacts/security-local e codigo nao zero em falha.
.NOTES
Requer PowerShell 7.4+, Docker com BuildKit e rede para imagens/banco Trivy.
O scanner Trivy 0.72.0 possui licenca Apache-2.0 verificada na origem oficial.
.LINK
../../docs/ci-cd-seguranca.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('all', 'api', 'bff', 'worker', 'web', 'migrations')]
    [string] $Image = 'all',
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$')]
    [string] $Tag = 'ci',
    [string] $ReleaseEntriesPath,
    [switch] $DryRun,
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; exit 0 }
if ($RemainingArguments.Count -gt 0) { throw 'Argumento desconhecido. Use --help.' }
$repository = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$output = Join-Path $repository 'artifacts/security-local'
$temporary = Join-Path ([IO.Path]::GetTempPath()) "nodra-images-$([Guid]::NewGuid().ToString('N'))"
$cache = Join-Path $temporary 'cache'
$scanner = 'docker.io/aquasec/trivy:0.72.0@sha256:c6e969c5662a546ad5de4a73c2a6b7a7c627f86d916903e175aa623af5b97ada'
$definitions = [ordered]@{
    api = 'backend/apps/api/Dockerfile'
    bff = 'backend/apps/bff/Dockerfile'
    worker = 'backend/apps/worker/Dockerfile'
    web = 'frontend/web/Dockerfile'
    migrations = 'deploy/production/migrations.Dockerfile'
}

function Invoke-Docker([string[]] $Arguments) {
    if ($DryRun) { Write-Output "[dry-run] docker $($Arguments -join ' ')"; return }
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw 'Validacao de imagens falhou; nenhum push ou deploy foi executado. Use --help.' }
}

Push-Location $repository
try {
    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $output -Force | Out-Null
        New-Item -ItemType Directory -Path $cache -Force | Out-Null
    }
    $entries = @()
    if ($ReleaseEntriesPath) {
        foreach ($file in Get-ChildItem -LiteralPath $ReleaseEntriesPath -Filter '*.json' -File) {
            $entries += @(Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json)
        }
        if ($entries.Count -ne 5) { throw 'Exigidas exatamente cinco entradas de imagem. Use --help.' }
        foreach ($name in $definitions.Keys) {
            $matches = @($entries | Where-Object { $_.name -ceq "nodra-$name" })
            if ($matches.Count -ne 1 -or $matches[0].kind -cne 'container' -or
                $matches[0].reference -cne "ghcr.io/altivy-it/nodra-$name" -or
                $matches[0].digest -cnotmatch '^sha256:[a-f0-9]{64}$') {
                throw 'Entrada de imagem invalida; scan/publicacao da release bloqueado. Use --help.'
            }
        }
    }
    else { Invoke-Docker @('buildx', 'inspect', '--bootstrap') }
    foreach ($name in $definitions.Keys) {
        if ($Image -ne 'all' -and $Image -ne $name) { continue }
        if ($ReleaseEntriesPath) {
            $entry = @($entries | Where-Object { $_.name -ceq "nodra-$name" })[0]
            $mounts = @('run', '--rm', '--env', 'TRIVY_USERNAME', '--env', 'TRIVY_PASSWORD', '--volume', "${output}:/reports", '--volume', "${cache}:/root/.cache/trivy", $scanner, 'image', '--image-src', 'remote', "$($entry.reference)@$($entry.digest)")
        }
        else {
        $reference = "nodra-${name}:$Tag"
        $build = @('buildx', 'build', '--load', '--provenance=mode=max', '--sbom=true', '--build-arg', 'RELEASE_VERSION=0.0.0-ci', '--file', $definitions[$name], '--tag', $reference, '--metadata-file', (Join-Path $output "$name-build.json"))
        if ($name -eq 'web') { $build += @('--build-arg', 'WEB_CONFIGURATION=production', '--build-arg', 'WEB_BASE_HREF=/') }
        Invoke-Docker ($build + @('.'))
        Invoke-Docker @('save', '--output', (Join-Path $temporary "$name.tar"), $reference)
        $mounts = @('run', '--rm', '--volume', "${temporary}:/images:ro", '--volume', "${output}:/reports", '--volume', "${cache}:/root/.cache/trivy", $scanner, 'image', '--input', "/images/$name.tar")
        }
        Invoke-Docker ($mounts + @('--format', 'cyclonedx', '--output', "/reports/$name.cdx.json"))
        Invoke-Docker ($mounts + @('--ignore-unfixed', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', '--format', 'json', '--output', "/reports/$name-vulnerabilities.json"))
    }
}
finally {
    Pop-Location
    if (-not $DryRun -and (Test-Path -LiteralPath $temporary)) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
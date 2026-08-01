<#
.SYNOPSIS
Produz os assets de marca do Thothis a partir dos SVGs aprovados.

.DESCRIPTION
Executa SVGO 3.3.2 e sharp 0.33.5 em uma imagem Node fixa para otimizar SVGs,
gerar PNGs e registrar checksums SHA-256 em docs/brand/assets/dist.

.PARAMETER RepositoryRoot
Raiz do repositório. O padrão é dois níveis acima de tools/scripts.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./tools/scripts/build-brand-assets.ps1

.INPUTS
Nenhum.

.OUTPUTS
SVGs otimizados, PNGs e SHA256SUMS em docs/brand/assets/dist.

.NOTES
Requer PowerShell 7, Docker 29 ou superior e acesso ao registry npm no fluxo
funcional. A ajuda não requer Docker, rede ou dependências opcionais.

.LINK
../../docs/brand/guide.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $RepositoryRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),

    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'

if ($RepositoryRoot -eq '--help' -or $RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}

if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumento desconhecido: $($RemainingArguments -join ' '). Use --help.")
    exit 2
}

$repositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    throw 'Docker não encontrado. Instale Docker 29 ou superior e use --help para detalhes.'
}

$image = 'lzocateli/node:24.15.0-bookworm'
$mount = "${repositoryRoot}:/workspace"
$command = @'
set -eu
npm install --prefix /tmp/brand --no-audit --no-fund svgo@3.3.2 sharp@0.33.5 >/dev/null
NODE_PATH=/tmp/brand/node_modules node /workspace/tools/scripts/build-brand-assets.mjs /workspace
'@
$command = $command.Replace("`r", '')

& $docker.Source run --rm --volume $mount $image sh -lc $command
if ($LASTEXITCODE -ne 0) {
    throw "A produção dos assets falhou com código $LASTEXITCODE."
}
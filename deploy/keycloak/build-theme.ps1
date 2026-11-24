<#
.SYNOPSIS
Gera o tema de login do Keycloak do Nodra a partir da fonte versionada.

.DESCRIPTION
Monta o tema de login do Keycloak do Nodra (slot `altivy`) em um diretório de
saída limpo. Combina a fonte versionada em deploy/keycloak/themes/altivy com os
tokens, as fontes OFL-1.1 e o símbolo da marca mantidos em frontend/web, para
que app e login compartilhem uma única fonte visual (spec 007). Gera SHA256SUMS
de todos os arquivos. Não usa rede, credenciais, exports de realm ou secrets.

.PARAMETER OutputPath
Diretório que receberá o artefato de tema gerado. O conteúdo existente é removido.

.EXAMPLE
./deploy/keycloak/build-theme.ps1 --help

.EXAMPLE
./deploy/keycloak/build-theme.ps1 -OutputPath ./deploy/keycloak/.build/keycloak-theme

.NOTES
Requer PowerShell 7. Documentação: docs/configuracao-keycloak.md.

.LINK
https://www.keycloak.org/ui-customization/themes
#>
[CmdletBinding()]
param(
    [Parameter()]
    [Alias('h')]
    [switch] $Help,

    [Parameter()]
    [string] $OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Help -or $OutputPath -in @('--help', '-h')) {
    Get-Help $PSCommandPath -Full
    exit 0
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot 'deploy/keycloak/.build/keycloak-theme'
}

if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $outputRoot = [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    $outputRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
}
$themeSource = Join-Path $repositoryRoot 'deploy/keycloak/themes/altivy/login'
$themeOutput = Join-Path $outputRoot 'altivy/login'

$styleSource = Join-Path $repositoryRoot 'frontend/web/src/styles'
$brandSource = Join-Path $repositoryRoot 'frontend/web/public/brand'

# Origem relativa ao repositório -> destino relativo ao tema gerado.
$themeFiles = [ordered]@{
    'deploy/keycloak/themes/altivy/login/theme.properties'        = 'theme.properties'
    'deploy/keycloak/themes/altivy/login/resources/css/login.css' = 'resources/css/login.css'
    'frontend/web/src/styles/tokens.css'                          = 'resources/css/tokens.css'
    'frontend/web/public/brand/nodra-mark.svg'                    = 'resources/img/nodra-mark.svg'
}
foreach ($font in Get-ChildItem -LiteralPath (Join-Path $styleSource 'fonts') -File | Where-Object { $_.Extension -in @('.woff2', '.txt') } | Sort-Object Name) {
    $themeFiles["frontend/web/src/styles/fonts/$($font.Name)"] = "resources/fonts/$($font.Name)"
}

foreach ($source in $themeFiles.Keys) {
    $sourceFile = Join-Path $repositoryRoot $source
    if (-not (Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
        throw "Arquivo versionado do tema não encontrado: $sourceFile"
    }
    if ((Get-Item -LiteralPath $sourceFile).LinkType) {
        throw "Links simbólicos não são permitidos no tema: $sourceFile"
    }
}
foreach ($required in @('outfit-latin-400-normal.woff2', 'OFL-Outfit.txt', 'OFL-JetBrainsMono.txt')) {
    if (-not $themeFiles.Contains("frontend/web/src/styles/fonts/$required")) {
        throw "Fonte ou licença obrigatória ausente em ${styleSource}/fonts: $required"
    }
}
if (-not (Test-Path -LiteralPath $themeSource -PathType Container)) {
    throw "Diretório-fonte do tema não encontrado: $themeSource"
}
if (-not (Test-Path -LiteralPath $brandSource -PathType Container)) {
    throw "Diretório da marca não encontrado: $brandSource"
}

if (Test-Path -LiteralPath $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}

foreach ($entry in $themeFiles.GetEnumerator()) {
    $target = Join-Path $themeOutput $entry.Value
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $entry.Key) -Destination $target
}

# No tema, fonts.css fica em resources/css e as fontes em resources/fonts.
$fontsCss = (Get-Content -LiteralPath (Join-Path $styleSource 'fonts.css') -Raw) -replace '\./fonts/', '../fonts/'
if ($fontsCss -match 'url\("(?!\.\./fonts/)') {
    throw 'fonts.css referencia arquivos fora de ./fonts/; atualize o build do tema.'
}
Set-Content -LiteralPath (Join-Path $themeOutput 'resources/css/fonts.css') -Value $fontsCss -NoNewline -Encoding utf8NoBOM

$checksums = @()
foreach ($file in Get-ChildItem -LiteralPath $themeOutput -File -Recurse | Sort-Object FullName) {
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $relativePath = $file.FullName.Substring($outputRoot.Length).TrimStart('\', '/') -replace '\\', '/'
    $checksums += "$hash  $relativePath"
}
$checksums | Set-Content -LiteralPath (Join-Path $outputRoot 'SHA256SUMS') -Encoding ascii

Write-Output "Artefato do tema criado em $outputRoot"
Write-Output "Arquivos: $($checksums.Count)"

<#
.SYNOPSIS
Gera o tema de login do Keycloak do Nodra a partir da fonte versionada.

.DESCRIPTION
Empacota o tema de Keycloak versionado em deploy/keycloak em um diretório de
saída limpo. O diretório canônico da marca é opcional e serve apenas para
verificar os assets versionados. Não são necessários rede, credenciais,
usuários, exports de realm ou secrets.

.PARAMETER BrandRoot
Diretório canônico opcional da marca Altivy usado para comparar checksums locais.

.PARAMETER OutputPath
Diretório que receberá o artefato de tema gerado. O conteúdo existente é removido.

.EXAMPLE
./deploy/keycloak/build-theme.ps1 --help

.EXAMPLE
./deploy/keycloak/build-theme.ps1 -OutputPath ./deploy/keycloak/.build/keycloak-theme

.NOTES
Requer PowerShell 7. Quando informado, o repositório canônico da marca deve
conter os assets, fontes e manifests SHA256SUMS aprovados.

.LINK
https://www.keycloak.org/ui-customization/themes
#>
[CmdletBinding()]
param(
    [Parameter()]
    [Alias('h')]
    [switch] $Help,

    [Parameter()]
    [string] $BrandRoot = '',

    [Parameter()]
    [string] $OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Help -or $BrandRoot -in @('--help', '-h')) {
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

$themeFiles = @(
    'theme.properties',
    'resources/css/login.css',
    'resources/fonts/Manrope-Regular-Latin.woff2',
    'resources/fonts/Manrope-Medium-Latin.woff2',
    'resources/fonts/Manrope-SemiBold-Latin.woff2',
    'resources/img/logo.svg',
    'resources/img/logo-dark.svg',
    'resources/js/theme-toggle.js'
)

foreach ($themeFile in $themeFiles) {
    $sourceFile = Join-Path $themeSource $themeFile
    if (-not (Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
        throw "Arquivo versionado do tema não encontrado: $sourceFile"
    }
}

if (-not [string]::IsNullOrWhiteSpace($BrandRoot)) {
    $BrandRoot = (Resolve-Path $BrandRoot).Path
    $canonicalFiles = @(
        @{ Local = 'resources/img/logo.svg'; Canonical = 'assets/dist/logo.svg' },
        @{ Local = 'resources/img/logo-dark.svg'; Canonical = 'assets/dist/logo-dark.svg' },
        @{ Local = 'resources/fonts/Manrope-Regular-Latin.woff2'; Canonical = 'fonts/Manrope-Regular-Latin.woff2' },
        @{ Local = 'resources/fonts/Manrope-Medium-Latin.woff2'; Canonical = 'fonts/Manrope-Medium-Latin.woff2' },
        @{ Local = 'resources/fonts/Manrope-SemiBold-Latin.woff2'; Canonical = 'fonts/Manrope-SemiBold-Latin.woff2' }
    )
    foreach ($canonicalFile in $canonicalFiles) {
        $canonicalPath = Join-Path $BrandRoot $canonicalFile.Canonical
        $localPath = Join-Path $themeSource $canonicalFile.Local
        if (-not (Test-Path -LiteralPath $canonicalPath -PathType Leaf)) {
            throw "Arquivo canônico da marca não encontrado: $canonicalPath"
        }

        $canonicalHash = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash
        $localHash = (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash
        if ($canonicalHash -ine $localHash) {
            throw "O asset versionado do tema difere da marca canônica: $($canonicalFile.Local)"
        }
    }
}

if (Test-Path -LiteralPath $themeSource -PathType Container) {
    foreach ($themeFile in $themeFiles) {
        $sourceFile = Join-Path $themeSource $themeFile
        if ((Get-Item -LiteralPath $sourceFile).LinkType) {
            throw "Links simbólicos não são permitidos no tema versionado: $sourceFile"
        }
    }
}

if (-not (Test-Path -LiteralPath $themeSource -PathType Container)) {
    throw "Diretório-fonte do tema não encontrado: $themeSource"
}

if (Test-Path -LiteralPath $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $themeOutput -Force | Out-Null
Get-ChildItem -LiteralPath $themeSource -Force | Copy-Item -Destination $themeOutput -Recurse -Force

$checksums = @()
foreach ($file in Get-ChildItem -LiteralPath $themeOutput -File -Recurse | Sort-Object FullName) {
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $relativePath = $file.FullName.Substring($outputRoot.Length).TrimStart('\', '/') -replace '\\', '/'
    $checksums += "$hash  $relativePath"
}
$checksums | Set-Content -LiteralPath (Join-Path $outputRoot 'SHA256SUMS') -Encoding ascii

Write-Output "Artefato do tema criado em $outputRoot"
Write-Output "Arquivos: $($checksums.Count)"

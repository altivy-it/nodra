<#
.SYNOPSIS
Produz as fontes self-hosted e o wordmark vetorial do Thothis.

.DESCRIPTION
Obtém Inter 4.1 e IBM Plex 6.4.0 das distribuições oficiais, cria subsets
latinos WOFF2, valida cobertura e orçamento, converte o wordmark Inter
SemiBold em paths e registra checksums SHA-256.

.PARAMETER RepositoryRoot
Raiz do repositório. O padrão é dois níveis acima de tools/scripts.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./tools/scripts/build-brand-fonts.ps1

.INPUTS
Nenhum.

.OUTPUTS
Fontes, licenças e SHA256SUMS em docs/brand/fonts, além dos logos vetoriais
atualizados em docs/brand/assets.

.NOTES
Requer PowerShell 7, uv 0.9 ou superior, Docker 29 ou superior e acesso à
internet no fluxo funcional. Usa fonttools[woff] 4.59.0 e @ibm/plex 6.4.0.
A ajuda não requer rede nem dependências opcionais instaladas.

.LINK
../../docs/brand/tokens/typography.md
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
$uv = Get-Command uv -ErrorAction SilentlyContinue
$docker = Get-Command docker -ErrorAction SilentlyContinue

if (-not $uv) {
    throw 'uv não encontrado. Instale uv 0.9 ou superior e use --help para detalhes.'
}

if (-not $docker) {
    throw 'Docker não encontrado. Instale Docker 29 ou superior e use --help para detalhes.'
}

$fontDirectory = Join-Path $repositoryRoot 'docs/brand/fonts'
$assetDirectory = Join-Path $repositoryRoot 'docs/brand/assets'
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "thothis-brand-fonts-$([guid]::NewGuid().ToString('N'))"
$sourceDirectory = Join-Path $temporaryRoot 'source'
$outputDirectory = Join-Path $temporaryRoot 'output'
$interArchive = Join-Path $temporaryRoot 'Inter-4.1.zip'
$interDirectory = Join-Path $sourceDirectory 'inter'
$plexDirectory = Join-Path $sourceDirectory 'plex'

try {
    New-Item $sourceDirectory, $outputDirectory, $plexDirectory -ItemType Directory -Force | Out-Null

    Invoke-WebRequest `
        -Uri 'https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip' `
        -OutFile $interArchive
    Expand-Archive -Path $interArchive -DestinationPath $interDirectory

    $mount = "${plexDirectory}:/output"
    $plexCommand = @'
set -eu
npm install --prefix /tmp/fonts --no-audit --no-fund @ibm/plex@6.4.0 >/dev/null
serif=/tmp/fonts/node_modules/@ibm/plex/IBM-Plex-Serif/fonts/split/woff2
mono=/tmp/fonts/node_modules/@ibm/plex/IBM-Plex-Mono/fonts/split/woff2
cp "$serif/IBMPlexSerif-Regular-Latin1.woff2" /output/
cp "$serif/IBMPlexSerif-Bold-Latin1.woff2" /output/
cp "$mono/IBMPlexMono-Regular-Latin1.woff2" /output/
cp "$mono/IBMPlexMono-SemiBold-Latin1.woff2" /output/
cp /tmp/fonts/node_modules/@ibm/plex/IBM-Plex-Serif/fonts/split/woff2/license.txt /output/LICENSE-IBM-Plex-6.4.0.txt
'@.Replace("`r", '')

    & $docker.Source run --rm --volume $mount lzocateli/node:24.15.0-bookworm sh -lc $plexCommand
    if ($LASTEXITCODE -ne 0) {
        throw "A obtenção do IBM Plex falhou com código $LASTEXITCODE."
    }

    $subsetArguments = @(
        '--flavor=woff2',
        '--unicodes=U+0020-007E,U+00A0-00FF',
        '--layout-features=kern,liga,calt',
        '--no-hinting'
    )
    $interVariable = Join-Path $interDirectory 'web/InterVariable.woff2'
    $interOutput = Join-Path $outputDirectory 'Inter-Latin1.woff2'

    & $uv.Source tool run --from 'fonttools[woff]==4.59.0' pyftsubset `
        $interVariable "--output-file=$interOutput" @subsetArguments
    if ($LASTEXITCODE -ne 0) {
        throw "O subset do Inter falhou com código $LASTEXITCODE."
    }

    Copy-Item (Join-Path $plexDirectory '*.woff2') $outputDirectory

    Copy-Item (Join-Path $interDirectory 'LICENSE.txt') (Join-Path $outputDirectory 'LICENSE-Inter-4.1.txt')
    Copy-Item (Join-Path $plexDirectory 'LICENSE-IBM-Plex-6.4.0.txt') $outputDirectory

    $wordmarkScript = @'
import sys

from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont

font = TTFont(sys.argv[1])
glyph_set = font.getGlyphSet()
cmap = font.getBestCmap()
scale = 40 / font["head"].unitsPerEm
cursor = 78
commands = []

for character in "Thothis":
    glyph_name = cmap[ord(character)]
    pen = SVGPathPen(glyph_set)
    transformed_pen = TransformPen(pen, (scale, 0, 0, -scale, cursor, 45))
    glyph_set[glyph_name].draw(transformed_pen)
    commands.append(pen.getCommands())
    cursor += font["hmtx"][glyph_name][0] * scale

print(" ".join(commands))
'@
    $wordmarkFont = Join-Path $interDirectory 'web/Inter-SemiBold.woff2'
    $wordmarkPath = & $uv.Source tool run --from 'fonttools[woff]==4.59.0' python -c $wordmarkScript $wordmarkFont
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($wordmarkPath)) {
        throw 'A conversão do wordmark para paths falhou.'
    }

    $requiredCharacters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789çãâàáéêíóôúüñÇÃÂÀÁÉÊÍÓÔÚÜÑ'
    $validationScript = @'
import sys

from fontTools.ttLib import TTFont

required = set(sys.argv[1])
for path in sys.argv[2:]:
    available = {chr(codepoint) for codepoint in TTFont(path).getBestCmap()}
    missing = "".join(sorted(required - available))
    if missing:
        raise SystemExit(f"{path}: caracteres ausentes: {missing}")
'@
    $generatedFonts = Get-ChildItem $outputDirectory -Filter '*.woff2' | Sort-Object Name
    & $uv.Source tool run --from 'fonttools[woff]==4.59.0' python -c `
        $validationScript $requiredCharacters $generatedFonts.FullName
    if ($LASTEXITCODE -ne 0) {
        throw 'A validação de cobertura tipográfica falhou.'
    }

    $familyBudgets = @{
        'Inter' = @($generatedFonts | Where-Object Name -Like 'Inter-*')
        'IBM Plex Serif' = @($generatedFonts | Where-Object Name -Like 'IBMPlexSerif-*')
        'IBM Plex Mono' = @($generatedFonts | Where-Object Name -Like 'IBMPlexMono-*')
    }
    foreach ($family in $familyBudgets.GetEnumerator()) {
        $size = ($family.Value | Measure-Object Length -Sum).Sum
        if ($size -ge 80KB) {
            throw "$($family.Key) excede o orçamento: $size bytes (limite menor que 81920 bytes)."
        }
        Write-Host "$($family.Key): $size bytes"
    }

    New-Item $fontDirectory -ItemType Directory -Force | Out-Null
    Get-ChildItem $fontDirectory -Filter '*.woff2' -ErrorAction SilentlyContinue | Remove-Item -Force
    Copy-Item (Join-Path $outputDirectory '*') $fontDirectory -Force

    foreach ($logo in @('thothis-logo.svg', 'thothis-logo-dark.svg')) {
        $logoPath = Join-Path $assetDirectory $logo
        $contents = Get-Content $logoPath -Raw
        $fill = if ($logo -eq 'thothis-logo.svg') { '#1A202C' } else { '#FFFFFF' }
        $replacement = "  <path data-wordmark=`"true`" fill=`"$fill`" d=`"$wordmarkPath`"/>"
        $pattern = '(?m)^  (?:<text[^>]*>Thothis</text>|<path data-wordmark="true"[^>]*/>)\r?$'
        $updated = [regex]::Replace($contents, $pattern, $replacement)
        if ($updated -eq $contents -and $contents -notmatch 'data-wordmark="true"') {
            throw "Wordmark não encontrado em $logo."
        }
        [IO.File]::WriteAllText($logoPath, $updated.Replace("`r`n", "`n"))
    }

    $hashLines = Get-ChildItem $fontDirectory -File |
        Where-Object Name -NE 'SHA256SUMS' |
        Sort-Object Name |
        ForEach-Object { "$(Get-FileHash $_.FullName -Algorithm SHA256 | Select-Object -ExpandProperty Hash)  $($_.Name)" }
    [IO.File]::WriteAllText(
        (Join-Path $fontDirectory 'SHA256SUMS'),
        "$($hashLines -join "`n")`n"
    )
}
finally {
    Remove-Item $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
}
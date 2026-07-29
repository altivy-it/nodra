[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$gitDirectoryPrefix = (Join-Path $repositoryRoot '.git') + [IO.Path]::DirectorySeparatorChar
$failures = [System.Collections.Generic.List[string]]::new()

$requiredPaths = @(
    'README.md'
    'docs/backend.md'
    'docs/ci-cd-seguranca.md'
    'docs/evolucao-produto.md'
    'docs/frontend.md'
    '.github/CONTRIBUTING.md'
    '.github/CODEOWNERS'
    '.github/dependabot.yml'
    '.github/PULL_REQUEST_TEMPLATE.md'
    '.github/SECURITY.md'
    '.github/workflows/ci.yml'
    'docs/github-administracao.md'
)

foreach ($requiredPath in $requiredPaths) {
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $requiredPath))) {
        $failures.Add("Arquivo obrigatório ausente: $requiredPath")
    }
}

$markdownFiles = Get-ChildItem -LiteralPath $repositoryRoot -File -Recurse -Force |
    Where-Object {
        $_.Extension -eq '.md' -and
        -not $_.FullName.StartsWith($gitDirectoryPrefix, [StringComparison]::OrdinalIgnoreCase)
    }

function Test-LocalLink {
    param(
        [Parameter(Mandatory)]
        [string] $Destination,

        [Parameter(Mandatory)]
        [IO.FileInfo] $SourceFile,

        [Parameter(Mandatory)]
        [string] $RelativeSource
    )

    $normalizedDestination = $Destination.Trim().Trim('<', '>')
    if ($normalizedDestination -match '^(?:https?://|mailto:|#)') {
        return
    }

    $pathPart = ($normalizedDestination -split '#', 2)[0]
    if ([string]::IsNullOrWhiteSpace($pathPart)) {
        return
    }

    $decodedPath = [Uri]::UnescapeDataString($pathPart)
    $resolvedPath = Join-Path $SourceFile.DirectoryName $decodedPath
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        $failures.Add("Link local inválido em ${RelativeSource}: $normalizedDestination")
    }
}

foreach ($markdownFile in $markdownFiles) {
    $content = Get-Content -Raw -LiteralPath $markdownFile.FullName
    if ($null -eq $content) {
        $content = ''
    }

    $relativeFile = [IO.Path]::GetRelativePath($repositoryRoot, $markdownFile.FullName).Replace('\', '/')
    $explicitHeadingIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    foreach ($line in ($content -split "`r?`n")) {
        if ($line -match '^#{1,6}\s+.+?\s+\{#([^}]+)\}\s*$') {
            $headingId = $Matches[1]
            if (-not $explicitHeadingIds.Add($headingId)) {
                $failures.Add("ID de heading duplicado em ${relativeFile}: $headingId")
            }
        }
    }

    $inlineLinkPattern = '!?(?:\[[^\]]*\])\((?<target><[^>]+>|[^\s)]+)(?:\s+(?:"[^"]*"|''[^'']*''|\([^)]*\)))?\)'
    foreach ($linkMatch in [regex]::Matches($content, $inlineLinkPattern)) {
        Test-LocalLink -Destination $linkMatch.Groups['target'].Value -SourceFile $markdownFile -RelativeSource $relativeFile
    }

    $referenceIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $referenceLinkPattern = '(?m)^\s{0,3}\[(?<id>[^\]]+)\]:\s*(?<target><[^>]+>|[^\s]+)'
    foreach ($linkMatch in [regex]::Matches($content, $referenceLinkPattern)) {
        [void] $referenceIds.Add($linkMatch.Groups['id'].Value.Trim())
        Test-LocalLink -Destination $linkMatch.Groups['target'].Value -SourceFile $markdownFile -RelativeSource $relativeFile
    }

    $referenceUsagePattern = '!?(?:\[(?<text>[^\]]+)\])\[(?<id>[^\]]*)\]'
    foreach ($linkMatch in [regex]::Matches($content, $referenceUsagePattern)) {
        $referenceId = $linkMatch.Groups['id'].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($referenceId)) {
            $referenceId = $linkMatch.Groups['text'].Value.Trim()
        }

        if (-not $referenceIds.Contains($referenceId)) {
            $failures.Add("Referência Markdown sem definição em ${relativeFile}: $referenceId")
        }
    }
}

$workflowDirectory = Join-Path $repositoryRoot '.github/workflows'
$workflowFiles = Get-ChildItem -LiteralPath $workflowDirectory -File |
    Where-Object { $_.Extension -in @('.yml', '.yaml') }

foreach ($workflowFile in $workflowFiles) {
    $relativeWorkflow = [IO.Path]::GetRelativePath($repositoryRoot, $workflowFile.FullName).Replace('\', '/')
    foreach ($line in (Get-Content -LiteralPath $workflowFile.FullName)) {
        if ($line -match '^\s*(?:-\s*)?uses:\s*([^\s#]+)') {
            $actionReference = $Matches[1]
            if ($actionReference -notmatch '^\./' -and
                $actionReference -notmatch '@[0-9a-fA-F]{40}$' -and
                $actionReference -notmatch '^docker://.+@sha256:[0-9a-fA-F]{64}$') {
                $failures.Add("Action sem SHA completo em ${relativeWorkflow}: $actionReference")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Validação concluída: $($markdownFiles.Count) arquivos Markdown verificados."
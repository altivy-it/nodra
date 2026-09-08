<#
.SYNOPSIS
Valida estrutura, Markdown, planos e referências imutáveis do Altivy.

.DESCRIPTION
Verifica arquivos obrigatórios, links Markdown locais, IDs explícitos de headings,
status geral e por etapa dos planos e referências imutáveis em GitHub Actions.

.PARAMETER RepositoryRoot
Raiz do repositório. O padrão é dois níveis acima de tools/scripts.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help; outros valores retornam erro de uso.

.EXAMPLE
./tools/scripts/validate-repository.ps1

.EXAMPLE
./tools/scripts/validate-repository.ps1 -RepositoryRoot C:\src\altivy

.INPUTS
Nenhum.

.OUTPUTS
Mensagem de sucesso ou erros acionáveis no stderr.

.NOTES
Requer PowerShell 7 ou superior e não usa dependências externas.

.LINK
../../.github/SCRIPTING.md
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
$gitDirectoryPrefix = (Join-Path $repositoryRoot '.git') + [IO.Path]::DirectorySeparatorChar
$failures = [System.Collections.Generic.List[string]]::new()

$requiredPaths = @(
    'README.md'
    'docs/backend.md'
    'docs/ci-cd-seguranca.md'
    'docs/evolucao-produto.md'
    'docs/frontend.md'
    '.github/CONTRIBUTING.md'
    '.github/ADR_TEMPLATE.md'
    '.github/PLAN_TEMPLATE.md'
    '.github/CODEOWNERS'
    '.github/dependabot.yml'
    '.github/PULL_REQUEST_TEMPLATE.md'
    '.github/SECURITY.md'
    '.github/workflows/ci.yml'
    'docs/github-administracao.md'
    'tools/scripts/test-validate-repository.ps1'
    'tools/scripts/validate-repository.ps1'
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
    $markdownContent = [regex]::Replace(
        $content,
        '(?ms)^(?<fence>`{3,}|~{3,})[^\r\n]*\r?\n.*?^\k<fence>\s*$',
        ''
    )
    $markdownContent = [regex]::Replace(
        $markdownContent,
        '(?m)(?<ticks>`+)[^\r\n]*?\k<ticks>',
        ''
    )
    $explicitHeadingIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    foreach ($line in ($markdownContent -split "`r?`n")) {
        if ($line -match '^#{1,6}\s+.+?\s+\{#([^}]+)\}\s*$') {
            $headingId = $Matches[1]
            if (-not $explicitHeadingIds.Add($headingId)) {
                $failures.Add("ID de heading duplicado em ${relativeFile}: $headingId")
            }
        }
    }

    $inlineLinkPattern = '!?(?:\[[^\]]*\])\((?<target><[^>]+>|[^\s)]+)(?:\s+(?:"[^"]*"|''[^'']*''|\([^)]*\)))?\)'
    foreach ($linkMatch in [regex]::Matches($markdownContent, $inlineLinkPattern)) {
        Test-LocalLink -Destination $linkMatch.Groups['target'].Value -SourceFile $markdownFile -RelativeSource $relativeFile
    }

    $referenceIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $referenceLinkPattern = '(?m)^\s{0,3}\[(?<id>[^\]]+)\]:\s*(?<target><[^>]+>|[^\s]+)'
    foreach ($linkMatch in [regex]::Matches($markdownContent, $referenceLinkPattern)) {
        [void] $referenceIds.Add($linkMatch.Groups['id'].Value.Trim())
        Test-LocalLink -Destination $linkMatch.Groups['target'].Value -SourceFile $markdownFile -RelativeSource $relativeFile
    }

    $referenceUsagePattern = '!?(?:\[(?<text>[^\]]+)\])\[(?<id>[^\]]*)\]'
    foreach ($linkMatch in [regex]::Matches($markdownContent, $referenceUsagePattern)) {
        $referenceId = $linkMatch.Groups['id'].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($referenceId)) {
            $referenceId = $linkMatch.Groups['text'].Value.Trim()
        }

        if (-not $referenceIds.Contains($referenceId)) {
            $failures.Add("Referência Markdown sem definição em ${relativeFile}: $referenceId")
        }
    }
}

$planDirectory = Join-Path $repositoryRoot 'docs/plan'
if (Test-Path -LiteralPath $planDirectory) {
    $allowedPlanStatuses = @('draft', 'proposed', 'approved', 'in-progress', 'blocked', 'completed', 'cancelled', 'superseded')
    $allowedStepStatuses = @('not-started', 'in-progress', 'blocked', 'completed', 'skipped')

    foreach ($planFile in (Get-ChildItem -LiteralPath $planDirectory -File -Filter '*.md')) {
        $planContent = Get-Content -Raw -LiteralPath $planFile.FullName
        if ($null -eq $planContent) {
            $planContent = ''
        }

        $relativePlan = [IO.Path]::GetRelativePath($repositoryRoot, $planFile.FullName).Replace('\', '/')
        $generalStatusMatch = [regex]::Match($planContent, '(?m)^\*\*Status:\*\*\s+(?<status>\S+)')
        if (-not $generalStatusMatch.Success -or $generalStatusMatch.Groups['status'].Value -notin $allowedPlanStatuses) {
            $failures.Add("Status geral ausente ou inválido em ${relativePlan}")
        }

        $stepMatches = [regex]::Matches(
            $planContent,
            '(?ms)^###\s+[^\r\n]*\b(?:P|WP)-\d+\b[^\r\n]*\r?\n(?<body>.*?)(?=^###\s+|\z)'
        )
        if ($stepMatches.Count -eq 0) {
            $failures.Add("Plano sem etapas P-NN ou WP-NN em ${relativePlan}")
            continue
        }

        foreach ($stepMatch in $stepMatches) {
            $heading = ($stepMatch.Value -split "`r?`n", 2)[0].Trim()
            $stepStatusMatch = [regex]::Match(
                $stepMatch.Groups['body'].Value,
                '(?m)^\*\*Status:\*\*\s+(?<status>\S+)'
            )
            if (-not $stepStatusMatch.Success -or $stepStatusMatch.Groups['status'].Value -notin $allowedStepStatuses) {
                $failures.Add("Status de etapa ausente ou inválido em ${relativePlan}: $heading")
            }
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
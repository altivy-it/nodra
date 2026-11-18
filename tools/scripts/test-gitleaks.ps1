<#
.SYNOPSIS
Valida a exceção pontual do Gitleaks e suas proteções contra regressões.
.DESCRIPTION
Executa o Gitleaks 8.30.1 em patches sintéticos, sem acesso à rede durante a
varredura e sem exibir valores sinalizados. Por padrão, Docker prepara e executa
o runner em container usando as imagens fixadas do projeto e do CI.
.PARAMETER RepositoryRoot
Raiz do repositório; padrão dois níveis acima deste script.
.PARAMETER ScannerPath
Caminho opcional do executável Gitleaks 8.30.1 em um container de testes.
Sem esse parâmetro, o script usa Docker e a imagem do CI.
.PARAMETER HistoricalFixture
Usa a linha pública da configuração quando a tarefa original ainda não existe.
.PARAMETER RemainingArguments
Aceita somente --help.
.EXAMPLE
./tools/scripts/test-gitleaks.ps1
.EXAMPLE
./tools/scripts/test-gitleaks.ps1 -ScannerPath /usr/local/bin/gitleaks
.EXAMPLE
./tools/scripts/test-gitleaks.ps1 -HistoricalFixture
.INPUTS
Configuração Gitleaks e, por padrão, tarefa local versionada; não lê credenciais.
.OUTPUTS
Resumo dos cenários aprovados ou erro acionável.
.NOTES
Requer PowerShell 7.6 e Docker Engine com a imagem
docker.io/lzocateli/gitleaks:8.30.1 e a imagem do SDK
docker.io/lzocateli/dotnet-sdk:10.0.403-noble-v1, ou o executável correspondente
no container. O primeiro uso de Docker pode exigir acesso ao registro.
.LINK
../../.github/SCRIPTING.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $RepositoryRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string] $ScannerPath = '',
    [switch] $HistoricalFixture,
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}
if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine('Argumento desconhecido. Use --help.')
    exit 2
}

$configPath = Join-Path $RepositoryRoot '.gitleaks.toml'
$taskPath = Join-Path $RepositoryRoot '.vscode/tasks.json'
$requiredPaths = @($configPath)
if (-not $HistoricalFixture) { $requiredPaths += $taskPath }
foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Arquivo necessário ausente: $path. Use --help."
    }
}

if (-not $ScannerPath) {
    $docker = Get-Command docker -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $docker) {
        throw 'Docker não encontrado; use o container de testes apropriado. Use --help.'
    }

    $resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
    $runnerRoot = Join-Path ([IO.Path]::GetTempPath()) "nodra-gitleaks-runner-$([Guid]::NewGuid().ToString('N'))"
    $containerId = $null
    try {
        New-Item -ItemType Directory -Path $runnerRoot | Out-Null
        $created = @(& $docker.Source create --network none --read-only docker.io/lzocateli/gitleaks:8.30.1 version)
        if ($LASTEXITCODE -ne 0 -or $created.Count -eq 0 -or $created[-1] -cnotmatch '^[0-9a-f]{12,64}$') {
            throw 'Não foi possível preparar a imagem fixada do Gitleaks.'
        }
        $containerId = $created[-1]
        $runnerScanner = Join-Path $runnerRoot 'gitleaks'
        & $docker.Source cp "${containerId}:/usr/local/bin/gitleaks" $runnerScanner
        if ($LASTEXITCODE -ne 0) {
            throw 'Não foi possível disponibilizar o Gitleaks para o runner em container.'
        }
        & $docker.Source rm $containerId | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw 'Não foi possível remover o container temporário do Gitleaks.'
        }
        $containerId = $null

        $runnerArguments = @(
            '-NoProfile', '-File', '/workspace/tools/scripts/test-gitleaks.ps1',
            '-RepositoryRoot', '/workspace', '-ScannerPath', '/usr/local/bin/gitleaks'
        )
        if ($HistoricalFixture) { $runnerArguments += '-HistoricalFixture' }
        & $docker.Source run --rm --network none --read-only --tmpfs /tmp:rw --user 0:0 --env HOME=/tmp `
            --volume "${resolvedRoot}:/workspace:ro" `
            --volume "${runnerScanner}:/usr/local/bin/gitleaks:ro" `
            --workdir /workspace --entrypoint pwsh docker.io/lzocateli/dotnet-sdk:10.0.403-noble-v1 `
            @runnerArguments
        if ($LASTEXITCODE -ne 0) {
            throw 'Os testes do Gitleaks falharam no runner em container.'
        }
    }
    finally {
        try {
            if ($containerId) {
                & $docker.Source rm $containerId | Out-Null
                if ($LASTEXITCODE -ne 0) {
                    throw 'Não foi possível remover o container temporário do Gitleaks.'
                }
            }
        }
        finally {
            if (Test-Path -LiteralPath $runnerRoot) {
                Remove-Item -LiteralPath $runnerRoot -Recurse -Force
            }
        }
    }
    exit 0
}

if (-not (Test-Path -LiteralPath $ScannerPath -PathType Leaf)) {
    throw 'Executável Gitleaks não encontrado. Use --help.'
}
$scannerVersion = & $ScannerPath version
if ($LASTEXITCODE -ne 0 -or "$scannerVersion".Trim() -cne 'v8.30.1') {
    throw 'O executável de teste deve ser o Gitleaks 8.30.1. Use --help.'
}

function Invoke-Scan([string] $Directory, [string] $Configuration, [string] $Report) {
    $arguments = @(
        'dir', $Directory, '--redact', '--no-banner', '--log-level', 'error',
        '--report-format', 'json', '--config', $Configuration, '--report-path', $Report
    )

    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $ScannerPath @arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($exitCode -notin @(0, 1) -or -not (Test-Path -LiteralPath $Report -PathType Leaf)) {
        throw "Falha ao executar Gitleaks: $($output | Out-String)"
    }
    $findings = @(ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($Report)))
    return @{ ExitCode = $exitCode; Findings = $findings }
}

function Assert-Finding([hashtable] $Result, [string] $FileName, [string] $Rule, [int] $Minimum = 1) {
    $found = @($Result.Findings | Where-Object {
        $_.File.EndsWith($FileName, [StringComparison]::Ordinal) -and $_.RuleID -ceq $Rule
    })
    if ($found.Count -lt $Minimum) {
        throw "O scanner não bloqueou o cenário $FileName pela regra $Rule."
    }
}

if ($HistoricalFixture) {
    $literalLines = @([IO.File]::ReadAllLines($configPath) | Where-Object { $_.Contains('\Q"command":') })
    if ($literalLines.Count -ne 1) {
        throw 'A linha pública da configuração mudou; revise a exceção antes de continuar.'
    }
    $literal = [regex]::Match($literalLines[0], '\\Q(?<prefix>.*?)\\E\\x3a\\Q(?<suffix>.*?)\\E')
    if (-not $literal.Success) {
        throw 'A linha pública da configuração mudou; revise a exceção antes de continuar.'
    }
    $taskLine = $literal.Groups['prefix'].Value + ':' + $literal.Groups['suffix'].Value
}
else {
    $taskLines = @([IO.File]::ReadAllLines($taskPath) | Where-Object {
        $_.Contains('"command":') -and $_.Contains('podman image inspect')
    })
    if ($taskLines.Count -ne 1) {
        throw 'A linha pública da tarefa local mudou; revise a exceção antes de continuar.'
    }
    $taskLine = $taskLines[0]
}
$tagMatch = [regex]::Match($taskLine, 'nodra-api:(?<tag>[A-Za-z0-9._-]+)')
if (-not $tagMatch.Success) {
    throw 'A tag da tarefa local mudou; revise a exceção antes de continuar.'
}
$tag = $tagMatch.Groups['tag'].Value
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) "nodra-gitleaks-$([Guid]::NewGuid().ToString('N'))"
$allowedDirectory = Join-Path $fixtureRoot 'allowed'
$blockedDirectory = Join-Path $fixtureRoot 'blocked'
$reportDirectory = Join-Path $fixtureRoot 'reports'
$encoding = [Text.UTF8Encoding]::new($false)

try {
    New-Item -ItemType Directory -Path @($allowedDirectory, $blockedDirectory, $reportDirectory) | Out-Null
    $originalName = ('a' * 40) + '.patch'
    $futureName = ('b' * 40) + '.patch'
    $configName = ('c' * 40) + '.patch'
    [IO.File]::WriteAllText(
        (Join-Path $allowedDirectory $originalName),
        "diff --git a/.vscode/tasks.json b/.vscode/tasks.json`n+$taskLine`n",
        $encoding
    )
    [IO.File]::WriteAllText((Join-Path $allowedDirectory $futureName), "-$taskLine`n", $encoding)
    $configPatch = (([IO.File]::ReadAllLines($configPath) | ForEach-Object { "+$_" }) -join "`n") + "`n"
    [IO.File]::WriteAllText((Join-Path $allowedDirectory $configName), $configPatch, $encoding)

    $allowed = Invoke-Scan $allowedDirectory $configPath (Join-Path $reportDirectory 'allowed.json')
    if ($allowed.ExitCode -ne 0 -or $allowed.Findings.Count -ne 0) {
        throw 'A linha pública ou o patch da configuração continua bloqueado.'
    }

    $elsewhereName = ('d' * 40) + '.patch'
    $modifiedName = ('e' * 40) + '.patch'
    $otherRuleName = ('f' * 40) + '.patch'
    $digestBytes = [Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes('nodra-fixture-gitleaks')
    )
    $digest = [Convert]::ToHexString($digestBytes)
    $syntheticKey = $digest.Substring(0, 32)
    $awsAlphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
    $syntheticAwsKey = ('AK' + 'IA') + (-join ($digestBytes[0..15] | ForEach-Object {
        $awsAlphabet[$_ % $awsAlphabet.Length]
    }))
    [IO.File]::WriteAllText((Join-Path $blockedDirectory $elsewhereName), "+api:$tag`n", $encoding)
    $changedLine = "+$taskLine " + ('api' + '_key') + "=`"$syntheticKey`"`n"
    [IO.File]::WriteAllText((Join-Path $blockedDirectory $modifiedName), $changedLine, $encoding)
    [IO.File]::WriteAllText((Join-Path $blockedDirectory $otherRuleName), "+cloud_access_key=$syntheticAwsKey`n", $encoding)

    $blocked = Invoke-Scan $blockedDirectory $configPath (Join-Path $reportDirectory 'blocked.json')
    if ($blocked.ExitCode -ne 1) {
        throw 'A varredura deve reprovar as credenciais sintéticas.'
    }
    Assert-Finding $blocked $elsewhereName 'generic-api-key'
    Assert-Finding $blocked $modifiedName 'generic-api-key' 2
    Assert-Finding $blocked $otherRuleName 'aws-access-token'

    $customConfigPath = Join-Path $fixtureRoot 'custom.toml'
    $pattern = [regex]::Escape('api:' + $tag)
    $customConfig = [IO.File]::ReadAllText($configPath) +
        "`n[[rules]]`nid = 'independent-test-rule'`nregex = '''$pattern'''`n"
    [IO.File]::WriteAllText($customConfigPath, $customConfig, $encoding)
    $independent = Invoke-Scan $allowedDirectory $customConfigPath (Join-Path $reportDirectory 'independent.json')
    if ($independent.ExitCode -ne 1) {
        throw 'A exceção não deve afetar outra regra na linha pública.'
    }
    Assert-Finding $independent $originalName 'independent-test-rule'
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}

Write-Output 'Testes do Gitleaks concluídos: exceção e cenários negativos aprovados.'
exit 0

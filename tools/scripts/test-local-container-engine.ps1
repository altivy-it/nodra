<#
.SYNOPSIS
Testa o contrato de selecao de engine do inicializador self-hosted e do laboratorio Podman.

.DESCRIPTION
Executa tools/scripts/start-self-hosted.ps1 e tools/scripts/start-local-lab.ps1
com engines falsos em um PATH temporario, sem Docker, Podman, rede ou secrets
reais. Verifica ajuda sem dependencias, engine invalido, recusa de provider
incompativel antes de qualquer configuracao e preservacao do fluxo Docker.

.PARAMETER RemainingArguments
Aceita somente --help; outros valores retornam erro de uso.

.EXAMPLE
pwsh -NoProfile -File tools/scripts/test-local-container-engine.ps1

.INPUTS
Nenhum.

.OUTPUTS
Resumo dos cenarios aprovados ou erro com o cenario divergente.

.NOTES
Requer PowerShell 7.4+, bash e diretorio temporario sem noexec (em container,
use --tmpfs /tmp:rw,exec). Arquivos temporarios sao removidos ao final.

.LINK
../../deploy/self-hosted/README.md
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; exit 0 }
if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumento desconhecido: $($RemainingArguments -join ' '). Use --help.")
    exit 2
}
$bashCommand = Get-Command bash -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $bashCommand) { throw 'bash e necessario para os engines falsos.' }
# O PATH dos cenarios contem somente o engine falso e o pwsh; o shebang precisa ser absoluto.
$bashPath = $bashCommand.Source

$starter = Join-Path $PSScriptRoot 'start-self-hosted.ps1'
$lab = Join-Path $PSScriptRoot 'start-local-lab.ps1'
$pwshDirectory = Split-Path (Get-Process -Id $PID).Path
$root = Join-Path ([IO.Path]::GetTempPath()) "nodra-engine-test-$([Guid]::NewGuid().ToString('N'))"
$failures = [Collections.Generic.List[string]]::new()
$passed = 0

function New-FakeEngine {
    param([string] $Name, [string] $ComposeVersion)
    $bin = Join-Path $root "bin-$Name-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $bin | Out-Null
    $log = Join-Path $bin 'calls.log'
    $script = @"
#!$bashPath
printf '%s\n' "`$*" >> '$log'
if [ "`$1 `$2" = 'compose version' ]; then echo '$ComposeVersion'; exit 0; fi
echo 'falha simulada' >&2
exit 1
"@
    $path = Join-Path $bin $Name
    [IO.File]::WriteAllText($path, $script.Replace("`r`n", "`n"))
    & chmod 0755 $path
    return [pscustomobject]@{ Bin = $bin; Log = $log }
}

function Invoke-Script {
    param([string] $Script, [string[]] $Arguments, [string] $Bin)
    $pathValue = if ($Bin) { "${Bin}:$pwshDirectory" } else { $pwshDirectory }
    $previousPath = $env:PATH
    try {
        $env:PATH = $pathValue
        $output = & (Join-Path $pwshDirectory 'pwsh') -NoProfile -File $Script @Arguments 2>&1 | Out-String
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    } finally { $env:PATH = $previousPath }
}

function Get-Calls { param([string] $Log) if (Test-Path -LiteralPath $Log) { @(Get-Content -LiteralPath $Log) } else { @() } }

function Assert-Case {
    param([string] $Name, [scriptblock] $Condition, [string] $Detail)
    if (& $Condition) { $script:passed++ } else { $script:failures.Add("${Name}: $Detail") }
}

New-Item -ItemType Directory -Path $root | Out-Null
try {
    $probe = New-FakeEngine -Name 'probe' -ComposeVersion 'ok'
    $probeOk = try { & (Join-Path $probe.Bin 'probe') compose version *> $null; $LASTEXITCODE -eq 0 } catch { $false }
    if (-not $probeOk) { throw 'Diretorio temporario sem permissao de execucao (noexec). Use --help.' }
    $envFile = Join-Path $root 'altivy.env'
    [IO.File]::WriteAllText($envFile, "ALTIVY_PUBLIC_ORIGIN=https://localhost:7443/notes`n")
    $common = @('-EnvironmentFile', $envFile, '-CertificateDirectory', (Join-Path $root 'certs'), '-DataRoot', (Join-Path $root 'data'), '-ValidateOnly')

    $result = Invoke-Script -Script $starter -Arguments @('--help')
    Assert-Case 'ajuda do inicializador' { $result.ExitCode -eq 0 -and $result.Output -match 'ContainerEngine' } "exit $($result.ExitCode)"

    $result = Invoke-Script -Script $starter -Arguments ($common + @('-ContainerEngine', 'nerdctl'))
    Assert-Case 'engine invalido' { $result.ExitCode -ne 0 -and $result.Output -match 'ContainerEngine' } "exit $($result.ExitCode)"

    $result = Invoke-Script -Script $starter -Arguments ($common + @('-ContainerEngine', 'podman'))
    Assert-Case 'engine ausente' { $result.ExitCode -ne 0 -and $result.Output -match 'indisponivel no PATH' } "exit $($result.ExitCode)"

    $fake = New-FakeEngine -Name 'podman' -ComposeVersion 'podman-compose version 1.5.0'
    $result = Invoke-Script -Script $starter -Arguments ($common + @('-ContainerEngine', 'podman')) -Bin $fake.Bin
    $calls = @(Get-Calls $fake.Log)
    Assert-Case 'provider incompativel recusado' { $result.ExitCode -ne 0 -and $result.Output -match 'Docker Compose v2' } "exit $($result.ExitCode)"
    Assert-Case 'provider recusado antes da configuracao' { $calls.Count -eq 1 -and $calls[0] -eq 'compose version' } ($calls -join ' | ')

    $fake = New-FakeEngine -Name 'podman' -ComposeVersion 'Docker Compose version v5.8.0'
    $result = Invoke-Script -Script $starter -Arguments ($common + @('-ContainerEngine', 'podman')) -Bin $fake.Bin
    $calls = @(Get-Calls $fake.Log)
    Assert-Case 'provider v2+ aceito' { $calls.Count -ge 2 -and $calls[1] -match '^compose -f .*configuration\.yaml config --no-interpolate' } ($calls -join ' | ')
    Assert-Case 'falha sanitizada' { $result.ExitCode -ne 0 -and $result.Output -notmatch 'falha simulada' } 'saida do engine exposta em etapa capturada'

    $fake = New-FakeEngine -Name 'docker' -ComposeVersion 'Docker Compose version v2.40.0'
    $result = Invoke-Script -Script $starter -Arguments $common -Bin $fake.Bin
    $calls = @(Get-Calls $fake.Log)
    Assert-Case 'docker continua padrao sem verificacao de provider' { $calls.Count -ge 1 -and $calls[0] -match '^compose -f .*configuration\.yaml config --no-interpolate' } ($calls -join ' | ')

    $result = Invoke-Script -Script $lab -Arguments @('--help')
    Assert-Case 'ajuda do laboratorio' { $result.ExitCode -eq 0 -and $result.Output -match 'RunE2E' } "exit $($result.ExitCode)"

    $result = Invoke-Script -Script $lab -Arguments @('--desconhecido')
    Assert-Case 'argumento desconhecido do laboratorio' { $result.ExitCode -eq 2 } "exit $($result.ExitCode)"

    $result = Invoke-Script -Script $lab -Arguments @('-LabRoot', (Join-Path $root 'lab'))
    Assert-Case 'laboratorio sem podman falha antes de criar dados' { $result.ExitCode -ne 0 -and -not (Test-Path -LiteralPath (Join-Path $root 'lab')) } "exit $($result.ExitCode)"
} finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine("FALHA $_") }
    exit 1
}
Write-Host "Contrato de engine local aprovado ($passed verificacoes)."

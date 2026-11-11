<#
.SYNOPSIS
Valida os contratos estruturais de CI/CD do Nodra sem publicar ou implantar.
.DESCRIPTION
Le os workflows com YamlDotNet e verifica eventos, permissoes, gates e imagens.
Executar em container apos compilar o backend para disponibilizar YamlDotNet.
.PARAMETER RepositoryRoot
Raiz do repositorio; padrao dois niveis acima deste script.
.PARAMETER YamlAssemblyPath
DLL YamlDotNet existente no build Release da API, relativa a raiz.
.PARAMETER RemainingArguments
Aceita somente --help.
.EXAMPLE
./tools/scripts/test-cicd-contracts.ps1
.EXAMPLE
./tools/scripts/test-cicd-contracts.ps1 --help
.INPUTS
Arquivos versionados; nenhum secret ou conexao remota.
.OUTPUTS
Resumo dos contratos aprovados ou erro acionavel.
.NOTES
Requer PowerShell 7.6/.NET 10, Bash e YamlDotNet 18.1.0 do backend existente.
.LINK
../../specs/011-padronizacao-cicd/quickstart.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $RepositoryRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string] $YamlAssemblyPath = 'backend/apps/api/bin/Release/net10.0/YamlDotNet.dll',
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}
if ($RemainingArguments.Count -gt 0) {
    throw 'Argumento desconhecido. Use --help.'
}

$assembly = Join-Path $RepositoryRoot $YamlAssemblyPath
if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) {
    throw 'Compile a API em Release para disponibilizar YamlDotNet. Use --help.'
}
$yamlAssembly = [Reflection.Assembly]::LoadFrom($assembly)
$builderType = $yamlAssembly.GetType('YamlDotNet.Serialization.DeserializerBuilder', $true)
$deserializer = [Activator]::CreateInstance($builderType).Build()

function Read-Workflow([string] $Name) {
    $path = Join-Path $RepositoryRoot ".github/workflows/$Name"
    return $deserializer.Deserialize([IO.File]::ReadAllText($path), [object])
}

function Assert-Contract([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw "Contrato CI/CD invalido: $Message" }
}

$ci = Read-Workflow 'ci.yml'
foreach ($eventName in @('pull_request', 'merge_group', 'push', 'workflow_dispatch')) {
    Assert-Contract ($ci['on'].ContainsKey($eventName)) "evento $eventName ausente"
}
Assert-Contract ($ci['permissions'].Count -eq 0) 'permissoes globais devem ser vazias'
foreach ($jobName in @('hygiene', 'gitleaks')) {
    Assert-Contract ($ci['jobs'].ContainsKey($jobName)) "gate $jobName ausente"
}
foreach ($jobName in $ci['jobs'].Keys) {
    if ($jobName -ne 'result') {
        Assert-Contract ($jobName -in $ci['jobs']['result']['needs']) "Result nao agrega $jobName"
    }
}
$serializedCi = $ci | ConvertTo-Json -Depth 100
Assert-Contract ($serializedCi -notmatch 'secrets\.') 'CI nao deve consumir secrets'
Assert-Contract ($serializedCi -notmatch 'pull_request_target') 'evento privilegiado nao permitido'
foreach ($job in $ci['jobs'].Values) {
    Assert-Contract ($job.ContainsKey('permissions')) 'job sem permissoes explicitas'
    foreach ($permission in $job['permissions'].Values) {
        Assert-Contract ($permission -eq 'read') 'CI nao deve obter permissoes de escrita'
    }
}
Assert-Contract ($ci['jobs']['result']['if'] -eq '${{ always() }}') 'Result deve executar sempre'

$publish = Read-Workflow 'publish.yml'
$images = @($publish['jobs']['containers']['with']['images'] | ConvertFrom-Json)
$expectedImages = @('nodra-api', 'nodra-bff', 'nodra-worker', 'nodra-web', 'nodra-migrations')
Assert-Contract ($images.Count -eq $expectedImages.Count) 'quantidade de imagens incorreta'
foreach ($name in $expectedImages) {
    Assert-Contract ($name -in $images.name) "imagem $name ausente"
}
foreach ($image in $images) {
    Assert-Contract (Test-Path -LiteralPath (Join-Path $RepositoryRoot $image.dockerfile)) "Dockerfile ausente: $($image.dockerfile)"
}
Assert-Contract ($publish['jobs']['containers']['with']['registry'] -ceq 'ghcr.io/altivy-it') 'namespace GHCR incorreto'
Assert-Contract ($publish['jobs']['security']['needs'] -eq 'containers') 'scan deve usar os digests publicados'
Assert-Contract ($publish['jobs']['build-assets']['needs'] -eq 'security') 'release nao deve terminar antes do scan'

$deploy = Read-Workflow 'deploy.yml'
foreach ($jobName in @('deploy', 'smoke')) {
    Assert-Contract ($deploy['jobs'][$jobName].ContainsKey('environment')) "job $jobName sem Environment"
}
$config = Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot '.github/ci-cd.json') | ConvertFrom-Json
Assert-Contract (-not $config.publishSnapshots) 'snapshots nao autorizados'
Assert-Contract ([string]::IsNullOrWhiteSpace($config.deploy.autoDeployEnvironment)) 'auto-deploy nao autorizado'
Assert-Contract ($config.attestations -eq $true) 'atestacoes nao devem ser desabilitadas implicitamente'
$setup = $config.repositorySetup
$expectedEnvironments = @('ci', 'copilot', 'quality', 'release', 'distribution', 'production')
$actualEnvironments = @($setup.environments | ForEach-Object { $_.name })
Assert-Contract ($actualEnvironments.Count -eq 6 -and (($actualEnvironments | Sort-Object) -join '|') -ceq (($expectedEnvironments | Sort-Object) -join '|')) 'inventario deve declarar os seis ambientes'
foreach ($name in @('quality', 'release', 'distribution', 'production')) {
    $environment = @($setup.environments | Where-Object name -ceq $name)[0]
    Assert-Contract ($environment.requiredReviewers -and $environment.reviewerGroup -ceq 'Administradores da Altivy') "$name deve requerer administradores da Altivy"
    Assert-Contract (-not $environment.reviewerTeamSlugConfigured) "$name nao pode inventar slug de equipe"
}
foreach ($name in @('ci', 'copilot')) {
    $environment = @($setup.environments | Where-Object name -ceq $name)[0]
    Assert-Contract (-not $environment.requiredReviewers) "$name nao deve herdar aprovacao de deploy"
}
$requiredVariables = @('RELEASE_APP_ID|repository', 'ALTIVY_ADMIN_TEAM|repository', 'DEPLOY_SSH_TARGET|environment:quality', 'DEPLOY_HOOK|environment:quality', 'DEPLOY_SSH_TARGET|environment:production', 'DEPLOY_HOOK|environment:production')
$actualVariables = @($setup.variables | ForEach-Object { "$($_.name)|$($_.scope)" })
Assert-Contract ($actualVariables.Count -eq $requiredVariables.Count -and (($actualVariables | Sort-Object) -join '|') -ceq (($requiredVariables | Sort-Object) -join '|')) 'nomes/escopos das variables divergem do contrato'
$requiredSecrets = @('RELEASE_APP_PRIVATE_KEY|repository', 'DEPLOY_SSH_KEY|environment:quality', 'DEPLOY_SSH_KNOWN_HOSTS|environment:quality', 'DEPLOY_SSH_KEY|environment:production', 'DEPLOY_SSH_KNOWN_HOSTS|environment:production')
$actualSecrets = @($setup.secrets | ForEach-Object { "$($_.name)|$($_.scope)" })
Assert-Contract ($actualSecrets.Count -eq $requiredSecrets.Count -and (($actualSecrets | Sort-Object) -join '|') -ceq (($requiredSecrets | Sort-Object) -join '|')) 'nomes/escopos dos secrets divergem do contrato'
Assert-Contract (($setup.variables | Where-Object { $_.PSObject.Properties.Name -contains 'value' }).Count -eq 0 -and ($setup.secrets | Where-Object { $_.PSObject.Properties.Name -contains 'value' }).Count -eq 0) 'inventario nao pode armazenar valores'

$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) "nodra-manifest-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null

function Invoke-ContractProcess([string] $Executable, [string[]] $Arguments, [hashtable] $Environment = @{}) {
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.WorkingDirectory = $RepositoryRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    foreach ($name in $Environment.Keys) { $start.Environment[$name] = $Environment[$name] }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) {
            $process.Kill($true)
            throw 'Contrato offline excedeu 30 segundos.'
        }
        return @{ Code = $process.ExitCode; Output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult() }
    }
    finally { $process.Dispose() }
}

try {
    $pwsh = (Get-Process -Id $PID).Path
    $resultStep = @($ci['jobs']['result']['steps'] | Where-Object { $_.ContainsKey('run') })[0]
    $aggregatePath = Join-Path $fixtureRoot 'result.sh'
    [IO.File]::WriteAllText($aggregatePath, $resultStep['run'])
    $jobResults = @{}
    foreach ($name in $resultStep['env'].Keys) { $jobResults[$name] = 'success' }
    $result = Invoke-ContractProcess 'bash' @($aggregatePath) $jobResults
    Assert-Contract ($result.Code -eq 0) 'Result recusou todos os jobs verdes'
    foreach ($name in $jobResults.Keys) {
        foreach ($status in @('failure', 'cancelled', 'skipped', '')) {
            $caseResults = $jobResults.Clone()
            $caseResults[$name] = $status
            $result = Invoke-ContractProcess 'bash' @($aggregatePath) $caseResults
            Assert-Contract ($result.Code -ne 0) "Result aceitou $name com status '$status'"
        }
    }

    $approvalPath = Join-Path $fixtureRoot 'approvals.ps1'
    $approvalScript = $publish['jobs']['approvals']['steps'][0]['run']
    [IO.File]::WriteAllText($approvalPath, $approvalScript)
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $approvalPath) @{ ADMIN_TEAM = '' }
    Assert-Contract ($result.Code -ne 0 -and $result.Output.Contains('Configure ALTIVY_ADMIN_TEAM')) 'aprovacao ausente nao recusada antes da API'
    foreach ($reviewerCase in @('missing', 'team', 'mixed')) {
        $reviewers = @(if ($reviewerCase -ne 'missing') { @{ type = 'Team'; reviewer = @{ slug = 'administracao-test' } } })
        if ($reviewerCase -eq 'mixed') { $reviewers += @{ type = 'User'; reviewer = @{ login = 'individual-fixture' } } }
        $metadata = @{ protection_rules = @(@{ type = 'required_reviewers'; reviewers = $reviewers }) } | ConvertTo-Json -Depth 10 -Compress
        $mock = "function gh { `$global:LASTEXITCODE = 0; '$metadata' }`n"
        [IO.File]::WriteAllText($approvalPath, $mock + $approvalScript)
        $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $approvalPath) @{ ADMIN_TEAM = 'administracao-test'; GITHUB_REPOSITORY = 'altivy-it/nodra' }
        Assert-Contract (($result.Code -eq 0) -eq ($reviewerCase -eq 'team')) 'gate de equipes nao corresponde a politica sintetica'
    }

    $guard = Join-Path $RepositoryRoot 'tools/scripts/ci/Assert-DeployableRelease.ps1'
    foreach ($arguments in @(
        @('-Tag', 'main', '-Environment', 'quality'),
        @('-Tag', 'v0.1.0', '-Environment', 'development'),
        @('-Tag', 'v0.1.0', '-Environment', 'production', '-Trigger', 'release')
    )) {
        $result = Invoke-ContractProcess $pwsh (@('-NoProfile', '-File', $guard, '-Repository', 'altivy-it/nodra') + $arguments)
        Assert-Contract ($result.Code -ne 0 -and $result.Output -match 'Deploy só|não está em deploy.environments|Auto-deploy em production') 'guard deveria recusar antes de consultar release ou host'
    }

    $fixture = @{
        schemaVersion = 1
        repository = 'altivy-it/nodra'
        version = '0.1.0'
        tag = 'v0.1.0'
        commit = ('a' * 40)
        rollbackMinimumVersion = '0.0.0'
        attestation = 'github'
        createdAt = '2026-11-11T00:00:00Z'
        artifacts = @($expectedImages | ForEach-Object {
            @{ kind = 'container'; name = $_; reference = "ghcr.io/altivy-it/$_"; digest = ('sha256:' + ('b' * 64)) }
        })
    }
    $fixturePath = Join-Path $fixtureRoot 'release-manifest.json'
    $entriesPath = Join-Path $fixtureRoot 'entries'
    New-Item -ItemType Directory -Path $entriesPath | Out-Null
    $fixture.artifacts | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $entriesPath 'containers.json')
    $scanner = Join-Path $RepositoryRoot 'tools/scripts/validate-release-images.ps1'
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $scanner, '-ReleaseEntriesPath', $entriesPath, '-DryRun')
    Assert-Contract ($result.Code -eq 0 -and $result.Output.Contains('--image-src remote ghcr.io/altivy-it/nodra-api@sha256:') -and -not $result.Output.Contains('buildx')) 'scan deve consumir digests sem rebuild'
    $fixture | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $fixturePath
    $converter = Join-Path $RepositoryRoot 'tools/scripts/convert-release-manifest.ps1'
    $output = & (Get-Process -Id $PID).Path -NoProfile -File $converter -ManifestPath $fixturePath
    Assert-Contract ($LASTEXITCODE -eq 0 -and @($output).Count -eq 5) 'conversao de manifesto valido falhou'
    $validManifest = $fixture | ConvertTo-Json -Depth 10
    foreach ($mutation in @(
        { param($candidate) $candidate.artifacts[0].reference = 'ghcr.io/other/nodra-api' },
        { param($candidate) $candidate.repository = 'altivy-it/other' },
        { param($candidate) $candidate.attestation = 'none' },
        { param($candidate) $candidate.artifacts[0].digest = 'sha256:invalid' },
        { param($candidate) $candidate.artifacts = @($candidate.artifacts[0..3]) },
        { param($candidate) $candidate.artifacts[1] = $candidate.artifacts[0].Clone() }
    )) {
        $candidate = $validManifest | ConvertFrom-Json -AsHashtable
        & $mutation $candidate
        $candidate | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $fixturePath
        $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $converter, '-ManifestPath', $fixturePath)
        Assert-Contract ($result.Code -ne 0) 'manifesto mutado nao foi rejeitado'
    }
    Set-Content -LiteralPath $fixturePath -Value $validManifest
    $imagesPath = Join-Path $fixtureRoot 'images.env'
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $converter, '-ManifestPath', $fixturePath, '-OutputPath', $imagesPath)
    Assert-Contract ($result.Code -eq 0) 'criacao de images.env falhou'
    $original = [IO.File]::ReadAllText($imagesPath)
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $converter, '-ManifestPath', $fixturePath, '-OutputPath', $imagesPath)
    Assert-Contract ($result.Code -ne 0 -and [IO.File]::ReadAllText($imagesPath) -ceq $original) 'destino existente foi sobrescrito'
}
finally { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
Write-Output 'Contratos estruturais CI/CD aprovados; nenhuma operacao remota executada.'
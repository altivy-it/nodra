<#
.SYNOPSIS
Valida os contratos estruturais de CI/CD do Nodra sem publicar ou implantar.
.DESCRIPTION
Le os workflows com YamlDotNet e verifica eventos, permissoes, gates e imagens.
Confere os arquivos gerenciados em checkouts Git com diferentes finais de linha.
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
Requer PowerShell 7.6/.NET 10, Bash, Git e YamlDotNet 18.1.0 do backend existente.
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

$imageSteps = @($ci['jobs']['images']['steps'])
$dockerSetup = @($imageSteps | Where-Object { $_.ContainsKey('uses') -and $_['uses'] -like 'docker/setup-docker-action@*' })
$imageBuild = @($imageSteps | Where-Object { $_['name'] -eq 'Build production images in BuildKit' })
$attestationCheck = @($imageSteps | Where-Object { $_['name'] -eq 'Verify BuildKit attestations' })
Assert-Contract ($dockerSetup.Count -eq 1 -and $dockerSetup[0]['uses'] -cmatch '^docker/setup-docker-action@[a-f0-9]{40}$') 'setup Docker deve usar SHA imutavel'
Assert-Contract ($dockerSetup[0]['with']['version'] -cmatch '^v[0-9]+\.[0-9]+\.[0-9]+$') 'Engine Docker deve ter versao fixa'
$dockerVersion = [version]$dockerSetup[0]['with']['version'].TrimStart('v')
Assert-Contract ($dockerVersion -ge [version]'29.6.0') 'API de atestacoes requer Docker Engine 29.6 ou superior'
Assert-Contract ([string]$dockerSetup[0]['with']['set-host'] -eq 'true') 'socket isolado do Docker deve estar disponivel no job'
$daemonConfig = $dockerSetup[0]['with']['daemon-config'] | ConvertFrom-Json -AsHashtable
Assert-Contract ($daemonConfig.features.'containerd-snapshotter' -eq $true -and $daemonConfig.features.'lazy-pull' -eq $false) 'store compativel e download completo das camadas sao obrigatorios'
Assert-Contract ($imageBuild.Count -eq 1 -and $attestationCheck.Count -eq 1 -and
    [array]::IndexOf($imageSteps, $dockerSetup[0]) -lt [array]::IndexOf($imageSteps, $imageBuild[0]) -and
    [array]::IndexOf($imageSteps, $imageBuild[0]) -lt [array]::IndexOf($imageSteps, $attestationCheck[0])) 'setup, build e verificacao de atestacoes devem executar nessa ordem'
Assert-Contract ($attestationCheck[0]['run'].Contains('for image in api bff worker web migrations') -and
    $attestationCheck[0]['run'].Contains('assert-buildkit-attestations.ps1')) 'as cinco imagens devem ter as atestacoes verificadas'

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
    $dockerDirs = @((Join-Path $fixtureRoot 'docker-first'), (Join-Path $fixtureRoot 'docker-second'))
    New-Item -ItemType Directory -Path $dockerDirs | Out-Null
    $firstDocker = Join-Path $dockerDirs[0] 'docker'
    $secondDocker = Join-Path $dockerDirs[1] 'docker'
    $dockerLog = Join-Path $fixtureRoot 'docker-calls'
    $dockerCopyPath = Join-Path $fixtureRoot 'docker-copy-path'
    $dockerMock = @'
#!/bin/sh
printf '%s\n' "$1" >> "$NODRA_DOCKER_LOG"
case "$1" in
    create) printf '%064d\n' 1 ;;
    cp) : > "$3"; printf '%s\n' "$3" > "$NODRA_DOCKER_COPY_PATH" ;;
    rm|run) ;;
    *) exit 2 ;;
esac
'@
    [IO.File]::WriteAllText($firstDocker, $dockerMock.Replace("`r`n", "`n"))
    [IO.File]::WriteAllText($secondDocker, "#!/bin/sh`nexit 9`n")
    & chmod 755 $firstDocker $secondDocker
    if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel preparar Docker sintetico.' }
    $gitleaksTest = Join-Path $RepositoryRoot 'tools/scripts/test-gitleaks.ps1'
    $dockerEnvironment = @{
        PATH = ($dockerDirs + @($env:PATH)) -join [IO.Path]::PathSeparator
        NODRA_DOCKER_LOG = $dockerLog
        NODRA_DOCKER_COPY_PATH = $dockerCopyPath
    }
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $gitleaksTest, '-RepositoryRoot', $RepositoryRoot) $dockerEnvironment
    Assert-Contract ($result.Code -eq 0) "selecao do primeiro Docker no PATH falhou: $($result.Output)"
    Assert-Contract (((Get-Content -LiteralPath $dockerLog) -join ',') -ceq 'create,cp,rm,run') 'orquestracao Docker incompleta'
    $copiedScanner = [IO.File]::ReadAllText($dockerCopyPath).Trim()
    Assert-Contract (-not (Test-Path -LiteralPath (Split-Path -Parent $copiedScanner))) 'diretorio temporario do scanner nao foi removido'
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $gitleaksTest, '-RepositoryRoot', $RepositoryRoot) @{ PATH = (Join-Path $fixtureRoot 'docker-missing') }
    Assert-Contract ($result.Code -ne 0 -and $result.Output.Contains('Docker não encontrado')) 'ausencia do Docker nao foi recusada explicitamente'

    $subject = @(@{ digest = @{ sha256 = ('a' * 64) } })
    $sbom = @{
        PredicateType = 'https://spdx.dev/Document'
        Statement = @{ predicateType = 'https://spdx.dev/Document'; subject = $subject; predicate = @{ SPDXID = 'SPDXRef-DOCUMENT'; spdxVersion = 'SPDX-2.3' } }
    }
    $provenance = @{
        PredicateType = 'https://slsa.dev/provenance/v1'
        Statement = @{
            predicateType = 'https://slsa.dev/provenance/v1'
            subject = $subject
            predicate = @{ buildDefinition = @{ internalParameters = @{ buildConfig = @{ llbDefinition = @(@{ id = 'step0' }) } } } }
        }
    }
    $attestations = @($sbom, $provenance)
    $validAttestations = ConvertTo-Json -InputObject $attestations -Depth 20 -Compress
    $missingSubject = $validAttestations | ConvertFrom-Json -AsHashtable
    $missingSubject[0].Statement.subject = @()
    $minimumProvenance = $validAttestations | ConvertFrom-Json -AsHashtable
    $minimumProvenance[1].Statement.predicate.buildDefinition.internalParameters.Remove('buildConfig')
    $invalidPredicate = $validAttestations | ConvertFrom-Json -AsHashtable
    $invalidPredicate[0].Statement.predicateType = 'https://example.invalid/other'
    $invalidSbom = $validAttestations | ConvertFrom-Json -AsHashtable
    $invalidSbom[0].Statement.predicate.Remove('spdxVersion')
    $differentSubject = $validAttestations | ConvertFrom-Json -AsHashtable
    $differentSubject[1].Statement.subject[0].digest.sha256 = ('b' * 64)
    $attestationCases = @(
        @{ Name = 'validas'; Json = $validAttestations; Valid = $true },
        @{ Name = 'sem SBOM'; Json = (ConvertTo-Json -InputObject @($provenance) -Depth 20 -Compress); Valid = $false },
        @{ Name = 'subject ausente'; Json = (ConvertTo-Json -InputObject $missingSubject -Depth 20 -Compress); Valid = $false },
        @{ Name = 'proveniencia minima'; Json = (ConvertTo-Json -InputObject $minimumProvenance -Depth 20 -Compress); Valid = $false },
        @{ Name = 'predicado divergente'; Json = (ConvertTo-Json -InputObject $invalidPredicate -Depth 20 -Compress); Valid = $false },
        @{ Name = 'SBOM incompleto'; Json = (ConvertTo-Json -InputObject $invalidSbom -Depth 20 -Compress); Valid = $false },
        @{ Name = 'subject divergente'; Json = (ConvertTo-Json -InputObject $differentSubject -Depth 20 -Compress); Valid = $false },
        @{ Name = 'resposta invalida'; Json = '{'; Valid = $false }
    )
    $attestationScript = Join-Path $RepositoryRoot 'tools/scripts/assert-buildkit-attestations.ps1'
    $attestationFixture = Join-Path $fixtureRoot 'attestations.json'
    foreach ($case in $attestationCases) {
        [IO.File]::WriteAllText($attestationFixture, $case.Json)
        $result = Invoke-ContractProcess 'bash' @('-c', 'exec "$1" -NoProfile -File "$2" < "$3"', 'bash', $pwsh, $attestationScript, $attestationFixture)
        Assert-Contract (($result.Code -eq 0) -eq $case.Valid) "validacao de atestacoes aceitou ou recusou incorretamente: $($case.Name)"
    }

    $managedManifest = Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot '.groundwork/manifest.json') | ConvertFrom-Json -AsHashtable
    $managedSource = Join-Path $fixtureRoot 'managed-source'
    New-Item -ItemType Directory -Path $managedSource | Out-Null
    foreach ($relative in @('.gitattributes', '.groundwork/manifest.json') + @($managedManifest.files.Keys)) {
        $destination = Join-Path $managedSource $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $RepositoryRoot $relative) -Destination $destination
    }
    $result = Invoke-ContractProcess 'git' @('-C', $managedSource, 'init', '--quiet')
    Assert-Contract ($result.Code -eq 0) 'nao foi possivel inicializar fixture Git'
    $result = Invoke-ContractProcess 'git' @('-C', $managedSource, '-c', 'core.autocrlf=input', '-c', 'core.safecrlf=false', 'add', '--all')
    Assert-Contract ($result.Code -eq 0) 'nao foi possivel preparar fixture Git'

    foreach ($autoCrlf in @('false', 'true')) {
        $checkout = Join-Path $fixtureRoot "managed-$autoCrlf"
        New-Item -ItemType Directory -Path $checkout | Out-Null
        $prefix = "$checkout$([IO.Path]::DirectorySeparatorChar)"
        $result = Invoke-ContractProcess 'git' @('-C', $managedSource, '-c', "core.autocrlf=$autoCrlf", 'checkout-index', '--all', "--prefix=$prefix")
        Assert-Contract ($result.Code -eq 0) "checkout sintetico falhou para core.autocrlf=$autoCrlf"

        $managedCheck = Join-Path $checkout 'tools/scripts/ci/Test-ManagedFiles.ps1'
        $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $managedCheck, '-Root', $checkout)
        Assert-Contract ($result.Code -eq 0) "arquivos gerenciados divergiram com core.autocrlf=$autoCrlf`: $($result.Output)"
        foreach ($relative in $managedManifest.files.Keys) {
            $hash = (Get-FileHash -LiteralPath (Join-Path $checkout $relative) -Algorithm SHA256).Hash.ToLowerInvariant()
            Assert-Contract ($hash -ceq $managedManifest.files[$relative].sha256.ToLowerInvariant()) "hash divergente para $relative com core.autocrlf=$autoCrlf"
        }
    }
    $tampered = 'tools/scripts/ci/Test-PullRequestTitle.ps1'
    Add-Content -LiteralPath (Join-Path $checkout $tampered) -Value '# alteracao sintetica'
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $managedCheck, '-Root', $checkout)
    Assert-Contract ($result.Code -ne 0 -and $result.Output.Contains($tampered)) 'alteracao de arquivo gerenciado nao foi rejeitada'

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
    $result = Invoke-ContractProcess $pwsh @('-NoProfile', '-File', $scanner, '-Image', 'all', '-DryRun')
    $buildCommands = @($result.Output -split "`n" | Where-Object { $_.Contains('docker buildx build ') })
    Assert-Contract ($result.Code -eq 0 -and $buildCommands.Count -eq 5) 'cinco imagens devem ser construidas localmente'
    foreach ($command in $buildCommands) {
        Assert-Contract ($command.Contains('--load') -and $command.Contains('--provenance=mode=max') -and
            $command -cmatch '--sbom=generator=\S+:[^@\s]+@sha256:[a-f0-9]{64}') 'build deve carregar a imagem com proveniencia e gerador SBOM imutavel'
    }
    Assert-Contract (@($result.Output -split "`n" | Where-Object { $_.Contains('docker save ') }).Count -eq 5 -and
        @($result.Output -split "`n" | Where-Object { $_.Contains('--format cyclonedx') }).Count -eq 5 -and
        @($result.Output -split "`n" | Where-Object { $_.Contains('--severity HIGH,CRITICAL --exit-code 1') }).Count -eq 5) 'arquivos devem passar pelos dois scans Trivy'
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
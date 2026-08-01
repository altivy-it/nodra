<#
.SYNOPSIS
Executa os testes funcionais do validador do Thothis.

.DESCRIPTION
Cria fixtures temporárias para verificar cenários válidos e falhas de links,
referências imutáveis e status geral e por etapa dos planos.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help; outros valores retornam erro de uso.

.EXAMPLE
./tools/scripts/test-validate-repository.ps1

.INPUTS
Nenhum.

.OUTPUTS
Resumo dos cenários aprovados ou erro com o fixture divergente.

.NOTES
Requer PowerShell 7 ou superior e não usa dependências externas.

.LINK
../../.github/SCRIPTING.md
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'

if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}

if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumento desconhecido: $($RemainingArguments -join ' '). Use --help.")
    exit 2
}

$validator = Join-Path $PSScriptRoot 'validate-repository.ps1'
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

function New-ValidationFixture {
    $fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) "thothis-validation-$([Guid]::NewGuid())"

    foreach ($requiredPath in $requiredPaths) {
        $absolutePath = Join-Path $fixtureRoot $requiredPath
        $directory = Split-Path -Parent $absolutePath
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        Set-Content -LiteralPath $absolutePath -Value "# $requiredPath"
    }

    Set-Content -LiteralPath (Join-Path $fixtureRoot 'asset.png') -Value 'fixture'
    $planPath = Join-Path $fixtureRoot 'docs/plan/fixture.md'
    New-Item -ItemType Directory -Path (Split-Path -Parent $planPath) -Force | Out-Null
    Set-Content -LiteralPath $planPath -Value @'
# Plano fixture

**Status:** proposed

### P-01: Etapa válida

**Status:** not-started
'@
    Set-Content -LiteralPath (Join-Path $fixtureRoot 'README.md') -Value @'
# Fixture

[Documento](docs/backend.md "Título")
![Imagem](asset.png)
[Referência][backend]

[backend]: docs/backend.md

Use `<tipo>[escopo opcional][!]: <descrição>` para commits.

```text
<tipo>[escopo opcional][!]: <descrição>
```
'@
    Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/workflows/ci.yml') -Value @'
jobs:
  fixture:
    steps:
            - name: Git action
                uses: owner/action@0123456789abcdef0123456789abcdef01234567
            - name: Docker action
                uses: docker://example/image@sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef
'@

    return $fixtureRoot
}

function Invoke-FixtureValidation {
    param(
        [Parameter(Mandatory)]
        [string] $FixtureRoot,

        [Parameter(Mandatory)]
        [bool] $ShouldPass
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & (Get-Process -Id $PID).Path -NoProfile -File $validator -RepositoryRoot $FixtureRoot 2>&1
        $passed = $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($passed -ne $ShouldPass) {
        throw "Resultado inesperado para fixture '$FixtureRoot': $($output | Out-String)"
    }
}

$fixtures = [System.Collections.Generic.List[string]]::new()

try {
    $validFixture = New-ValidationFixture
    $fixtures.Add($validFixture)
    Invoke-FixtureValidation -FixtureRoot $validFixture -ShouldPass $true

    $brokenLinkFixture = New-ValidationFixture
    $fixtures.Add($brokenLinkFixture)
    Add-Content -LiteralPath (Join-Path $brokenLinkFixture 'README.md') -Value '[Ausente](docs/ausente.md)'
    Invoke-FixtureValidation -FixtureRoot $brokenLinkFixture -ShouldPass $false

    $mutableActionFixture = New-ValidationFixture
    $fixtures.Add($mutableActionFixture)
    Add-Content -LiteralPath (Join-Path $mutableActionFixture '.github/workflows/ci.yml') -Value @'
            - name: Mutable action
                uses: owner/action@v1
            - uses: docker://example/image:latest
'@
    Invoke-FixtureValidation -FixtureRoot $mutableActionFixture -ShouldPass $false

    $missingReferenceFixture = New-ValidationFixture
    $fixtures.Add($missingReferenceFixture)
    Add-Content -LiteralPath (Join-Path $missingReferenceFixture 'README.md') -Value '[Referência ausente][missing]'
    Invoke-FixtureValidation -FixtureRoot $missingReferenceFixture -ShouldPass $false

    $invalidPlanStatusFixture = New-ValidationFixture
    $fixtures.Add($invalidPlanStatusFixture)
    (Get-Content -Raw -LiteralPath (Join-Path $invalidPlanStatusFixture 'docs/plan/fixture.md')).Replace(
        '**Status:** proposed',
        '**Status:** unknown'
    ) | Set-Content -LiteralPath (Join-Path $invalidPlanStatusFixture 'docs/plan/fixture.md')
    Invoke-FixtureValidation -FixtureRoot $invalidPlanStatusFixture -ShouldPass $false

    $missingStepStatusFixture = New-ValidationFixture
    $fixtures.Add($missingStepStatusFixture)
    (Get-Content -Raw -LiteralPath (Join-Path $missingStepStatusFixture 'docs/plan/fixture.md')).Replace(
        '**Status:** not-started',
        '**Situação:** not-started'
    ) | Set-Content -LiteralPath (Join-Path $missingStepStatusFixture 'docs/plan/fixture.md')
    Invoke-FixtureValidation -FixtureRoot $missingStepStatusFixture -ShouldPass $false

    $invalidSecondStepFixture = New-ValidationFixture
    $fixtures.Add($invalidSecondStepFixture)
    Add-Content -LiteralPath (Join-Path $invalidSecondStepFixture 'docs/plan/fixture.md') -Value @'

### P-02: Segunda etapa inválida

**Status:** unknown
'@
    Invoke-FixtureValidation -FixtureRoot $invalidSecondStepFixture -ShouldPass $false
}
finally {
    foreach ($fixture in $fixtures) {
        Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Output 'Testes do validador concluídos: 7 cenários aprovados.'
exit 0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$validator = Join-Path $PSScriptRoot 'validate-repository.ps1'
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

function New-ValidationFixture {
    $fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) "basaltnotes-validation-$([Guid]::NewGuid())"

    foreach ($requiredPath in $requiredPaths) {
        $absolutePath = Join-Path $fixtureRoot $requiredPath
        $directory = Split-Path -Parent $absolutePath
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        Set-Content -LiteralPath $absolutePath -Value "# $requiredPath"
    }

    Set-Content -LiteralPath (Join-Path $fixtureRoot 'asset.png') -Value 'fixture'
    Set-Content -LiteralPath (Join-Path $fixtureRoot 'README.md') -Value @'
# Fixture

[Documento](docs/backend.md "Título")
![Imagem](asset.png)
[Referência][backend]

[backend]: docs/backend.md
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
}
finally {
    foreach ($fixture in $fixtures) {
        Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Output 'Testes do validador concluídos: 4 cenários aprovados.'
exit 0
<#
.SYNOPSIS
Valida SBOM e proveniencia de uma imagem construida pelo BuildKit.
.DESCRIPTION
Le da entrada padrao a resposta da API de atestacoes do Docker Engine e exige
SBOM SPDX, proveniencia SLSA v1 em mode=max e subjects com digest SHA-256.
Nao imprime o conteudo das atestacoes nem acessa Docker, rede ou arquivos.
.PARAMETER RemainingArguments
Aceita somente --help.
.EXAMPLE
cat attestations.json | pwsh -NoProfile -File tools/scripts/assert-buildkit-attestations.ps1
.INPUTS
JSON da API Docker Engine 1.55+ com statement=1, pela entrada padrao.
.OUTPUTS
Resumo da verificacao ou codigo nao zero em caso de falha.
.NOTES
Requer PowerShell 7.4+. O Docker Engine 29.9.3 fornece a API utilizada no CI.
.LINK
../../specs/011-padronizacao-cicd/quickstart.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; exit 0 }
if ($RemainingArguments.Count -gt 0) { throw 'Argumento desconhecido. Use --help.' }

$records = @([Console]::In.ReadToEnd() | ConvertFrom-Json)
$subjectDigests = @{}
foreach ($type in @('https://spdx.dev/Document', 'https://slsa.dev/provenance/v1')) {
    $found = @($records | Where-Object { $_.PredicateType -ceq $type })
    if ($found.Count -ne 1 -or $found[0].Statement.predicateType -cne $type) {
        throw "Atestacao ausente ou invalida: $type"
    }
    $subjectDigests[$type] = @($found[0].Statement.subject |
        Where-Object { $_.digest.sha256 -cmatch '^[a-f0-9]{64}$' } |
        ForEach-Object { $_.digest.sha256 })
    if ($subjectDigests[$type].Count -eq 0) { throw "Subject com digest invalido: $type" }
}
if (@($subjectDigests['https://spdx.dev/Document'] | Where-Object { $_ -cin $subjectDigests['https://slsa.dev/provenance/v1'] }).Count -eq 0) {
    throw 'SBOM e proveniencia nao identificam a mesma imagem.'
}
$sbom = @($records | Where-Object { $_.PredicateType -ceq 'https://spdx.dev/Document' })[0].Statement
if ($sbom.predicate.SPDXID -cne 'SPDXRef-DOCUMENT' -or $sbom.predicate.spdxVersion -cnotmatch '^SPDX-[0-9]+\.[0-9]+$') {
    throw 'SBOM nao contem um documento SPDX valido.'
}
$provenance = @($records | Where-Object { $_.PredicateType -ceq 'https://slsa.dev/provenance/v1' })[0].Statement
if (-not $provenance.predicate.buildDefinition.internalParameters.buildConfig.llbDefinition) {
    throw 'Proveniencia nao contem os detalhes de mode=max.'
}
Write-Output 'SBOM e proveniencia mode=max presentes na imagem carregada.'

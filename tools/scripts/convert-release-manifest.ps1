<#
.SYNOPSIS
Converte manifesto Nodra validado em referencias images.env por digest.
.DESCRIPTION
Valida estrutura e as cinco imagens do Nodra. Nao verifica atestacao remota,
nao autentica, nao faz pull e nao implanta; consumir somente apos o guard oficial.
.PARAMETER ManifestPath
Arquivo release-manifest.json previamente validado pelo guard.
.PARAMETER OutputPath
Arquivo novo chamado images.env; vazio escreve somente referencias na saida.
.PARAMETER RemainingArguments
Aceita somente --help.
.EXAMPLE
./tools/scripts/convert-release-manifest.ps1 -ManifestPath artifacts/release-manifest.json
.EXAMPLE
./tools/scripts/convert-release-manifest.ps1 --help
.INPUTS
Manifesto JSON, sem credenciais.
.OUTPUTS
Cinco linhas ALTIVY_*_IMAGE com referencias OCI imutaveis.
.NOTES
Requer PowerShell 7.4+. Nenhum acesso remoto. Nao sobrescreve arquivos.
.LINK
../../docs/runbooks/deploy-projeto.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $ManifestPath,
    [string] $OutputPath,
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    exit 0
}
if ($RemainingArguments.Count -gt 0 -or [string]::IsNullOrWhiteSpace($ManifestPath)) {
    throw 'Informe -ManifestPath. Use --help.'
}

try {
    $manifestFile = [IO.Path]::GetFullPath($ManifestPath)
    if ([IO.Path]::GetExtension($manifestFile) -cne '.json') { throw 'Formato invalido.' }
    $manifest = Get-Content -Raw -LiteralPath $manifestFile | ConvertFrom-Json -AsHashtable
    Import-Module (Join-Path $PSScriptRoot 'ci/GroundworkRelease.psm1') -Force
    Assert-ReleaseManifest -Manifest $manifest
    if ($manifest.repository -cne 'altivy-it/nodra' -or $manifest.attestation -cne 'github') {
        throw 'Origem ou politica de atestacao invalida.'
    }
    $names = [ordered]@{
        'nodra-api' = 'ALTIVY_API_IMAGE'
        'nodra-bff' = 'ALTIVY_BFF_IMAGE'
        'nodra-worker' = 'ALTIVY_WORKER_IMAGE'
        'nodra-web' = 'ALTIVY_WEB_IMAGE'
        'nodra-migrations' = 'ALTIVY_MIGRATIONS_IMAGE'
    }
    $containers = @($manifest.artifacts | Where-Object { $_.kind -ceq 'container' })
    if ($containers.Count -ne $names.Count) { throw 'Quantidade de imagens invalida.' }
    $lines = foreach ($name in $names.Keys) {
        $entries = @($containers | Where-Object { $_.name -ceq $name })
        if ($entries.Count -ne 1) { throw 'Imagem ausente ou duplicada.' }
        $entry = $entries[0]
        if ($entry.reference -cne "ghcr.io/altivy-it/$name" -or $entry.digest -cnotmatch '^sha256:[a-f0-9]{64}$') {
            throw 'Referencia nao permitida.'
        }
        '{0}={1}@{2}' -f $names[$name], $entry.reference, $entry.digest
    }
    $content = ($lines -join "`n") + "`n"
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Write-Output $content.TrimEnd()
    }
    else {
        $destination = [IO.Path]::GetFullPath($OutputPath)
        if ([IO.Path]::GetFileName($destination) -cne 'images.env') { throw 'Destino nao permitido.' }
        $stream = [IO.File]::Open($destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try {
            $bytes = [Text.UTF8Encoding]::new($false).GetBytes($content)
            $stream.Write($bytes, 0, $bytes.Length)
        }
        finally { $stream.Dispose() }
    }
}
catch {
    [Console]::Error.WriteLine('Manifesto ou destino invalido; nenhum deploy foi executado. Use --help.')
    exit 1
}
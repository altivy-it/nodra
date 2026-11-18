<#
.SYNOPSIS
Emite ou valida certificados para Production local sem sobrescrever arquivos.
.DESCRIPTION
Usa OpenSSL em container e uma CA mkcert ja confiavel no host. Gera TLS localhost,
TLS interno api e PFX dedicado. Nunca cria CA nem instala confianca automaticamente.
.PARAMETER EnvironmentFile
Arquivo externo aprovado com ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD.
.PARAMETER CertificateDirectory
Diretorio externo. Padrao Windows: APPDATA/Microsoft/UserSecrets/Altivy.Notes/certs-prd.
.PARAMETER AuthorityDirectory
CA externa contendo rootCA.pem e rootCA-key.pem. Necessaria apenas para emissao.
.PARAMETER ValidateOnly
Valida cadeia, SAN, validade, chave privada e senha do PFX sem alterar arquivos.
.PARAMETER RemainingArguments
Aceita literalmente --help.
.EXAMPLE
pwsh -NoProfile -File tools/scripts/new-local-production-certificates.ps1 -AuthorityDirectory "$env:LOCALAPPDATA/mkcert"
.EXAMPLE
pwsh -NoProfile -File tools/scripts/new-local-production-certificates.ps1 -EnvironmentFile /secure/altivy.env -CertificateDirectory /secure/certs -ValidateOnly
.INPUTS
Arquivo externo de ambiente e CA externa aprovada pelo operador.
.OUTPUTS
Somente confirmacao sanitizada; nao exibe chaves, senhas ou certificado PFX.
.NOTES
Requer PowerShell 7.4+, Docker Linux e CA mkcert previamente instalada/confiavel.
OpenSSL executa em lzocateli/dotnet-sdk:10.0.400-noble. Nao emite TLS publico.
.LINK
../../deploy/self-hosted/README.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $EnvironmentFile,
    [string] $CertificateDirectory,
    [string] $AuthorityDirectory,
    [switch] $ValidateOnly,
    [Parameter(ValueFromRemainingArguments)][string[]] $RemainingArguments
)
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; return }
if ($RemainingArguments.Count -gt 0) { throw 'Argumento desconhecido. Use --help.' }
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot)
if ($IsWindows) {
    if (-not $EnvironmentFile) { $EnvironmentFile = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/.env' }
    if (-not $CertificateDirectory) { $CertificateDirectory = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/certs-prd' }
}
if (-not $EnvironmentFile -or -not $CertificateDirectory) { throw 'Informe paths externos. Use --help.' }
foreach ($path in @($EnvironmentFile,$CertificateDirectory,$AuthorityDirectory) | Where-Object { $_ }) {
    if ([IO.Path]::GetFullPath($path).StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Secrets e certificados devem ficar fora do repositorio. Use --help.'
    }
}
if (-not (Test-Path -LiteralPath $EnvironmentFile -PathType Leaf)) { throw 'Arquivo externo ausente. Use --help.' }
$files = @('tls.crt','tls.key','rootCA.pem','api.crt','api.key','altivy-data-protection.pfx')
if ($ValidateOnly) {
    foreach ($file in $files) {
        if (-not (Test-Path -LiteralPath (Join-Path $CertificateDirectory $file) -PathType Leaf)) {
            throw "Arquivo $file ausente. Use --help."
        }
    }
} else {
    foreach ($file in $files) {
        if (Test-Path -LiteralPath (Join-Path $CertificateDirectory $file)) {
            throw 'Destino ja possui certificados; nenhuma sobrescrita permitida. Use -ValidateOnly ou um diretorio novo.'
        }
    }
    if (-not $AuthorityDirectory) { throw 'Informe -AuthorityDirectory de uma CA confiavel existente. Use --help.' }
    foreach ($file in @('rootCA.pem','rootCA-key.pem')) {
        if (-not (Test-Path -LiteralPath (Join-Path $AuthorityDirectory $file) -PathType Leaf)) {
            throw 'CA externa incompleta. Use --help.'
        }
    }
    New-Item -ItemType Directory -Force -Path $CertificateDirectory | Out-Null
    if ($IsWindows) {
        $owner = [Security.Principal.WindowsIdentity]::GetCurrent().Name
        & icacls $CertificateDirectory /inheritance:r /grant:r "${owner}:(OI)(CI)F" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao restringir ACL de certificados.' }
    }
}
$certificatePath = [IO.Path]::GetFullPath($CertificateDirectory)
$arguments = @('run','--rm','--network','none','--read-only','--tmpfs','/tmp:rw,noexec,nosuid,size=16m','--user','0:0','--env-file',[IO.Path]::GetFullPath($EnvironmentFile))
$mode = if ($ValidateOnly) { 'ro' } else { 'rw' }
$arguments += @('--volume',"${certificatePath}:/certs:$mode")
$generate = ''
if (-not $ValidateOnly) {
    $authorityPath = [IO.Path]::GetFullPath($AuthorityDirectory)
    $arguments += @('--volume',"${authorityPath}:/ca:ro")
    $generate = @'
umask 077
mkdir -m 0700 /tmp/generated
trap 'rm -rf /tmp/generated /tmp/tls.csr /tmp/api.csr /tmp/certificate-public.pem /tmp/key-public.pem /tmp/pfx-certificate-public.pem /tmp/pfx-key-public.pem' EXIT
for file in tls.crt tls.key rootCA.pem api.crt api.key altivy-data-protection.pfx; do
    test ! -e "/certs/$file"
done
openssl req -new -newkey rsa:3072 -nodes -keyout /tmp/generated/tls.key -out /tmp/tls.csr -subj '/CN=localhost' -addext 'subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1'
openssl x509 -req -in /tmp/tls.csr -CA /ca/rootCA.pem -CAkey /ca/rootCA-key.pem -set_serial "0x$(openssl rand -hex 16)" -days 825 -sha256 -copy_extensions copy -out /tmp/generated/tls.crt
openssl req -new -newkey rsa:3072 -nodes -keyout /tmp/generated/api.key -out /tmp/api.csr -subj '/CN=api' -addext 'subjectAltName=DNS:api,DNS:localhost,IP:127.0.0.1,IP:::1'
openssl x509 -req -in /tmp/api.csr -CA /ca/rootCA.pem -CAkey /ca/rootCA-key.pem -set_serial "0x$(openssl rand -hex 16)" -days 825 -sha256 -copy_extensions copy -out /tmp/generated/api.crt
cp /ca/rootCA.pem /tmp/generated/rootCA.pem
openssl req -x509 -newkey rsa:3072 -nodes -keyout /tmp/generated/protection.key -out /tmp/generated/protection.crt -days 730 -sha256 -subj '/CN=Nodra Data Protection'
openssl pkcs12 -export -inkey /tmp/generated/protection.key -in /tmp/generated/protection.crt -out /tmp/generated/altivy-data-protection.pfx -passout env:ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD -keypbe AES-256-CBC -certpbe AES-256-CBC -macalg SHA256 -iter 100000
chmod 0444 /tmp/generated/*.crt /tmp/generated/rootCA.pem
chown 101:101 /tmp/generated/tls.key
chown 1654:1654 /tmp/generated/api.key /tmp/generated/altivy-data-protection.pfx
chmod 0400 /tmp/generated/*.key /tmp/generated/altivy-data-protection.pfx
for file in tls.crt tls.key rootCA.pem api.crt api.key altivy-data-protection.pfx; do
    mv -n "/tmp/generated/$file" "/certs/$file"
    test ! -e "/tmp/generated/$file"
done
chmod 0755 /certs
'@
}
$validate = @'
: "${ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD:?Senha PFX ausente}"
openssl verify -CAfile /certs/rootCA.pem -verify_hostname localhost /certs/tls.crt
openssl verify -CAfile /certs/rootCA.pem -verify_hostname api /certs/api.crt
for certificate in tls api; do
  openssl x509 -in "/certs/$certificate.crt" -checkend 86400 -noout
  openssl x509 -in "/certs/$certificate.crt" -pubkey -noout > /tmp/certificate-public.pem
  openssl pkey -in "/certs/$certificate.key" -pubout > /tmp/key-public.pem
  cmp /tmp/certificate-public.pem /tmp/key-public.pem
 done
openssl pkcs12 -in /certs/altivy-data-protection.pfx -passin env:ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD -noout
openssl pkcs12 -in /certs/altivy-data-protection.pfx -passin env:ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD -clcerts -nokeys | openssl x509 -checkend 86400 -noout
openssl pkcs12 -in /certs/altivy-data-protection.pfx -passin env:ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD -clcerts -nokeys | openssl x509 -pubkey -noout > /tmp/pfx-certificate-public.pem
openssl pkcs12 -in /certs/altivy-data-protection.pfx -passin env:ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD -nocerts -nodes | openssl pkey -pubout > /tmp/pfx-key-public.pem
cmp /tmp/pfx-certificate-public.pem /tmp/pfx-key-public.pem
'@
$script = ': "${ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD:?Senha PFX ausente}"' + "`n" + $generate + "`n" + $validate
$arguments += @('--entrypoint','/bin/bash','lzocateli/dotnet-sdk:10.0.400-noble','-ec',($script -replace "`r`n","`n"))
$localPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $output = & docker @arguments 2>&1
    $exitCode = $LASTEXITCODE
} finally { $ErrorActionPreference = $localPreference; $output = $null }
if ($exitCode -ne 0) { throw 'Emissao/validacao TLS falhou; confira CA, paths e senha externamente. Nenhum arquivo existente foi sobrescrito. Use --help.' }
Write-Host 'Certificados locais validados: cadeia, SAN, validade, correspondencia de chaves e PFX.'

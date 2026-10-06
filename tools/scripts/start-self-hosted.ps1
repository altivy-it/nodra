<#
.SYNOPSIS
Valida, provisiona e inicia Altivy Notes em Production por Docker Compose.
.DESCRIPTION
Consome arquivos externos via Compose, resolve aliases em memoria, valida
certificados, faz backup coordenado, provisiona roles por stdin e aplica migrations.
Nao grava secrets, nao remove dados e nao depende de um profile PowerShell.
.PARAMETER EnvironmentFile
Arquivo externo AltivyNotes. No Windows usa o caminho aprovado em APPDATA.
.PARAMETER IdentityEnvironmentFile
Arquivo externo opcional com credenciais administrativas centralizadas.
.PARAMETER CertificateDirectory
Diretorio externo com TLS, CA, certificado API e PFX Data Protection.
.PARAMETER DataRoot
Diretorio persistente externo. Padrao: LOCALAPPDATA/AltivyNotes/Production no Windows.
.PARAMETER PublicOrigin
Origem HTTPS opcional, sobrepondo a configuracao externa.
.PARAMETER ProjectName
Nome Compose. Padrao: altivy-notes.
.PARAMETER SyncOidc
Sincroniza callbacks por script pertencente a architecture-knowledge-base.
.PARAMETER IdentityScript
Caminho do sincronizador compartilhado; pode ser fornecido em qualquer host.
.PARAMETER ReleaseImageManifest
images.env e images.env.sha256 de uma release oficial, selecionados por digest.
.PARAMETER ValidateOnly
Valida sem iniciar/parar servicos, aplicar SQL ou alterar Keycloak.
.PARAMETER NoBuild
Reutiliza imagens locais ja construidas.
.PARAMETER ValidationVaultId
Fixture opcional lab-* para ensaio de escrita. Nao cria nem altera grants Keycloak.
.PARAMETER RecreateConsumers
Recria web, BFF, API e worker sem alterar a stack compartilhada.
.PARAMETER RemainingArguments
Aceita somente --help.
.EXAMPLE
./tools/scripts/start-self-hosted.ps1 -SyncOidc -IdentityEnvironmentFile "$env:APPDATA/Microsoft/UserSecrets/Altivy.Identity/.env"
.EXAMPLE
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 -EnvironmentFile /secure/altivy.env -CertificateDirectory /secure/certs -DataRoot /srv/altivy -ValidateOnly
.EXAMPLE
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 -EnvironmentFile /secure/altivy.env -IdentityEnvironmentFile /secure/identity.env -ReleaseImageManifest /secure/release/images.env -NoBuild -SyncOidc
.INPUTS
Secrets apenas em arquivos externos ou variaveis injetadas com seguranca.
.OUTPUTS
Etapas sanitizadas e URL da aplicacao. Nunca mostra configuracao resolvida.
.NOTES
Requer PowerShell 7.4+, Docker Engine/Desktop e Compose v2. Runtimes rodam em containers.
Keycloak, SMTP e certificados confiaveis precisam estar configurados pelo operador.
.LINK
../../deploy/self-hosted/README.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $EnvironmentFile,
    [string] $IdentityEnvironmentFile,
    [string] $CertificateDirectory,
    [string] $DataRoot,
    [string] $PublicOrigin,
    [ValidatePattern('^[a-z0-9][a-z0-9_-]+$')][string] $ProjectName = 'altivy-notes',
    [switch] $SyncOidc,
    [string] $IdentityScript,
    [string] $ReleaseImageManifest,
    [switch] $ValidateOnly,
    [switch] $NoBuild,
    [ValidatePattern('^lab-[a-z0-9][a-z0-9-]{0,58}$')][string] $ValidationVaultId,
    [switch] $RecreateConsumers,
    [Parameter(ValueFromRemainingArguments)][string[]] $RemainingArguments
)
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; return }
if ($RemainingArguments.Count -gt 0) { throw 'Argumento desconhecido. Use --help.' }
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot)
if (-not $EnvironmentFile) {
    if (-not $IsWindows) { throw 'Informe -EnvironmentFile externo neste host. Use --help.' }
    $EnvironmentFile = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/.env'
}
$environmentFiles = @()
if ($IdentityEnvironmentFile) { $environmentFiles += $IdentityEnvironmentFile }
$environmentFiles += $EnvironmentFile
foreach ($file in $environmentFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw 'Arquivo externo ausente. Use --help.' }
    if ([IO.Path]::GetFullPath($file).StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Secrets nao podem ficar dentro do repositorio. Use --help.'
    }
}

function Invoke-Docker {
    param([string[]] $Arguments, [switch] $Capture, [switch] $Quiet, [string] $InputText)
    $localPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $result = if ($PSBoundParameters.ContainsKey('InputText')) {
            $InputText | & docker @Arguments 2>&1
        } else { & docker @Arguments 2>&1 }
        $nativeExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $localPreference }
    if ($nativeExit -ne 0) { throw "Etapa Docker falhou (exit $nativeExit); configuracao/saida sensivel nao exibida. Use --help." }
    if ($Capture) { return ($result | Out-String).Trim() }
    if (-not $Quiet) { foreach ($entry in $result) { Write-Host $entry } }
}

function Set-ProcessSetting {
    param([string] $Name, [AllowNull()][object] $Value)
    if (-not $savedEnvironment.ContainsKey($Name)) {
        $savedEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process')
    }
    if ($null -eq $Value) {
        Remove-Item -LiteralPath "Env:$Name" -ErrorAction SilentlyContinue
    } else {
        [Environment]::SetEnvironmentVariable($Name, [string]$Value, 'Process')
    }
}

function Get-RoleConnection {
    param([string] $Text, [string] $Role)
    if ([string]::IsNullOrWhiteSpace($Text)) { throw "Connection string da role $Role ausente. Use --help." }
    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.set_ConnectionString($Text)
    if (-not $builder.ContainsKey('Username') -or $builder.get_Item('Username') -ne $Role -or
        -not $builder.ContainsKey('Password') -or [string]::IsNullOrWhiteSpace($builder.get_Item('Password'))) {
        throw "Connection string deve usar $Role com senha nao vazia. Use --help."
    }
    $builder.set_Item('Host', 'postgres')
    $builder.set_Item('Port', 5432)
    $builder.set_Item('Database', 'altivy')
    return $builder
}

$savedEnvironment = @{}
Push-Location $repository
try {
    $fileArguments = @()
    foreach ($file in $environmentFiles) { $fileArguments += @('--env-file', [IO.Path]::GetFullPath($file)) }
    $sourcePath = Join-Path $repository 'deploy/self-hosted/configuration.yaml'
    $template = Invoke-Docker -Arguments @('compose', '-f', $sourcePath, 'config', '--no-interpolate', '--format', 'json') -Capture | ConvertFrom-Json
    foreach ($property in $template.services.source.environment.PSObject.Properties) {
        foreach ($match in [regex]::Matches([string]$property.Value, '\$\{([A-Z0-9_]+)')) {
            Set-ProcessSetting $match.Groups[1].Value $null
        }
    }
    $resolved = Invoke-Docker -Arguments (@('compose') + $fileArguments + @('-f', $sourcePath, 'config', '--format', 'json')) -Capture | ConvertFrom-Json
    $source = $resolved.services.source.environment
    foreach ($required in @('RUNTIME','MIGRATOR','BFF_SECRET','PFX_PASSWORD','SMTP_HOST','SMTP_USERNAME','SMTP_PASSWORD','SMTP_FROM','SMTP_TO')) {
        if ([string]::IsNullOrWhiteSpace($source.$required)) { throw "Configuracao $required ausente. Use --help." }
    }
    if (-not $PublicOrigin) { $PublicOrigin = $source.ORIGIN }
    $origin = [Uri]$PublicOrigin
    $authority = [Uri]$source.AUTHORITY
    if (-not $origin.IsAbsoluteUri -or $origin.Scheme -ne 'https' -or $origin.AbsolutePath.TrimEnd('/') -ne '/notes' -or
        $origin.Query -or $origin.Fragment -or -not $authority.IsAbsoluteUri -or $authority.Scheme -ne 'https') {
        throw 'Origem e issuer devem ser HTTPS validos. Use --help.'
    }
    if (-not $DataRoot) { $DataRoot = $source.DATA_ROOT }
    if (-not $DataRoot) {
        $baseData = if ($IsWindows) { $env:LOCALAPPDATA } else { Join-Path $HOME '.local/share' }
        $DataRoot = Join-Path $baseData 'AltivyNotes/Production'
    }
    if (-not $CertificateDirectory) { $CertificateDirectory = $source.CONFIG_ROOT }
    if (-not $CertificateDirectory -and $IsWindows) {
        $CertificateDirectory = Join-Path $env:APPDATA 'Microsoft/UserSecrets/Altivy.Notes/certs-prd'
    }
    if (-not $CertificateDirectory) { throw 'Informe -CertificateDirectory externo. Use --help.' }
    foreach ($path in @($DataRoot,$CertificateDirectory)) {
        if ([IO.Path]::GetFullPath($path).StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Dados e certificados devem ficar fora do repositorio.'
        }
    }
    $certificates = @{
        ALTIVY_TLS_CA_PATH='rootCA.pem'; ALTIVY_API_TLS_CERTIFICATE_PATH='api.crt'
        ALTIVY_API_TLS_PRIVATE_KEY_PATH='api.key'
    }
    foreach ($name in $certificates.Keys) {
        $path = Join-Path $CertificateDirectory $certificates[$name]
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Certificado $($certificates[$name]) ausente. Use --help." }
        Set-ProcessSetting $name ([IO.Path]::GetFullPath($path))
    }
    if (-not (Test-Path (Join-Path $CertificateDirectory 'altivy-data-protection.pfx') -PathType Leaf)) { throw 'PFX Data Protection ausente. Use --help.' }
    $runtime = Get-RoleConnection $source.RUNTIME 'altivy_runtime'
    $migrator = Get-RoleConnection $source.MIGRATOR 'altivy_migrator'
    $settings = @{
        ALTIVY_DATA_ROOT=[IO.Path]::GetFullPath($DataRoot); ALTIVY_CONFIG_ROOT=[IO.Path]::GetFullPath($CertificateDirectory)
        ALTIVY_PUBLIC_HOST=$origin.Host; ALTIVY_PUBLIC_ORIGIN=$origin.GetLeftPart([UriPartial]::Authority)
        ALTIVY_KEYCLOAK_AUTHORITY=$authority.AbsoluteUri.TrimEnd('/')
        ALTIVY_WEB_IMAGE=$source.WEB_IMAGE; ALTIVY_BFF_IMAGE=$source.BFF_IMAGE
        ALTIVY_API_IMAGE=$source.API_IMAGE; ALTIVY_WORKER_IMAGE=$source.WORKER_IMAGE
        ALTIVY_MIGRATIONS_IMAGE=$source.MIGRATIONS_IMAGE
        ALTIVY_BACKEND_VERSION=$source.BACKEND_VERSION; ALTIVY_WEB_VERSION=$source.WEB_VERSION
        ALTIVY_IMAGE_PULL_POLICY=$source.IMAGE_PULL_POLICY
        ALTIVY_BFF_CLIENT_SECRET=$source.BFF_SECRET; ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD=$source.PFX_PASSWORD
        ALTIVY_POSTGRES_RUNTIME_CONNECTION_STRING=$runtime.get_ConnectionString()
        ALTIVY_POSTGRES_MIGRATOR_CONNECTION_STRING=$migrator.get_ConnectionString()
        ALTIVY_SMTP_HOST=$source.SMTP_HOST; ALTIVY_SMTP_PORT=$source.SMTP_PORT
        ALTIVY_SMTP_USERNAME=$source.SMTP_USERNAME; ALTIVY_SMTP_PASSWORD=$source.SMTP_PASSWORD
        ALTIVY_SMTP_FROM=$source.SMTP_FROM; ALTIVY_SMTP_TO=$source.SMTP_TO
        ALTIVY_PROVISIONER_CLIENT_SECRET=$source.PROVISIONER_SECRET
    }
    if ($ReleaseImageManifest) {
        if (-not $NoBuild) { throw 'ReleaseImageManifest exige -NoBuild; build local foi bloqueado.' }
        $manifestPath = [IO.Path]::GetFullPath($ReleaseImageManifest)
        if ([IO.Path]::GetFileName($manifestPath) -cne 'images.env' -or
            -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            throw 'Informe o images.env original da release. Use --help.'
        }
        $checksumPath = "$manifestPath.sha256"
        if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
            throw 'images.env.sha256 ausente; release nao verificada. Use --help.'
        }
        $manifestBytes = [IO.File]::ReadAllBytes($manifestPath)
        $manifestHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($manifestBytes)).ToLowerInvariant()
        $checksumText = [IO.File]::ReadAllText($checksumPath)
        $checksumLines = @($checksumText -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($checksumLines.Count -ne 1 -or $checksumLines[0] -cne ("{0}  images.env" -f $manifestHash)) {
            throw 'Checksum de images.env invalido; release rejeitada.'
        }
        try {
            $manifestText = [Text.UTF8Encoding]::new($false,$true).GetString($manifestBytes)
            $releaseImages = ConvertFrom-StringData -StringData $manifestText
        } catch { throw 'Formato de images.env invalido; release rejeitada.' }
        $expectedImages = @{
            ALTIVY_WEB_IMAGE='altivy-web'; ALTIVY_API_IMAGE='altivy-api'
            ALTIVY_BFF_IMAGE='altivy-bff'; ALTIVY_WORKER_IMAGE='altivy-worker'
            ALTIVY_MIGRATIONS_IMAGE='altivynotes-migrations'
        }
        $actualKeys = @($releaseImages.Keys | Sort-Object)
        $expectedKeys = @($expectedImages.Keys | Sort-Object)
        if ($actualKeys.Count -ne $expectedKeys.Count -or ($actualKeys -join '|') -cne ($expectedKeys -join '|')) {
            throw 'images.env deve conter exatamente as cinco imagens Altivy da release.'
        }
        foreach ($key in $expectedImages.Keys) {
            $pattern = '^ghcr\.io/lzocateli/' + [regex]::Escape($expectedImages[$key]) + '@sha256:[a-f0-9]{64}$'
            if ([string]$releaseImages[$key] -cnotmatch $pattern) { throw "Referencia imutavel invalida para $key." }
            $settings[$key] = [string]$releaseImages[$key]
        }
        $settings.ALTIVY_IMAGE_PULL_POLICY = 'always'
        $manifestBytes = $null; $manifestText = $null; $checksumText = $null
    }
    foreach ($name in $settings.Keys) { Set-ProcessSetting $name ([string]$settings[$name]) }
    $gatewayIp = Invoke-Docker -Arguments @('inspect','--format','{{with index .NetworkSettings.Networks "identity-global"}}{{.IPAddress}}{{end}}','altivy-identity-gateway-1') -Capture
    if (-not [Net.IPAddress]::TryParse($gatewayIp, [ref]([Net.IPAddress]$null))) { throw 'Gateway compartilhado sem IP valido na identity-global.' }
    Set-ProcessSetting 'IDENTITY_GATEWAY_IP' $gatewayIp
    $compose = @('compose','--progress','plain') + $fileArguments + @('-p',$ProjectName,'-f','deploy/self-hosted/compose.yaml')
    Invoke-Docker -Arguments ($compose + @('config','--quiet')) -Quiet
    Write-Host 'Preflight Production aprovado; secrets mantidos somente em memoria.'
    if ($ValidateOnly) { return }
    if ($ReleaseImageManifest) {
        Invoke-Docker -Arguments ($compose + @('pull','web','bff','api','worker'))
    }
    foreach ($folder in @('vaults','bff-keys','backups')) {
        New-Item -ItemType Directory -Force -Path (Join-Path $DataRoot $folder) | Out-Null
    }
    if ($IsWindows) {
        $owner = [Security.Principal.WindowsIdentity]::GetCurrent().Name
        & icacls $DataRoot /inheritance:r /grant:r "${owner}:(OI)(CI)F" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'ACL de dados falhou.' }
    }
    Invoke-Docker -Arguments @('run','--rm','--network','none','--user','0:0','--volume',"${DataRoot}:/data",'--entrypoint','/bin/sh','lzocateli/dotnet-aspnet:10.0.11-noble','-ec','chown 1654:1654 /data/vaults /data/bff-keys; chmod 0700 /data/vaults /data/bff-keys /data/backups') -Quiet
    $databaseContainer = Invoke-Docker -Arguments @('ps','--filter','label=com.docker.compose.project=altivy-identity','--filter','label=com.docker.compose.service=postgres','--format','{{.ID}}') -Capture
    if ([string]::IsNullOrWhiteSpace($databaseContainer) -or $databaseContainer -match "`r?`n") { throw 'PostgreSQL compartilhado altivy-identity nao esta em execucao de forma univoca.' }
    $databaseImageId = Invoke-Docker -Arguments @('inspect','--format','{{.Image}}',$databaseContainer) -Capture
    if ($databaseImageId -notmatch '^sha256:[a-f0-9]{64}$') { throw 'Imagem PostgreSQL nao resolvida por ID.' }
    $backupId = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmss') + '-' + [Guid]::NewGuid().ToString('N')
    $metadataPartialPath = Join-Path $DataRoot "backups/$backupId.metadata.json.partial"
    $metadataPartialCreated = $false
    $writersStopped = $false
    try {
        $runningServices = Invoke-Docker -Arguments @('ps','--filter',"label=com.docker.compose.project=$ProjectName",'--format','{{.Label "com.docker.compose.service"}}') -Capture
        $runningWriters = @($runningServices -split "`r?`n" | Where-Object { $_ -in @('bff','api','worker') })
        if ($runningWriters.Count -gt 0) {
            Invoke-Docker -Arguments ($compose + @('stop','--timeout','30','bff','api','worker')) -Quiet
            $writersStopped = $true
        }
        Invoke-Docker -Arguments @('exec',$databaseContainer,'sh','-c','umask 077; exec pg_dump -U "$POSTGRES_USER" -d altivy --format=custom --file=/tmp/pre-migration.dump') -Quiet
        Invoke-Docker -Arguments @('cp',"${databaseContainer}:/tmp/pre-migration.dump",(Join-Path $DataRoot "backups/$backupId.dump")) -Quiet
        Invoke-Docker -Arguments @('exec',$databaseContainer,'rm','/tmp/pre-migration.dump') -Quiet
        if (Test-Path -LiteralPath $metadataPartialPath) { throw 'Destino de metadata de backup ja existe.' }
        $metadata = [ordered]@{
            backupId = $backupId
            createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
            projectName = $ProjectName
            postgresImageId = $databaseImageId
        } | ConvertTo-Json -Compress
        $metadataBytes = [Text.Encoding]::UTF8.GetBytes($metadata)
        $metadataStream = [IO.File]::Open($metadataPartialPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $metadataPartialCreated = $true
        try { $metadataStream.Write($metadataBytes,0,$metadataBytes.Length) }
        finally { $metadataStream.Dispose() }
        $archiveCommand = "set -eu; umask 077; trap 'rm -f /data/backups/$backupId.tar.gz.partial /data/backups/$backupId.sha256.partial /data/backups/$backupId.files.sha256.partial /data/backups/$backupId.metadata.json.partial' EXIT; cd /data; find vaults bff-keys -type f -print0 | sort -z | xargs -0 -r sha256sum > /data/backups/$backupId.files.sha256.partial; tar -czf /data/backups/$backupId.tar.gz.partial -C /data vaults bff-keys; mv /data/backups/$backupId.tar.gz.partial /data/backups/$backupId.tar.gz; mv /data/backups/$backupId.files.sha256.partial /data/backups/$backupId.files.sha256; mv /data/backups/$backupId.metadata.json.partial /data/backups/$backupId.metadata.json; cd /data/backups; sha256sum $backupId.dump $backupId.tar.gz $backupId.metadata.json > $backupId.sha256.partial; sha256sum --check $backupId.sha256.partial; mv $backupId.sha256.partial $backupId.sha256; cd /data; sha256sum --check backups/$backupId.files.sha256; chmod 0600 backups/$backupId.dump backups/$backupId.tar.gz backups/$backupId.sha256 backups/$backupId.files.sha256 backups/$backupId.metadata.json"
        Invoke-Docker -Arguments @('run','--rm','--network','none','--user','0:0','--volume',"${DataRoot}:/data",'--entrypoint','/bin/sh','lzocateli/dotnet-aspnet:10.0.11-noble','-ec',$archiveCommand) -Quiet
        Write-Host "Backup pre-migration verificado (ID $backupId)."
        $migrationImage = [string]$settings.ALTIVY_MIGRATIONS_IMAGE
        if (-not $NoBuild) {
            Invoke-Docker -Arguments @('build','--build-arg',"IMAGE_VERSION=$($source.BACKEND_VERSION)",'--tag',$migrationImage,'--file','deploy/production/migrations.Dockerfile','.')
        } elseif ($ReleaseImageManifest) {
            Invoke-Docker -Arguments @('pull',$migrationImage)
        }
        Set-ProcessSetting 'ALTIVY_DESIGNTIME_CONNECTION_STRING' $migrator.get_ConnectionString()
        try {
            Invoke-Docker -Arguments @('run','--rm','--network','identity-client','--read-only','--tmpfs','/tmp:rw,noexec,nosuid,size=16m','--env','ALTIVY_DESIGNTIME_CONNECTION_STRING',$migrationImage)
        } finally {
            Set-ProcessSetting 'ALTIVY_DESIGNTIME_CONNECTION_STRING' $null
        }
        Write-Host 'Migrations validadas no PostgreSQL compartilhado.'
        if ($ValidationVaultId) {
            $fixtureDirectory = Join-Path $DataRoot "vaults/$ValidationVaultId"
            if ((Test-Path -LiteralPath $fixtureDirectory) -and
                (Get-Item -LiteralPath $fixtureDirectory).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw 'Diretorio de fixture nao pode ser link simbolico.'
            }
            $fixtureSql = @"
BEGIN;
INSERT INTO altivy.vaults ("Id", "Name", state)
VALUES ('$ValidationVaultId', 'Validacao self-hosted', 2) ON CONFLICT ("Id") DO NOTHING;
DO `$fixture`$ BEGIN
IF NOT EXISTS (SELECT 1 FROM altivy.vaults WHERE "Id" = '$ValidationVaultId' AND "Name" = 'Validacao self-hosted' AND state = 2)
THEN RAISE EXCEPTION 'Fixture colide com vault existente; nenhum dado foi alterado'; END IF;
END `$fixture`$;
COMMIT;
"@
            if (-not (Test-Path -LiteralPath $fixtureDirectory)) {
                New-Item -ItemType Directory -Path $fixtureDirectory | Out-Null
                Invoke-Docker -Arguments @('run','--rm','--network','none','--user','0:0','--volume',"${fixtureDirectory}:/fixture",'--entrypoint','/bin/sh','lzocateli/dotnet-aspnet:10.0.11-noble','-ec','chown 1654:1654 /fixture; chmod 0700 /fixture') -Quiet
            }
            Set-ProcessSetting 'PGPASSWORD' $migrator.get_Item('Password')
            Invoke-Docker -Arguments @('run','--rm','--network','identity-client','--env','PGPASSWORD','--entrypoint','psql','lzocateli/postgresql:18.4-pgvector0.8.6-bookworm','-X','-q','-h','postgres','-U','altivy_migrator','-d','altivy','-v','ON_ERROR_STOP=1') -InputText $fixtureSql -Quiet
            Set-ProcessSetting 'PGPASSWORD' $null
            Write-Host 'Fixture sintetica pronta; autorizacao continua sendo exclusiva do Keycloak.'
        }
        if ($SyncOidc) {
            if (-not $IdentityScript) { $IdentityScript = Join-Path (Split-Path $repository) 'architecture-knowledge-base/platforms/identity/scripts/sync-product-oidc.ps1' }
            if (-not (Test-Path $IdentityScript -PathType Leaf)) { throw 'Informe -IdentityScript da plataforma compartilhada. Use --help.' }
            & $IdentityScript -BaseUrl $authority.GetLeftPart([UriPartial]::Authority) -PublicOrigin $origin.AbsoluteUri.TrimEnd('/')
            if (-not $?) { throw 'Sincronizacao OIDC falhou.' }
            Write-Host 'Callbacks OIDC sincronizados na identidade compartilhada.'
        }
        $startArguments = @('up','-d','--wait','--wait-timeout','240')
        if (-not $NoBuild) { $startArguments += '--build' }
        $startServices = @()
        if ($RecreateConsumers) {
            $startArguments += @('--no-deps','--force-recreate')
            $startServices = @('web','bff','api','worker')
        }
        Invoke-Docker -Arguments ($compose + $startArguments + $startServices)
        $writersStopped = $false
    } finally {
        if ($writersStopped) { Invoke-Docker -Arguments ($compose + @('start','api','worker','bff')) -Quiet }
        if ($metadataPartialCreated -and (Test-Path -LiteralPath $metadataPartialPath)) {
            Remove-Item -LiteralPath $metadataPartialPath -Force
        }
    }
    Invoke-Docker -Arguments @('run','--rm','--network','host','--volume',"${CertificateDirectory}:/certs:ro",'--entrypoint','curl','lzocateli/dotnet-sdk:10.0.400-noble','--cacert','/certs/rootCA.pem','--silent','--show-error','--fail',($origin.AbsoluteUri.TrimEnd('/') + '/bff/session'))
    Write-Host "Production online em $($origin.AbsoluteUri.TrimEnd('/'))."
} finally {
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name,$savedEnvironment[$name],'Process') }
    $source = $null; $settings = $null; $resolved = $null; $runtime = $null; $migrator = $null
    $metadata = $null; $metadataBytes = $null; $metadataStream = $null
    Pop-Location
}
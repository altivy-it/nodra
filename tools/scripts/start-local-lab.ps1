<#
.SYNOPSIS
Sobe o laboratório local completo do Nodra (identidade AKB + Nodra) com Podman.

.DESCRIPTION
Orquestra, sem alterar o repositório architecture-knowledge-base, a stack de
identidade (gateway nginx, PostgreSQL e Keycloak) e a stack Nodra (web, BFF,
API e worker) para testes locais antes do CI/CD. Na primeira execução cria,
fora do repositório e sem sobrescrever arquivos, os secrets aleatórios, uma CA
própria do laboratório, certificados, temas e dados. Depois reconcilia banco,
realm Keycloak, usuário de laboratório, autorizações UMA e fixtures sintéticas,
e delega a subida do Nodra para start-self-hosted.ps1. Reexecuções são
idempotentes e preservam dados. O acesso ocorre somente pela borda nginx em
https://localhost:7443/notes.

.PARAMETER LabRoot
Diretório externo do laboratório. Padrão: ~/.local/share/nodra-lab.

.PARAMETER IdentityRoot
Diretório platforms/identity do AKB. Padrão: repositório irmão
architecture-knowledge-base.

.PARAMETER ComposeProvider
Binário Docker Compose v2+ usado como provider do podman compose. Padrão:
~/.local/lib/nodra-lab/docker-compose.

.PARAMETER Build
Reconstrói as imagens localhost/nodra-*:lab com --format docker.

.PARAMETER RecreateConsumers
Recria web, BFF, API e worker para provar persistência após recriação.

.PARAMETER RunE2E
Executa os testes Playwright reais pelo nginx após subir a stack.

.PARAMETER Stop
Para e remove containers e redes das duas stacks, preservando dados e secrets.

.PARAMETER RemainingArguments
Aceita somente --help.

.EXAMPLE
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 --help
.EXAMPLE
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -Build
.EXAMPLE
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -RunE2E
.EXAMPLE
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -Stop

.NOTES
Requer Linux, PowerShell 7.4+, Podman 5+ rootless com podman.socket e o
provider Docker Compose v2+ (testado com v5.8.0, Apache-2.0). Runtimes, OpenSSL,
psql e Playwright rodam em containers. Secrets ficam em <LabRoot>/secrets com
modo 0600 e nunca são exibidos.

.LINK
../../specs/012-laboratorio-local-podman/quickstart.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $LabRoot = (Join-Path $HOME '.local/share/nodra-lab'),
    [string] $IdentityRoot,
    [string] $ComposeProvider = (Join-Path $HOME '.local/lib/nodra-lab/docker-compose'),
    [switch] $Build,
    [switch] $RecreateConsumers,
    [switch] $RunE2E,
    [switch] $Stop,
    [Parameter(ValueFromRemainingArguments)][string[]] $RemainingArguments
)
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; return }
if ($RemainingArguments.Count -gt 0) { [Console]::Error.WriteLine('Argumento desconhecido. Use --help.'); exit 2 }
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot)
$publicBase = 'https://localhost:7443'
$origin = "$publicBase/notes"
$labVaults = @('lab-0001', 'lab-0002', 'lab-write-e2e', 'lab-denied')
$ownedVaults = @('lab-0001', 'lab-0002', 'lab-write-e2e')
$helperImage = 'docker.io/lzocateli/dotnet-sdk:10.0.400-noble'
$postgresImage = 'docker.io/lzocateli/postgresql:18.4-pgvector0.8.6-bookworm'
$playwrightImage = 'docker.io/lzocateli/playwright-e2e:0.1.0'

function Invoke-Podman {
    param([string[]] $Arguments, [switch] $Capture, [string] $InputText)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $result = if ($PSBoundParameters.ContainsKey('InputText')) { $InputText | & podman @Arguments 2>&1 } else { & podman @Arguments 2>&1 }
        $exitCode = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        $result | ForEach-Object { [Console]::Error.WriteLine([string]$_) }
        throw "podman $($Arguments[0]) falhou (codigo $exitCode)."
    }
    if ($Capture) { return (($result | Where-Object { $_ -isnot [Management.Automation.ErrorRecord] }) -join "`n").Trim() }
    $result | ForEach-Object { Write-Host ([string]$_) }
}

function New-PrivateDirectory {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        [IO.Directory]::CreateDirectory($Path, [IO.UnixFileMode]'UserRead, UserWrite, UserExecute') | Out-Null
    }
    if ((Get-Item -LiteralPath $Path -Force).LinkType) { throw "Diretorio do laboratorio nao pode ser link: $Path" }
}

function New-Secret {
    param([int] $Length = 32)
    $alphabet = [char[]]'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
    return [Security.Cryptography.RandomNumberGenerator]::GetString($alphabet, $Length)
}

function Write-SecretFile {
    param([string] $Path, [Collections.Specialized.OrderedDictionary] $Values)
    $options = [IO.FileStreamOptions]@{
        Mode = [IO.FileMode]::CreateNew; Access = [IO.FileAccess]::Write
        UnixCreateMode = [IO.UnixFileMode]'UserRead, UserWrite'
    }
    $writer = [IO.StreamWriter]::new($Path, [Text.UTF8Encoding]::new($false), $options)
    try { foreach ($key in $Values.Keys) { $writer.Write("$key=$($Values[$key])`n") } }
    finally { $writer.Dispose() }
}

function Read-SecretFile {
    param([string] $Path)
    $values = @{}
    foreach ($line in [IO.File]::ReadAllLines($Path)) {
        if ($line -match '^(?<name>[A-Z0-9_]+)=(?<value>.*)$') { $values[$Matches.name] = $Matches.value }
    }
    return $values
}

function Set-ProcessSetting {
    param([string] $Name, [AllowNull()][string] $Value)
    if (-not $savedEnvironment.ContainsKey($Name)) { $savedEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process') }
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
}

function Invoke-TrustedPwsh {
    # .NET le SSL_CERT_FILE apenas na inicializacao; um processo filho confia na CA do laboratorio sem afetar o Podman.
    param([string[]] $Arguments)
    $previous = [Environment]::GetEnvironmentVariable('SSL_CERT_FILE', 'Process')
    [Environment]::SetEnvironmentVariable('SSL_CERT_FILE', (Join-Path $certificates 'rootCA.pem'), 'Process')
    try { & pwsh -NoProfile @Arguments; $exitCode = $LASTEXITCODE }
    finally { [Environment]::SetEnvironmentVariable('SSL_CERT_FILE', $previous, 'Process') }
    if ($exitCode -ne 0) { throw "Etapa Keycloak falhou (codigo $exitCode)." }
}

# Laboratorio: usuario com senha permanente, e-mail verificado e sem acoes obrigatorias para login automatizado.
$labUserScript = @'
$ErrorActionPreference = 'Stop'
$base = 'https://localhost:7443'
$token = (Invoke-RestMethod -Method Post -Uri "$base/realms/master/protocol/openid-connect/token" -ContentType 'application/x-www-form-urlencoded' -Body @{
    grant_type = 'password'; client_id = 'admin-cli'; username = $env:ALTIVY_KEYCLOAK_ADMIN_USERNAME; password = $env:ALTIVY_KEYCLOAK_ADMIN_PASSWORD }).access_token
$headers = @{ Authorization = "Bearer $token" }
$users = "$base/admin/realms/altivy/users"
$labUser = @(Invoke-RestMethod -Uri "${users}?username=$($env:ALTIVY_E2E_USERNAME)&exact=true" -Headers $headers) | Select-Object -First 1
if (-not $labUser) { throw 'Usuario de laboratorio nao foi criado pelo configure-keycloak.' }
$update = @{ requiredActions = @(); emailVerified = $true } | ConvertTo-Json -Compress
Invoke-RestMethod -Method Put -Uri "$users/$($labUser.id)" -Headers $headers -ContentType 'application/json' -Body $update | Out-Null
$credentials = @(Invoke-RestMethod -Uri "$users/$($labUser.id)/credentials" -Headers $headers | ForEach-Object { $_ } | Where-Object { $_.type -eq 'password' })
if ($credentials.Count -eq 0) {
    $password = @{ type = 'password'; value = $env:ALTIVY_E2E_PASSWORD; temporary = $false } | ConvertTo-Json -Compress
    Invoke-RestMethod -Method Put -Uri "$users/$($labUser.id)/reset-password" -Headers $headers -ContentType 'application/json' -Body $password | Out-Null
}
Write-Output 'Usuario de laboratorio pronto para login automatizado.'
'@

$savedEnvironment = @{}
Push-Location $repository
try {
    if (-not $IsLinux) { throw 'Este laboratorio Podman suporta somente Linux. Use start-self-hosted.ps1 com Docker em outros hosts.' }
    if (-not (Get-Command podman -CommandType Application -ErrorAction SilentlyContinue)) { throw 'Podman indisponivel no PATH. Use --help.' }
    if (-not $IdentityRoot) { $IdentityRoot = Join-Path (Split-Path $repository) 'architecture-knowledge-base/platforms/identity' }
    $identityCompose = Join-Path $IdentityRoot 'compose.yaml'
    if (-not (Test-Path -LiteralPath $identityCompose -PathType Leaf)) { throw 'Compose da identidade AKB nao encontrado; informe -IdentityRoot. Use --help.' }
    $LabRoot = [IO.Path]::GetFullPath($LabRoot)
    if ($LabRoot.StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) { throw 'LabRoot deve ficar fora do repositorio.' }
    if (-not (Test-Path -LiteralPath $ComposeProvider -PathType Leaf)) { throw 'Provider Docker Compose v2+ ausente; consulte o quickstart da Spec 012. Use --help.' }

    $socket = "/run/user/$(& id -u)/podman/podman.sock"
    if (-not (Test-Path -LiteralPath $socket)) {
        & systemctl --user start podman.socket
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $socket)) { throw 'podman.socket indisponivel. Execute: systemctl --user start podman.socket' }
    }
    Set-ProcessSetting 'PODMAN_COMPOSE_PROVIDER' ([IO.Path]::GetFullPath($ComposeProvider))
    Set-ProcessSetting 'PODMAN_COMPOSE_WARNING_LOGS' 'false'
    Set-ProcessSetting 'DOCKER_HOST' "unix://$socket"
    Set-ProcessSetting 'NODRA_LAB_GATEWAY_NGINX_CONF' (Join-Path $repository 'deploy/local-lab/gateway-nginx.podman.conf')

    $secrets = Join-Path $LabRoot 'secrets'
    $identityEnv = Join-Path $secrets 'identity.env'
    $nodraEnv = Join-Path $secrets 'nodra.env'
    $userEnv = Join-Path $secrets 'lab-user.env'
    $certificates = Join-Path $LabRoot 'certs'
    $identityCommand = @('compose', '--progress', 'plain', '--env-file', $identityEnv, '-f', $identityCompose, '-f', (Join-Path $repository 'deploy/local-lab/identity.podman.compose.yaml'))
    $nodraCommand = @('compose', '--progress', 'plain', '--env-file', $nodraEnv, '-p', 'nodra', '-f', (Join-Path $repository 'deploy/self-hosted/compose.yaml'), '-f', (Join-Path $repository 'deploy/self-hosted/compose.podman.yaml'))

    if ($Stop) {
        if (Test-Path -LiteralPath $nodraEnv) {
            # down nao usa os valores; preenche somente as variaveis obrigatorias ausentes para a interpolacao.
            $composeText = [IO.File]::ReadAllText((Join-Path $repository 'deploy/self-hosted/compose.yaml'))
            foreach ($match in [regex]::Matches($composeText, '\$\{([A-Z0-9_]+):\?') | Select-Object -ExpandProperty Groups | Where-Object Name -eq '1' | Select-Object -ExpandProperty Value -Unique) {
                if (-not [Environment]::GetEnvironmentVariable($match, 'Process')) { Set-ProcessSetting $match '/unused' }
            }
            Set-ProcessSetting 'IDENTITY_GATEWAY_IP' '127.0.0.1'
            Invoke-Podman -Arguments ($nodraCommand + @('down'))
        }
        if (Test-Path -LiteralPath $identityEnv) { Invoke-Podman -Arguments ($identityCommand + @('down')) }
        Write-Host "Laboratorio parado; dados e secrets preservados em $LabRoot."
        return
    }

    foreach ($folder in @('', 'secrets', 'ca', 'themes', 'postgres', 'data')) { New-PrivateDirectory (Join-Path $LabRoot $folder) }
    # Rootless: o diretorio montado precisa pertencer ao uid postgres (999) da imagem dentro do namespace.
    & podman unshare chown 999:999 (Join-Path $LabRoot 'postgres')
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao atribuir o diretorio do PostgreSQL ao uid do container.' }
    if (-not (Test-Path -LiteralPath $identityEnv)) {
        Write-SecretFile $identityEnv ([ordered]@{
            KEYCLOAK_MASTER_USERNAME = 'lab-admin'; KEYCLOAK_MASTER_PASSWORD = (New-Secret)
            POSTGRES_ADMIN_USERNAME = 'keycloak'; POSTGRES_ADMIN_PASSWORD = (New-Secret)
            KEYCLOAK_DB_SCHEMA = 'identity'; KEYCLOAK_PUBLIC_URL = $publicBase; KEYCLOAK_MANAGEMENT_URL = $publicBase
            TLS_CERTIFICATE_PATH = (Join-Path $certificates 'tls.crt'); TLS_PRIVATE_KEY_PATH = (Join-Path $certificates 'tls.key')
            POSTGRES_DATA_PATH = (Join-Path $LabRoot 'postgres'); KEYCLOAK_THEMES_ROOT = (Join-Path $LabRoot 'themes')
            IDENTITY_HTTPS_PORT = '7443'; POSTGRES_HOST_PORT = '55432'
        })
    }
    if (-not (Test-Path -LiteralPath $nodraEnv)) {
        Write-SecretFile $nodraEnv ([ordered]@{
            ALTIVY_PUBLIC_HOST = 'localhost'; ALTIVY_PUBLIC_ORIGIN = $origin
            ALTIVY_KEYCLOAK_AUTHORITY = "$publicBase/realms/altivy"
            ALTIVY_BFF_CLIENT_SECRET = (New-Secret); ALTIVY_PROVISIONER_CLIENT_SECRET = (New-Secret)
            ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD = (New-Secret)
            ALTIVY_DATA_ROOT = (Join-Path $LabRoot 'data'); ALTIVY_CONFIG_ROOT = $certificates
            ALTIVY_POSTGRES_RUNTIME_CONNECTION_STRING = "Host=postgres;Database=altivy;Username=altivy_runtime;Password=$(New-Secret)"
            ALTIVY_POSTGRES_MIGRATOR_CONNECTION_STRING = "Host=postgres;Database=altivy;Username=altivy_migrator;Password=$(New-Secret)"
            ALTIVY_WEB_IMAGE = 'localhost/nodra-web:lab'; ALTIVY_BFF_IMAGE = 'localhost/nodra-bff:lab'
            ALTIVY_API_IMAGE = 'localhost/nodra-api:lab'; ALTIVY_WORKER_IMAGE = 'localhost/nodra-worker:lab'
            ALTIVY_MIGRATIONS_IMAGE = 'localhost/nodra-migrations:lab'; ALTIVY_IMAGE_PULL_POLICY = 'never'
            ALTIVY_SMTP_HOST = 'smtp.lab.invalid'; ALTIVY_SMTP_PORT = '587'; ALTIVY_SMTP_USERNAME = 'lab'
            ALTIVY_SMTP_PASSWORD = (New-Secret); ALTIVY_SMTP_FROM = 'nodra@example.test'; ALTIVY_SMTP_TO = 'operator@example.test'
        })
    }
    if (-not (Test-Path -LiteralPath $userEnv)) {
        Write-SecretFile $userEnv ([ordered]@{
            ALTIVY_E2E_USERNAME = 'lzocateli'; ALTIVY_E2E_PASSWORD = "Lab-$(New-Secret 24)"
            ALTIVY_FULANO_EMAIL = 'lzocateli@example.test'
        })
    }
    $identity = Read-SecretFile $identityEnv
    $nodra = Read-SecretFile $nodraEnv
    $user = Read-SecretFile $userEnv

    $authority = Join-Path $LabRoot 'ca'
    if (-not (Test-Path -LiteralPath (Join-Path $authority 'rootCA.pem'))) {
        Invoke-Podman -Arguments @('run', '--rm', '--network', 'none', '--userns=keep-id', '--user', "$(& id -u):$(& id -g)",
            '--volume', "${authority}:/ca", '--entrypoint', '/bin/bash', $helperImage, '-ec',
            "umask 077; test ! -e /ca/rootCA-key.pem; openssl req -x509 -newkey rsa:3072 -nodes -keyout /ca/rootCA-key.pem -out /ca/rootCA.pem -days 825 -sha256 -subj '/CN=Nodra Local Lab CA' -addext 'basicConstraints=critical,CA:TRUE' -addext 'keyUsage=critical,keyCertSign,cRLSign'; chmod 0444 /ca/rootCA.pem")
        Write-Host 'CA local do laboratorio criada.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $certificates 'tls.crt'))) {
        & (Join-Path $PSScriptRoot 'new-local-production-certificates.ps1') -ContainerEngine podman -EnvironmentFile $nodraEnv -CertificateDirectory $certificates -AuthorityDirectory $authority
    }
    $themes = Join-Path $LabRoot 'themes'
    $themeBuild = Join-Path $LabRoot 'theme-build'
    & (Join-Path $repository 'deploy/keycloak/build-theme.ps1') -OutputPath $themeBuild | Out-Null
    $themeSources = @{ identity = (Join-Path $IdentityRoot 'themes/identity'); altivy = (Join-Path $themeBuild 'altivy') }
    foreach ($theme in @('identity', 'altivy', 'dokpod')) {
        $target = Join-Path $themes $theme
        if ($theme -eq 'altivy' -and (Test-Path -LiteralPath $target)) {
            # Atualiza o conteúdo sem recriar o diretório, que é bind mount do Keycloak em execução.
            Get-ChildItem -LiteralPath $target -Force | Remove-Item -Recurse -Force
            Get-ChildItem -LiteralPath $themeSources[$theme] -Force | Copy-Item -Destination $target -Recurse -Force
            continue
        }
        if (Test-Path -LiteralPath $target) { continue }
        if ($themeSources.ContainsKey($theme)) { Copy-Item -LiteralPath $themeSources[$theme] -Destination $target -Recurse }
        else { New-Item -ItemType Directory -Path $target | Out-Null }
    }
    if (-not $IsWindows) {
        # O Keycloak roda com UID mapeado (subuid) e precisa ler os temas públicos mesmo com umask 077 no host.
        & chmod -R 'u=rwX,go=rX' $themes
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao ajustar permissoes de leitura dos temas do Keycloak.' }
    }

    Invoke-Podman -Arguments ($identityCommand + @('up', '-d', '--wait', '--wait-timeout', '120', 'postgres'))
    $databaseContainer = Invoke-Podman -Arguments @('ps', '--filter', 'label=com.docker.compose.project=altivy-identity', '--filter', 'label=com.docker.compose.service=postgres', '--format', '{{.ID}}') -Capture
    if ([string]::IsNullOrWhiteSpace($databaseContainer) -or $databaseContainer -match "`n") { throw 'PostgreSQL da identidade nao esta em execucao de forma univoca.' }
    $schema = $identity.KEYCLOAK_DB_SCHEMA
    if ($schema -notmatch '^[a-z_][a-z0-9_]*$') { throw 'KEYCLOAK_DB_SCHEMA invalido.' }
    Invoke-Podman -Arguments @('exec', '-i', $databaseContainer, 'sh', '-c', 'exec psql -X -q -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d keycloak') -InputText "CREATE SCHEMA IF NOT EXISTS $schema;"
    Invoke-Podman -Arguments ($identityCommand + @('up', '-d', '--wait', '--wait-timeout', '300'))
    Write-Host 'Identidade AKB saudavel em https://localhost:7443.'

    $exists = Invoke-Podman -Arguments @('exec', $databaseContainer, 'sh', '-c', 'psql -X -tA -U "$POSTGRES_USER" -d keycloak -c "SELECT 1 FROM pg_database WHERE datname = ''altivy''"') -Capture
    if ($exists -ne '1') { Invoke-Podman -Arguments @('exec', $databaseContainer, 'sh', '-c', 'createdb -U "$POSTGRES_USER" altivy') }
    $connections = @{}
    foreach ($role in @('RUNTIME', 'MIGRATOR')) {
        $builder = [Data.Common.DbConnectionStringBuilder]::new()
        $builder.set_ConnectionString($nodra["ALTIVY_POSTGRES_${role}_CONNECTION_STRING"])
        $connections[$role] = [string]$builder.get_Item('Password')
        if ($connections[$role] -notmatch '^[A-Za-z0-9]+$') { throw 'Senha de role do laboratorio deve ser alfanumerica.' }
    }
    $bootstrap = "\set altivy_migrator_password '$($connections.MIGRATOR)'`n\set altivy_runtime_password '$($connections.RUNTIME)'`n" +
        [IO.File]::ReadAllText((Join-Path $repository 'backend/libs/infrastructure/data/sql/001-bootstrap-altivy-roles.sql'))
    Invoke-Podman -Arguments @('exec', '-i', $databaseContainer, 'sh', '-c', 'exec psql -X -q -U "$POSTGRES_USER" -d altivy') -InputText $bootstrap
    $bootstrap = $null; $connections = $null
    Write-Host 'Database altivy e roles reconciliados.'

    Set-ProcessSetting 'ALTIVY_KEYCLOAK_ADMIN_USERNAME' $identity.KEYCLOAK_MASTER_USERNAME
    Set-ProcessSetting 'ALTIVY_KEYCLOAK_ADMIN_PASSWORD' $identity.KEYCLOAK_MASTER_PASSWORD
    Set-ProcessSetting 'ALTIVY_BFF_CLIENT_SECRET' $nodra.ALTIVY_BFF_CLIENT_SECRET
    Set-ProcessSetting 'ALTIVY_PROVISIONER_CLIENT_SECRET' $nodra.ALTIVY_PROVISIONER_CLIENT_SECRET
    Set-ProcessSetting 'ALTIVY_FULANO_EMAIL' $user.ALTIVY_FULANO_EMAIL
    Set-ProcessSetting 'ALTIVY_E2E_USERNAME' $user.ALTIVY_E2E_USERNAME
    Set-ProcessSetting 'ALTIVY_E2E_PASSWORD' $user.ALTIVY_E2E_PASSWORD
    foreach ($name in @('APPDATA', 'ALTIVY_FULANO_TEMPORARY_PASSWORD', 'ALTIVY_SMTP_HOST')) { Set-ProcessSetting $name $null }
    Invoke-TrustedPwsh @('-File', (Join-Path $PSScriptRoot 'configure-keycloak.ps1'), '-Bootstrap', '-BaseUrl', $publicBase, '-BffBaseUrl', $origin)
    Invoke-TrustedPwsh @('-Command', $labUserScript)
    foreach ($vault in $ownedVaults) {
        Invoke-TrustedPwsh @('-File', (Join-Path $PSScriptRoot 'provision-vault-owner-authorization.ps1'), '-VaultId', $vault, '-OwnerUsername', $user.ALTIVY_E2E_USERNAME, '-BaseUrl', $publicBase)
    }
    Invoke-TrustedPwsh @('-File', (Join-Path $PSScriptRoot 'provision-sample-vault-authorization.ps1'), '-BaseUrl', $publicBase)
    foreach ($name in @('ALTIVY_KEYCLOAK_ADMIN_USERNAME', 'ALTIVY_KEYCLOAK_ADMIN_PASSWORD', 'ALTIVY_BFF_CLIENT_SECRET', 'ALTIVY_PROVISIONER_CLIENT_SECRET', 'ALTIVY_E2E_USERNAME', 'ALTIVY_E2E_PASSWORD')) { Set-ProcessSetting $name $null }

    if (-not $Build) {
        foreach ($service in @('web', 'bff', 'api', 'worker', 'migrations')) {
            & podman image exists "localhost/nodra-${service}:lab"
            if ($LASTEXITCODE -ne 0) { throw "Imagem localhost/nodra-${service}:lab ausente; execute com -Build." }
        }
    }
    $startParameters = @{
        ContainerEngine = 'podman'; EnvironmentFile = $nodraEnv; CertificateDirectory = $certificates
        DataRoot = (Join-Path $LabRoot 'data'); NoBuild = (-not $Build); RecreateConsumers = $RecreateConsumers
    }
    & (Join-Path $PSScriptRoot 'start-self-hosted.ps1') @startParameters

    $migrator = [Data.Common.DbConnectionStringBuilder]::new()
    $migrator.set_ConnectionString($nodra.ALTIVY_POSTGRES_MIGRATOR_CONNECTION_STRING)
    Set-ProcessSetting 'PGPASSWORD' ([string]$migrator.get_Item('Password'))
    foreach ($vault in $labVaults) {
        Invoke-Podman -Arguments @('run', '--rm', '--network', 'none', '--user', '0:0', '--volume', "$(Join-Path $LabRoot 'data/vaults'):/vaults", '--entrypoint', '/bin/sh', $helperImage, '-ec',
            "test ! -L /vaults/$vault; if [ ! -e /vaults/$vault ]; then mkdir /vaults/$vault; chown 1654:1654 /vaults/$vault; chmod 0700 /vaults/$vault; fi; test -d /vaults/$vault")
        $fixture = "INSERT INTO altivy.vaults (""Id"", ""Name"", state) VALUES ('$vault', 'Laboratorio $vault', 2) ON CONFLICT (""Id"") DO NOTHING;"
        Invoke-Podman -Arguments @('run', '--rm', '-i', '--network', 'identity-client', '--env', 'PGPASSWORD', '--entrypoint', 'psql', $postgresImage, '-X', '-q', '-h', 'postgres', '-U', 'altivy_migrator', '-d', 'altivy', '-v', 'ON_ERROR_STOP=1') -InputText $fixture
    }
    Set-ProcessSetting 'PGPASSWORD' $null

    Write-Host 'Documento modelo disponivel no vault de exemplo (nodra-exemplo), somente leitura.'
    Write-Host "Fixtures sinteticas prontas: $($labVaults -join ', ') (lab-denied sem permissao)."

    if ($RunE2E) {
        Set-ProcessSetting 'ALTIVY_E2E_USERNAME' $user.ALTIVY_E2E_USERNAME
        Set-ProcessSetting 'ALTIVY_E2E_PASSWORD' $user.ALTIVY_E2E_PASSWORD
        Set-ProcessSetting 'ALTIVY_E2E_WRITE_VAULT_ID' 'lab-write-e2e'
        Set-ProcessSetting 'ALTIVY_E2E_DENIED_VAULT_ID' 'lab-denied'
        # Nota gravada por uma execucao anterior prova persistencia no bind mount apos recriar os consumidores.
        $persistedNote = Invoke-Podman -Capture -Arguments @('run', '--rm', '--network', 'none', '--user', '0:0', '--volume', "$(Join-Path $LabRoot 'data/vaults'):/vaults:ro", '--entrypoint', '/bin/sh', $helperImage, '-c',
            'ls -t /vaults/lab-write-e2e/autosave-*.md 2>/dev/null | head -n 1 | xargs -r basename')
        Set-ProcessSetting 'ALTIVY_E2E_PERSISTED_NOTE_PATH' $(if ($persistedNote) { $persistedNote } else { $null })
        if ($persistedNote) { Write-Host "Validando persistencia da nota $persistedNote gravada anteriormente." }
        Invoke-Podman -Arguments @('run', '--rm', '--network', 'host', '--env', 'ALTIVY_E2E_USERNAME', '--env', 'ALTIVY_E2E_PASSWORD',
            '--env', 'ALTIVY_E2E_WRITE_VAULT_ID', '--env', 'ALTIVY_E2E_DENIED_VAULT_ID', '--env', 'ALTIVY_E2E_PERSISTED_NOTE_PATH',
            '--volume', "$(Join-Path $repository 'frontend/tests'):/app/tests:ro",
            $playwrightImage, '/app/tests/e2e/test_bff_session.py', "--base-url=$origin", '-p', 'no:cacheprovider')
        Write-Host 'E2E real aprovado pelo nginx.'
    }
    Write-Host "Laboratorio pronto em $origin (CA: $(Join-Path $certificates 'rootCA.pem'); usuario em $userEnv)."
} finally {
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
    $identity = $null; $nodra = $null; $user = $null; $migrator = $null
    Pop-Location
}

<#
.SYNOPSIS
Testa restore de snapshot, ownership migrator e privilegios runtime.
.DESCRIPTION
Restaura PostgreSQL e o archive em container efemero sem rede, reprovisiona roles
com senhas sinteticas, executa o bootstrap real e valida o bundle atual.
Secrets externos, PFX e identidade nao sao lidos nem copiados.
.PARAMETER DataRoot
Raiz externa persistente. Padrao Windows: LOCALAPPDATA/Nodra/Production.
.PARAMETER BackupId
ID do snapshot. Padrao: metadata mais recente em backups/.
.PARAMETER PostgresImage
Imagem PostgreSQL ja presente localmente. Padrao: image ID gravado no metadata.
.PARAMETER CurrentMigrationImage
Bundle atual ja presente localmente. Padrao: nodra-migrations:0.1.0.
.PARAMETER RemainingArguments
Aceita literalmente --help.
.EXAMPLE
./tools/scripts/test-self-hosted-recovery.ps1
.EXAMPLE
pwsh -NoProfile -File tools/scripts/test-self-hosted-recovery.ps1 -BackupId 20261005T220302-5030740f173a4025931761707053275c
.INPUTS
Snapshot externo e bootstrap SQL versionado; passwords do teste sao temporarias.
.OUTPUTS
Contagem agregada e validacao de privilegios; nunca imprime dados de notas.
.NOTES
Requer PowerShell 7.4+, Docker Engine/Desktop e images locais. Container usa --network none.
PFX/Keycloak ficam fora do snapshot; o ensaio nao valida autenticacao HTTP ou grants externos.
.LINK
../../docs/runbooks/recuperacao-self-hosted.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $DataRoot,
    [ValidatePattern('^[0-9]{8}T[0-9]{6}-[a-f0-9]{32}$')][string] $BackupId,
    [string] $PostgresImage,
    [string] $CurrentMigrationImage = 'nodra-migrations:0.1.0',
    [Parameter(ValueFromRemainingArguments)][string[]] $RemainingArguments
)
if ($RemainingArguments -contains '--help') { Get-Help $PSCommandPath -Full; return }
if ($RemainingArguments.Count -gt 0) { throw 'Argumento desconhecido. Use --help.' }
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot)
if (-not $DataRoot) {
    if (-not $IsWindows) { throw 'Informe -DataRoot externo neste host. Use --help.' }
    $DataRoot = Join-Path $env:LOCALAPPDATA 'Nodra/Production'
}
$DataRoot = [IO.Path]::GetFullPath($DataRoot)
if ($DataRoot.StartsWith($repository + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {
    throw 'Dados e backups devem permanecer fora do repositorio.'
}
$backupDirectory = Join-Path $DataRoot 'backups'
if (-not $BackupId) {
    $metadataFile = Get-ChildItem -LiteralPath $backupDirectory -Filter '*.metadata.json' -File |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $metadataFile) { throw 'Metadata do snapshot ausente. Use --help.' }
    $BackupId = [regex]::Replace($metadataFile.Name,'\.metadata\.json$','')
} else {
    $metadataFile = Get-Item -LiteralPath (Join-Path $backupDirectory "$BackupId.metadata.json") -ErrorAction SilentlyContinue
    if (-not $metadataFile) { throw 'Metadata do snapshot ausente. Use --help.' }
}
$metadata = Get-Content -LiteralPath $metadataFile.FullName -Raw | ConvertFrom-Json
if ($metadata.backupId -ne $BackupId -or $metadata.postgresImageId -notmatch '^sha256:[a-f0-9]{64}$') {
    throw 'Metadata de snapshot invalido.'
}
if (-not $PostgresImage) { $PostgresImage = $metadata.postgresImageId }
foreach ($extension in @('.dump','.tar.gz','.sha256','.files.sha256','.metadata.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $backupDirectory "$BackupId$extension") -PathType Leaf)) {
        throw 'Conjunto de backup incompleto. Use --help.'
    }
}

function Invoke-Docker {
    param([string[]] $Arguments, [switch] $Capture, [switch] $Quiet)
    $localPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $result = & docker @Arguments 2>&1; $nativeExit = $LASTEXITCODE }
    finally { $ErrorActionPreference = $localPreference }
    if ($nativeExit -ne 0) {
        $outputText = (@($result | ForEach-Object { [string]$_ }) -join "`n")
        $stageMatches = [regex]::Matches($outputText,'P04_STAGE=([A-Z0-9_]+)')
        $stageName = if ($stageMatches.Count) { $stageMatches[$stageMatches.Count - 1].Groups[1].Value } else { 'STARTUP' }
        $diagnostics = @($outputText -split "`r?`n" | Where-Object {
            $_ -match '(?i)(error|failed|denied|no such file|invalid|unable|cannot)'
        } | Select-Object -Last 3 | ForEach-Object {
            [regex]::Replace([string]$_,'(?i)(Password=)[^;\s]+','$1[REDACTED]')
        })
        if ($diagnostics.Count) {
            throw "Restore isolado falhou na etapa ${stageName}: $($diagnostics -join ' | ')"
        }
        throw "Restore isolado falhou na etapa $stageName; nenhuma origem foi alterada. Consulte --help."
    }
    if ($Capture) { return ($result | Out-String).Trim() }
    if (-not $Quiet) {
        foreach ($line in $result) {
            if ([string]$line -notmatch '^P04_STAGE=') { Write-Host $line }
        }
    }
}

$postgresImageId = Invoke-Docker -Arguments @('image','inspect',$PostgresImage,'--format','{{.Id}}') -Capture
$null = Invoke-Docker -Arguments @('image','inspect',$CurrentMigrationImage,'--format','{{.Id}}') -Capture
$bundleContainer = $null
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('altivy-p04-restore-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $bundleContainer = Invoke-Docker -Arguments @('create',$CurrentMigrationImage) -Capture
    Invoke-Docker -Arguments @('cp',"${bundleContainer}:/app/efbundle",(Join-Path $testRoot 'current-efbundle')) -Quiet
    Invoke-Docker -Arguments @('rm',$bundleContainer) -Quiet
    $bundleContainer = $null
    $restoreScript = @'
echo P04_STAGE=START
set -euo pipefail
echo P04_STAGE=SET_OPTIONS
chmod 0755 /test/current-efbundle
echo P04_STAGE=CHMOD_BUNDLE
mkdir -p /tmp/altivy-recovery/pgdata /tmp/altivy-recovery/socket /tmp/altivy-recovery/files
echo P04_STAGE=MAKE_TMP
chown -R postgres:postgres /tmp/altivy-recovery
echo P04_STAGE=OWN_TMP
trap 'gosu postgres pg_ctl -D /tmp/altivy-recovery/pgdata -m fast stop >/dev/null 2>&1 || true' EXIT
cd /backups
echo P04_STAGE=CHECKSUMS
sha256sum --check "$BACKUP_ID.sha256" >/dev/null
pg_restore --list "$BACKUP_ID.dump" >/tmp/altivy-recovery/toc
echo P04_STAGE=INITDB
gosu postgres initdb -D /tmp/altivy-recovery/pgdata --auth-local=trust --auth-host=reject --username=restore_operator >/dev/null
gosu postgres pg_ctl -D /tmp/altivy-recovery/pgdata -o "-c listen_addresses='' -c unix_socket_directories='/tmp/altivy-recovery/socket' -p 55432" -w start >/dev/null
export PGHOST=/tmp/altivy-recovery/socket PGPORT=55432 PGUSER=restore_operator
gosu postgres createdb restore_lab
runtime_password=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
migrator_password=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
psql -Xq -d postgres -v ON_ERROR_STOP=1 \
  -c "CREATE ROLE altivy_migrator LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '$migrator_password'" \
  -c "CREATE ROLE altivy_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '$runtime_password'" \
  -c 'GRANT CREATE ON DATABASE restore_lab TO altivy_migrator'
echo P04_STAGE=PG_RESTORE
pg_restore --no-owner --no-privileges --exit-on-error --dbname=restore_lab "/backups/$BACKUP_ID.dump"
echo P04_STAGE=BOOTSTRAP
{
  printf "\\set altivy_runtime_password '%s'\n" "$runtime_password"
  printf "\\set altivy_migrator_password '%s'\n" "$migrator_password"
  cat /bootstrap/001-bootstrap-altivy-roles.sql
} | psql -Xq -d restore_lab -v ON_ERROR_STOP=1
echo P04_STAGE=OWNERSHIP
psql -Xq -d restore_lab -v ON_ERROR_STOP=1 <<'SQL'
DO $p04$
DECLARE item record;
BEGIN
    FOR item IN
        SELECT c.relname,
            CASE c.relkind
                WHEN 'r' THEN 'TABLE'
                WHEN 'p' THEN 'TABLE'
                WHEN 'v' THEN 'VIEW'
                WHEN 'm' THEN 'MATERIALIZED VIEW'
                WHEN 'f' THEN 'FOREIGN TABLE'
                WHEN 'S' THEN 'SEQUENCE'
            END AS object_type
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'altivy'
            AND c.relkind IN ('r','p','v','m','f','S')
            AND c.relowner = 'restore_operator'::regrole
    LOOP
        EXECUTE format('ALTER %s %I.%I OWNER TO altivy_migrator',item.object_type,'altivy',item.relname);
    END LOOP;

    FOR item IN
        SELECT p.oid::regprocedure AS identity,
            CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END AS object_type
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'altivy'
            AND p.prokind IN ('f','p')
            AND p.proowner = 'restore_operator'::regrole
    LOOP
        EXECUTE format('ALTER %s %s OWNER TO altivy_migrator',item.object_type,item.identity);
    END LOOP;

    FOR item IN
        SELECT t.typname
        FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'altivy'
            AND t.typtype IN ('d','e')
            AND t.typowner = 'restore_operator'::regrole
    LOOP
        EXECUTE format('ALTER TYPE %I.%I OWNER TO altivy_migrator','altivy',item.typname);
    END LOOP;
END
$p04$;
SQL
echo P04_STAGE=FILES
cd /tmp/altivy-recovery/files
tar -xzf "/backups/$BACKUP_ID.tar.gz"
sha256sum --check "/backups/$BACKUP_ID.files.sha256" >/dev/null
echo P04_STAGE=MIGRATIONS
ALTIVY_DESIGNTIME_CONNECTION_STRING="Host=/tmp/altivy-recovery/socket;Port=55432;Database=restore_lab;Username=altivy_migrator;Password=$migrator_password" /test/current-efbundle >/tmp/altivy-recovery/migrator.log 2>&1
echo P04_STAGE=RUNTIME_ROLE
vault_count=$(PGPASSWORD="$runtime_password" psql -XAt -h /tmp/altivy-recovery/socket -p 55432 -U altivy_runtime -d restore_lab -c 'SELECT count(*) FROM altivy.vaults')
role_flags=$(psql -XAt -d postgres -c "SELECT rolsuper::text||'|'||rolcreatedb::text||'|'||rolcreaterole::text||'|'||rolbypassrls::text||'|'||rolcanlogin::text FROM pg_roles WHERE rolname='altivy_runtime'")
test "$role_flags" = 'false|false|false|false|true'
if PGPASSWORD="$runtime_password" psql -Xq -h /tmp/altivy-recovery/socket -p 55432 -U altivy_runtime -d restore_lab -v ON_ERROR_STOP=1 -c 'CREATE TABLE altivy.p04_forbidden (id integer)' >/dev/null 2>&1; then exit 31; fi
tar -tzf "/backups/$BACKUP_ID.tar.gz" | sed 's:/$::' | sort > /tmp/altivy-recovery/archive-tree
find vaults bff-keys -print | sort > /tmp/altivy-recovery/restored-tree
cmp /tmp/altivy-recovery/archive-tree /tmp/altivy-recovery/restored-tree
printf 'Restore com roles aprovado: %s vaults; migrator aceitou bundle; runtime sem DDL.\n' "$vault_count"
'@
    $restoreScript = $restoreScript.Replace('$BACKUP_ID',$BackupId).Replace("`r`n","`n")
    $arguments = @(
        'run','--rm','--pull=never','--network','none','--read-only','--user','0:0',
        '--tmpfs','/tmp:rw,noexec,nosuid,size=768m',
            '--volume',"${backupDirectory}:/backups:ro",
            '--volume',"${testRoot}:/test:rw",
        '--volume',"${repository}/backend/libs/infrastructure/data/sql/001-bootstrap-altivy-roles.sql:/bootstrap/001-bootstrap-altivy-roles.sql:ro",
        '--entrypoint','/bin/bash',$postgresImageId,'-ec',$restoreScript
    )
    Invoke-Docker -Arguments $arguments
} finally {
    if ($bundleContainer) { try { Invoke-Docker -Arguments @('rm','-f',$bundleContainer) -Quiet } catch { Write-Warning 'Container auxiliar de bundle requer verificacao manual.' } }
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}

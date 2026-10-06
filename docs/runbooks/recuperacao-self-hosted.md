# Runbook: Recuperacao self-hosted

## Escopo e sinais

Use quando PostgreSQL/readiness estiver indisponivel, uma atualizacao falhar ou
for necessario restaurar dados. Arquivos Markdown e banco devem ser recuperados
como um conjunto coerente. Keycloak e seus backups pertencem ao operador da
plataforma de identidade, nao ao backup do Altivy.

Este procedimento nao autoriza restaurar sobre um banco compartilhado nem
aplicar downgrade de schema. Consulte o
[plano de deploy](../plan/deploy-self-hosted-compose.md) para gates pendentes.

## Diagnostico e mitigacao

Na raiz do repositorio, em PowerShell 7, verifique containers, mounts persistentes
e readiness. Inspecione apenas destinos dos mounts, nunca o ambiente resolvido:

```powershell
docker ps --format 'table {{.Names}}\t{{.Status}}'
docker inspect --format '{{range .Mounts}}{{.Type}} -> {{.Destination}}{{println}}{{end}}' $PostgresContainer
docker exec $PostgresContainer sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

O operador fornece `$PostgresContainer`. Reinicio de banco compartilhado exige
autorizacao explicita e janela para todos os consumidores. Apos restart,
aguarde readiness usando o health check da plataforma; falha de conexao durante
startup nao prova perda de dados. Nao repita restart nem force encerramento.

## Backup coordenado

Pre-requisitos: Docker Engine/Desktop, espaco em disco e diretorio de dados
externo protegido por ACL (Windows) ou modo `0700` (Linux). `pg_dump`,
`pg_restore`, `tar` e `sha256sum` executam nos containers; nao e necessario
instalar runtime de aplicacao no host. Os artefatos sao privados: nunca os
inclua no Git, relatorios de CI ou contexto de imagem. Backups usam modo `0600`.

O boot por `tools/scripts/start-self-hosted.ps1` ja executa o backup coordenado
antes das migrations, com BFF/API/worker parados graciosamente. Ele gera no
diretorio externo `backups/` um dump `.dump`, um tar `.tar.gz` contendo `vaults/`
e `bff-keys/`, metadata JSON com o ID imutavel da imagem PostgreSQL, um
`.sha256` do dump/archive/metadata e um `.files.sha256` capturado durante a
mesma janela sem writers. Checksums sao verificados antes de continuar; o script
imprime apenas o ID do snapshot. Em falha, writers sao retomados.

O PFX de Data Protection nao entra no archive. Guarde-o, sua senha e os demais
secrets por mecanismo externo aprovado e protegido; restaurar `bff-keys/` exige
o mesmo PFX. Backups Keycloak/UMA/grants continuam responsabilidade da
plataforma de identidade e nao fazem parte deste conjunto.

1. O snapshot registra a imagem PostgreSQL por ID; registre tambem versao/digests
   dos consumidores, mounts e baseline de contagens para rollback da aplicacao.
2. Pare graciosamente BFF/API/worker; impeça tambem alteracoes externas no vault.
3. Capture dump custom-format do database dedicado e arquivo tar de `vaults/` e
   `bff-keys/` na mesma janela sem writers.
4. Valide exit codes, integridade dos arquivos e hashes antes de qualquer migration.
5. Retome os consumidores em `finally` se a captura falhar; nao promova imagens.
6. Guarde PFX e demais secrets por mecanismo separado aprovado, nunca em logs;
   o keyring e incluido no archive coordenado.

No laboratorio com database compartilhado, o ensaio exporta apenas `altivy`:

```powershell
docker exec $PostgresContainer sh -c 'umask 077; exec pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --schema=altivy --format=custom --no-owner --no-privileges --file="$1"' sh $DumpTemp
if ($LASTEXITCODE -ne 0) { throw 'Backup falhou; nao executar migration.' }
docker cp "${PostgresContainer}:$DumpTemp" $DumpPath
if ($LASTEXITCODE -ne 0) { throw 'Copia do backup falhou.' }
```

O operador define `$DumpTemp` como caminho temporario novo no container e
`$DumpPath` fora do repositorio. Remova o temporario depois da copia verificada.
Dump parcial do schema e vault sintetico nao equivalem a backup completo.

## Restore isolado

Nunca use o container/banco de origem como destino. O comando abaixo monta
somente o backup em read-only e executa PostgreSQL efemero na camada descartavel
do container: `--network none`, sem portas publicadas e com listener TCP
desabilitado. A camada e removida automaticamente. Use a mesma versao/digest
PostgreSQL da origem, disponivel localmente; o ensaio nao le credenciais.

Na raiz do AltivyNotes, informe o ID emitido pelo boot e execute:

```powershell
$dataRoot = Join-Path $env:LOCALAPPDATA 'AltivyNotes\Production'
$backupId = '<id-do-snapshot>'
$backupDirectory = Join-Path $dataRoot 'backups'
$metadataName = "$backupId.metadata.json"
$metadataPath = Join-Path $backupDirectory $metadataName
$manifestPath = Join-Path $backupDirectory "$backupId.sha256"
$metadataHash = (Get-FileHash -LiteralPath $metadataPath -Algorithm SHA256).Hash.ToLowerInvariant()
$manifestLines = Get-Content -LiteralPath $manifestPath
$expectedMetadataEntry = '{0}  {1}' -f $metadataHash, $metadataName
if (@($manifestLines) -notcontains $expectedMetadataEntry) { throw 'Metadata nao confere com o manifesto.' }
$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ($metadata.backupId -ne $backupId -or $metadata.postgresImageId -notmatch '^sha256:[a-f0-9]{64}$') {
    throw 'Metadata do snapshot invalido.'
}
$postgresImageId = $metadata.postgresImageId
$availableImageId = docker image inspect $postgresImageId --format '{{.Id}}'
if ($LASTEXITCODE -ne 0 -or $availableImageId -ne $postgresImageId) { throw 'A imagem PostgreSQL original nao esta disponivel localmente.' }
$restoreCommand = @'
set -euo pipefail
mkdir -p /tmp/altivy-recovery/pgdata /tmp/altivy-recovery/files
chown -R postgres:postgres /tmp/altivy-recovery
trap 'gosu postgres pg_ctl -D /tmp/altivy-recovery/pgdata -m fast stop >/dev/null 2>&1 || true' EXIT
cd /backups
sha256sum --check "$BACKUP_ID.sha256" >/dev/null
pg_restore --list "$BACKUP_ID.dump" >/tmp/altivy-recovery/toc
gosu postgres initdb -D /tmp/altivy-recovery/pgdata --auth-local=trust --auth-host=reject --username=restore_operator >/dev/null
gosu postgres pg_ctl -D /tmp/altivy-recovery/pgdata -o "-c listen_addresses='' -c unix_socket_directories='/tmp/altivy-recovery' -p 55432" -w start >/dev/null
export PGHOST=/tmp/altivy-recovery PGPORT=55432 PGUSER=restore_operator
gosu postgres createdb restore_lab
pg_restore --no-owner --no-privileges --exit-on-error --dbname=restore_lab "/backups/$BACKUP_ID.dump"
vault_count=$(psql -XAt -d restore_lab -c 'SELECT count(*) FROM altivy.vaults')
table_count=$(psql -XAt -d restore_lab -c "SELECT count(*) FROM pg_tables WHERE schemaname = 'altivy'")
tar -xzf "/backups/$BACKUP_ID.tar.gz" -C /tmp/altivy-recovery/files
cd /tmp/altivy-recovery/files
sha256sum --check "/backups/$BACKUP_ID.files.sha256" >/dev/null
tar -tzf "/backups/$BACKUP_ID.tar.gz" | sed 's:/$::' | sort > /tmp/altivy-recovery/archive-tree
find vaults bff-keys -print | sort > /tmp/altivy-recovery/restored-tree
cmp /tmp/altivy-recovery/archive-tree /tmp/altivy-recovery/restored-tree
printf 'Restore isolado aprovado: %s vaults, %s tabelas; arquivos e keyring verificados.\n' "$vault_count" "$table_count"
'@
$restoreCommand = $restoreCommand.Replace('$BACKUP_ID', $backupId)
docker run --rm --pull=never --network none `
  --volume "${backupDirectory}:/backups:ro" `
  --entrypoint /bin/bash $postgresImageId -ec $restoreCommand
if ($LASTEXITCODE -ne 0) { throw 'Restore isolado falhou; nao promover destino.' }
```

Em instalações com `-DataRoot` diferente, ajuste `$dataRoot`. Em Linux,
substitua o default Windows pelo path externo protegido. A imagem é resolvida
do metadata conferido do snapshot, sem depender do container atual.

O teste prova leitura do dump, restore SQL e integridade/estrutura dos arquivos,
nao permissões operacionais, PFX, grants Keycloak ou tráfego HTTP. Em produção,
reprovisione roles/grants de menor privilégio, restaure o PFX pelo mecanismo
externo aprovado e valide API/worker exclusivamente pela borda nginx antes de
promover. `--no-owner --no-privileges` e autenticação local trust servem somente
ao ensaio descartavel; nunca use esses parâmetros em runtime.

Para validar tambem o bootstrap de roles e o acesso de runtime no snapshot, use
o segundo harness. Ele gera senhas aleatorias dentro do container, aplica o SQL
versionado, ajusta ownership somente dos objetos `altivy`, executa o bundle
atual como `altivy_migrator` e comprova que `altivy_runtime` lê o catálogo sem
superuser nem DDL:

```powershell
pwsh -NoProfile -File tools/scripts/test-self-hosted-recovery.ps1
```

O harness continua sem rede, nao lê o arquivo externo de secrets e nao restaura
PFX, Keycloak ou grants UMA. A credencial sintetica não sobrevive ao container.

## Upgrade, rollback e escalonamento

### Ensaio de compatibilidade cross-version

Use o harness versionado antes de promover uma migration. Ele cria PostgreSQL
efêmero sem rede, gera o bundle anterior a partir do commit Git local, aplica
até `-PreviousMigration`, executa o bundle atual e inicia somente a API anterior
por digest. O smoke acessa exclusivamente `/health/live` via NGINX; o teste não
usa Keycloak nem toca na Production.

```powershell
pwsh -NoProfile -File tools/scripts/test-self-hosted-upgrade.ps1 `
  -PreviousCommit 69431f03b72703983c3e2ad0df49cf1a9b5385c1 `
  -PreviousMigration 20260928234000_AddTrashAuditEvents `
  -PreviousApiImage lzocateli/altivy-api@sha256:1cb3433441b7d8ae98730dba16ff0d2e575177d7aca798ef1e8ac5d0a8d350ee
```

O harness verifica que o bundle atual avance a history, não altere a assinatura
do schema esperada neste caso e preserve a linha sintética. A asserção de schema
inalterado corresponde à migration `ReconcileCurrentModel`, que é no-op; para
uma migration futura com mudanças esperadas, atualize a comparação para validar
as alterações expandidas explicitamente. O healthcheck do API é liveness: isso
não comprova queries autenticadas, compatibilidade de todos os consumidores,
Keycloak, PFX ou tráfego de escrita.

Mantenha manifestos de imagens anteriores por digest. Apos backup verificado,
execute migrations compativeis com a versao anterior e promova os consumidores.
Se o smoke falhar, volte apenas as imagens compativeis; nao execute migrations
Down nem sobrescreva arquivos. Restore exige destino limpo e janela aprovada.

Escalone ao operador/DBA quando readiness nao recuperar, houver divergencia
de hashes/contagens ou o restore falhar. Preserve backups privados e logs
sanitizados; nao declare recuperacao completa com apenas health checks.

## Evidencia do ensaio

Em 2026-10-04, o restart autorizado do banco compartilhado preservou o baseline
de tres vaults, oito jobs concluidos e quatro projecoes do fixture. Sete testes
Playwright pelo gateway passaram. O backup coordenado do schema `altivy` foi
restaurado em PostgreSQL isolado e reproduziu essas contagens; as quatro notas
de `lab-write-e2e` restauradas em diretorio limpo tiveram hashes identicos.
O container do banco restaurado foi removido/recriado preservando seu bind
mount e manteve o baseline. Os helpers foram removidos apos o ensaio. Na origem,
dois testes de escrita/leitura posteriores passaram e o worker concluiu os
novos jobs, chegando a dez concluidos e cinco projecoes do fixture.

Naquele ensaio de 2026-10-04, nao foi validado restore completo de todos os
vaults, keyring/PFX, roles/grants, identidade ou upgrade/rollback entre versoes
diferentes; esses gates permaneciam abertos naquele ponto.

Em 2026-10-05, o snapshot coordenado Production foi restaurado em container
PostgreSQL efemero sem rede, usando a mesma imagem por ID. O restore passou para
um vault e 276 tabelas; os manifestos SHA-256 do dump, archive e arquivos foram
validados sem imprimir nomes ou conteudo. O archive continha os dados de
`vaults/` e `bff-keys/`. Depois da retomada da origem, cinco jornadas Playwright
Production passaram 5/5 pelo gateway. Isso nao prova restore do PFX/Keycloak,
permissoes operacionais, nem upgrade/rollback entre versoes.

Também em 2026-10-05, `test-self-hosted-upgrade.ps1` aplicou o bundle do commit
`69431f0` até `20260928234000_AddTrashAuditEvents`, inseriu uma linha sintética,
aplicou a migration atual `20260930123851_ReconcileCurrentModel` e verificou
11→12 entradas na history, assinatura do schema inalterada e linha preservada.
A imagem API anterior por digest ficou healthy e respondeu liveness pelo NGINX
allowlist-only. PostgreSQL/API/NGINX executaram em Compose descartável sem rede;
o teardown removeu containers e o bundle antigo gerado. Esse smoke não valida
outros serviços antigos, autenticação, CRUD ou identidade real.

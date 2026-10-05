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

Pre-requisitos: `pg_dump`, `pg_restore`, `tar`, espaco em disco, permissao
administrativa local e um diretorio externo novo protegido por ACL (Windows)
ou modo `0700` (Linux). Os artefatos sao privados: nunca os inclua no Git,
relatorios de CI ou contexto de imagem. Arquivos de backup usam modo `0600`.

1. Registre imagem por digest/ID, versao, mounts, baseline de contagens e hashes.
2. Pare graciosamente BFF/API/worker; impeça tambem alteracoes externas no vault.
3. Capture dump custom-format do database dedicado e arquivo tar do vault inteiro.
4. Valide exit codes, integridade dos arquivos e hashes antes de qualquer migration.
5. Retome os consumidores em `finally` se a captura falhar; nao promova imagens.
6. Guarde keyring/PFX e secrets por mecanismo separado aprovado, nunca em logs.

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

Nunca use o container/banco de origem como destino. Prepare um PostgreSQL limpo
com a mesma imagem por digest/ID, `--network none`, nenhum port publicado e
bind mount em diretorio novo. No ensaio descartavel, autenticacao local trust
e permitida somente sem rede e com `listen_addresses` vazio; nao use em runtime.

```powershell
docker cp $DumpPath "${RestoreContainer}:/tmp/altivy.dump"
if ($LASTEXITCODE -ne 0) { throw 'Copia para restore falhou.' }
docker exec $RestoreContainer pg_restore --username=restore_operator --dbname=restore_lab --no-owner --no-privileges --exit-on-error /tmp/altivy.dump
if ($LASTEXITCODE -ne 0) { throw 'Restore falhou; nao promover destino.' }
```

Restaure o tar somente em diretorio limpo. Compare hashes dos arquivos,
contagens, jobs e projecoes com o baseline. Teste restart e recriacao do destino
preservando seu bind mount. `--no-owner --no-privileges` serve ao ensaio: no
destino operacional, reprovisione roles/grants de menor privilegio e valide
API/worker pela borda nginx antes de promover. Nao teste com credenciais reais
no container isolado.

## Upgrade, rollback e escalonamento

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

Nao foi validado restore completo de todos os vaults, keyring/PFX, roles/grants,
identidade ou upgrade/rollback entre versoes diferentes. Esses gates continuam
abertos no P-04.
# Instalação self-hosted

Esta composição executa o Altivy Notes no host escolhido pelo operador. Ela inclui gateway TLS, web, BFF, API, worker e PostgreSQL; publica somente o gateway. O Keycloak continua sendo a autoridade externa obrigatória e deve ser fornecido pelo operador, localmente ou em uma rede alcançável.

Esta é a primeira fatia da distribuição self-hosted, ainda sem validação de uma instalação OIDC real nem de backup/restore. Não a trate como release estável até que os gates do plano sejam concluídos.

## Requisitos

- Docker Engine ou Docker Desktop com Compose v2 e containers Linux;
- espaço persistente para PostgreSQL, vault e chaves de Data Protection;
- Keycloak com realm `altivy`, client confidencial `altivy-bff` e Authorization Services configurados;
- URL do issuer Keycloak alcançável tanto pelo navegador quanto pelos containers, com certificado TLS confiável;
- certificado TLS cujo SAN corresponde ao host público escolhido e chave privada correspondente;
- certificado PKCS#12 dedicado à proteção das chaves do BFF;
- conta SMTP operacional para alertas do worker.

## Preparar configuração

No Windows, os valores sensíveis ficam exclusivamente em
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`. Em outros hosts,
use o mecanismo externo de secrets aprovado pelo operador. Nunca crie `.env` ou
arquivos com valores reais dentro do repositório.

Use [compose.env.example](compose.env.example) apenas como lista de nomes. No
arquivo externo, configure paths absolutos e valores reais para os placeholders.
`ALTIVY_DATA_ROOT` e `ALTIVY_CONFIG_ROOT` devem apontar para diretórios fora do
repositório. O diretório de dados contém `postgres/`, `vaults/` e `bff-keys/`.
O diretório de configuração contém `altivy-data-protection.pfx`.

O bootstrap do banco usa `POSTGRES_ADMIN_USERNAME` e
`POSTGRES_ADMIN_PASSWORD`, os mesmos nomes da plataforma de identidade. A
troca de nomes não importa automaticamente outro arquivo externo nem muda
senhas de um banco já inicializado. API/worker e migrator continuam usando as
connection strings próprias com roles distintas; não use a role administrativa
como credencial de runtime.

O certificado e a chave TLS são montados read-only no gateway. A chave precisa
ser legível pelo UID 101 do NGINX unprivileged; conceda somente essa leitura por
ACL/grupo no host, sem torná-la globalmente legível. O PFX do BFF deve ser
readable pelo UID do BFF e continua separado da chave TLS do gateway.

### Reutilizar Credenciais e TLS

Mediante autorização do operador, a composição aceita as referências externas
existentes sem copiar secrets ou certificados:

- `ALTIVY_BFF_CLIENT_SECRET` tem prioridade; se vazio/ausente, usa
	`ALTIVY_E2E_BFF_CLIENT_SECRET`;
- `ALTIVY_TLS_CERTIFICATE_PATH` tem prioridade; se vazio/ausente, usa
	`ALTIVY_E2E_TLS_CERTIFICATE_PATH`;
- `ALTIVY_TLS_PRIVATE_KEY_PATH` tem prioridade; se vazio/ausente, usa
	`ALTIVY_E2E_TLS_KEY_PATH`.

Os aliases não mudam o perfil Production. O certificado TLS não substitui o
PFX dedicado de Data Protection nem sua senha. TLS local só serve a hosts/SANs
e clientes que confiam na CA local; acesso público exige cadeia adequada.

As connection strings E2E não têm fallback automático: elas apontam para o
banco compartilhado, enquanto esta composição provisiona um PostgreSQL próprio.
Reutilizar credenciais exige configurar host/database corretos e provisionar
as roles separadas no destino. Não reutilize o diretório de dados do banco
compartilhado como `ALTIVY_DATA_ROOT/postgres`.

Em 2026-10-05, aliases e precedência passaram em validação Compose sintética;
o preflight real resolveu BFF/TLS, mas ainda bloqueou por PFX/senha, destinatário
SMTP e configuração específica de rede/origem/diretórios/banco. O PFX não foi
encontrado no diretório local conhecido de certificados; confirme o path
configurado antes de iniciar. Nenhum secret externo foi lido ou alterado.

`ALTIVY_BIND_ADDRESS` limita a publicação ao localhost por padrão. Para acesso
pela LAN, configure o endereço de interface pretendido e um certificado com SAN
compatível; não publique o PostgreSQL, API ou BFF.

No realm `altivy`, cadastre no client `altivy-bff`:

- redirect URI: `<ALTIVY_PUBLIC_ORIGIN>/signin-oidc`;
- post-logout redirect URI: `<ALTIVY_PUBLIC_ORIGIN>/signout-callback-oidc`;
- web origin: `<ALTIVY_PUBLIC_ORIGIN>`.

`ALTIVY_KEYCLOAK_AUTHORITY` precisa usar exatamente o issuer anunciado no
Discovery Document. Configure a URL de modo que o navegador e os containers
alcancem o mesmo issuer e validem sua cadeia TLS. Não habilite HTTP inseguro em
Production.

## Primeiro boot

### Desbloquear o preflight

A configuração E2E não configura automaticamente esta composição Production.
O preflight executado em 2026-10-04 identificou valores ausentes nos grupos:

- origem e rede: `ALTIVY_PUBLIC_HOST`, `ALTIVY_PUBLIC_ORIGIN`,
	`ALTIVY_GATEWAY_IP`, `ALTIVY_PROXY_BFF_SUBNET`;
- diretórios e TLS: `ALTIVY_DATA_ROOT`, `ALTIVY_CONFIG_ROOT`,
	`ALTIVY_TLS_CERTIFICATE_PATH`, `ALTIVY_TLS_PRIVATE_KEY_PATH`;
- OIDC e Data Protection: `ALTIVY_KEYCLOAK_AUTHORITY`,
	`ALTIVY_BFF_CLIENT_SECRET`, `ALTIVY_DATA_PROTECTION_CERTIFICATE_PASSWORD`;
- PostgreSQL próprio: `POSTGRES_ADMIN_USERNAME`,
	`POSTGRES_ADMIN_PASSWORD`, `ALTIVY_POSTGRES_RUNTIME_CONNECTION_STRING`,
	`ALTIVY_POSTGRES_MIGRATOR_CONNECTION_STRING`;
- destinatário SMTP: `ALTIVY_SMTP_TO`.

O operador configura esses valores exclusivamente no arquivo externo aprovado,
sem enviá-los ao chat. Confirme os demais campos SMTP de
[compose.env.example](compose.env.example), a presença do PFX no diretório de
configuração e os callbacks OIDC da origem escolhida. Um valor interpolado não
prova que arquivo, credential, role ou serviço externo está válido.

Não substitua secrets ausentes por placeholders nem habilite Development para
aprovar este gate. Use um project name distinto para não colidir com o E2E:

```powershell
$envFile = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\.env'
docker compose --env-file $envFile -p altivy-selfhosted-validation `
	-f deploy/self-hosted/compose.yaml config --quiet
```

Resultado esperado: exit code zero sem configuração resolvida no output. O
comando não inicia serviços. Use o mesmo `-p altivy-selfhosted-validation` em
todos os comandos seguintes se essa for a instalação isolada de validação.

A partir da raiz do repositório, valide a interpolação usando o arquivo externo
sem imprimir a configuração resolvida:

```powershell
$envFile = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\.env'
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml config --quiet
```

Crie previamente os diretórios de dados e de configuração, com acesso de
escrita para os UIDs de runtime dos containers. Suba primeiro somente o banco:

```powershell
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml up -d postgres
```

No banco `altivy`, provisionado pelo serviço PostgreSQL dessa stack, crie as
roles separadas `altivy_migrator` e `altivy_runtime` e os grants conforme
[001-bootstrap-altivy-roles.sql](../../backend/libs/infrastructure/data/sql/001-bootstrap-altivy-roles.sql).
Use um processo administrativo que injete os valores de `psql` sem colocá-los
em argumentos visíveis, histórico de shell ou logs. O SQL de bootstrap existente
requer as variáveis psql `altivy_migrator_password` e
`altivy_runtime_password`; o script local de laboratório não serve para esta
composição porque aponta intencionalmente para `localhost:5432/keycloak`.

Aplique as migrations com a role exclusiva de migration:

```powershell
docker compose --env-file $envFile --profile operations -f deploy/self-hosted/compose.yaml run --rm migrator
```

Depois construa e suba a aplicação. O processo aguarda os health checks do banco,
API e gateway:

```powershell
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml up --build --wait -d
```

Acesse `https://<ALTIVY_PUBLIC_HOST>:<ALTIVY_HTTPS_PORT>`. Verifique login e
logout OIDC, leitura/escrita autorizada de vault e estado de health. Se o host
não for `localhost`, a origem exata precisa constar no client do Keycloak.

## Operação

### Identidade Local Compartilhada

Para o issuer local `https://localhost:7443/realms/altivy`, use também
[compose.local-identity.yaml](compose.local-identity.yaml). Descubra
`IDENTITY_GATEWAY_IP` no gateway em execução, sem assumir o endereço antigo:

```powershell
$env:IDENTITY_GATEWAY_IP = docker inspect --format '{{with index .NetworkSettings.Networks "identity-global"}}{{.IPAddress}}{{end}}' altivy-identity-gateway-1
docker compose --env-file $envFile -p altivy-selfhosted-production `
	-f deploy/self-hosted/compose.yaml -f deploy/self-hosted/compose.local-identity.yaml `
	up -d --build --wait
```

O override conecta somente BFF/API à rede externa da identidade e preserva o
PostgreSQL próprio. Para o ensaio local, as variáveis administrativas são
fornecidas também pelo arquivo externo da plataforma de identidade, sem copiar
secrets; passe esse `--env-file` antes do arquivo externo AltivyNotes.

API e BFF usam HTTPS interno em Production. Configure um certificado separado
com SAN `api` em `ALTIVY_API_TLS_CERTIFICATE_PATH` e
`ALTIVY_API_TLS_PRIVATE_KEY_PATH`, além da CA em `ALTIVY_TLS_CA_PATH`. O BFF não
aceita HTTP interno em Production. O migrator recebe a connection string por
ambiente, sem `--connection` nos argumentos.

Reserve um IP de gateway diferente do primeiro endereço atribuído
automaticamente aos consumidores, como no exemplo. Os tmpfs da web precisam
preservar UID/GID 101 para NGINX não-root.

O boot em 2026-10-05 passou em PostgreSQL, API, BFF, web e gateway; a sessão
anônima respondeu por HTTPS com CA validada. Login ainda não foi comprovado:
as tentativas administrativas de acrescentar callbacks de `localhost:8443`
receberam HTTP 400, e a borda compartilhada substitui o corpo do erro. O
cenário anônimo SignalR retornou 500 durante challenge/PAR. Não considere a
instalação funcionalmente concluída antes de resolver e retestar OIDC.

```powershell
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml ps
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml logs --tail=100 gateway bff api worker
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml down
```

`down` preserva os dados nos bind mounts. Não use `down --volumes` como rotina
de parada. Nunca remova `postgres/`, `vaults/` ou `bff-keys/` durante upgrade ou
rollback.

## Limites pendentes

O [runbook de recuperacao](../../docs/runbooks/recuperacao-self-hosted.md)
descreve backup coordenado e restore isolado. O ensaio de laboratorio cobre
somente o schema Altivy e o vault sintetico, nao a instalacao completa.

O fluxo de bootstrap ainda não possui uma ferramenta dedicada e testada para
provisionar roles sem expor senhas. Restore completo (todos os vaults, chaves
BFF e grants), upgrade/rollback entre versões e primeiro boot autenticado
desta composição isolada permanecem abertos no plano
[deploy-self-hosted-compose](../../docs/plan/deploy-self-hosted-compose.md).

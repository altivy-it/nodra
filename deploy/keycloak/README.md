# Laboratório Keycloak do Altivy

Este diretório inicia a stack compartilhada local com NGINX reverse proxy,
Keycloak, PostgreSQL, o realm `altivy` e o tema de login personalizado da marca
Altivy. O proxy é compartilhado por produtos e aplicações que entram na rede
`altivy-edge`.

O ambiente usa as imagens fixadas:

- `lzocateli/keycloak:26.7.0`;
- `lzocateli/postgresql:18.4-pgvector0.8.6-bookworm`.

Não use este Compose em produção. Ele executa o Keycloak em `start-dev`, usa
HTTP e usa credenciais locais no arquivo externo
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, fora do repositório.

## Pré-requisitos

- PowerShell 7;
- Docker Desktop com containers Linux;
- portas locais `8080` e `9000` livres;
- porta local `7443` livre para o `nginx-proxy` compartilhado;
- diretórios `$env:APPDATA\keycloak\postgres-data` e `$env:APPDATA\keycloak\backup` criados;
- certificado e chave TLS locais informados em `ALTIVY_E2E_TLS_CERTIFICATE_PATH` e `ALTIVY_E2E_TLS_KEY_PATH`;
- pelo menos 2 GiB de memória disponível.

Execute os comandos abaixo a partir de `deploy/keycloak`:

```powershell
Set-Location ./deploy/keycloak
```

## Configurar credenciais

Crie ou edite o arquivo local único de configuração:

```powershell
$SecretsDirectory = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes'
New-Item -ItemType Directory -Force $SecretsDirectory | Out-Null
notepad (Join-Path $SecretsDirectory '.env')
```

Variáveis compartilhadas da stack Keycloak/PostgreSQL:

- `ALTIVY_LAB_POSTGRES_PASSWORD`, obrigatória, senha do usuário `keycloak` no PostgreSQL compartilhado;
- `ALTIVY_KEYCLOAK_ADMIN_USERNAME`, opcional, usuário administrativo de bootstrap; padrão `admin`;
- `ALTIVY_KEYCLOAK_ADMIN_PASSWORD`, obrigatória, senha administrativa de bootstrap da instância compartilhada;
- `ALTIVY_KEYCLOAK_BASE_URL`, recomendada para scripts e clientes administrativos locais; padrão esperado `http://127.0.0.1:8080`;
- `ALTIVY_KEYCLOAK_MANAGEMENT_URL`, recomendada para health e métricas locais; padrão esperado `http://127.0.0.1:9000`;
- `ALTIVY_KEYCLOAK_ISSUER_URL`, recomendada para formar o issuer interno sem a borda TLS; padrão esperado `http://localhost:8080`;
- `ALTIVY_KEYCLOAK_HOSTNAME`, opcional, hostname público anunciado pelo Keycloak;
- `ALTIVY_KEYCLOAK_ADMIN_HOSTNAME`, opcional, hostname público do Admin Console;
- `ALTIVY_KEYCLOAK_REALM`, obrigatória para consumidores do produto; use `altivy` neste projeto.

Variáveis específicas do AltivyNotes:

- `ALTIVY_BFF_BASE_URL`, origem externa do BFF; padrão local `https://localhost:7443`;
- `ALTIVY_BFF_CLIENT_SECRET`, opcional para fixar o secret do client confidencial; vazio mantém o secret atual;
- `ALTIVY_FULANO_EMAIL`, opcional para criar o usuário de laboratório;
- `ALTIVY_FULANO_TEMPORARY_PASSWORD`, opcional e com mínimo de 14 caracteres;
- `ALTIVY_SMTP_*` e `ALTIVY_IDP_*`, opcionais para SMTP e provedores sociais;
- `ALTIVY_E2E_*`, opcionais para o ambiente E2E;
- `ALTIVY_INTEGRATION_*_CONNECTION_STRING`, opcionais para testes de integração locais.

Não versione o arquivo e não reutilize credenciais de outros ambientes.

Os dados e backups locais da stack compartilhada ficam fora do repositório:

```powershell
New-Item -ItemType Directory -Force "$env:APPDATA\keycloak\postgres-data" | Out-Null
New-Item -ItemType Directory -Force "$env:APPDATA\keycloak\backup" | Out-Null
```

## Gerar a customização

O script empacota o tema versionado em `.build/keycloak-theme`, que é montado
no container em modo somente leitura:

```powershell
./build-theme.ps1
```

Para também conferir os assets contra a fonte canônica da marca no repositório
irmão `Altivy`:

```powershell
./build-theme.ps1 -BrandRoot ../../../Altivy/docs/brand
```

Resultado esperado:

```text
Artefato do tema criado em .../deploy/keycloak/.build/keycloak-theme
Arquivos: 7
```

## Validar o Compose

Valide o arquivo e as variáveis do `.env` sem imprimir os valores resolvidos:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml config --quiet
```

O comando termina sem saída quando a configuração é válida.

## Criar a rede compartilhada `altivy-edge`

Este Compose e o [laboratório E2E](../e2e/README.md) declaram `altivy-edge`
como rede externa; nenhum dos dois a cria. Crie-a uma única vez antes do
primeiro `up` (o comando não falha se a rede já existir):

```powershell
if (-not (docker network ls --filter name=^altivy-edge$ --format '{{.Name}}')) {
  docker network create altivy-edge
}
```

## Iniciar os containers

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml up -d --wait
```

O Compose inicia o NGINX reverse proxy compartilhado, PostgreSQL e Keycloak. O
Keycloak aguarda o health check do PostgreSQL antes de iniciar. No primeiro boot,
o Keycloak importa
`realm/altivy-realm.json`, criando:

- realm `altivy`;
- tema de login `altivy`;
- baseline de segurança, sessões e auditoria;
- clients `altivy-lab`, `altivy-api`, `altivy-bff` e
  `altivy-authorization-spike`;
- roles e grupos iniciais do Altivy.

O serviço `nginx-proxy` publica `https://localhost:7443` e encaminha `/admin`,
`/realms` e `/resources` para o Keycloak. Quando aplicações como o E2E do
AltivyNotes estiverem na rede `altivy-edge`, o mesmo proxy também encaminha
rotas de aplicação como `/bff`, `/api/v1`, `/hubs` e `/` para seus aliases.

O laboratório usa `pg_isready` no health check do PostgreSQL. Essa verificação
barata confirma que o banco aceita conexões sem executar consultas SQL durante
o ciclo de saúde; o readiness do Keycloak continua validando a conexão real com
o banco.

Por padrão, `KC_HOSTNAME` é `http://localhost:8080` e o console fica em
`http://localhost:8080/admin/`, publicado diretamente pelo container.

Para o laboratório E2E completo (Web, BFF e API em
`https://localhost:7443`), defina `ALTIVY_KEYCLOAK_HOSTNAME` e
`ALTIVY_KEYCLOAK_ADMIN_HOSTNAME` como `https://localhost:7443` e siga
[deploy/e2e/README.md](../e2e/README.md). Nesse modo, o `nginx-proxy` encaminha
`/admin`, `/realms` e `/resources` para este container pela rede `altivy-edge`,
e o console passa a ficar em `https://localhost:7443/admin/master/console/`.
Não acesse `http://localhost:8080/admin/` nesse modo: o hostname anunciado
divergiria da origem usada pelo navegador e quebraria assets e redirects do
console. A abordagem antiga com hosts file e `keycloak.altivy.test` foi
substituída pelo `nginx-proxy` compartilhado na rede `altivy-edge` e não deve
mais ser usada.

Sem hosts ou DNS local, mantenha o Keycloak em execução e use a Admin REST pelo
loopback para reconciliar o client. Execute o script a partir da raiz do
repositório:

```powershell
.\tools\scripts\configure-keycloak.ps1 `
  -BaseUrl http://localhost:8080 `
  -BffBaseUrl https://host.containers.internal:7443 `
  -SkipUser
```

O script não imprime tokens ou secrets e só altera o secret do BFF se
`ALTIVY_BFF_CLIENT_SECRET` estiver preenchido no ambiente.

## Limite da service account da API

A service account `service-account-altivy-api` recebe somente o role
`uma_protection`. Esse role permite que o client registre recursos no próprio
Authorization Services pelo Protection API, mas não concede criação ou edição
de policies e permissions. Não atribua `realm-management/manage-clients`: o
role permite administrar todos os clients do realm e não atende ao princípio de
menor privilégio. O provisionamento automático de permissões de proprietário
permanece desabilitado até uma alternativa aprovada restringir essa operação ao
resource server `altivy-api`.

O import ocorre apenas na criação do realm. Para reconciliar URLs, secrets,
SMTP, usuário e login social pela Admin REST API, use o script a partir da raiz
do repositório:

```powershell
./tools/scripts/configure-keycloak.ps1 --help
./tools/scripts/configure-keycloak.ps1 -DryRun
./tools/scripts/configure-keycloak.ps1 -SkipUser
```

Use `-SkipUser` para reconciliar clients, scopes, roles e provedores sem criar
ou atualizar o usuário configurado em `ALTIVY_FULANO_EMAIL`.

Consulte a seção
[Automatizar JSON e Admin REST API](../../docs/configuracao-keycloak.md#65-automatizar-json-e-admin-rest-api)
para preparar o `.env`, aplicar a configuração e entender os limites.

Confira o estado dos serviços:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml ps
```

Os serviços `postgres` e `keycloak` devem aparecer como `healthy`.

## Links do laboratório

- Login customizado do realm `altivy`:
  <http://localhost:8080/realms/altivy/protocol/openid-connect/auth?client_id=altivy-lab&redirect_uri=http%3A%2F%2Flocalhost%3A8080%2F&response_type=code&scope=openid>
- Discovery OIDC do realm:
  <http://localhost:8080/realms/altivy/.well-known/openid-configuration>
- Health/readiness:
  <http://localhost:9000/health/ready>
- Console administrativo:
  <http://localhost:8080/admin/>

O console administrativo autentica inicialmente pelo realm `master`, cuja
tela permanece com o tema padrão do Keycloak. Para validar a customização,
use o link de login do realm `altivy`.

## Logs

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml logs -f keycloak
```

Para consultar somente as últimas linhas:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml logs --tail 100 keycloak
```

## Exportar e restaurar o realm

O export offline deve ser feito com o Keycloak parado, para manter consistência
com o PostgreSQL. Backups locais devem ficar fora do repositório, em:

```text
$env:APPDATA/keycloak/backup/altivy-realm.json
$env:APPDATA/keycloak/backup/altivy-users-0.json
```

Esses arquivos não devem ser publicados. O export atual inclui o client `altivy-authorization-spike`, o
grupo `/altivy/readers`, o recurso `vault-lab-0001`, a policy e a permission
UMA do spike.

Para gerar um novo export:

```powershell
Set-Location ./deploy/keycloak
$BackupRoot = Join-Path $env:APPDATA 'keycloak/backup'
New-Item -ItemType Directory -Force $BackupRoot | Out-Null
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml stop keycloak
docker run --rm `
  --network altivy-keycloak-lab_keycloak-internal `
  -e KC_DB=postgres `
  -e KC_DB_URL=jdbc:postgresql://postgres:5432/keycloak `
  -e KC_DB_USERNAME=keycloak `
  -e KC_DB_PASSWORD=$env:ALTIVY_LAB_POSTGRES_PASSWORD `
  -v "${BackupRoot}:/tmp/export" `
  lzocateli/keycloak:26.7.0 export --dir /tmp/export --realm altivy --users same_file
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml up -d --wait keycloak
```

Para restaurar em um laboratório vazio, coloque os arquivos exportados em uma
pasta temporária montada em `/opt/keycloak/data/import`, execute o import com o
Keycloak parado e só então suba o Compose. A restauração substitui o estado do
realm; valide o backup em ambiente limpo antes de qualquer uso operacional.

## Parar o laboratório

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml down
```

Esse comando preserva os dados do PostgreSQL em
`$env:APPDATA\keycloak\postgres-data`.

## Recriar o realm do zero

O import usa a estratégia `IGNORE_EXISTING`. Alterar
`realm/altivy-realm.json` não sobrescreve um realm que já exista no banco.
Para apagar todos os dados locais e executar novamente o import:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml down
Remove-Item "$env:APPDATA\keycloak\postgres-data" -Recurse -Force
./build-theme.ps1
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f ./docker-compose-keycloak.yaml up -d --wait
```

Essa operação remove usuários, sessões e qualquer configuração criada no
laboratório. Não execute esse procedimento em ambientes reais.

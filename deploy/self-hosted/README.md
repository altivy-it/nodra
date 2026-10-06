# Instalação self-hosted

Esta composição executa o Altivy Notes no host escolhido pelo operador. Ela inclui gateway TLS, web, BFF, API, worker e PostgreSQL; publica somente o gateway. O Keycloak continua sendo a autoridade externa obrigatória e deve ser fornecido pelo operador, localmente ou em uma rede alcançável.

O fluxo de instalação versionado já inicia a stack Production e aguarda os health checks. Isso não certifica uma release: recuperação integral, upgrade e rollback entre versões continuam sendo gates separados no [plano](../../docs/plan/deploy-self-hosted-compose.md).

## Requisitos

- Docker Engine ou Docker Desktop com Compose v2 e containers Linux;
- espaço persistente para PostgreSQL, vault e chaves de Data Protection;
- Keycloak com realm `altivy`, client confidencial `altivy-bff` e Authorization Services configurados;
- URL do issuer Keycloak alcançável tanto pelo navegador quanto pelos containers, com certificado TLS confiável;
- certificado TLS cujo SAN corresponde ao host público escolhido e chave privada correspondente;
- certificado PKCS#12 dedicado à proteção das chaves do BFF;
- conta SMTP operacional para alertas do worker.

## Primeiro boot reproduzível

No Windows, mantenha os valores sensíveis em
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`; o sincronizador de OIDC
usa também `$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env`. Em outro
host, passe ambos por `-EnvironmentFile` e `-IdentityEnvironmentFile` usando
paths externos protegidos. O PostgreSQL é próprio desta instalação; as
connection strings runtime e migrator podem partir dos aliases de integração,
mas o script ajusta host e database em memória e valida os nomes das roles.

Para uma autoridade local já instalada e confiável, gere certificados em um
diretório externo vazio. A CA precisa existir e já ser confiada pelo host; a
ferramenta não instala nem substitui uma CA:

```powershell
$certificates = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\certs-prd'
$authority = (mkcert -CAROOT).Trim()
pwsh -NoProfile -File tools/scripts/new-local-production-certificates.ps1 `
	-CertificateDirectory $certificates -AuthorityDirectory $authority
```

O emissor usa OpenSSL em container, recusa sobrescrever qualquer destino e
gera certificados localhost/API e PFX dedicado. Para validar arquivos já
existentes sem alterá-los, execute o mesmo script com `-ValidateOnly`. A CA
local não é adequada a acesso público; nesse caso, forneça certificados e CA
emitidos para o hostname público.

Com a plataforma de identidade compartilhada já online, execute na raiz do
AltivyNotes:

```powershell
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 `
	-IdentityEnvironmentFile "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" `
	-LocalIdentity -SyncOidc
```

O comando valida certificados e Compose antes de tocar nos serviços; depois
prepara diretórios/ACLs, inicia PostgreSQL, captura snapshot pré-migration de
database, vaults e keyring com SHA-256, provisiona roles por stdin, aplica
migrations, sincroniza callbacks sem remover os existentes, constrói e aguarda
os serviços. O PFX fica fora do archive e requer backup externo protegido.
`-ValidateOnly` executa somente os preflights;
`-NoBuild` reutiliza imagens existentes; `-RecreateConsumers` força a
recriação de web/BFF/API/worker/gateway sem recriar o PostgreSQL. Para outro
host, passe explicitamente `-EnvironmentFile`, `-CertificateDirectory`,
`-DataRoot` e, se necessário,
`-IdentityScript` apontando para
`architecture-knowledge-base/platforms/identity/scripts/sync-product-oidc.ps1`.
Os artefatos compartilhados de Keycloak, PostgreSQL e NGINX pertencem somente
ao repositório `architecture-knowledge-base`.

`-ValidationVaultId lab-write-e2e` é opcional e destinado a um ensaio sintético.
Cria somente a projeção e o diretório se ainda não existirem; falha diante de
colisão com outro vault e nunca cria grants. A autorização precisa já existir
no Keycloak. Não use essa opção como seed de uma instalação de usuário.

## Imagens de release

Para instalar imagens publicadas, baixe `images.env` e
`images.env.sha256` da release oficial do GitHub para um diretório externo. Não
edite o manifesto: o inicializador confere o checksum, exige exatamente as
cinco referências Altivy com `@sha256` e configura pull obrigatório do GHCR.
Docker precisa estar autenticado para ler os pacotes; use o credential helper
aprovado do host, nunca passe tokens como argumentos.

```powershell
$releaseManifest = 'C:\AltivyNotes\Releases\v1.2.3\images.env'
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 `
	-IdentityEnvironmentFile "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" `
	-LocalIdentity -SyncOidc -ReleaseImageManifest $releaseManifest -NoBuild
```

`-ReleaseImageManifest` exige `-NoBuild`; falha fechada se checksum, nomes ou
digests forem inválidos. As imagens são baixadas antes do backup e de qualquer
parada de writer; falha de acesso ao GHCR não interrompe a stack ativa. Sem essa
opção, o fluxo continua construindo e usando imagens `:local`. Cada snapshot
pré-migration registra o ID da imagem
PostgreSQL. Para rollback de aplicação, selecione o manifesto da release
anterior somente após confirmar compatibilidade com o schema atual; o script
nunca executa migrations `Down`. Se não houver compatibilidade expand-contract,
recupere o backup em destino limpo conforme o [runbook](../../docs/runbooks/recuperacao-self-hosted.md).

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

O preflight valida a existência e a integridade criptográfica da cadeia TLS,
SANs, correspondência das chaves e senha/validade do PFX antes de iniciar.
Configure todos os valores necessários no arquivo externo; mensagens do script
são sanitizadas e não imprimem a configuração resolvida.

`ALTIVY_BIND_ADDRESS` limita a publicação ao localhost por padrão. Para acesso
pela LAN, configure o endereço de interface pretendido e um certificado com SAN
compatível; não publique o PostgreSQL, API ou BFF. `ALTIVY_PROXY_BFF_SUBNET` e
`ALTIVY_GATEWAY_IP` têm valores de exemplo no arquivo de configuração; escolha
uma sub-rede privada sem conflito com as redes Docker existentes e um IP nela.

No realm `altivy`, o script de sincronização mantido pela plataforma
`architecture-knowledge-base` acrescenta ao client `altivy-bff` sem remover
valores existentes:

- redirect URI: `<ALTIVY_PUBLIC_ORIGIN>/signin-oidc`;
- post-logout redirect URI: `<ALTIVY_PUBLIC_ORIGIN>/signout-callback-oidc`;
- web origin: `<ALTIVY_PUBLIC_ORIGIN>`.

`ALTIVY_KEYCLOAK_AUTHORITY` precisa usar exatamente o issuer anunciado no
Discovery Document. Navegador e containers devem alcançar o mesmo issuer e
validar sua cadeia TLS. Não habilite HTTP inseguro em Production.

## Diagnóstico manual

O procedimento a seguir é referência para diagnóstico de baixo nível. Para
instalações novas e upgrades, use o script reproduzível acima, que coordena
backup, migrations, segredos e health checks.

### Referência de configuração manual

A configuração E2E não configura automaticamente esta composição Production.
Um preflight histórico de 2026-10-04 identificou valores ausentes nos grupos
abaixo. Esse inventário é evidência histórica, não o estado atual da instalação:

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

O boot Production foi repetido pelo script versionado: PostgreSQL, API, BFF,
web, worker e gateway ficaram healthy, a sessão anônima respondeu por HTTPS
com CA validada e o sincronizador confirmou callbacks OIDC. A borda de
identidade mantém respostas JSON nativas do OAuth. Testes do BFF garantem que
challenges anônimos de API/SignalR retornem 401 em vez de redirecionar para
login interativo. Consulte no plano quais jornadas browser e gates de
recuperação ainda aguardam evidência.

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

Restore integral de todos os vaults, chaves BFF e projeções, além de
upgrade/rollback entre versões, permanece aberto no plano
[deploy-self-hosted-compose](../../docs/plan/deploy-self-hosted-compose.md).
O fluxo de primeiro boot e o bootstrap seguro das roles agora são executados
por `tools/scripts/start-self-hosted.ps1`.

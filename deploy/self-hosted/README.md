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

O certificado e a chave TLS são montados read-only no gateway. A chave precisa
ser legível pelo UID 101 do NGINX unprivileged; conceda somente essa leitura por
ACL/grupo no host, sem torná-la globalmente legível. O PFX do BFF deve ser
readable pelo UID do BFF e continua separado da chave TLS do gateway.

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

```powershell
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml ps
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml logs --tail=100 gateway bff api worker
docker compose --env-file $envFile -f deploy/self-hosted/compose.yaml down
```

`down` preserva os dados nos bind mounts. Não use `down --volumes` como rotina
de parada. Nunca remova `postgres/`, `vaults/` ou `bff-keys/` durante upgrade ou
rollback.

## Limites pendentes

O fluxo de bootstrap ainda não possui uma ferramenta dedicada e testada para
provisionar roles sem expor senhas. Backup coordenado de vault + PostgreSQL,
teste de restore, upgrade/rollback de versão e um smoke test com Keycloak real
são etapas abertas do plano [deploy-self-hosted-compose](../../docs/plan/deploy-self-hosted-compose.md).

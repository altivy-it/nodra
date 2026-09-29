# Runbook: Deploy de produção em VPS

## Objetivo e limites

Operar `https://notes.altivy.it` em uma VPS dedicada com NGINX, PostgreSQL e
Keycloak já existentes. O Keycloak compartilhado permanece em
`https://credential.altivy.it`.

O deploy da aplicação altera somente os serviços `web`, `bff`, `api`, `worker`
e `migrator` do projeto Compose `altivynotes`. O workflow de infraestrutura
altera somente o virtual host Altivy e entrega o SQL bootstrap. Nenhum workflow
reinicia o NGINX/PostgreSQL, executa SQL administrativo, altera Keycloak, toca
em `/userapps/var/` compartilhado ou promove produção automaticamente em push.

## Pré-requisitos do host

O operador provisiona os diretórios uma vez, fora dos workflows:

| Caminho | Uso |
| --- | --- |
| `/userapps/altivynotes/` | Compose, scripts, manifestos de release e SQL entregue |
| `/userapps/altivynotes/releases/` | arquivos de cada release imutável |
| `/userapps/altivynotes/scripts/` | operador remoto instalado pelo workflow |
| `/userapps/configs/altivynotes/compose.env` | paths, redes e nomes dos containers compartilhados |
| `/userapps/configs/altivynotes/bff.env` | OIDC, sessão e proxies confiáveis do BFF |
| `/userapps/configs/altivynotes/altivy-data-protection.pfx` | certificado dedicado para proteger as chaves de sessão do BFF |
| `/userapps/configs/altivynotes/api.env` | connection string runtime e configuração Keycloak/API |
| `/userapps/configs/altivynotes/worker.env` | connection string runtime e configuração do worker |
| `/userapps/configs/altivynotes/migrator.env` | connection string da role `altivy_migrator` |
| `/userapps/configs/nginx/altivynotes/` | virtual host `notes.altivy.it` |
| `/userapps/configs/keycloak/altivynotes/` | arquivos de configuração/export do realm, se mantidos pelo operador |
| `/userapps/ssl/altivy.it.pem` e `/userapps/ssl/altivy.it.key` | certificado/chave já usados pelo NGINX |
| `/userapps/var/altivynotes/vaults/` | Markdown e anexos persistentes |
| `/userapps/var/altivynotes/bff-keys/` | chaves persistentes de Data Protection |

Use `umask 077`; arquivos de runtime devem ser legíveis somente pelo operador
de deploy e pelo daemon/container que precisa deles. Vaults e chaves BFF devem
ser graváveis pelo UID de runtime dos containers. Não copie secrets para o
repositório nem para os artifacts do GitHub.

O certificado PFX de Data Protection é distinto do certificado TLS do NGINX.
Crie-o e distribua-o por canal operacional seguro; monte-o read-only e dê
leitura somente ao UID/grupo de runtime do BFF. A senha fica em `bff.env` e
nunca deve aparecer em argumentos, logs, Compose versionado ou artifacts.

### Docker e rede

- Instale Docker Engine e Docker Compose v2 no host.
- Crie `compose.env` com `ALTIVY_DATA_ROOT=/userapps/var/altivynotes`,
  `ALTIVY_CONFIG_ROOT=/userapps/configs/altivynotes`, os nomes reais das redes
  externas `ALTIVY_EDGE_NETWORK`/`ALTIVY_POSTGRES_NETWORK`, e os nomes reais
  `NGINX_CONTAINER`/`POSTGRES_CONTAINER`.
- O NGINX precisa estar conectado à rede edge e conseguir resolver os aliases
  `altivynotes-web` e `altivynotes-bff`. API, worker e migrator precisam alcançar
  o PostgreSQL pela rede externa.
- Defina `ALTIVY_KEYCLOAK_PROXY_IP` com o IPv4 do container NGINX na rede edge.
  BFF e API resolvem `credential.altivy.it` para esse IP interno, preservando
  SNI, certificado e issuer públicos sem hairpin pelo DNS externo.
- O NGINX container precisa ler os certificados nos paths
  `/userapps/ssl/altivy.it.pem` e `/userapps/ssl/altivy.it.key`.
- A configuração global do NGINX deve incluir os arquivos
  `/userapps/configs/nginx/altivynotes/*.conf`. O workflow falha sem reload se
  `nginx -T` não mostrar `server_name notes.altivy.it;`.
- Preencha `Security__TrustedProxies__*` e `Security__TrustedNetworks__*` do BFF
  com o IP/subnet real do NGINX na rede edge; não confie em redes genéricas.

## PostgreSQL e Keycloak

Antes do primeiro deploy:

1. No PostgreSQL existente, crie database dedicado `altivy`; não reutilize o
   database `keycloak` em produção.
2. Aplique `001-bootstrap-altivy-roles.sql` ao database `altivy` por processo
   administrativo do operador. Forneça valores psql sem eco e fora dos logs;
   não os passe como argumentos visíveis ou arquivo versionado.
3. Configure `altivy_runtime` para API/worker e `altivy_migrator` somente para
   migrations. Ambas são roles sem superuser; runtime não cria schema, role ou
   database.
4. Configure os env files externos com
   `ConnectionStrings__AltivyPostgres`. API/worker usam runtime; migrator usa
   migration. Configure `Keycloak__Authority` e
   `Authentication__Keycloak__Authority` para
   `https://credential.altivy.it/realms/altivy`.
5. No realm `altivy`, cadastre callbacks BFF em
   `https://notes.altivy.it/signin-oidc` e
   `https://notes.altivy.it/signout-callback-oidc`, além do web origin
   `https://notes.altivy.it`. Use o client confidencial `altivy-bff` e guarde
   seu secret somente em `bff.env` externo.
6. Configure `bff.env` com `Security__DataProtectionKeysPath=/keys`,
  `Security__DataProtectionCertificatePath=/certs/altivy-data-protection.pfx`,
  `Security__DataProtectionCertificatePassword`,
  `Security__AllowedOrigins__0=https://notes.altivy.it` e proxies/redes
  confiáveis explícitos. O client precisa usar os callbacks locais
  `/signin-oidc` e `/signout-callback-oidc`.
7. Configure `api.env` com `VaultStorage__Root=/vaults`,
  `Keycloak__Authority`, `Keycloak__Audience=altivy-api` e a connection string
  runtime. Configure `worker.env` com `VaultStorage__Root=/vaults`, a mesma
  connection string runtime e as opções obrigatórias `ResidueAlerts__Smtp__Host`,
  `Port`, `Username`, `Password`, `From` e `To`.

O workflow não altera `/userapps/configs/keycloak/` nem o realm compartilhado.
O operador mantém ali exports/configuração sem secrets e aplica as alterações
de client/callback no Keycloak por procedimento administrativo separado.

O arquivo SQL entregue pelo workflow não é executado automaticamente. As
migrations são um bundle da versão publicada e rodam somente no deploy de
backend, antes de substituir API/BFF/worker. Não aplique downgrade automático
em rollback.

## GitHub

Configure uma GitHub App instalada somente neste repositório:

- `Contents: write` e `Pull requests: write`, sem permissões administrativas;
- repository variable `RELEASE_APP_ID` e repository secret
  `RELEASE_APP_PRIVATE_KEY`;
- habilite a App no repositório para que release PRs e tags criados por ela
  disparem os workflows normais.

Crie o Environment `production`, restrinja branches/tags autorizadas e exija
revisor humano. Configure nele:

- variables: `VPS_HOST`, `VPS_PORT`, `VPS_USER`, `VPS_KNOWN_HOSTS`;
- secret: `VPS_SSH_PRIVATE_KEY`, chave dedicada sem passphrase, rotacionável e
  restrita ao host;
- repository variable `ALTIVY_VPS_PLATFORM` com `linux/amd64` ou plataforma
  efetiva da VPS;
- habilite publicação/consulta de pacotes GHCR pelo `GITHUB_TOKEN` no
  repositório.

`VPS_KNOWN_HOSTS` deve conter a chave pública do host coletada e conferida por
canal confiável; o workflow não usa `ssh-keyscan` nem desativa
`StrictHostKeyChecking`. Não use agent forwarding. O usuário SSH precisa gravar
somente os caminhos do Altivy e executar Docker Compose. Acesso ao Docker daemon
é equivalente a root no host e, nesta VPS, pode afetar containers compartilhados;
proteja o Environment, limite os revisores e trate a chave como credencial
privilegiada.

## Releases e deploy da aplicação

1. Merge de Conventional Commits em `main` passa pelo workflow `CI`.
2. Após CI verde, `Release` abre/atualiza PR com SemVer e `CHANGELOG.md`.
3. Um humano revisa e faz merge da PR; o workflow cria a tag `vX.Y.Z`, constrói
   imagens, gera SBOM/scan/proveniência e anexa `images.env` com digests à
   GitHub Release.
4. No primeiro deploy, execute `Deploy application` com `deploy`, a tag
  publicada e `all`. Depois do bootstrap, escolha `frontend`, `backend` ou
  `all` conforme a promoção desejada. O Environment exige aprovação.
5. `frontend` troca somente web. `backend` aplica migrations e troca API/BFF/
   worker. `all` faz ambos.
6. Confirme `https://notes.altivy.it/bff/session` sem desabilitar validação TLS;
   a resposta anônima esperada contém `"authenticated":false`.

Se a GitHub Release foi criada mas a publicação de imagens falhou, execute
`Release` manualmente a partir de `main`, informando a mesma tag. O workflow
valida que a release existe, que seu commit está integrado em `main` e que há
CI verde para aquele commit; reutiliza os candidatos por commit, reexecuta os
scans e não substitui digests SemVer já publicados por conteúdo diferente.

O workflow de `rollback` restaura a composição e as imagens do deploy anterior.
O rollback não desfaz mudanças do schema, arquivos do vault ou alterações no
Keycloak. `deploy-shared-infra.yml` opera NGINX e a entrega do SQL em workflow
separado, também protegido pelo Environment `production`.

Antes de qualquer migration de backend, o operador remoto grava um dump lógico
custom-format do database `altivy` e um tar gzip do vault em
`/userapps/var/altivynotes/backups`, com diretório `0700` e arquivos `0600`.
Para manter os dois artefatos coerentes, BFF, API e worker são parados
graciosamente durante o backup e permanecem parados até concluir a migration e
subir o backend novo; isso causa uma breve indisponibilidade do backend.
Se qualquer backup falhar, nenhuma migration ou promoção ocorre. Os backups
ficam exclusivamente na VPS e não são enviados ao GitHub. Após cada backup
completo, artefatos Altivy anteriores a `ALTIVY_BACKUP_RETENTION_DAYS` (padrão
30 dias) são removidos; somente os nomes de dump e tar criados pelo operador
são elegíveis. Ajuste essa variável em
`/userapps/configs/altivynotes/compose.env` conforme a política do host.

## Falhas e recuperação

- **SSH/host key falha:** não aceite chave nova automaticamente; compare a
  fingerprint fora do GitHub Actions e atualize `VPS_KNOWN_HOSTS`.
- **Rede externa ausente:** crie/identifique a rede compartilhada correta e
  corrija `compose.env`; não conecte containers à rede host.
- **`nginx -t` falha:** configuração anterior é restaurada e não ocorre reload.
  Verifique include, sintaxe e leitura do certificado antes de reexecutar.
- **Workflow NGINX não encontra `server_name`:** a pasta não está incluída na
  configuração global. Ajuste o include global por procedimento operacional
  aprovado, fora deste workflow, e execute `nginx -t` antes de reexecutar.
- **Bootstrap PostgreSQL falha:** o workflow apenas entrega o SQL; corrija o
  provisionamento com o DBA/operador. Não reinicie o container nem edite o
  bind mount de dados.
- **Migration falha:** deploy para antes da promoção de backend. Preserve os
  logs sanitizados e faça correção forward; não use `EnsureCreated` nem
  downgrade automático. O dump PostgreSQL e o arquivo do vault da execução
  permanecem em `backups/`; restaure-os somente por procedimento aprovado.
- **Smoke falha após deploy:** o operador remoto restaura as imagens e Compose
  anteriores. Confirme health checks, logs sem secrets, Keycloak e PostgreSQL;
  se necessário execute `rollback` manual pelo workflow protegido.
- **Restore de dados:** siga o processo de backup/restore PostgreSQL e vault
  do host. O primeiro release estável exige teste de restauração em ambiente
  limpo e janela de recuperação aprovada.
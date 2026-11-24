# Pesquisa: Laboratório local com Podman

## Topologia

**Decisão**: Preservar duas stacks, conforme a
[ADR 2026-0008](../../docs/adr/2026-0008-deploy-self-hosted-compose.md).

**Justificativa**: A composição self-hosted e o laboratório E2E dependem de
`altivy-identity`. A composição local de identidade do produto está obsoleta.

**Alternativas consideradas**: Duplicar infraestrutura no produto contraria a
ADR aceita. Substituir identidade por mocks não qualifica funcionalidades reais.

## Engine e provider

**Decisão**: Propor Podman com Docker Compose v2 como provider.

**Justificativa**: O engine disponível é Podman 5.8.2. O provider
`podman-compose 1.5.0` não oferece `config --no-interpolate --format json`,
`--progress plain` ou `up --wait`, consumidos pelo inicializador existente.
As saídas de ajuda do provider foram verificadas.

**Alternativas consideradas**: Reimplementar parsing de configuração, espera
e bootstrap em um script paralelo aumenta risco e escopo. A instalação de um
provider depende de verificar sua versão e licença exatas; não foi realizada.

**Atualização — 2026-11-23**: Provider instalado fora do `PATH`, em
`~/.local/lib/nodra-lab/docker-compose`, e selecionado por
`PODMAN_COMPOSE_PROVIDER`: Docker Compose `v5.8.0` (binário oficial
`docker/compose`, licença Apache-2.0), SHA-256
`b8840f5008c447f41fce0a57ae3adeab42c89b1eb6e6012524bcabbfb4ffbaf6`.
Nenhuma dependência da aplicação foi alterada.

## Builds existentes — 2026-11-23

**Decisão**: Não alterar Dockerfiles sem falha demonstrada.

**Justificativa**: Os cinco builds atuais terminaram com código zero:

| Componente | Imagem local de qualificação |
|---|---|
| API | `localhost/nodra-api:lab-012` |
| BFF | `localhost/nodra-bff:lab-012` |
| Worker | `localhost/nodra-worker:lab-012` |
| Migrations | `localhost/nodra-migrations:lab-012` |
| Web | `localhost/nodra-web:lab-012` |

Os builds foram serializados, usando `--jobs=1 --memory=3g --cpu-period=100000
--cpu-quota=200000`, sobre o checkout atual, sem alterar código. Houve uso de
cache; isso não comprova build limpo, readiness ou runtime saudável.
A tentativa inicial com `--cpus=2` foi rejeitada e substituída pelos parâmetros
de quota suportados. O primeiro build web usou a base padrão `/`.

**Alternativas consideradas**: “Podmanfiles” duplicados não são necessários:
Podman já constrói os Dockerfiles existentes.

## Preservação dos healthchecks

**Decisão**: Usar `--format=docker` para os builds locais.

**Justificativa**: O formato OCI padrão descartou os `HEALTHCHECK`s; a inspeção
das imagens iniciais retornou `null`. Os cinco builds foram repetidos em formato
Docker e terminaram com código zero. A web usou `WEB_BASE_HREF=/notes/`.

| Imagem final disponível | Usuário | Healthcheck definido |
|---|---|---|
| `localhost/nodra-api:lab-012-docker` | `1654` | `/health/live` |
| `localhost/nodra-bff:lab-012-docker` | `1654` | `/health/ready` |
| `localhost/nodra-worker:lab-012-docker` | `1654` | Não definido no Dockerfile |
| `localhost/nodra-migrations:lab-012-docker` | `1654` | Não definido; processo transitório |
| `localhost/nodra-web:lab-012-docker` | `nginx` | `/` |

Entrypoints inspecionados: DLLs correspondentes para API/BFF/worker,
`/app/efbundle` para migrations e `/docker-entrypoint.sh` para web. Nenhum
healthcheck foi executado contra uma aplicação em funcionamento neste ensaio.

**Alternativas consideradas**: Definir healthchecks no Compose poderia compensar
o formato OCI, mas duplicaria a configuração já declarada nos Dockerfiles.

## Pré-requisitos externos

**Decisão**: Não iniciar serviços nem acessar dados sem preparação externa.

**Justificativa**: Não há containers ou redes `altivy-identity` neste engine.
O checkout do AKB existe, mas seus scripts locais pressupõem Windows e
configuração externa. O operador não indicou um arquivo externo.
Nenhum secret foi lido, criado ou alterado.

**Alternativas consideradas**: Inventar credenciais ou usar dados e serviços
de outro projeto foi rejeitado.

**Atualização — 2026-11-23**: A identidade é iniciada a partir do checkout
irmão do AKB, sem alterá-lo, por `tools/scripts/start-local-lab.ps1`. Secrets
sintéticos são gerados uma única vez em `~/.local/share/nodra-lab/secrets`
(`0600`), fora do repositório. Dois ajustes locais e versionados no Nodra
foram necessários para o Podman rootless:

- `deploy/local-lab/identity.podman.compose.yaml` fixa a sub-rede de
  `identity-global` e monta `deploy/local-lab/gateway-nginx.podman.conf`,
  porque o `resolver` do gateway AKB aponta para o DNS embutido do Docker
  (`127.0.0.11`), inexistente no Podman.
- `deploy/self-hosted/compose.podman.yaml` troca `uid=`/`gid=` do tmpfs do web
  pela opção `U` e remove `noexec` de `/var/run`, que no nginx Debian aponta
  para `/run`, onde o Podman monta `/run/podman-init` (`init: true`).

## Defeitos encontrados na qualificação real

| Defeito | Correção |
|---------|----------|
| PostgreSQL rootless sem permissão no bind mount | `podman unshare chown 999:999` no laboratório |
| `SSL_CERT_FILE` ignorado pelo .NET já iniciado | Scripts Keycloak executados em `pwsh` filho |
| `configure-keycloak.ps1` perdia o status 404 e não criava o realm | Status preservado em `Exception.Data['StatusCode']` |
| Service account do provisioner sem roles de `realm-management` | `manage-clients`, `manage-users` e `manage-authorization` no bootstrap |
| `provision-vault-owner-authorization.ps1` exigia `APPDATA` | Arquivo local opcional fora do Windows |
| Backup pré-migration falhava com vaults vazios (`sha256sum --check` de manifesto vazio) | Verificação somente com manifesto não vazio; afeta também Docker |
| Saída de `compose up` ocultada em falhas não sensíveis | Exibida somente em etapas já não silenciosas |
| Recriar só o gateway mudava seu IP dinâmico; BFF/API mantinham `localhost` no IP antigo via `extra_hosts`, readiness falhava e `/notes/bff/login` retornava 500 | Gateway fixado em `10.89.250.250` no override `identity.podman.compose.yaml`; E2E 11/11 e login `302` após reiniciar o gateway |

## Defeito externo corrigido (AKB)

O gateway `platforms/identity/gateway/conf.d/identity.conf` usa
`proxy_intercept_errors on` no `server`, e `custom-error.locations` define
`error_page` para todos os 4xx. A rota `/notes/(bff|api/v1|hubs|…)` não
declara `proxy_intercept_errors off`, ao contrário da rota do Dokpod (commit
AKB `fcb16ee`). Assim, respostas `application/problem+json` do Nodra chegam ao
navegador como HTML (ex.: `401 text/html` anônimo em `/notes/api/v1/vaults`).
O E2E `test_denied_vault_is_not_exposed_through_bff_proxy` falhava por isso.
Por solicitação explícita do operador, a correção foi aplicada no repositório
AKB (não neste repositório): `proxy_intercept_errors off;` na location do BFF do
Nodra. Após `nginx -s reload`, o `401` anônimo chega sem página HTML e o E2E
passou 11/11.

## Gate humano

A confirmação de revisão do plano e tarefas não foi obtida: o formulário
retornou usuário indisponível. Isso não constitui aprovação.

**Atualização — 2026-11-23**: O operador solicitou explicitamente a execução:
"corrija o que for necessario para que a stack funcione, crie ou ajuste o
dockerfile (podmanfile) para que eu possa subir a stack completa na minha
maquina local". A solicitação autoriza a implementação do recorte; a revisão
humana dos artefatos e do diff continua pendente para o pull request.

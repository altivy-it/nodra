# Validação inicial: Laboratório local com Podman

**Estado**: Implementado e qualificado localmente em 2026-11-23 (Podman
5.8.2 rootless, Docker Compose v5.8.0 como provider). A falha E2E causada pelo
gateway do AKB foi corrigida no AKB; ver [research.md](research.md#defeito-externo-corrigido-akb).

## Pré-requisitos

- Confirmar Linux com Podman rootless ou indicar a máquina alvo.
- Revisar [spec.md](spec.md), [plan.md](plan.md) e as tarefas.
- Preparar `altivy-identity` pelo repositório proprietário, sem copiar seu
  Compose para o Nodra.
- Configurar realm, clients, roles e grants pelo operador.
- Indicar arquivo externo de configuração, certificados e diretório de dados.
- Para o fluxo proposto, disponibilizar Docker Compose v2 como provider;
  `podman-compose 1.5.0` não atende ao inicializador.

Nunca enviar credenciais pelo chat nem criar `.env` no repositório.

## Builds já executáveis

Na raiz do Nodra, os comandos abaixo constroem imagens locais, sem iniciar a
aplicação nem publicar artefatos. `/notes/` é a base do ambiente self-hosted.

```bash
podman build --format=docker --jobs=1 --memory=3g --cpu-period=100000 --cpu-quota=200000 \
  -t localhost/nodra-api:lab-012-docker -f backend/apps/api/Dockerfile .
podman build --format=docker --jobs=1 --memory=3g --cpu-period=100000 --cpu-quota=200000 \
  -t localhost/nodra-bff:lab-012-docker -f backend/apps/bff/Dockerfile .
podman build --format=docker --jobs=1 --memory=3g --cpu-period=100000 --cpu-quota=200000 \
  -t localhost/nodra-worker:lab-012-docker -f backend/apps/worker/Dockerfile .
podman build --format=docker --jobs=1 --memory=3g --cpu-period=100000 --cpu-quota=200000 \
  -t localhost/nodra-migrations:lab-012-docker -f deploy/production/migrations.Dockerfile .
podman build --format=docker --jobs=1 --memory=3g --cpu-period=100000 --cpu-quota=200000 \
  --build-arg WEB_BASE_HREF=/notes/ \
  -t localhost/nodra-web:lab-012-docker -f frontend/web/Dockerfile .
```

Builds com cache de API, BFF, worker, migrations e web passaram em 2026-11-23.
O primeiro build web usou a base padrão `/`; outro build com `/notes/` passou.
O formato OCI padrão descartou os healthchecks; os cinco builds foram repetidos
com `--format=docker` e as imagens finais acima ficaram disponíveis. Healthchecks
de API, BFF e web, usuários não root e entrypoints foram inspecionados.
Builds não confirmam readiness, login ou persistência.

## Subida do laboratório

Na raiz do Nodra (detalhes em
[deploy/self-hosted/README.md](../../deploy/self-hosted/README.md#laboratorio-local-com-podman-linux)):

```bash
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -Build -RunE2E
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -RecreateConsumers -RunE2E
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -Stop
```

A aplicação fica em `https://localhost:7443/notes`; a CA está em
`~/.local/share/nodra-lab/certs/rootCA.pem` e o usuário sintético em
`~/.local/share/nodra-lab/secrets/lab-user.env` (nunca exibido).

## Evidência sanitizada — 2026-11-23

| Verificação | Resultado |
|-------------|-----------|
| `test-local-container-engine.ps1` | 11 verificações aprovadas; contra o `start-self-hosted.ps1` de `HEAD`, 6 falham |
| Identidade AKB (`gateway`, `keycloak`, `postgres`) | `healthy` em `https://localhost:7443` |
| Backup pré-migration e migrations | Backup verificado; 12 migrations aplicadas no primeiro boot, idempotentes depois |
| `web`, `bff`, `api` | `healthy`; `worker` em execução; API e BFF sem portas no host |
| Smoke anônimo `GET /notes/bff/session` pela borda | `{"authenticated":false,...}` |
| `-Build` (cinco imagens `--format docker`) + E2E | 10 aprovados, 1 falha (AKB, antes da correção) |
| `-RunE2E` após `proxy_intercept_errors off` no AKB | 11 aprovados |
| `-RecreateConsumers -RunE2E` | Nota `autosave-*.md` gravada antes foi lida após recriação |
| `-Stop` seguido de nova subida | Containers removidos, dados preservados; nova subida em ~90 s, nota persistida |

Testes E2E aprovados: tokens ausentes no navegador anônimo, SignalR negado ao
anônimo e negociado autenticado, acesso a vault autorizado, criação e autosave
com ETag/If-Match, leitura após recriação, logout, catálogo somente com vaults
autorizados, troca de vault sem reload e recarga preservando vault e módulo.
`test_denied_vault_is_not_exposed_through_bff_proxy` recebia `403` com
`text/html` pela interceptação de erros do gateway AKB; após a correção no AKB,
aprovado (11/11).

## Qualificação posterior à implementação

Seguir o [contrato de operação](contracts/operacao-local.md) e o
[guia self-hosted](../../deploy/self-hosted/README.md):

1. Validar provider e preflight sem alterar serviços.
2. Confirmar plataforma externa saudável e borda HTTPS.
3. Construir ou selecionar explicitamente as cinco imagens locais.
4. Executar backup e migrations antes da subida dos consumidores.
5. Verificar sessão anônima em `https://localhost:7443/notes/bff/session`.
6. Autenticar, criar e editar uma nota; recriar consumidores e reabri-la.
7. Parar somente o Nodra, preservando dados e identidade.

Não executar o inicializador com `podman-compose 1.5.0`: ele é recusado antes
de qualquer configuração.

## Limitações

- Somente Linux com Podman rootless foi qualificado; o laboratório depende do
  checkout irmão `architecture-knowledge-base`.
- Logs dos containers usam journald no host qualificado; `podman logs` pode
  vir vazio.
- O `worker` não possui healthcheck; a prontidão é inferida pela execução.
- Em VS Code Remote SSH, a porta `7443` precisa ser encaminhada para a mesma
  porta local; ver [README self-hosted](../../deploy/self-hosted/README.md#laboratorio-local-com-podman-linux).

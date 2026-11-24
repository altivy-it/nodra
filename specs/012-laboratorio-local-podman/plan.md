# Plano de Implementação: Laboratório local com Podman

**Branch**: `012-laboratorio-local-podman` | **Data**: 2026-11-23 | **Spec**: [spec.md](spec.md)

**Entrada**: Pedido de teste local antes da conclusão do CI/CD.

**Estado**: Proposta para revisão humana; não autoriza retomar a Spec 005,
administrar a plataforma compartilhada ou declarar a stack homologada.

## Resumo

Reutilizar os cinco Dockerfiles e a composição self-hosted existentes. Os cinco
builds passaram com Podman no checkout atual. Não criar Dockerfiles alternativos
nem copiar infraestrutura do AKB.

Hipótese local: adaptar explicitamente o inicializador ao engine Podman, usando
Docker Compose v2 como provider, preserva preflight, backup, migrations e
readiness. O teste discriminante é um primeiro boot com PostgreSQL e Keycloak
reais, seguido de login e persistência pela borda nginx.

## Contexto Tecnico

**Linguagem/Versão**: PowerShell 7.4+, YAML, .NET 10 e Angular existentes.

**Dependências Principais**: Podman rootless e Docker Compose v2 como provider;
nginx, PostgreSQL e Keycloak da plataforma externa.

**Persistência**: Bind mounts externos de vault e keyring; PostgreSQL externo.
Nenhuma mudança de schema.

**Testes**: Builds OCI, testes do contrato de comandos e preflight em container;
E2E real com login e nota pela borda nginx.

**Plataforma Alvo**: Linux com Podman rootless como hipótese inicial.
Windows depende de confirmação e não será declarado validado.

**Tipo de Projeto**: Adaptação operacional do monorepo existente.

**Metas de Desempenho**: Readiness limitada pelo timeout existente de 240 segundos;
builds serializados e com recursos limitados.

**Restrições**: Sem secret recebido pelo chat, sem alteração de outros projetos,
sem publicação, deploy remoto ou administração do AKB.

**Escala/Escopo**: Inicializador, guia local e testes relacionados; Dockerfiles
e Compose só mudam se um teste demonstrar necessidade.

## Verificação da Constituição

- Integridade: preservar backups, migrations e dados existentes.
- Identidade: manter OIDC e decisões reais do Keycloak; nenhuma identidade mock.
- Privacidade: configuração externa não é lida ou exibida pelo assistente.
- Modularidade: preservar ownership da ADR 2026-0008.
- Qualidade: builds não substituem login, readiness e persistência.
- Governança: implementação pendente de revisão humana dos novos artefatos.

O desenho preserva a ADR aceita. Se for solicitada uma stack autossuficiente
com infraestrutura duplicada, obter decisão humana antes de alterar o plano.

## Estrutura do Projeto

### Documentação desta Funcionalidade

```text
specs/012-laboratorio-local-podman/
  spec.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/operacao-local.md
  tasks.md
  checklists/requirements.md
```

### Código-Fonte (raiz do repositório)

```text
tools/scripts/start-self-hosted.ps1
tools/scripts/test-local-container-engine.ps1
deploy/self-hosted/README.md
backend/apps/{api,bff,worker}/Dockerfile
frontend/web/Dockerfile
deploy/production/migrations.Dockerfile
```

**Decisão de Estrutura**: Adaptar o inicializador existente, sem fluxo paralelo
de bootstrap. O novo teste só será criado na implementação.

## Desenho proposto

1. Acrescentar seleção explícita de engine ao inicializador, mantendo Docker
   como padrão e os consumidores atuais compatíveis.
2. Validar o provider antes de resolver configuração; recusar providers sem
   `config --format json`, `config --no-interpolate` e `up --wait`.
3. Normalizar apenas formatos equivalentes de identificação de imagem do
   engine; nunca aceitar uma imagem não identificável.
4. Validar permissões de bind mounts e confiar somente no gateway descoberto.
   Não relaxar autorização nem alterar ownership de dados compartilhados.
5. Reutilizar build, backup e migrations existentes, sem duplicar essas etapas.
6. Documentar bootstrap externo pelo operador e parada sem remover volumes.
7. Usar `--format=docker` nos builds Podman para preservar os `HEALTHCHECK`s
   definidos nos Dockerfiles; o formato OCI padrão os descarta.

## Bloqueios atuais

- Máquina alvo ainda não confirmada.
- Plano e tarefas novos ainda não receberam revisão humana.
- `altivy-identity` e suas redes não estão em execução neste engine.
- Nenhum arquivo externo foi indicado pelo operador.
- O provider disponível é `podman-compose 1.5.0`, sem o contrato exigido.

**Atualização — 2026-11-23**: Bloqueios resolvidos na máquina Linux do
operador, por solicitação explícita: provider Docker Compose v5.8.0, identidade
iniciada por `tools/scripts/start-local-lab.ps1` e configuração externa gerada
em `~/.local/share/nodra-lab`. Permanece pendente a revisão humana do diff; a
correção do gateway foi aplicada no AKB ([research.md](research.md#defeito-externo-corrigido-akb)).

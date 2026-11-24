# Tarefas: Laboratório local com Podman

**Entrada**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md),
[data-model.md](data-model.md) e [contrato](contracts/operacao-local.md).

**Estado**: Implementação executada por solicitação explícita do operador em
2026-11-23; revisão humana do diff pendente no pull request.

## Fase 1: Preparação

- [X] T001 Registrar revisão humana do escopo, plano e tarefas em specs/012-laboratorio-local-podman/plan.md; confirmar máquina alvo, provider e preservação da ADR 2026-0008, sem inventar aprovação.

## Fase 2: Fundação

- [X] T002 Verificar versão e licença exatas do provider Docker Compose v2 e registrar configuração suportada em specs/012-laboratorio-local-podman/research.md; não alterar dependências da aplicação nem instalar provider não verificado.
- [X] T003 Confirmar com o operador os paths externos e a plataforma altivy-identity saudável, registrando somente resultados sanitizados em specs/012-laboratorio-local-podman/quickstart.md; não ler secrets, administrar AKB ou tocar nos serviços de outros projetos.

## Fase 3: US1 — Imagens locais (P1)

**Teste independente**: Cinco builds reais com formato que preserve healthchecks,
sem publicação, seguidos de inspeção de usuário e entrypoint.

- [X] T004 [US1] Adaptar o guia deploy/self-hosted/README.md para builds Podman com --format=docker e WEB_BASE_HREF=/notes/, recursos limitados e namespace local explícito; alterar Dockerfiles somente diante de falha demonstrada no recorte.
- [X] T005 [US1] Construir os cinco Dockerfiles, verificar healthchecks de API/BFF/web, usuário não root e entrypoints, e registrar resultados em specs/012-laboratorio-local-podman/quickstart.md; não confundir imagem construída com aplicação saudável.

## Fase 4: US2 — Execução local completa (P1)

**Teste independente**: Preflight negativo sem efeitos persistentes, boot real,
login e persistência da nota pela borda nginx.

- [X] T006 [US2] Criar tools/scripts/test-local-container-engine.ps1 com testes do contrato proposto: engine inválido, provider incompatível recusado antes de configuração, formatos equivalentes de image ID, ajuda sem dependências e preservação dos comandos Docker; executar em container e comprovar falha antes da adaptação.
- [X] T007 [US2] Adaptar tools/scripts/start-self-hosted.ps1 com -ContainerEngine docker|podman, padrão docker, validação prévia de Docker Compose v2 e identificação de imagem compatível; preservar backup, migrations, parâmetros, erros sanitizados e fluxo Docker existente.
- [X] T008 [US2] Executar preflight e boot com os pré-requisitos externos confirmados e imagens locais, verificar permissões rootless e sessão anônima pela borda nginx, e registrar evidência em specs/012-laboratorio-local-podman/quickstart.md; recusar falha de readiness sem fallback.
- [X] T009 [US2] Executar login, criação/edição de nota e reabertura após recriação de consumidores com o runner real documentado em frontend/tests/e2e/README.md; registrar evidência em specs/012-laboratorio-local-podman/quickstart.md e preservar identidade, dados e autorização.

## Fase 5: Refinamento

- [X] T010 Consolidar preparação, seleção de provider, configuração externa, subida e parada não destrutiva em deploy/self-hosted/README.md; validar ajuda/testes do inicializador, links e ausência de alterações fora do recorte, registrando limitações em specs/012-laboratorio-local-podman/quickstart.md.

## Dependências e Ordem de Execução

- T001 → T002 → T003 liberam a implementação e qualificação operacional.
- T004 → T005 concluem US1.
- T006 → T007 → T008 → T009 concluem US2; T008 também depende de T005.
- T010 depende de US1 e US2.
- Os builds exploratórios registrados em research.md não concluem estas tarefas.

## Paralelismo

Nenhuma tarefa marcada `[P]`: seleção de engine, preparação e qualificação
compartilham estado e precisam de uma sequência auditável. Builds são
serializados para reduzir pressão sobre a máquina compartilhada.

## Estratégia de Implementação

Revisar primeiro; entregar imagens e guia de build como MVP; adaptar o
inicializador com testes negativos; qualificar a stack real pela borda.
Não concluir US2 sem identidade externa e evidência de persistência.

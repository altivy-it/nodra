# Tarefas: Provisionamento Pessoal de Vaults

**Entrada**: Artefatos de desenho em `specs/002-provisionamento-vaults/`

**Pré-requisitos**: `plan.md` e `spec.md`.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, marcadores, comandos e caminhos.

**Testes**: Necessários em PostgreSQL e filesystem reais para as regras de integridade, autorização, retry e recuperação.

## Histórico Legado

P-08, P-09 e P-10 constam como concluídas no plano legado; P-11 está em andamento; P-12 está bloqueada por menor privilégio; P-13 a P-16 não foram iniciadas. A migração não transforma registros históricos em checkboxes concluídos.

## Fase 1: Concluir provisioning seguro (US1, US2)

**Objetivo**: Fechar os itens restantes do filesystem/worker e comprovar compensação e reconciliação.

**Teste independente**: Exercitar criação/retry/lease com filesystem e PostgreSQL reais, incluindo falha parcial e ausência de diretório.

- [ ] T001 [US1] Completar quarentena, alerta operacional e recuperação de resíduos no worker em `backend/apps/worker/Vaults/VaultProvisioningBackgroundService.cs` e módulos correlatos de `backend/apps/worker/`.
- [ ] T002 [US2] Validar criação idempotente, confinamento de paths, reparse points, retry e projeção `ready`/`degraded` com PostgreSQL e filesystem reais em `backend/tests/`.

## Fase 2: Resolver autorização de menor privilégio (US1)

**Objetivo**: Desbloquear P-12 sem conceder permissões administrativas amplas no Keycloak.

**Gate**: Nenhum adapter de provisioning de autorização pode ser implementado até decisão arquitetural humana aprovar uma alternativa confinada ao resource server.

- [ ] T003 [US1] Registrar e aprovar decisão arquitetural para provisionar recurso e permissions do proprietário sem `manage-clients`, referenciando `docs/adr/2026-0005-criacao-autogerenciada-de-vaults.md` e a decisão de bloqueio em `docs/plan/alternancia-vaults.md`.
- [ ] T003 [US1] Investigar e registrar em `specs/002-provisionamento-vaults/plan.md` uma alternativa de menor privilégio para provisionar recurso e permissions do proprietário; manter P-12 bloqueada até decisão humana e propor ADR separado se a arquitetura precisar mudar.

## Fase 3: Contrato e experiência de criação (US1, US2)

**Objetivo**: Expor operação assíncrona e integrar a experiência somente após autorização de menor privilégio aprovada.

- [ ] T004 [US1] Definir e validar `POST /api/v1/vaults`, consulta de operação e arquivamento em `contracts/openapi/altivy-api.yaml`, incluindo `202`, erros e `application/problem+json`.
- [ ] T005 [US1] Implementar endpoints e autorização de provisioning em `backend/apps/api/` conforme o contrato OpenAPI e o ADR aprovado.
- [ ] T006 [US2] Implementar ação de criação, quota visível, progresso, falha, retry e atualização do catálogo em `frontend/web/src/app/features/`.

## Fase 4: Reconciliação, E2E e operação (US1, US2, US3)

**Objetivo**: Demonstrar que estados, autorização, notificações e recuperação convergem em instalação integrada.

- [ ] T007 [P] [US3] Preparar fixtures sintéticas de usuário com dois vaults, um vault, nenhum vault, revogação e retry sem copiar memberships para PostgreSQL em `backend/tests/`.
- [ ] T008 [US1] Validar criação, retry idempotente, Keycloak indisponível, filesystem negado e vault `failed` não listado em `frontend/tests/e2e/` pelo gateway nginx.
- [ ] T009 [US2] Validar arquivamento, alerta de resíduos, retenção e ausência de exclusão física automática com PostgreSQL/filesystem reais em `backend/tests/`.
- [ ] T010 [US3] Documentar backup, restore, observabilidade e recuperação nos runbooks e documentos operacionais referenciados pelo plano legado.

## Dependências e Ordem de Execução

- T003 bloqueia T005 e T006; a autorização Keycloak não pode ser contornada.
- T001 e T002 podem avançar dentro do escopo já autorizado de filesystem/worker.
- T004 precede T005; T005 precede T006.
- T007 precede os cenários integrados T008 e T009; os testes E2E usam somente fixtures sintéticas.

## Estratégia de Implementação

Não iniciar as fases 2 a 4 sem autorização humana explícita e sem resolver o gate de menor privilégio. Cada tarefa de código exige sua própria hipótese, validação focal e evidência de integração real conforme a constituição.

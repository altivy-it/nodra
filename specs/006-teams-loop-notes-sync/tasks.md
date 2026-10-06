# Tarefas: Espelhamento de Notas Loop do Teams

**Entrada**: `specs/006-teams-loop-notes-sync/`

**Pré-requisitos**: `spec.md` e `plan.md` revisados.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, comandos, APIs, estados e caminhos.

**Testes**: Obrigatórios para Graph, parser/sanitização, PostgreSQL/filesystem e recuperação.

## Gate de autorização

O plano legado permanece bloqueado por decisão humana. Nenhum consentimento, POC contra tenant, acesso Graph ou implementação pode começar até nova autorização explícita. Todas as tarefas abaixo permanecem desmarcadas.

## Fase 1: Decisão, POC e topologia (US1, US3)

- [ ] T001 [US1] Registrar ADR e threat model para Graph, fonte canônica, scopes, polling/webhook e recuperação em `docs/adr/` e `docs/seguranca.md`.
- [ ] T002 [US1] Provar descoberta ou vínculo assistido e leitura de arquivo `.loop` pela API Graph v1.0 em uma POC documentada em `specs/006-teams-loop-notes-sync/`.
- [ ] T003 [US3] Executar gates G1/G2 de viabilidade e topologia, registrando decisão de polling baseline e webhook opcional em `specs/006-teams-loop-notes-sync/plan.md`.

## Fase 2: Contratos, dados e conteúdo (US1, US2)

- [ ] T004 [US1] Definir contratos administrativos e de status de sincronização em `contracts/openapi/altivy-api.yaml`.
- [ ] T005 [US2] Modelar mapping, subscription, cursor e execução idempotente no domínio e migration PostgreSQL em `backend/libs/`.
- [ ] T006 [US2] Definir frontmatter/documento Markdown gerenciado com preservação de edições locais em `specs/006-teams-loop-notes-sync/plan.md` e testes.
- [ ] T007 [US2] Validar dependência de conversão HTML→Markdown, licença, sanitização, scripts e vetores XSS antes da adoção.

## Fase 3: Sincronização durável e interface (US2, US3)

- [ ] T008 [US2] Implementar autenticação Graph, scopes mínimos, delta polling e cursor durável em `backend/apps/worker/`.
- [ ] T009 [US2] Implementar parser Loop→Markdown determinístico, sanitização e conflito com ETag em `backend/libs/`.
- [ ] T010 [US2] Implementar pipeline de sync idempotente com lease, retry, `410 Gone`, throttling e reconciliação em `backend/apps/worker/`.
- [ ] T011 [US3] Implementar detecção por webhook somente se autorizada, com validação de `clientState` e resposta dentro dos limites do Graph em `backend/apps/api/`.
- [ ] T012 [US1] Implementar administração, estado de conflito e consentimento sem expor credenciais em `frontend/web/src/app/features/`.

## Fase 4: Qualidade e rollout (US1, US2, US3)

- [ ] T013 [P] [US3] Adicionar métricas e diagnóstico sem conteúdo, tokens ou URLs pré-autenticadas em `backend/apps/worker/`.
- [ ] T014 [US2] Testar sincronização, conflitos e recovery com PostgreSQL e filesystem reais em `backend/tests/`.
- [ ] T015 [US2] Testar autorização, UI e atualização por gateway containerizado em `frontend/tests/e2e/`.
- [ ] T016 [US3] Documentar rollout incremental, disable, rollback, retenção e recuperação em `docs/runbooks/`.

## Dependências e Ordem de Execução

T001–T003 são gates e bloqueiam consentimento/implementação. T004–T007 precedem T008–T012. T013–T016 validam o comportamento integrado.

## Estratégia de Implementação

Não executar nenhuma tarefa até haver autorização humana. Começar com POC limitada; bloquear implementação se descoberta, scopes ou política de exposição não forem aprovados. Polling permanece funcional sem entrada pública.

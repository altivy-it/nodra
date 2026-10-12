# Tarefas: Workspace do AltivyNotes

**Entrada**: `specs/008-workspace-frontend/`

**Pré-requisitos**: `spec.md` e `plan.md` revisados.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, marcadores, comandos e caminhos.

**Testes**: Necessários para editor, notas, acessibilidade, responsividade e persistência de preferências.

## Histórico Legado

O plano `docs/plan/workspace-frontend-inspirado-no-obsidian.md` registra P-01–P-04 e P-09–P-11 `in-progress`, P-05–P-08 `not-started`; esta migração não converte nenhum estado em checkbox concluído.

## Fase 1: Arquitetura de informação e shell (US1)

- [ ] T001 [US1] Validar hierarquia, densidade e protótipo responsivo do workspace em `frontend/web/src/app/` antes de ampliar a interface.
- [ ] T002 [US1] Evoluir regiões persistentes do shell, foco e estado responsivo em `frontend/web/src/app/`.
- [ ] T003 [US1] Implementar barra de atividades e navegação modular acessível em `frontend/web/src/app/`.
- [ ] T004 [US1] Evoluir árvore do explorer com seleção, operações e estados vazios/erro em `frontend/web/src/app/features/explorer/`.

## Fase 2: Edição e contexto do workspace (US1, US2)

- [ ] T005 [US1] Integrar abas, editor e barra de status sem descartar edição em `frontend/web/src/app/`.
- [ ] T006 [US1] Implementar toolbar e menu de conta encaminhando identidade/MFA ao Keycloak em `frontend/web/src/app/`.
- [ ] T007 [US2] Implementar central de notificações reconstruível pela API e deduplicada após SignalR em `frontend/web/src/app/`.
- [ ] T008 [US2] Implementar painel contextual e slots controlados sem código remoto em `frontend/web/src/app/`.

## Fase 3: Acessibilidade, ajuda e qualidade (US1, US2)

- [ ] T009 [US1] Validar responsividade, teclado, contraste e preferências isoladas por usuário/vault em `frontend/web/src/app/`.
- [ ] T010 [US2] Entregar ajuda somente leitura fora do vault e excluída de busca/indexação em `frontend/web/src/app/`.
- [ ] T011 [US1] Consolidar desempenho, telemetria e rollout e executar unit/E2E/screenshot nos fluxos afetados em `frontend/tests/e2e/`.

## Dependências e Ordem de Execução

T001 precede T002–T004; T002 precede T005–T008; T009–T011 validam os fluxos integrados. Reavalie dependências entre histórias após o desenho Spec Kit.

## Estratégia de Implementação

Implementar um incremento validável por vez, preservando abas, texto não salvo, seleção e scroll. Usar screenshots desktop/mobile antes de concluir mudanças visuais.

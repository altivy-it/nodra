# Tarefas: Plugin Microsoft Teams e External Push

**Entrada**: `specs/004-app-teams-external-push/`

**Pré-requisitos**: `spec.md` e `plan.md` revisados; decisão humana explícita de entrada na Fase 5.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, marcadores, comandos e caminhos.

**Testes**: Obrigatórios para contratos, autorização, idempotência, PostgreSQL/filesystem e jornada Teams.

## Gate de autorização

O plano legado está `blocked`. Nenhuma tarefa de implementação pode começar até que uma pessoa autorize a Fase 5 e desbloqueie a tarefa correspondente. Tarefas abaixo permanecem desmarcadas.

## Fase 1: Arquitetura e viabilidade (US1)

- [ ] T001 [US1] Registrar fronteiras do Plugin Host e decisão de fase em `docs/adr/2026-0003-app-teams-external-push.md`; atualizar análise de ameaças em `docs/ci-cd-seguranca.md`.
- [ ] T001 [US1] Registrar fronteiras do Plugin Host, análise de ameaças e decisão de fase na seção de segurança do ADR `docs/adr/2026-0003-app-teams-external-push.md`.
- [ ] T002 [US1] Validar message extension, tenant, callback HTTPS e política de apps personalizados em documentação de prova de conceito sob `docs/`.
- [ ] T003 [US1] Definir manifesto, capabilities, compatibilidade e ciclo de vida do host em `backend/libs/` e cobrir com teste de arquitetura.
- [ ] T004 [US1] Definir contrato independente do Teams para `external-push/v1` em `contracts/openapi/altivy-api.yaml`.
- [ ] T005 [US1] Validar licença, versão e manutenção do SDK candidato antes de incluir qualquer pacote no backend.

## Fase 2: Plataforma e entrega durável (US3)

- [ ] T006 [P] [US3] Implementar validação de manifesto, compatibilidade e desativação de plugin em `backend/libs/`.
- [ ] T007 [US3] Implementar conexão e referência segura a secrets externos sem persistir credenciais no PostgreSQL em `backend/libs/`.
- [ ] T008 [US3] Implementar inbox, idempotência e persistência antes da resposta em `backend/libs/` e `backend/apps/api/`.
- [ ] T009 [US3] Implementar autenticação do produtor e vínculo explícito de identidade, delegando autorização ao Keycloak em `backend/apps/api/`.
- [ ] T010 [US3] Implementar processamento worker e gravação Markdown segura com conflito preservado em `backend/apps/worker/`.

## Fase 3: Plugin Teams e experiência (US2)

- [ ] T011 [US2] Criar pacote e manifesto instalável do Teams em `deploy/` conforme o contrato do host.
- [ ] T012 [US2] Implementar adapter de atividade Teams isolado no plugin e cobrir mapeamento de payload.
- [ ] T013 [US2] Implementar revisão de título, conteúdo e destino em `frontend/web/src/app/features/`.
- [ ] T014 [US2] Implementar fallback Outgoing Webhook somente se autorizado, com HMAC e limites documentados.

## Fase 4: Operação, qualidade e rollout (US1, US2, US3)

- [ ] T015 [P] [US3] Adicionar métricas, logs sanitizados, limits e health para plugins em `backend/apps/worker/` e `backend/apps/api/`.
- [ ] T016 [US2] Executar testes PostgreSQL/filesystem, contrato e Playwright em tenant de teste via gateway nginx e registrar evidências em `specs/004-app-teams-external-push/`.
- [ ] T017 [US2] Documentar rollout incremental, feature flag e rollback sem excluir notas existentes em `deploy/` e `docs/runbooks/`.

## Dependências e Ordem de Execução

- T001 e T002 são gates humanos e bloqueiam toda implementação posterior.
- T003 define a plataforma antes de T006–T010; T004 precede o ingress.
- T005 bloqueia a adoção do SDK, mas não impede protótipo sem nova dependência.
- T006–T010 precedem T011–T014; T015–T017 fecham validação e rollout.

## Estratégia de Implementação

Não iniciar até haver autorização humana. Implementar capacidades neutras antes do plugin Teams; manter external-push idempotente, sem proxy genérico de engine/provider e sem leitura de Graph.

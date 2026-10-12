# Tarefas: Deploy Self-Hosted por Docker Compose

**Entrada**: `specs/005-deploy-self-hosted-compose/`

**Pré-requisitos**: `spec.md` e `plan.md` revisados.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, comandos, variáveis e caminhos.

**Testes**: Build, Compose, integração real PostgreSQL/filesystem e E2E autenticado via gateway quando aplicável.

## Histórico Legado

P-01 e P-04 constam como concluídas no plano legado, com limitações documentadas; P-02 e P-03 estão `in-progress`. As tarefas abaixo permanecem desmarcadas até validação/revisão Spec Kit.

## Fase 1: Composição e gateway (US1)

**Objetivo**: Fechar diferenças entre a composição versionada e os requisitos de operação.

**Teste independente**: Validar Compose e build sem iniciar produção ou acessar serviços externos reais.

- [ ] T001 [US1] Revisar e completar os serviços persistentes, redes e variáveis da stack em `deploy/self-hosted/`.
- [ ] T002 [US1] Validar `docker compose config --quiet`, builds por serviço e health checks da composição em `deploy/self-hosted/`.

## Fase 2: Primeiro boot autenticado (US2)

**Objetivo**: Confirmar OIDC, autorização, persistência e reconexão pelo gateway.

- [ ] T003 [US2] Validar preflight, TLS/CA, PFX, roles PostgreSQL e migrations via `deploy/self-hosted/` sem exibir secrets.
- [ ] T004 [US2] Executar login/logout, autorização, CRUD e SignalR pelo gateway HTTPS; registrar evidências no artefato `specs/005-deploy-self-hosted-compose/quickstart.md`.
- [ ] T004 [US2] Executar login/logout, autorização, CRUD e SignalR pelo gateway HTTPS seguindo `deploy/self-hosted/README.md`; registrar as evidências de execução no relatório da tarefa.
- [ ] T005 [US2] Recriar consumidores sem recriar o banco e confirmar que vault, projeção e chaves do BFF persistem em `deploy/self-hosted/`.

## Fase 3: Recuperação e promoção (US3)

**Objetivo**: Confirmar os limites de backup/restore e release por digest.

- [ ] T006 [US3] Validar backup e restore de banco/vault/keyring em ambiente efêmero sem tocar no banco compartilhado em `docs/runbooks/recuperacao-self-hosted.md`.
- [ ] T007 [US3] Validar preflight de manifesto de imagens por digest e documentar rollback sem downgrade de schema em `deploy/self-hosted/`.

## Dependências e Ordem de Execução

T001 precede T002; T002 e os certificados/configuração do operador precedem T003–T005; T006 e T007 são gates antes de declarar recuperação/promoção suportadas.

## Estratégia de Implementação

Não executar operações de produção, remoções ou promoções sem autorização explícita. Testes que sobem HTTP usam somente a borda do gateway nginx containerizado.

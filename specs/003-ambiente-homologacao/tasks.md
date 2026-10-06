# Tarefas: Ambiente Real de Homologação

**Entrada**: `specs/003-ambiente-homologacao/`

**Pré-requisitos**: `spec.md` e `plan.md` revisados.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, comandos e caminhos.

**Testes**: Inclua validação Compose, TLS e OIDC conforme cada etapa.

## Fase 1: Decisão e isolamento (US1)

**Objetivo**: Registrar a escolha de topologia e os limites de isolamento.

**Teste independente**: Revisar decisão e configuração para confirmar que homologação não compartilha recursos sensíveis.

- [ ] T001 [US1] Registrar a opção de host, custo e justificativa aprovados pela pessoa mantenedora em `specs/003-ambiente-homologacao/spec.md` antes de qualquer provisionamento.
- [ ] T002 [US1] Criar documentação de variáveis e limites de homologação em `docs/configuracao-keycloak.md` sem inserir secrets reais.

## Fase 2: Host e stack dedicada (US2)

**Objetivo**: Provisionar host, DNS, TLS, realm e banco separados.

- [ ] T003 [US2] Provisionar DNS e TLS próprios e comprovar HTTPS válido no host aprovado, conforme `specs/003-ambiente-homologacao/plan.md`.
- [ ] T004 [US2] Criar a stack Compose isolada em `deploy/staging/` com realm, banco, volumes e secrets exclusivos.
- [ ] T005 [US2] Validar `docker compose config`, health checks e login/erro/recuperação OIDC via gateway HTTPS em `deploy/staging/`.

## Fase 3: Observabilidade e promoção (US2)

**Objetivo**: Habilitar validação do tema e rollback verificável.

- [ ] T006 [US2] Documentar métrica/alerta para falha de assets do tema em `docs/observabilidade-alertas-slo.md`.
- [ ] T007 [US2] Promover o tema por checksum, validar fluxos OIDC e executar rollback no ambiente homologação conforme `docs/configuracao-keycloak.md`.

## Dependências e Ordem de Execução

T001 bloqueia T003–T007. T003 bloqueia T004; T004 bloqueia T005–T007.

## Estratégia de Implementação

Não executar T003–T007 até decisão humana sobre a opção. Homologação deve permanecer separada do laboratório E2E e da produção.

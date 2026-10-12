# Tarefas: Tema Personalizado de Login do Keycloak

**Entrada**: `specs/007-tema-login-keycloak/`

**Pré-requisitos**: `spec.md` e `plan.md` revisados; ambiente de homologação validado.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, comandos, nomes de assets e paths.

**Testes**: Validar todos os fluxos Keycloak habilitados e rollback em ambiente representativo.

## Histórico Legado

P-01 a P-03 foram registrados como concluídos no laboratório. P-04 segue bloqueada; nenhum checkbox representa sua conclusão.

## Fase 1: Preparar observabilidade e homologação (US2)

- [ ] T001 [US2] Criar alerta/métrica de falha em assets do tema e documentar em `docs/observabilidade-alertas-slo.md`.
- [ ] T002 [US2] Provisionar ambiente de homologação isolado conforme `specs/003-ambiente-homologacao/` e validar o gate antes de promoção.

## Fase 2: Promover e validar o tema (US1, US2)

- [ ] T003 [US1] Promover artefato por checksum e validar login, erro, OTP, recuperação e atualização de credencial na homologação em `deploy/keycloak/`.
- [ ] T004 [US2] Executar rollback ao tema padrão e registrar a validação em `docs/configuracao-keycloak.md`.
- [ ] T005 [US1] Revisar segurança do pacote e acessibilidade visual, sem recurso externo, em `deploy/keycloak/`.

## Dependências e Ordem de Execução

T001 e T002 precedem promoção; T003 precede rollback T004. Não realizar promoção de produção nesta tarefa.

## Estratégia de Implementação

Manter assets canônicos no repositório `Altivy`; não alterar fluxo OIDC, conta, usuário, secret ou policy. Parar diante de falta de decisão ou ambiente real.

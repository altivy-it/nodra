# Plano de Implementação: Plugin Microsoft Teams e External Push

**Branch**: A definir | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração de `docs/plan/app-altivy-notes-teams-external-push.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, IDs, nomes técnicos e caminhos.

## Resumo

Construir Plugin Host no monólito modular, um contrato `external-push/v1` durável e o primeiro plugin Microsoft Teams de envio explícito. A pessoa revisa o conteúdo e destino; Keycloak mantém decisão de autorização. Não inclui leitura de Graph nem código de plugin carregado dinamicamente.

## Contexto Técnico

**Linguagem/Versão**: .NET 10 e Angular 22.

**Dependências Principais**: PostgreSQL jobs/outbox existentes; Teams SDK somente após análise de licença, manutenção e compatibilidade.

**Armazenamento**: PostgreSQL para inbox/jobs e metadados; Markdown em filesystem como fonte canônica.

**Testes**: Contrato OpenAPI, integração PostgreSQL/filesystem real, tenant de teste e Playwright/E2E por gateway nginx.

**Plataforma Alvo**: Compose self-hosted; callback público HTTPS necessário para interação direta do Teams.

**Tipo de Projeto**: Extensão modular em API, worker e frontend existentes.

**Objetivos de Desempenho**: Persistir a entrega antes de confirmar; limites de payload, retry e idempotência serão definidos no desenho.

**Restrições**: Plugin confiável da distribuição; não carregar código arbitrário; nenhuma identidade Teams substitui Keycloak; sem microsserviço.

**Escopo**: contratos/capabilities, manifesto, conexão, auth/link, ingress, Teams message extension, worker, administração e operação.

## Verificação da Constituição

- Keycloak decide acesso; API aplica autorização antes de revelar vault.
- Operações mutáveis são idempotentes, duráveis e auditáveis.
- Conteúdo Markdown permanece fonte canônica; secrets não entram no banco ou logs.
- Dependências e Teams SDK exigem licença permissiva verificada.
- Fase 5 exige nova autorização humana; plano legado está bloqueado.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/004-app-teams-external-push/{spec.md,plan.md,tasks.md}
```

### Código-fonte

```text
backend/apps/api/
backend/apps/worker/
backend/libs/
contracts/openapi/altivy-api.yaml
frontend/web/src/app/features/
deploy/
```

**Decisão de Estrutura**: Hosts continuam composition roots; plugins dependem apenas de contratos/capabilities do host; integração e SDK permanecem confinados ao plugin.

## Rastreabilidade da Migração

As dezesseis etapas P-01 a P-16 estão mantidas no arquivo legado. P-01 permanece bloqueada por decisão humana; as seguintes estão `not-started`. Esta migração registra seus objetivos como tarefas Spec Kit, sem executar tenant, consentimento, app registration ou código.

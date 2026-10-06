# Plano de Implementação: Espelhamento de Notas Loop do Teams

**Branch**: A definir | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração de `docs/plan/integracao-teams-notas-reuniao.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, APIs, caminhos e identificadores.

## Resumo

Construir sincronização unilateral de notas Loop via Microsoft Graph v1.0, usando delta polling como baseline privada, conversão segura para Markdown e reconciliação idempotente; webhook permanece opcional.

## Contexto Técnico

**Linguagem/Versão**: .NET 10 e Angular 22.

**Dependências Principais**: Microsoft Graph v1.0; dependência HTML→Markdown somente após licença/manutenção aprovadas.

**Armazenamento**: PostgreSQL para mappings, cursors e jobs; filesystem Markdown como fonte canônica.

**Testes**: POC Graph, integração PostgreSQL/filesystem, segurança de HTML/callback e E2E via gateway.

**Plataforma Alvo**: Compose self-hosted, inclusive instalação sem HTTPS público quando polling for suficiente.

**Tipo de Projeto**: Integração modular no worker/API/frontend existentes.

**Objetivos de Desempenho**: SLO legado até 2 minutos via webhook e intervalo de polling + 2 minutos via polling; validar antes de ratificar.

**Restrições**: API v1.0 somente; scopes mínimos; sem webhook obrigatório, microsserviço, broker ou exclusão automática.

**Escopo**: Prova de conceito, consentimento, mapping, conversão, delta, conflito, administração, observabilidade e rollout.

## Verificação da Constituição

- Identidade e autorização permanecem no Keycloak; provider não concede acesso a vault.
- Não enviar conteúdo a serviços externos para conversão.
- Filesystem e PostgreSQL são validados por testes reais.
- Callback público, quando opcionalmente habilitado, requer threat model e gates de segurança.
- Não iniciar antes de autorização humana explícita.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/006-teams-loop-notes-sync/{spec.md,plan.md,tasks.md}
```

### Código-fonte

```text
backend/apps/worker/
backend/apps/api/
backend/libs/
contracts/openapi/altivy-api.yaml
frontend/web/src/app/features/
```

**Decisão de Estrutura**: Integração como módulo/worker do monólito modular; reutilizar jobs duráveis e casos de uso Notes; sem serviço novo.

## Rastreabilidade da Migração

P-01 a P-16 estão `not-started`; o plano legado permanece `blocked` por decisão humana. A ordem de POC, decisão arquitetural, consentimento, polling baseline, contrato, integração, rollout e rollback foi transportada para `tasks.md` sem autorização para iniciar.

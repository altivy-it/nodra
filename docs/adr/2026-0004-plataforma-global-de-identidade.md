# ADR 2026-0004: Consumir a plataforma global de identidade

- **Status:** approved
- **Data:** 2026-09-14
- **Origem:** IA assistida
- **Revisor humano:** Lincoln Zocateli

## Contexto

A infraestrutura local de Keycloak, PostgreSQL e gateway está duplicada em `deploy/keycloak` e mistura rotas do produto com outros consumidores.

## Decisão proposta

O AltivyNotes consumirá a plataforma em `architecture-knowledge-base/platforms/identity`, usando o realm `altivy`, o hostname `notes.altivy.test`, database e role próprios e o tema publicado em `KEYCLOAK_THEMES_ROOT/altivy`. O produto manterá seus clients, roles, grupos, policies, usuários e script de reconciliação.

Credenciais do realm `master` não serão usadas em operações cotidianas. A reconciliação usará client credentials restritas ao realm `altivy`. A retirada do Compose antigo aguarda rollout paralelo, backup, testes e aceite humano.

## Pendências

Separar o database de aplicação do database `keycloak`, adaptar o Compose E2E, publicar o tema atomicamente, ajustar callbacks/issuer e trocar scripts administrativos para o client restrito. Até essas evidências existirem, este ADR permanece `proposed`.

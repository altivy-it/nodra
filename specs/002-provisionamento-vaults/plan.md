# Plano de Implementação: Provisionamento Pessoal de Vaults

**Branch**: A definir antes da implementação | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração das etapas P-08 a P-16 de `docs/plan/alternancia-vaults.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, nomes técnicos e caminhos literais.

## Resumo

Concluir o provisioning assíncrono de vault pessoal, sua autorização no Keycloak, contrato HTTP, integração Angular e validação E2E. A fila durável, estados do domínio e parte da criação segura no filesystem já existem; a etapa de Keycloak está bloqueada por ausência de credencial de menor privilégio.

## Contexto Técnico

**Linguagem/Versão**: .NET 10 e Angular 22.

**Dependências Principais**: PostgreSQL, filesystem real, worker .NET, Keycloak Authorization Services e OpenAPI.

**Armazenamento**: PostgreSQL para operações/fila e projeção; filesystem para conteúdo Markdown.

**Testes**: Testes de domínio e aplicação, integração PostgreSQL/filesystem reais e E2E via gateway nginx containerizado.

**Plataforma Alvo**: Containers; Keycloak e PostgreSQL conforme ambiente configurado pelo operador.

**Tipo de Projeto**: Feature vertical do monólito modular e do workspace Angular.

**Objetivos de Desempenho**: Provisionamento assíncrono e idempotente; limites de retry e lease conforme configuração operacional.

**Restrições**: Não expor Admin REST API ao browser; não conceder `manage-clients`; não usar nome como path; não excluir dados automaticamente.

**Escopo**: Worker `backend/apps/worker/Vaults/VaultProvisioningBackgroundService.cs`, módulos backend de vaults, `contracts/openapi/altivy-api.yaml`, frontend workspace e testes de integração/E2E.

## Verificação da Constituição

- Keycloak continua autoridade e a API aplica autorização antes de revelar vault.
- Filesystem seguro e PostgreSQL real são evidências obrigatórias.
- Operações mutáveis mantêm idempotência, retry, auditoria e reconciliação.
- Nenhum processo pode excluir volume/diretório implicitamente.
- Menor privilégio é gate; P-12 legado permanece bloqueado até decisão arquitetural segura.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/002-provisionamento-vaults/
├── spec.md
├── plan.md
└── tasks.md
```

### Código-fonte

- `backend/apps/worker/Vaults/VaultProvisioningBackgroundService.cs`
- `backend/apps/worker/`
- `backend/libs/` para domínio, aplicação e infraestrutura de vaults
- `backend/apps/api/` e `contracts/openapi/altivy-api.yaml`
- `frontend/web/src/app/features/`
- `backend/tests/` e `frontend/tests/e2e/`

**Decisão de Estrutura**: Continuar no monólito modular existente. Não adicionar microsserviço, datastore ou dependência externa sem decisão arquitetural e licença verificada.

## Rastreabilidade da Migração

- P-08 registra ADR 2026-0005 e decisão humana de vault pessoal assíncrono.
- P-09 e P-10 constam como concluídas com testes de domínio, aplicação e migration PostgreSQL; os registros estão em `docs/plan/alternancia-vaults.md`.
- P-11 está em andamento e possui implementação parcial de filesystem, worker, alertas, reconciliação e retenção; o status não será promovido por esta migração.
- P-12 está bloqueada: a service account atual não possui escopo suficiente e conceder `manage-clients` foi explicitamente rejeitado.
- P-13 a P-16 permanecem pendentes e serão representadas em `tasks.md`.
- Este plano está em rascunho de migração e não autoriza implementação.

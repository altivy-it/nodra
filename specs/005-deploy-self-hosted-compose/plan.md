# Plano de Implementação: Deploy Self-Hosted por Docker Compose

**Branch**: A definir | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração de `docs/plan/deploy-self-hosted-compose.md`.

**Idioma**: Português brasileiro (pt-BR); preserve nomes de arquivos, comandos e variáveis.

## Resumo

Finalizar a composição persistente do produto e validar primeiro boot autenticado, health, CRUD, persistência e recuperação usando a infraestrutura compartilhada do operador.

## Contexto Técnico

**Linguagem/Versão**: .NET 10, Angular 22, imagens OCI fixadas por digest.

**Dependências Principais**: Docker Compose, gateway nginx, Keycloak, PostgreSQL 18/pgvector.

**Armazenamento**: Bind mounts persistentes para vaults e chaves do BFF; PostgreSQL compartilhado.

**Testes**: Build containerizado, smoke OIDC e Playwright via gateway nginx, PostgreSQL/filesystem reais.

**Plataforma Alvo**: Docker Engine/Docker Desktop no host escolhido pelo operador.

**Tipo de Projeto**: Pacote de deployment self-hosted do monólito modular.

**Objetivos de Desempenho**: Readiness e health checks confiáveis; metas de carga dependem dos SLOs existentes.

**Restrições**: Não administrar Keycloak; sem portas próprias expostas; sem secret versionado; sem downgrade destrutivo de schema.

**Escopo**: `deploy/self-hosted/`, preflight, boot, migrations, validação autenticada e runbook de recuperação.

## Verificação da Constituição

- Serviços da aplicação e runners permanecem em containers.
- O navegador e runner acessam HTTP somente pelo gateway nginx.
- Migrations usam role migrator; runtime não recebe DDL.
- Restore é coordenado e nunca remove volumes implicitamente.
- Publicação/deploy de release não são autorizados por esta migração.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/005-deploy-self-hosted-compose/{spec.md,plan.md,tasks.md}
```

### Código-fonte

```text
deploy/self-hosted/
docs/runbooks/recuperacao-self-hosted.md
```

**Decisão de Estrutura**: Reutilizar a topologia já criada em `deploy/self-hosted/`; preservar infraestrutura compartilhada fora do ownership desta stack.

## Rastreabilidade da Migração

P-01 e P-04 constam como concluídas no legado; P-02 e P-03 estão em andamento. P-04 contém ensaios históricos de backup/restore e upgrade, com limitações registradas para rollback autenticado, downgrade e promotion entre releases. Não se converte status para tarefas concluídas sem revalidar evidências.

# Plano de Implementação: Alternância Segura entre Vaults

**Branch**: A definir antes da implementação | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração das etapas P-01 a P-07 de `docs/plan/alternancia-vaults.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, nomes técnicos e caminhos literais.

## Resumo

A maior parte do catálogo autorizado e da navegação multi-vault está registrada como concluída no plano legado. O escopo restante é validar o comportamento com dois vaults autorizados, revogação real, isolamento de preferências e apresentação responsiva usando o laboratório E2E autorizado.

## Contexto Técnico

**Linguagem/Versão**: .NET 10 e Angular 22.

**Dependências Principais**: Keycloak Authorization Services, `IAuthorizationDecisionService`, `VaultAuthorization`, `VaultRecord` e `PreferencesService` existentes.

**Armazenamento**: PostgreSQL como projeção reconstruível; arquivos Markdown no filesystem como fonte canônica.

**Testes**: Testes de contrato/API, integração PostgreSQL e filesystem quando aplicável, testes Angular e Playwright via gateway nginx containerizado.

**Plataforma Alvo**: Containers; validação E2E pelo gateway em `deploy/e2e/`.

**Tipo de Projeto**: Aplicação web Angular com API/BFF .NET modular.

**Objetivos de Desempenho**: Concorrência UMA limitada a quatro; paginação padrão 20 e máxima 100.

**Restrições**: Sem cache de decisão, offset, membership local, path físico no browser ou página parcial em falha de autorização.

**Escopo**: Catálogo autorizado, rota `/workspace/:vaultId/:module`, seletor acessível, isolamento por vault e validação E2E.

## Verificação da Constituição

- Keycloak permanece a autoridade de autorização e a API aplica a decisão.
- Nenhum metadado de vault negado pode ser exposto.
- PostgreSQL continua sendo projeção, e conteúdo Markdown não é movido para banco.
- Testes E2E devem acessar a aplicação pelo gateway nginx containerizado.
- Migração de documentação é pt-BR; comandos, IDs e contratos permanecem literais.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/001-alternancia-vaults/
├── spec.md
├── plan.md
└── tasks.md
```

### Código-fonte

- `backend/` para API e aplicação do catálogo, conforme o código existente.
- `frontend/web/src/app/` para seletor e estado do workspace.
- `frontend/tests/e2e/test_bff_session.py` para jornadas autenticadas já relacionadas.
- `deploy/e2e/` para laboratório e proxy usados nas validações.
- `docs/frontend.md`, `docs/backend.md` e `docs/configuracao-keycloak.md` como referências técnicas existentes.

**Decisão de Estrutura**: Reutilizar módulos e fixtures existentes; não adicionar migration ou dependência. As tarefas deste plano cobrem somente gaps ainda abertos, não refazem implementação histórica sem nova evidência.

## Rastreabilidade da Migração

- `docs/plan/alternancia-vaults.md` é a origem histórica e permanece congelado até revisão humana.
- P-01 a P-06 constam como concluídas no legado; suas evidências permanecem na origem e não são reabertas nesta migração.
- P-07 está `in-progress`. Catálogo e testes parciais existem, mas as jornadas multi-vault com credenciais apropriadas, revogação real e verificação visual ainda não foram comprovadas.
- P-08 a P-10 e P-11 a P-16 tratam da criação/provisioning de vaults e pertencem a `specs/002-provisionamento-vaults/`.
- Este plano está em rascunho de migração; sua existência não autoriza implementação.

# Tarefas: Alternância Segura entre Vaults

**Entrada**: Artefatos de desenho em `specs/001-alternancia-vaults/`

**Pré-requisitos**: `plan.md` e `spec.md`.

**Idioma**: Português brasileiro (pt-BR); preserve IDs, marcadores, comandos e caminhos.

**Testes**: Incluídos porque autorização, revogação e isolamento são requisitos de segurança e integridade.

**Organização**: Tarefas restantes da etapa legada P-07, organizadas pelas jornadas de usuário.

## Formato: `[ID] [P?] [Story?] Descrição com caminho do arquivo`

- **[P]**: Use somente para tarefas paralelizáveis sem dependências pendentes.
- **[Story]**: Use `[US1]`, `[US2]`, `[US3]` para relacionar tarefas às jornadas da spec.
- Informe caminhos exatos dos arquivos envolvidos.

## Histórico Legado

P-01 a P-06 estão registrados como concluídos em `docs/plan/alternancia-vaults.md`. Não foram convertidos em tarefas marcadas `[x]`; as evidências originais permanecem nesse documento até revisão humana da migração.

---

## Fase 1: Validar catálogo e autorização (US1)

**Objetivo**: Concluir a validação integrada do catálogo autorizado sem alterar memberships reais.

**Teste independente**: Usar fixtures sintéticas com usuários autorizados em dois vaults, em um vault e sem vault.

- [ ] T001 [US1] Executar jornadas de catálogo e autorização com o usuário E2E autorizado em `frontend/tests/e2e/test_bff_session.py` e registrar os cenários executados.
- [ ] T002 [US1] Validar revogação real e fallback seguro com fixture sintética autorizada, sem alterar grants de `lab-0001` ou `lab-0002`, usando `frontend/tests/e2e/test_bff_session.py`.

---

## Fase 2: Validar restauração e isolamento (US2, US3)

**Objetivo**: Comprovar recarga, persistência de contexto, isolamento por vault e comportamento responsivo.

**Teste independente**: Alternar entre dois vaults sintéticos, recarregar a aplicação e comparar conteúdo, preferências e respostas realtime.

- [ ] T003 [US2] Completar o cenário de troca entre dois vaults e preservação do módulo em `frontend/tests/e2e/test_bff_session.py`.
- [ ] T004 [US3] Executar a verificação visual desktop/mobile do seletor e dos estados de catálogo conforme `.github/instructions/frontend-visual-verification.instructions.md`.
- [ ] T005 [US3] Confirmar que os testes de isolamento por usuário e vault não expõem metadados negados em `frontend/tests/e2e/test_bff_session.py`.

---

## Dependências e Ordem de Execução

- T001 fornece a base de fixtures; T002 e T003 dependem de credenciais e permissões E2E para dois vaults.
- T004 pode executar em paralelo após o ambiente visual autorizado estar disponível.
- T005 depende de dados sintéticos e permissões sem grants reais alterados.

## Estratégia de Implementação

Não alterar código até haver autorização humana específica para esta feature e as credenciais E2E necessárias. Execute uma jornada por vez através do gateway nginx; registre como bloqueado qualquer cenário que dependa de credenciais ausentes.

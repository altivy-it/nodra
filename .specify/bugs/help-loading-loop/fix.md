# Correção de Bug: ajuda presa em "Carregando ajuda..."

- **Slug**: help-loading-loop
- **Corrigido em**: 2026-11-23
- **Avaliação**: ./assessment.md
- **Estado**: applied

## Resumo

A ajuda passa a ser buscada sob o base path da aplicação, o `effect` do shell
deixa de depender dos signals internos dos loaders (fim do loop de
requisições) e o painel oferece "Fechar ajuda" em todos os estados.

## Alterações

| Arquivo | Alteração | Notas |
|------|--------|-------|
| `frontend/web/src/app/shell/help.service.ts` | modificado | URL via `appPath('/help.md', document.baseURI)` |
| `frontend/web/src/app/shell/workspace-shell.ts` | modificado | loaders executados em `untracked` no `effect` |
| `frontend/web/src/app/shell/help-panel.ts` | modificado | cabeçalho com "Fechar ajuda" fora do bloco condicional |
| `frontend/web/src/app/shell/help.service.spec.ts` | teste adicionado | base href `/notes/` |
| `frontend/web/src/app/shell/workspace-shell.spec.ts` | teste adicionado | falha não gera nova requisição e permite sair |

## Testes Adicionados ou Atualizados

- `help.service.spec.ts` › "resolve help.md sob o base path da aplicação".
- `workspace-shell.spec.ts` › "não repete a carga da ajuda em loop após falha e mantém a saída disponível".

## Verificação Local

- `ng test --watch=false` (container `angular-cli:22.2.4`) → 115/115.
- `start-local-lab.ps1 -Build -RecreateConsumers -RunE2E` → `11 passed`.
- Playwright pela borda nginx: 1 requisição para `/notes/help.md` (antes, 558
  requisições 404 a `/help.md` em 3 s), título "Ajuda do Nodra" com 4 seções e
  "Fechar ajuda" retornando a `/notes/workspace/lab-0001/files`.

## Desvios da Avaliação

- Nenhum.

## Acompanhamento

- O parser simples da ajuda (`help-markdown.ts`) exibe ênfases como `**texto**`
  literal; é limitação pré-existente, fora do escopo deste bug.

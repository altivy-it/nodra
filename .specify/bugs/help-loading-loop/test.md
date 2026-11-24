# Verificação de Bug: ajuda presa em "Carregando ajuda..."

- **Slug**: help-loading-loop
- **Testado em**: 2026-11-23
- **Avaliação**: ./assessment.md
- **Correção**: ./fix.md
- **Resultado**: verified

## Resumo

A reprodução pela borda nginx do laboratório deixou de ocorrer: a ajuda faz
uma única requisição a `/notes/help.md`, exibe o conteúdo e fecha
normalmente. Não houve regressões nas suítes executadas.

## Verificações Executadas

| Verificação | Comando / Ação | Resultado | Notas |
|-------|------------------|--------|-------|
| Reprodução após correção | Playwright (`lzocateli/playwright-e2e:0.1.0`) em `https://localhost:7443/notes`, vault `lab-0001`, clique em "Ajuda" | pass | 1 requisição (antes 558), título "Ajuda do Nodra", "Fechar ajuda" volta a `files` |
| Testes novos / atualizados | `ng test --watch=false` | pass | 115/115, inclui os 2 novos |
| Suíte de regressão | `start-local-lab.ps1 -Build -RecreateConsumers -RunE2E` | pass | `11 passed`, E2E real pelo nginx |
| Lint / verificação de tipos | `ng build --configuration production`; `validate-repository.ps1` | pass | build sem erros; 269 arquivos Markdown |

## Trechos de Saída

```text
REQUESTS 1 {'https://localhost:7443/notes/help.md'}
H1 ['Ajuda do Nodra'] H2 4
URL https://localhost:7443/notes/workspace/lab-0001/files HELP 0
Tests  115 passed (115)
11 passed in 19.93s
```

## Riscos Residuais

- O parser simplificado da ajuda mostra `**negrito**` literal (pré-existente).

## Recomendação

Encerre o bug: verificado ponta a ponta no laboratório.

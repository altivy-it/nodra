# Verificação de Bug: salvamento automático reenvia versão desatualizada

- **Slug**: new-note-save-conflict
- **Testado em**: 2026-11-23T11:55:00-06:00
- **Avaliação**: ./assessment.md
- **Correção**: ./fix.md
- **Resultado**: verified

## Resumo

A reprodução pelo nginx, com o `POST` atrasado e digitação durante o
salvamento, deixou de gerar `POST 409`. A sequência agora é `POST 201` seguido
de `PUT 204` com o `If-Match` correto, o conteúdo digitado é gravado e a nota
aparece no explorador. Não houve regressões.

## Verificações Executadas

| Verificação | Comando / Ação | Resultado | Notas |
|-------|------------------|--------|-------|
| Reprodução antes da correção | `test_new_note_race.py` (Playwright pelo nginx, `POST` atrasado em 1,5 s) | fail (esperado) | `POST 201` seguido de `POST 409` |
| Reprodução após a correção | mesmo teste, duas execuções | pass | `POST 201` → `PUT 204`, sem mensagem de erro, nota no explorador, conteúdo `# Nova nota\n a` no disco |
| Testes novos | `ng test --watch=false` | pass | 2 casos em `app.spec.ts`; a verificação negativa falha sem a correção |
| Suíte de regressão frontend | `ng test --watch=false` | pass | 117/117 |
| Build | `ng build --configuration production` | pass | |
| E2E real | `start-local-lab.ps1 -Build -RecreateConsumers -RunE2E` | pass | `11 passed` |

## Trechos de Saída

```text
NET POST 201 api/v1/vaults/lab-0001/notes/nova-nota%20-%20race56139.md
NET PUT 204 api/v1/vaults/lab-0001/notes/nova-nota%20-%20race56139.md "7D5F…DCF4"
ERRMSG 0
Tests  117 passed (117)
============================== 11 passed in 21.27s ==============================
```

## Riscos Residuais

- O caso equivalente com `PUT` seguido de `PUT` (nota existente) foi coberto
  pelo mesmo mecanismo, mas não foi reproduzido separadamente no navegador.
- Se o caminho de uma nota nova for alterado durante o `POST`, o próximo
  salvamento cria um arquivo no novo caminho, o que é comportamento
  pré-existente.

## Recomendação

Encerre o bug: verificado ponta a ponta.

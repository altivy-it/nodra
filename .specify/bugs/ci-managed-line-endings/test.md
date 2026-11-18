# Verificação de Bug: hashes de arquivos gerenciados no CI

- **Slug**: ci-managed-line-endings
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

A reprodução local falhou antes da correção com os mesmos 14 arquivos do CI
e passou depois em checkouts Git isolados. A verificação remota do PR ainda
não foi executada com a alteração.

## Verificações Executadas

| Verificação | Comando / ação | Resultado | Notas |
|---|---|---|---|
| Reprodução após correção | `tools/scripts/test-cicd-contracts.ps1` | pass | Checkouts com `core.autocrlf=false` e `true`; 37 hashes conferidos. |
| Alteração real | Teste negativo no fixture | pass | O verificador rejeitou o script adulterado. |
| Arquivos gerenciados | `Test-ManagedFiles.ps1` com os oito componentes | pass | 37 arquivos íntegros no checkout atual. |
| Regressão do validador | `test-validate-repository.ps1` e `validate-repository.ps1` | pass | 10 cenários e 233 arquivos Markdown verificados. |
| Sintaxe e whitespace | Parser PowerShell e `git diff --check` | pass | `PSScriptAnalyzer` não está disponível na imagem utilizada. |
| CI remoto e Windows nativo | Execução do PR e checkout em Windows | not-run | A alteração não foi enviada; a configuração Windows foi simulada no Git em container Linux. |

As verificações PowerShell foram executadas em container, sem acesso à rede,
com a imagem `mcr.microsoft.com/dotnet/sdk:10.0.404-noble`.

## Trechos de Saída

```text
Contratos estruturais CI/CD aprovados; nenhuma operacao remota executada.
Todos os 37 arquivos gerenciados de CI/CD conferem com .groundwork/manifest.json.
Testes do validador concluídos: 10 cenários aprovados.
```

## Riscos Residuais

- O check remoto permanece no resultado anterior até receber a alteração.
- A simulação de `core.autocrlf=true` não substitui execução em Windows.
- O BuildKit e o scanner de histórico mantêm bloqueios independentes.

## Recomendação

Após a atualização autorizada da branch, reexecutar o CI do PR e confirmar
`Hygiene / Managed files` e `Result` sem dispensar os demais gates.

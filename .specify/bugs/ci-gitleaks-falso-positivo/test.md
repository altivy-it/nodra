# Verificação de Bug: falso positivo do Gitleaks no histórico

- **Slug**: ci-gitleaks-falso-positivo
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

O sintoma foi reproduzido com o scanner e o formato de patches do CI e deixou
de ocorrer com a configuração nova. Os testes negativos passaram; a execução
efetiva no GitHub e o daemon Docker do runner ainda não foram verificados.

## Verificações Executadas

| Verificação | Comando / Ação | Resultado | Notas |
|---|---|---|---|
| Reprodução após correção | `gitleaks dir /history --redact --config /gitleaks.toml` | pass | Imagem do CI e patches das refs remotas; zero achados. |
| Reprodução anterior | Mesmo scanner sem `--config` | pass | Saída `1` e um achado no commit documentado na avaliação. |
| Testes novos | `test-gitleaks.ps1 -ScannerPath /usr/local/bin/gitleaks` | pass | Scanner real em container; linha permitida e credenciais sintéticas bloqueadas. |
| Mutações | Remover ou ampliar a exceção, torná-la global e remover regras padrão | pass | As quatro configurações incorretas foram rejeitadas. |
| Runner | Simulador do comando Docker, com o scanner real | pass | Argumentos e limpeza após falha verificados; não substitui Docker real. |
| Suíte de regressão | `test-cicd-contracts.ps1`, `Test-ManagedFiles.ps1`, `test-validate-repository.ps1` e `validate-repository.ps1` | pass | Contratos, 37 hashes, 10 cenários e 236 documentos. |
| Lint / tipos | Parser PowerShell, `Get-Help`, `--help` e `git diff --check` | partial | Sintaxe e ajuda aprovadas; `PSScriptAnalyzer` indisponível na imagem. |
| CI remoto | `Gitleaks / Full History` no PR | not-run | Requer atualização autorizada da branch. |

## Trechos de Saída

```text
original: código=1, achados=1
corrigido: código=0, achados=0
Testes do Gitleaks concluídos: exceção e cenários negativos aprovados.
```

## Riscos Residuais

- O runner do GitHub e sua execução real com Docker ainda não foram testados.
- Alterar a linha da tarefa exige revisar a exceção e seus testes.
- A validação não cobre PowerShell e Docker nativos no Windows.

## Recomendação

Aguardar atualização autorizada do PR e confirmar os checks remotos antes de
considerar o bug encerrado.

# Verificação de Bug: falso positivo no scanner legado do PR 31

- **Slug**: ci-gitleaks-fluxo-legado
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

O fluxo exato do workflow passou ao varrer todas as refs locais; os testes
reais mantiveram a detecção de credenciais sintéticas e regras independentes.
O novo job remoto ainda precisa confirmar o resultado.

## Verificações Executadas

| Verificação | Comando / Ação | Resultado | Notas |
|---|---|---|---|
| Reprodução após correção | Passo `Scan full Git history` extraído do workflow | pass | Todas as refs locais, Gitleaks 8.30.1, container sem rede. |
| Regressões | `pwsh -NoProfile -File tools/scripts/test-gitleaks.ps1 -HistoricalFixture` | pass | Runner e scanner em containers; cenários negativos aprovados. |
| Ausência de tarefa | `pwsh -NoProfile -File tools/scripts/test-gitleaks.ps1` | pass | Falha explícita sem a tarefa no modo padrão. |
| Lint | `actionlint` 1.7.12 em container | pass | Workflow legado. |
| Documentação | `validate-repository.ps1` em container | pass | 209 documentos verificados antes deste relatório. |
| Job remoto | `Gitleaks / Full History` no PR 31 | not-run | A correção local ainda não foi enviada. |

## Trechos de Saída

- Antes: um `generic-api-key` no commit `bbb9846`, referente à tag pública.
- Depois: `Testes do Gitleaks concluídos: exceção e cenários negativos aprovados.`
- Depois: workflow legado aprovado sobre o histórico completo.

## Riscos Residuais

- A execução local usa Podman compatível; o runner GitHub usa Docker Engine.

## Recomendação

Após o push autorizado, confirmar o job remoto sem dispensar outros gates.

# Verificação de Bug: seleção ambígua do Docker no teste Gitleaks

- **Slug**: ci-gitleaks-docker-path
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

A reprodução local com dois caminhos de Docker passou após a correção, assim
como o teste sintético de ordem e limpeza. Ainda não foi executada a
orquestração com Docker Engine real no GitHub.

## Verificações Executadas

| Verificação | Comando / Ação | Resultado | Notas |
|---|---|---|---|
| Reprodução após correção | `pwsh -NoProfile -File tools/scripts/test-gitleaks.ps1` | pass | Podman compatível orquestrou as imagens locais; os testes e o scanner rodaram em container sem rede. |
| Regressão e testes novos | `podman run --rm --pull=never --network none --read-only --tmpfs /tmp:rw --user 0:0 --env HOME=/tmp --volume "$PWD:/workspace:ro" --workdir /workspace --entrypoint pwsh docker.io/lzocateli/dotnet-sdk:10.0.403-noble-v1 -NoProfile -File tools/scripts/test-cicd-contracts.ps1` | pass | Verifica seleção do primeiro aplicativo, sequência, limpeza e erro sem Docker. |
| Sintaxe e ajuda | Parser PowerShell, `--help` e `Get-Help` em container | pass | Ambos os scripts alterados. |
| Análise estática | `PSScriptAnalyzer` na imagem SDK existente | not-run | Módulo indisponível. |
| Docker Engine real | Job `Gitleaks / Full History` no GitHub | not-run | Requer atualização remota autorizada. |

## Trechos de Saída

- Antes: os dois caminhos eram concatenados e o executável não era reconhecido.
- Depois: `Contratos estruturais CI/CD aprovados`.
- Depois: `Testes do Gitleaks concluídos: exceção e cenários negativos aprovados.`

## Riscos Residuais

- O resultado local usa Podman compatível, não o daemon Docker do runner GitHub.
- O lint específico `PSScriptAnalyzer` não foi executado.

## Recomendação

Reexecutar o job do GitHub após a atualização autorizada e confirmar o lint
quando houver imagem de ferramentas com `PSScriptAnalyzer`.

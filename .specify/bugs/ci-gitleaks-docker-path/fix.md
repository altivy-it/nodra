# Correção de Bug: seleção ambígua do Docker no teste Gitleaks

- **Slug**: ci-gitleaks-docker-path
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

O teste Gitleaks seleciona somente o primeiro executável Docker localizado no
`PATH`, mantendo a falha explícita quando não há aplicativo disponível.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| [test-gitleaks.ps1](../../../tools/scripts/test-gitleaks.ps1) | modificado | Seleciona o primeiro `Application` retornado pelo PowerShell. |
| [test-cicd-contracts.ps1](../../../tools/scripts/test-cicd-contracts.ps1) | teste atualizado | Exercita dois executáveis sintéticos, ordem, limpeza e ausência de Docker. |

## Testes Adicionados ou Atualizados

- `test-cicd-contracts.ps1` — o primeiro Docker sintético executa os quatro
  passos esperados, o segundo falha se for usado e o diretório do scanner é
  removido. Um `PATH` sem Docker também deve falhar com mensagem explícita.

## Verificação Local

- Antes da correção, o contrato com dois executáveis falhou reproduzindo a
  concatenação dos caminhos. Após a correção, os contratos CI/CD passaram em
  container isolado.
- `pwsh -NoProfile -File tools/scripts/test-gitleaks.ps1` orquestrou as imagens
  locais por Podman compatível e aprovou os cenários do scanner em container.
- Parser PowerShell e contratos `--help`/`Get-Help` passaram em container.
  `PSScriptAnalyzer` não está disponível na imagem existente.

## Desvios da Avaliação

- Nenhum.

## Acompanhamento

- Validar também com Docker Engine real no runner GitHub, que não está
  disponível neste host.

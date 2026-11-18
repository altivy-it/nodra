# Correção de Bug: limpeza do cache do Trivy no CI

- **Slug**: ci-trivy-cache-permissions
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

O cache passou de bind mount no host para volume Docker exclusivo, reutilizado
pelos scans e removido ao final. Falhas do scanner e de limpeza são informadas
explicitamente, sem desabilitar o gate de vulnerabilidades.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| [validate-release-images.ps1](../../../tools/scripts/validate-release-images.ps1) | modificado | Cria e remove o volume e mantém o erro do scanner visível. |
| [test-cicd-contracts.ps1](../../../tools/scripts/test-cicd-contracts.ps1) | teste atualizado | Exercita sucesso e falhas independentes ou simultâneas. |
| [ci-cd-seguranca.md](../../../docs/ci-cd-seguranca.md) | documentação | Explica o ciclo de vida do cache. |
| [quickstart.md](../../../specs/011-padronizacao-cicd/quickstart.md) | documentação | Registra o volume descartável. |

## Testes Adicionados ou Atualizados

- `test-cicd-contracts.ps1` — verifica criação, compartilhamento e remoção do
  volume, limpeza dos arquivos temporários e erros de scanner e de remoção.
- O teste novo falhou antes da correção porque nenhum volume era criado.

## Verificação Local

- Contratos CI/CD em container: aprovados após a correção.
- Volume descartável com diretório privado criado por root no container:
  removido pelo engine sem alterar o daemon compartilhado.
- Ajuda `--help`: aprovada em container sem rede.

## Desvios da Avaliação

- Nenhum.

## Acompanhamento

- Reexecutar o job real das cinco imagens e preservar o bloqueio de achados
  `HIGH,CRITICAL`; a imagem da API ainda apresenta vulnerabilidades.

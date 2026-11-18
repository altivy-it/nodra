# Correção de Bug: falso positivo no scanner legado do PR 31

- **Slug**: ci-gitleaks-fluxo-legado
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

O workflow legado mantém a varredura de todas as refs e usa patches por
commit com a exceção restrita já aplicada à feature 011. O teste de regras
também funciona antes de a tarefa original existir na branch.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| [.github/workflows/gitleaks.yml](../../../.github/workflows/gitleaks.yml) | modificado | Gera patches e executa scanner e testes sem rede no container. |
| [.gitleaks.toml](../../../.gitleaks.toml) | adicionado | Preserva as regras padrão e isenta somente a linha pública exata. |
| [test-gitleaks.ps1](../../../tools/scripts/test-gitleaks.ps1) | adicionado | Testa regras com Gitleaks real, inclusive usando a fixture histórica. |

## Testes Adicionados ou Atualizados

- `test-gitleaks.ps1 -HistoricalFixture` — testa a linha pública, sua
  presença em outro commit, credenciais sintéticas e uma regra independente.
- O passo de scan do próprio workflow foi executado sobre todas as refs locais
  com o scanner 8.30.1 em container.

## Verificação Local

- Scanner legado anterior: um achado `generic-api-key` no commit `bbb9846`,
  identificado como a tag pública da tarefa local.
- Workflow corrigido e testes negativos: aprovados com Podman compatível e
  Gitleaks real; `actionlint` 1.7.12 aprovou o YAML.
- Nenhum histórico foi reescrito, regra padrão desativada ou ref omitida.

## Desvios da Avaliação

- Nenhum.

## Acompanhamento

- Reexecutar o job GitHub após o push normal autorizado e confirmar o resultado.

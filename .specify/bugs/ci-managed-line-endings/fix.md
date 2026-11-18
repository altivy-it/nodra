# Correção de Bug: hashes de arquivos gerenciados no CI

- **Slug**: ci-managed-line-endings
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

Os atributos Git agora preservam os finais de linha exigidos pelo manifesto
Groundwork em checkouts com diferentes configurações de `core.autocrlf`.
Os arquivos gerenciados e seus hashes não foram alterados.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| `.gitattributes` | Modificado | LF para documentos, workflows e política gerenciados; CRLF para scripts PowerShell do catálogo. |
| `tools/scripts/test-cicd-contracts.ps1` | Teste adicionado | Cria checkouts Git isolados, confere os 37 hashes e verifica a rejeição de alteração real. |

## Testes Adicionados ou Atualizados

- O teste de contratos executa checkouts sintéticos com `core.autocrlf=false`
  e `core.autocrlf=true`, confere todos os 37 arquivos do manifesto e executa
  `Test-ManagedFiles.ps1` em cada checkout.
- O mesmo teste altera um arquivo no fixture e exige que a verificação o
  identifique como modificado.

## Verificação Local

- Antes da correção, o novo teste falhou para `core.autocrlf=false` com os
  mesmos 14 arquivos reportados pelo CI.
- Após a correção, `tools/scripts/test-cicd-contracts.ps1` passou em container
  sem rede usando `mcr.microsoft.com/dotnet/sdk:10.0.404-noble`.
- `tools/scripts/ci/Test-ManagedFiles.ps1 -Root /workspace` passou no checkout
  atual para os 31 arquivos selecionados por padrão.
- `tools/scripts/test-cicd-contracts.ps1 --help` e `git diff --check` passaram.

## Desvios da Avaliação

- Além dos scripts CRLF que falhavam no Linux, os atributos também fixam LF
  para documentos, workflows e política gerenciados. Isso preserva seus hashes
  em checkouts com `core.autocrlf=true`.

## Acompanhamento

- Executar `/speckit-bug-test slug=ci-managed-line-endings`.
- Atualizar a branch e conferir o CI remoto somente após autorização aplicável.
- Tratar separadamente a falha de atestação BuildKit e o achado do scanner.

# Correcao de Bug: peers Angular

- **Slug**: angular-peer-dependencies
- **Corrigido em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Estado**: partial

## Resumo

O manifest agora fixa o router em 22.1.3, a versao dos peers Angular existentes. A regeneracao estrita do lockfile revelou outro conflito preexistente em KaTeX/ngx-markdown, fora da avaliacao inicial, e foi interrompida sem flags permissivas.

## Alteracoes

| Arquivo | Alteracao | Notas |
| --- | --- | --- |
| frontend/web/package.json | Router 22.2.0 para 22.1.3 | Versao exata alinhada aos peers |

## Testes Adicionados ou Atualizados

Reutilizado o gate de instalacao estrita existente no CI. Nenhum teste e declarado aprovado.

## Verificacao Local

Container oficial: npm install --package-lock-only --ignore-scripts --no-audit --no-fund falhou com ERESOLVE em ngx-markdown@22.0.2, cujo peerOptional katex e ^0.16.0, versus katex@0.18.2 declarado. npm ci, testes e build subsequentes nao executaram.

## Desvios da Avaliacao

- Foi escolhida versao exata 22.1.3 em vez de range ^22.1.0 para nao resolver versoes adicionais durante a correcao.
- Existe outro conflito de peers nao coberto na avaliacao inicial. Nao alterar KaTeX nem ampliar atualizacoes sem nova avaliacao e autorizacao do escopo.

## Acompanhamento

Executar nova avaliacao do conflito KaTeX/ngx-markdown caso o usuario autorize o escopo adicional. Regenerar lockfile e executar os gates antes de considerar a correcao aplicada e validada.
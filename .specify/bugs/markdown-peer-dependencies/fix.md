# Correcao de Bug: peers Markdown

- **Slug**: markdown-peer-dependencies
- **Corrigido em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

KaTeX foi fixado em 0.16.47, compatível com ngx-markdown@22.0.2. O lockfile foi regenerado em container sem ignorar peers, mantendo as atualizacoes de dependencias anteriormente integradas.

## Alteracoes

| Arquivo | Alteracao | Notas |
| --- | --- | --- |
| frontend/web/package.json | KaTeX 0.16.47 | Versao exata com licenca MIT verificada |
| frontend/web/package-lock.json | Resolucao estrita regenerada | Inclui alinhamento do router 22.1.3 da correcao anterior |

## Testes Adicionados ou Atualizados

Reutilizados os gates de instalacao, 100 testes existentes em 18 arquivos e build de producao. Nenhum pacote novo foi adicionado.

## Verificacao Local

Imagem docker.io/lzocateli/angular-cli:22.1.0-node24.15.0-bookworm, com node_modules, dist e cache em tmpfs: regeneracao de lock, npm ci, npm test -- --watch=false e build de producao aprovados. Diff de dependencias limitado a 2 arquivos, 13 insercoes e 29 remocoes.

## Desvios da Avaliacao

Nenhum alem da combinacao necessaria com o alinhamento Angular anterior, explicitamente preservado na proposta.

## Acompanhamento

Reexecutar os mesmos gates depois da renomeacao. Avisos de scripts de instalacao bloqueados pelo npm foram exibidos, mas nao impediram testes ou build.
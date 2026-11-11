# Verificacao de Bug: peers Markdown

- **Slug**: markdown-peer-dependencies
- **Testado em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Correcao**: [fix.md](fix.md)
- **Resultado**: verified

## Resumo

A instalacao estrita deixou de reproduzir o ERESOLVE apos o alinhamento do KaTeX com o renderer Markdown. Suites existentes de Markdown, matematica e preview passaram juntamente com o restante dos testes frontend.

## Verificacoes Executadas

| Verificacao | Comando interno ao container oficial | Resultado | Notas |
| --- | --- | --- | --- |
| Reproducao estrita | npm ci --no-audit --no-fund | pass | Sem force ou legacy-peer-deps |
| Suites existentes | npm test -- --watch=false | pass | 100 testes em 18 arquivos |
| Compilacao/tipos | npm run build -- --configuration production | pass | Build de producao concluido |

## Trechos de Saida

As suites note-markdown, help-markdown e markdown-preview passaram; resumo total 100 passed (100).

## Riscos Residuais

Scripts de instalacao de pacotes nativos foram bloqueados por politica padrao do npm, sem falha de build/testes. Auditoria e verificacao visual permanecem gates da feature.

## Recomendacao

Encerrar o conflito de peers Markdown; repetir gates depois da renomeacao sem atualizar dependencias alheias.
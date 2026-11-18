# Verificação de Bug: vulnerabilidades nas dependências do frontend

- **Slug**: frontend-dependabot-alerts
- **Testado em**: 2026-11-18
- **Avaliação**: ./assessment.md
- **Correção**: ./fix.md
- **Resultado**: partial

## Resumo

A instalação estrita, a auditoria, os testes existentes e o build de produção
confirmaram as versões corrigidas e nenhuma vulnerabilidade reportada. A
renderização completa de SVG Mermaid não pôde ser validada no jsdom e requer
verificação em browser/CI.

## Verificações Executadas

| Verificação | Comando / Ação | Resultado | Notas |
|-------|------------------|--------|-------|
| Reprodução após correção | `npm audit --json` e inspeção da árvore instalada | pass | Código de saída 0; zero vulnerabilidades. Vite 8.1.6, Piscina 5.3.2, KaTeX 0.18.2 e família Angular 22.2.1. |
| Testes novos / atualizados | `ng test --watch=false` | pass | Nenhum teste novo; 18 arquivos e 101 testes existentes passaram, incluindo fórmulas KaTeX e transformação Mermaid. |
| Suíte de regressão | `npm ci --no-audit --no-fund` | pass | Instalação limpa no container oficial sem relaxar peers. |
| Build de produção | `ng build --configuration production --output-path=/tmp/nodra-web-build` | pass | Bundle de produção gerado com sucesso em diretório isolado do workspace. |
| Lint / verificação de tipos | `ng build --configuration production --output-path=/tmp/nodra-web-build` | pass | Não há script independente de lint configurado; o build Angular validou compilação e tipos. |
| Renderização completa Mermaid | `mermaid.render` em Vitest/jsdom | not-run | A tentativa falhou porque jsdom não implementa `SVGElement.getBBox`; não indica falha do pacote no browser. |

## Trechos de Saída

- Testes: `Test Files 18 passed (18)` e `Tests 101 passed (101)`.
- Build: `Application bundle generation complete`.
- Auditoria: `total: 0`; `exitCode: 0`.
- Árvore npm: todas as dependências Angular de runtime e `compiler-cli` em
  22.2.1; KaTeX 0.18.2 deduplicado sob Mermaid e ngx-markdown.

## Riscos Residuais

- A renderização Mermaid em DOM SVG real não foi exercitada; validar no CI ou
  em browser antes de encerrar a verificação.
- O build padrão, escrevendo em `dist/`, encontrou `EACCES` em um artefato
  preexistente. O mesmo build passou ao direcionar a saída para
  `/tmp/nodra-web-build`.
- Os checks remotos ainda precisam ser reexecutados após o push da história
  reescrita.

## Recomendação

A correção das vulnerabilidades está confirmada localmente, mas mantenha o
resultado como parcial até validar a renderização Mermaid em browser e obter
checks verdes dos novos SHAs no CI.

# Correção de Bug: vulnerabilidades nas dependências do frontend

- **Slug**: frontend-dependabot-alerts
- **Corrigido em**: 2026-11-18
- **Avaliação**: ./assessment.md
- **Estado**: applied

## Resumo

As versões vulneráveis de Vite, Piscina e KaTeX foram substituídas pelas
versões corrigidas, e a família Angular foi alinhada a 22.2.1 para remover
também o alerta do Router encontrado durante a auditoria. A atualização de
ngx-markdown permite usar KaTeX 0.18.2 sem relaxar a resolução de peers.

## Alterações

| Arquivo | Alteração | Notas |
|------|--------|-------|
| `frontend/web/package.json` | modificado | Alinha Angular e compiler-cli em 22.2.1, atualiza KaTeX e ngx-markdown e fixa versões corrigidas de KaTeX, Piscina e Vite em overrides. |
| `frontend/web/package-lock.json` | modificado | Registra as versões alinhadas e corrigidas e remove a cópia vulnerável de KaTeX da árvore do Mermaid. |

## Testes Adicionados ou Atualizados

A suíte existente cobre a renderização de fórmulas KaTeX e a transformação de
blocos Mermaid em `frontend/web/src/app/shell/markdown-preview.spec.ts`. Uma
tentativa de renderizar o SVG completo do Mermaid no jsdom não foi mantida como
teste porque o ambiente não implementa `SVGElement.getBBox`; a renderização
completa em browser permanece sem verificação nesta correção.

## Verificação Local

- `npm ci` no container oficial do frontend → passou sem relaxar peers.
- `ng test --watch=false` → 18 arquivos e 101 testes passaram.
- `ng build --configuration production --output-path=/tmp/nodra-web-build` → passou.
- `npm ls @angular/common @angular/core @angular/router @angular/compiler-cli katex ngx-markdown piscina vite` → confirmou Angular/compiler-cli 22.2.1, KaTeX 0.18.2, Piscina 5.3.2 e Vite 8.1.6.
- `npm audit` → zero vulnerabilidades informativas, baixas, moderadas, altas ou críticas.
- As versões exatas atualizadas usam licença MIT, conforme metadados npm.

## Desvios da Avaliação

- A atualização de `@angular/build` para 22.2.4 foi tentada inicialmente, mas a resolução estrita selecionou `@angular/compiler-cli` 22.3.0, incompatível com o compilador Angular 22.1.3 fixado. Para manter a mudança mínima, o builder permaneceu em 22.1.5; overrides explícitos atualizam Vite para 8.1.6 e Piscina para 5.3.2. A família de runtime e compiler-cli foi alinhada em 22.2.1, versão corrigida do Router que também satisfaz os peers exatos do Angular.
- O override de KaTeX 0.18.2 também se aplica à dependência transitiva do Mermaid, cujo peer declarado aceita apenas KaTeX `^0.16.47`. `npm ci`, a suíte de testes e o build passaram; a árvore resolvida contém uma única versão de KaTeX.
- A auditoria encontrou um alerta alto do Angular Router fora dos sete alertas originais. A família Angular foi atualizada como ampliação direta do escopo; a auditoria final passou sem vulnerabilidades.

## Acompanhamento

- Reexecutar CI após a reescrita do histórico e confirmar os novos checks do PR.
- Confirmar no CI que a renderização Mermaid permanece compatível; o teste unitário local não executa layout SVG real.
- Os alertas do Dependabot na branch padrão só serão encerrados após integração das alterações; os PRs devem permanecer em rascunho até decisão explícita de merge.

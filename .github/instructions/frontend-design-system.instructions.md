---
name: "Design System Frontend"
description: "Use ao criar ou alterar design system, tokens, temas, ícones e componentes base Angular do Nodra."
applyTo: "frontend/libs/frontend/design-system/**, frontend/web/src/app/design-system/**"
---

# Design system

- `design-system` compõe e tematiza componentes PO UI (`@po-ui/ng-components`); não recria controle de formulário, tabela, modal, navegação ou feedback que a biblioteca já resolve. Veja `frontend-po-ui.instructions.md`.
- Exceção aprovada: o editor Markdown (CodeMirror 6) e o grafo de notas (Cytoscape) permanecem implementações dedicadas por não terem equivalente no portfólio PO UI; use Angular CDK (foco, overlay, drag and drop) apenas dentro dessas duas superfícies.
- Tokens usam CSS custom properties do tema PO UI para superfície, texto, borda, foco, estado, tipografia, espaço e densidade; não espalhe cores, spacing ou z-index literais fora dos tokens.
- Suporte temas claro, escuro e alto contraste sem alterar semântica nem quebrar foco, contraste ou densidade já resolvidos pelo PO UI.
- Ícones de UI genérica vêm de `po-icon`; botões de ícone têm nome acessível e tooltip quando não óbvios. Lucide Angular permanece restrito ao editor e ao grafo.
- Não crie card customizado fora do que o PO UI já oferece; não aninhe estruturas equivalentes nem as use para layout de página, shell ou painéis.
- Componentes têm dimensões estáveis, estados disabled/loading/error e navegação por teclado.
- Texto não sobrepõe conteúdo nem escala por largura da viewport.
- Teste API pública, teclado, foco, contraste, temas, densidades e viewports relevantes.
---
name: "Design System Frontend"
description: "Use ao criar ou alterar design system, tokens, temas, ícones e componentes base Angular do Altivy."
applyTo: "frontend/libs/frontend/design-system/**, frontend/web/src/app/design-system/**"
---

# Design system

- Componentes base são acessíveis, sem regra de negócio e independentes de feature.
- Tokens usam CSS custom properties para superfície, texto, borda, foco, estado, tipografia, espaço e densidade.
- Não espalhe cores, spacing ou z-index literais fora dos tokens.
- Suporte temas claro, escuro e alto contraste sem alterar semântica.
- Use Angular CDK para foco, overlay e comportamentos complexos.
- Use Lucide Angular; botões de ícone têm nome acessível e tooltip quando não óbvios.
- Cards têm raio máximo de 8 px e servem apenas para itens ou ferramentas realmente delimitadas.
- Não aninhe cards nem use cards para layout de página, shell ou painéis.
- Componentes têm dimensões estáveis, estados disabled/loading/error e navegação por teclado.
- Texto não sobrepõe conteúdo nem escala por largura da viewport.
- Teste API pública, teclado, foco, contraste, temas, densidades e viewports relevantes.
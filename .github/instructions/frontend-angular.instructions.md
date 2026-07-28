---
name: "Frontend Angular"
description: "Use ao criar ou alterar Angular 21, TypeScript, componentes, páginas, estado, rotas, editor Markdown, grafo, acessibilidade ou UX do Basalt Notes."
applyTo: "apps/web/**, libs/frontend/**"
---

# Angular e TypeScript

## Arquitetura

- Use Angular 21, componentes standalone, TypeScript estrito e lazy loading por feature.
- Organize `apps/web/src/app` em `core`, `shell`, `features`, `data-access` e `design-system`.
- Coloque bibliotecas reutilizáveis e sem dependência do app em `libs/frontend`.
- Features não importam arquivos internos de outras features; integração ocorre por rotas, contratos públicos ou fachadas pequenas.
- Componentes de apresentação não chamam HTTP, IndexedDB ou SignalR diretamente.
- O cliente HTTP é gerado do OpenAPI; não duplique DTOs manualmente.

## Estado e assincronicidade

- Use Signals para estado local, computado e de sessão.
- Use RxJS nas bordas assíncronas, streams e cancelamento; converta para Signals próximo do consumidor quando útil.
- Não adicione store global por padrão. O dono do estado deve ter escopo claro.
- Não use `useMemo`-like abstractions ou caches sem medição; computed Signals cobrem derivações.
- Cancele pesquisa e carregamentos obsoletos; trate loading, vazio, erro, forbidden, offline e conflito.

## Componentes

- Prefira inputs/outputs tipados, APIs pequenas e change detection eficiente.
- Use control flow nativo (`@if`, `@for`, `@switch`) e `track` estável em listas.
- Não faça lógica relevante no template.
- Use Angular CDK para overlay, foco, drag and drop, virtualização e acessibilidade.
- Use Lucide Angular para ícones e tooltip em controles não óbvios.
- Não use cards para estruturar shell, editor ou painéis de trabalho.

## Editor e conteúdo

- CodeMirror 6 é o editor; Markdown permanece texto canônico.
- Preserve seleção, scroll e estado da aba sem alterar o conteúdo.
- Autosave envia `If-Match`; conflito preserva versão local e remota.
- IndexedDB serve apenas para rascunho recuperável.
- Nunca renderize HTML de Markdown sem a política única de sanitização.
- Não execute scripts, handlers, iframes ou plugins arbitrários do vault.

## Acessibilidade e desempenho

- Meta WCAG 2.2 AA, foco visível, teclado completo e anúncios de estado.
- Virtualize árvores, resultados e listas extensas.
- Use Web Worker para parsing auxiliar ou layouts pesados comprovados.
- Não escale fonte com viewport; mantenha texto legível e sem sobreposição.
- Respeite redução de movimento e temas claro, escuro e alto contraste.

## Qualidade

- Testes de unidade usam Vitest; fluxos de usuário usam Playwright.
- Teste estados e comportamento, não detalhes privados do Angular.
- Execute format, lint, typecheck, testes do recorte e build de produção.
- Mudanças visuais exigem verificação desktop e mobile e ausência de erros no console.

Consulte [implementação do frontend](../../docs/frontend.md).
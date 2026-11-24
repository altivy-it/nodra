---
name: "Frontend Angular"
description: "Use ao criar ou alterar Angular 22, TypeScript, componentes, páginas, estado, rotas, editor Markdown, grafo, acessibilidade ou UX do Nodra."
applyTo: "frontend/web/**, frontend/libs/frontend/**"
---

# Angular e TypeScript

## Execução local

- Execute os comandos Angular a partir de `frontend/web` usando o alias `ng` do
	profile do PowerShell. O alias fornece o runtime containerizado oficial do
	projeto.
- Quando um comando ou teste precisar manter a aplicação em execução, o
	Angular, BFF e API rodam exclusivamente em containers na rede Docker e
	todo HTTP do navegador/Playwright passa pelo nginx containerizado. Para
	Playwright simulado use `deploy/e2e/nginx-mocked.conf`; para integração real
	use o gateway descrito em `deploy/e2e/README.md`. Não acesse a porta do
	`ng serve` ou dos serviços diretamente, mesmo quando publicados no host.
- Antes de invocar `ng`, confirme o diretório atual. Quando o terminal iniciar
	na raiz do monorepo, execute
	`Set-Location $env:USERPROFILE\projetos\nodra\frontend\web`; a raiz não
	contém `angular.json` e resulta no erro “outside a workspace”.
- Envie somente o comando literal ao terminal; nunca prefixe `ng` com `^U`,
	`Ctrl+U` ou outra sequência de controle. Se essa sequência aparecer no prompt,
	cancele a entrada e execute novamente apenas o comando limpo.
- Aguarde o resumo final de `ng test` ou `ng build` antes de reportar o
	resultado. O Angular pode imprimir a geração do bundle antes de o Vitest
	terminar, e a saída do terminal pode ser entregue em partes.
- Use `ng build --configuration production` para o build e
	`ng test --watch=false` para os testes. Não execute `node`, `npm`, `npx` ou
	`ng` instalado localmente no host.

## Arquitetura

- Use Angular 22, componentes standalone, TypeScript estrito e lazy loading por feature.
- Organize `frontend/web/src/app` em `core`, `shell`, `features`, `data-access` e `design-system`.
- Coloque bibliotecas reutilizáveis e sem dependência do app em `frontend/libs/frontend`.
- Features não importam arquivos internos de outras features; integração ocorre por rotas, contratos públicos ou fachadas pequenas.
- Componentes de apresentação não chamam HTTP, IndexedDB ou SignalR diretamente.
- O cliente HTTP é gerado do OpenAPI; não duplique DTOs manualmente.
- Use componentes PO UI (`@po-ui/ng-components`) para toda UI genérica; não invente componente nem adote outra biblioteca de componentes fora do editor Markdown e do grafo de notas. Veja `frontend-po-ui.instructions.md`; enquanto o PO UI não suportar Angular 22, use os componentes `nd-*` de `frontend/web/src/app/design-system` (ADR 2026-0010).

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
- Use Angular CDK para overlay, foco, drag and drop, virtualização e acessibilidade no editor Markdown e no grafo de notas.
- Use ícones `po-icon` para UI genérica e tooltip em controles não óbvios; Lucide Angular permanece restrito ao editor e ao grafo.
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

Consulte [implementação do frontend](../../docs/frontend.md), `frontend-po-ui.instructions.md` e `frontend-visual-verification.instructions.md`.
---
name: "Frontend PO UI"
description: "Use ao criar, alterar ou revisar telas, formulários, tabelas, navegação e design system Angular do Nodra com a biblioteca PO UI."
applyTo: "frontend/web/**, frontend/libs/frontend/**"
---

# PO UI

- Compatibilidade de versão é requisito: não instale uma versão de PO UI cujos `peerDependencies` não incluam a versão major do Angular do projeto. Se isso bloquear uma entrega explicitamente solicitada, use controles Angular nativos e locais somente para as capacidades sem alternativa compatível, sem adicionar outra biblioteca visual; registre a exceção e seu escopo no plano. Reavalie a exceção quando PO UI suportar a major adotada. Exceção vigente: Angular 22 com o design system local `nd-*` da spec 007 ([ADR 2026-0010](../../docs/adr/2026-0010-design-system-proprio-base-obsidian.md)).

- PO UI (`@po-ui/ng-components`, com `@po-ui/ng-templates` quando um page template resolver o layout) é a única biblioteca de componentes de UI genéricos aprovada para o Nodra. Não instale nem invente componente visual alternativo (Material, PrimeNG, Bootstrap, CSS customizado que duplica um componente PO UI existente).
- Exceção explícita e já aprovada: o editor Markdown (CodeMirror 6) e o grafo de notas (Cytoscape) são superfícies especializadas sem equivalente no portfólio PO UI; permanecem implementações dedicadas. Fora dessas duas superfícies, siga a regra de portfólio abaixo.
- Antes de implementar qualquer tela, formulário, tabela, modal, navegação ou feedback, procure o componente PO UI equivalente. Não escreva markup ou CSS que reproduz comportamento que a biblioteca já resolve (foco, teclado, ARIA, validação, i18n).
- Consulte a documentação PO UI otimizada para IA antes de assumir uma API de componente:
  - índice: `https://po-ui.io/llms.txt`;
  - documentação completa: `https://po-ui.io/llms-full.txt`;
  - por componente/serviço/interface/enum: `https://po-ui.io/llms-generated/<slug>.md` (ex.: `po-table.md`, `po-modal.md`, `po-dialog-service.md`).
- Se o servidor MCP `po-ui` estiver configurado (`.vscode/mcp.json`), prefira as ferramentas `list_components`, `search_docs`, `get_component_docs`, `get_guide` e `get_component_examples` a adivinhar props, eventos ou comportamento.
- Se nenhum componente do portfólio PO UI atender à necessidade (fora do editor Markdown e do grafo de notas), pare a implementação, explicite a lacuna encontrada e pergunte ao responsável antes de criar alternativa. Não presuma aprovação de componente fora do portfólio.
- `frontend/libs/frontend/design-system` compõe e tematiza componentes PO UI (tokens, copy e wrappers finos para o contrato do Nodra); não reimplementa comportamento interno do componente nem recria controle de formulário, tabela, modal ou navegação já coberto pela biblioteca.
- Ícones de UI genérica vêm de `po-icon`; dentro do editor Markdown e do grafo de notas, quando `po-icon` não cobrir a necessidade, o ícone pode seguir a biblioteca já usada nessas duas superfícies.
- Formulários usam componentes `po-*` (ex.: `po-input`, `po-select`, `po-combo`, `po-checkbox`) com Reactive Forms. Diálogos usam `po-modal`/`PoDialogService`. Feedback assíncrono usa `PoNotificationService`/`po-toaster`.
- Layout de página segue os page templates PO UI (`po-page-default`, `po-page-list`, `po-page-dynamic-*`); o shell do editor e do painel de grafo permanecem customizados por serem as duas exceções aprovadas.
- Personalize tema e densidade por tokens/CSS custom properties do PO UI para refletir a identidade do Nodra; não sobreponha estilos que quebrem foco, contraste ou estados já resolvidos pela biblioteca.
- Toda tela nova documenta, no pull request, qual(is) componente(s) PO UI foram usados e por quê. Se a resposta for "nenhum equivalente" fora do editor/grafo, isso aparece como pergunta explícita ao responsável, não como decisão silenciosa.

Consulte `frontend-angular.instructions.md`, `frontend-design-system.instructions.md` e `docs/frontend.md`.

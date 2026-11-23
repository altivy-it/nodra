# Pesquisa: Identidade visual Nodra, tema de login e autenticação

## D1 — Licença do Dokpod

- **Decisão**: reproduzir padrões, não código.
- **Motivo**: o Dokpod é AGPL-3.0 e o Nodra é MIT; o ADR 2026-0009 já proíbe incorporar código do Dokpod.
- **Alternativa rejeitada**: copiar `altivy-kit.css`, `icon-paths.ts` e componentes `dok-*`.

## D2 — Biblioteca de componentes

- **Decisão**: design system local `nd-*` sem dependência nova.
- **Motivo**: `@po-ui/ng-components` 21.38.0 declara `peerDependencies` `@angular/core ^21`; o Nodra usa Angular 22. A própria regra de PO UI prevê controles locais nesse caso. O responsável pediu componentes como os do Dokpod (ADR 2026-0010).

## D3 — Base visual Obsidian

- **Decisão**: tema escuro padrão com cinzas neutros (`#1e1e1e` editor, `#262626` painéis, `#363636` bordas, `#dadada` texto) e acento violeta (`#a78bfa`); temas claro e alto contraste mantidos por tokens alternativos.
- **Motivo**: organização ribbon/explorador/abas/editor/painel/status do Obsidian; acento violeta associado ao Obsidian, distinto do teal do Dokpod.
- **Vidro**: somente na entrada anônima, nos estados e no login do Keycloak (como no Dokpod); o workspace usa superfícies sólidas para leitura longa.
- **Raios**: 6 px em controles do workspace, 14–20 px em cartões de vidro e pílula no CTA, revisando o limite de 8 px de `docs/frontend.md` (ADR 2026-0010).

## D4 — Fontes

- **Decisão**: Outfit (400/500/600) e JetBrains Mono (400/500), `@fontsource` 5.3.0, OFL-1.1.
- **Proveniência**: `outfit-5.3.0.tgz` sha256 `1060497f324868b0a2173b6834248d48859faac5664184ec4c15aee0586fad8d`; `jetbrains-mono-5.3.0.tgz` sha256 `1bbea47d1387406da5b6ccc4184cc61eae6851cc0db5c1d9bbd088ea9daa0b4a`. Somente subsets latin normais.
- Manrope e IBM Plex Mono deixam de ser usadas e são removidas.

## D5 — Marca

- **Símbolo**: quadrado arredondado violeta com um "N" formado por três nós ligados por arestas (grafo de notas conectadas), em tinta escura.
- **Wordmark**: "Nodra" em Outfit 600 convertido em texto SVG com fallback de sistema.
- **Favicons**: PNG 16/32/48/192 renderizados do SVG via Playwright em container; variantes claro/escuro preservam os IDs usados por `syncIcons`.

## D6 — Tema do Keycloak

- **Decisão**: manter o slot `altivy` (montado pelo AKB e referenciado em `products/altivynotes.json`) com conteúdo Nodra; `styles=css/styles.css css/fonts.css css/tokens.css css/login.css` (preserva o layout do `keycloak.v2`); sem JavaScript; `darkMode=false`; somente escuro.
- **Build**: `build-theme.ps1` copia `tokens.css`, `fonts.css` e as fontes de `frontend/web/src/styles` e gera `SHA256SUMS`; `start-local-lab.ps1` usa o artefato gerado.
- `displayName` do realm passa a "Nodra".

## D7 — Sessão

- **Decisão**: estado derivado `loading | error | anonymous | authenticated | expired`; `login()` monta `bff/login?returnUrl=` com caminho relativo ao base href; interceptor funcional marca `expired` em 401 de qualquer URL exceto `bff/session`.
- **Segurança**: `returnUrl` é validado pelo BFF com `LocalRedirect`; nenhum token no navegador.

## D8 — Claude Code

- **Decisão**: `CLAUDE.md` importando `@.github/copilot-instructions.md` e symlink `.claude/skills -> ../.github/skills`, como no Dokpod.

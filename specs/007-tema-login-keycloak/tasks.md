# Tarefas: Identidade visual Nodra, tema de login do Keycloak e autenticação

**Entrada**: `specs/007-tema-login-keycloak/` (`spec.md`, `plan.md`, `research.md`, `quickstart.md`)

**Idioma**: Português brasileiro (pt-BR); preserve IDs, comandos, nomes de assets e paths.

**Testes**: unitários Angular (`ng test`), build de produção, E2E do laboratório pelo nginx e loop visual com screenshots.

Checkboxes registram execução validada, não aprovação humana.

## Fase 1: Preparação (US5)

- [X] T010 [P] [US5] Criar `CLAUDE.md`, que importa `.github/copilot-instructions.md`, e o link `.claude/skills` → `../.github/skills`, sem duplicar conteúdo.
- [X] T011 [P] Registrar decisões D1–D8 em `research.md` e o [ADR 2026-0010](../../docs/adr/2026-0010-design-system-proprio-base-obsidian.md) (status `proposed`, revisor pendente).

## Fase 2: Fundação visual (US1, US4)

- [X] T020 [US1] Criar símbolo, wordmark e favicons Nodra em `frontend/web/public/brand`, `favicon.svg` e `public/icons/*.png` (16/32/48/192, claro e escuro).
- [X] T021 [US4] Versionar Outfit e JetBrains Mono (OFL-1.1) em `frontend/web/src/styles/fonts` e remover Manrope/IBM Plex.
- [X] T022 [US4] Criar `tokens.css` (escuro Obsidian padrão, claro, sistema e alto contraste), `fonts.css`, `nodra-kit.css` e reescrever `styles.css`.
- [X] T023 [US4] Criar o design system `nd-*` (`nd-icon`, `nd-button`, `nd-card`, `nd-empty-state`, `nd-brand`) em `frontend/web/src/app/design-system`.
- [X] T024 [US4] Testar aria de ícone, `loading`/`disabled` de botão e roles de estados em `design-system.spec.ts`.

## Fase 3: Autenticação (US3)

- [X] T030 [US3] Testar estados `loading`/`error`/`anonymous`/`authenticated`/`expired`, `loginUrl` com e sem `returnUrl` e o interceptor 401 em `session.service.spec.ts`.
- [X] T031 [US3] Implementar `SessionService.state`, `expire()`, `loginUrl()` e `session-expiry.interceptor.ts`, registrado em `app.config.ts`.
- [X] T032 [US3] Redesenhar a entrada anônima, a sessão expirada e os estados de carregamento/erro/sem vault em `app.html`/`app.css`/`app.ts`, preservando os seletores do E2E.

## Fase 4: Workspace Obsidian (US4)

- [X] T040 [US4] Remapear o shell e o explorador para os tokens, trocar glifos por `nd-icon` (`activity-registry.ts` usa `icon`) e usar `nd-brand` no topo.
- [X] T041 [US4] Sincronizar o tema escolhido em `document.documentElement` para overlays fora do shell.
- [X] T042 [US4] Atualizar `workspace-shell.spec.ts` (marca SVG e ícones da ribbon).

## Fase 5: Tema do Keycloak (US2)

- [X] T050 [US2] Reescrever `deploy/keycloak/themes/altivy/login` (`theme.properties` sem JS e `login.css` sobre tokens) e remover `theme-toggle.js`, Manrope e logos antigos.
- [X] T051 [US2] Fazer `deploy/keycloak/build-theme.ps1` copiar tokens, fontes, licenças e símbolo do frontend e gerar `SHA256SUMS`.
- [X] T052 [US2] Fazer `tools/scripts/start-local-lab.ps1` sincronizar o artefato no bind mount e definir `displayName` "Nodra" no realm (`configure-keycloak.ps1` e `realm/altivy-realm.json`).

## Fase 6: Documentação e validação

- [X] T060 Atualizar `docs/frontend.md`, `docs/configuracao-keycloak.md`, `deploy/keycloak/README.md`, `docs/auditoria-licencas-sbom.md` e as instruções de frontend.
- [X] T061 Executar `ng test` e `ng build --configuration production`.
- [X] T062 Executar `start-local-lab.ps1 -Build -RecreateConsumers -RunE2E` com Keycloak real pelo nginx.
- [X] T063 Capturar e revisar screenshots (desktop e mobile) da entrada, do login do Keycloak e do workspace nos temas escuro, claro e alto contraste.
- [X] T064 Executar `tools/scripts/validate-repository.ps1`.

## Fase 6.1: Ajustes pós-validação humana (2026-11-23)

- [X] T065 Exibir no botão de tema o ícone do tema atual (`monitor`, `sun`, `moon`, `contrast`) com rótulo acessível em `frontend/web/src/app/shell/`.
- [X] T066 Tornar o nome do usuário um link para `GET /bff/account`, que redireciona ao console de conta do Keycloak (`backend/apps/bff/Authentication/KeycloakAccountConsole.cs`).
- [X] T067 Preservar no preview todas as tags e atributos Markdown/HTML, bloqueando somente execução de código, com política única em `frontend/web/src/app/core/markdown-preview-policy.ts`.

## Fase 7: Promoção (US6, bloqueada)

P-01 a P-03 foram registrados como concluídos no laboratório. P-04 segue bloqueada.

- [ ] T001 [US6] Criar alerta/métrica de falha em assets do tema e documentar em `docs/observabilidade-alertas-slo.md`.
- [ ] T002 [US6] Provisionar ambiente de homologação isolado conforme `specs/003-ambiente-homologacao/` e validar o gate antes de promoção.
- [ ] T003 [US6] Promover artefato por checksum e validar login, erro, OTP, recuperação e atualização de credencial na homologação.
- [ ] T004 [US6] Executar rollback ao tema padrão e registrar a validação em `docs/configuracao-keycloak.md`.
- [ ] T005 [US6] Revisar segurança do pacote e acessibilidade visual, sem recurso externo.

## Dependências e ordem

Fase 2 precede 3 e 4; T022 precede T050/T051 (tokens compartilhados); Fases 1–5 precedem a Fase 6. A Fase 7 depende de homologação real e de revisão humana do ADR 2026-0010; não promover nesta feature.

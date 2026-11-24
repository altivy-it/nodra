# Plano de Implementação: Identidade visual Nodra, tema de login do Keycloak e autenticação

**Branch**: `007-tema-login-keycloak` | **Data**: 2026-11-23 | **Spec**: [spec.md](spec.md)

**Entrada**: [spec.md](spec.md), [research.md](research.md) e ADR
[2026-0010](../../docs/adr/2026-0010-design-system-proprio-base-obsidian.md).

**Idioma**: Português brasileiro (pt-BR); preserve comandos, paths e nomes de asset.

## Resumo

Criar a marca Nodra (símbolo, wordmark e favicons), tokens CSS de base Obsidian
compartilhados entre o app Angular e o tema `altivy` do Keycloak, um design
system local `nd-*`, o fluxo de sessão com estados explícitos, `returnUrl` e
expiração por 401, reestilizar o shell no padrão Obsidian e habilitar o Claude
Code com as instruções existentes. A promoção do tema em homologação continua
bloqueada (US6).

## Contexto Técnico

**Linguagem/Versão**: Angular 22 (TypeScript, Vitest), Keycloak `26.x` com tema pai `keycloak.v2`, PowerShell 7.

**Dependências Principais**: nenhuma dependência npm nova. Fontes Outfit e
JetBrains Mono (OFL-1.1, `@fontsource` 5.3.0) versionadas como `woff2` com
licença; ícones próprios em SVG (traço no estilo Lucide).

**Armazenamento**: Não se aplica (somente assets estáticos e estado em memória).

**Testes**: `ng test` e `ng build` em container; laboratório Podman com
`start-local-lab.ps1 -Build -RunE2E`; screenshots pela borda nginx; validação do repositório.

**Plataforma Alvo**: Navegadores modernos; Keycloak containerizado do AKB.

**Restrições**: Não alterar OIDC, cookies, callbacks, credenciais ou autorização;
não copiar código AGPL do Dokpod; sem CDN; tokens como única fonte de cor.

## Verificação da Constituição

- Keycloak mantém identidade e autorização; o app apenas redireciona ao BFF. ✅
- Assets locais, licenciados e com checksum no artefato do tema. ✅
- Nenhum secret ou dado real nos artefatos. ✅
- Exceção ao PO UI registrada (incompatível com Angular 22) e ADR 2026-0010 aceito pelo responsável. ✅
- Promoção bloqueada até homologação. ✅

## Estrutura do Projeto

```text
frontend/web/src/styles/{tokens.css,fonts.css,nodra-kit.css,fonts/}
frontend/web/src/app/design-system/{icon,button,card,empty-state,brand}/ + tone.ts + index.ts
frontend/web/src/app/core/{session.service.ts,session-expiry.interceptor.ts}
frontend/web/src/app/{app.html,app.css,app.ts,app.config.ts}
frontend/web/src/app/shell/*
frontend/web/public/{brand,icons}/
deploy/keycloak/themes/altivy/login/  (tema com marca Nodra)
deploy/keycloak/build-theme.ps1       (injeta tokens e fontes do frontend)
tools/scripts/start-local-lab.ps1     (usa o artefato do build-theme)
CLAUDE.md, .claude/skills -> ../.github/skills
docs/adr/2026-0010-design-system-proprio-base-obsidian.md
```

**Decisão de Estrutura**: o design system fica em `frontend/web/src/app/design-system`
(mesmo local do Dokpod), e os tokens em `frontend/web/src/styles` são a fonte
única copiada pelo `build-theme.ps1` para o tema do Keycloak.

## Revisão humana

Escopo e execução autorizados explicitamente pelo responsável (zocatel) na
solicitação de 2026-11-23 citada em [spec.md](spec.md). Revisão do diff pendente no pull request.

## Rastreabilidade da Migração

P-01 a P-03 constam como concluídas no laboratório. P-04 está bloqueada pela ausência de ambiente real de homologação, métrica específica de falha de tema e revisão formal de segurança.

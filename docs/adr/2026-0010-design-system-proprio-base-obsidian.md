# ADR 2026-0010: Design system próprio com base Obsidian e marca Nodra

**Status:** proposed
**Data:** 2026-11-23
**Responsáveis:** Administração da Altivy
**Origem:** IA assistida, no fluxo Spec Kit da [feature 007](../../specs/007-tema-login-keycloak/spec.md)
**Revisor humano:** pendente
**Relacionado:** [spec 007](../../specs/007-tema-login-keycloak/spec.md), [pesquisa](../../specs/007-tema-login-keycloak/research.md), [ADR 2026-0009](2026-0009-padronizacao-cicd-groundwork.md)

## Contexto

O usuário pediu: "replique toda identificação visual, temas de login do keycloak
processo de autenticação do dokpod para o nodra, obviamente, precisa ser criado
logo, icones de acordo ao projeto, use ou crie componentes como foi feito no
frontend do projeto dokpod, use sempre obsidian como base".

Três restrições moldam a resposta:

- O Dokpod é AGPL-3.0 e o Nodra é MIT. A política de dependências e o
  [ADR 2026-0009](2026-0009-padronizacao-cicd-groundwork.md) proíbem incorporar
  código do Dokpod; somente padrões podem ser reproduzidos.
- A instrução de frontend prioriza PO UI, mas o PO UI 21.38.0 declara peer
  `@angular/core ^21` e o Nodra usa Angular 22. A própria instrução admite
  controles locais quando o PO UI não cobre o caso com segurança.
- O guia visual anterior limitava raios a 8 px e usava Manrope/IBM Plex em tema
  claro, divergindo do padrão escuro do Obsidian e do Dokpod.

## Forças de decisão

- identidade visual consistente entre app e login do Keycloak;
- licenças permissivas e verificadas (MIT/OFL-1.1), sem código AGPL;
- acessibilidade AA, foco visível, `prefers-reduced-motion` e `prefers-reduced-transparency`;
- operação offline e self-hosted, sem CDN;
- reversibilidade do tema do Keycloak e compatibilidade com o E2E existente.

## Opções consideradas

### Opção A — adotar PO UI para todos os controles

Seguiria a instrução padrão, mas exigiria forçar peer dependency incompatível com
Angular 22 e não oferece a linguagem visual do Obsidian. Rejeitada enquanto não
houver PO UI compatível.

### Opção B — copiar componentes e CSS do Dokpod

Mais rápida, porém viola a licença AGPL-3.0 frente à distribuição MIT do Nodra.
Rejeitada.

### Opção C — design system local `nd-*` com tokens próprios

Componentes standalone em `frontend/web/src/app/design-system` (ícone, botão,
cartão, estado vazio/erro/carregando e marca), tokens CSS únicos e marca Nodra
criada no repositório, reproduzindo apenas os padrões (vidro, véus violeta, CTA
em pílula) com implementação própria.

## Decisão

Adotar a opção C:

- `frontend/web/src/styles/tokens.css` é a fonte única de cor, raio, sombra e
  tipografia; tema escuro Obsidian é o padrão, com claro, sistema e alto contraste.
- Tipografia Outfit e JetBrains Mono (OFL-1.1, `@fontsource` 5.3.0) versionadas localmente.
- Raios acima de 8 px são permitidos para cartões de vidro (até `--radius-xl`, 20 px)
  e CTAs em pílula; controles densos do workspace continuam com `--radius-sm`.
- O tema do Keycloak (slot `altivy`) é montado por `deploy/keycloak/build-theme.ps1`,
  que copia tokens, fontes e símbolo do frontend; não usa JavaScript.
- PO UI volta a ser avaliado quando houver versão compatível com Angular 22.

## Consequências

### Positivas

- App e login compartilham tokens sem duplicação manual.
- Nenhum código AGPL incorporado; marca e ícones são próprios.

### Negativas e trade-offs

- Componentes locais exigem manutenção e testes próprios.
- Desvio temporário da preferência por PO UI, registrado aqui.

## Segurança e privacidade

O tema do Keycloak altera somente apresentação. O fluxo de sessão aceita
`returnUrl` apenas como caminho local validado pelo BFF (`LocalRedirect`) e não
expõe tokens ao navegador.

## Dados, compatibilidade e migração

Sem impacto em dados. Preferências de tema existentes continuam válidas. O
`displayName` do realm passa a "Nodra".

## Observabilidade e operação

O artefato do tema gera `SHA256SUMS`; o laboratório local sincroniza o artefato
no diretório montado pelo Keycloak sem recriar o bind mount.

## Validação

Testes unitários Angular (design system, sessão, shell), build de produção,
E2E com Keycloak real pelo nginx do laboratório e verificação visual por
screenshots nos temas escuro, claro e alto contraste.

## Rollout e rollback

Rollback: restaurar o tema anterior a partir do Git ou definir `loginTheme`
para `keycloak.v2`; o app volta ao estado anterior revertendo os commits da
feature 007. A promoção para ambientes reais permanece bloqueada (US6 da spec 007).

## Referências

- [Spec 007](../../specs/007-tema-login-keycloak/spec.md) e [research.md](../../specs/007-tema-login-keycloak/research.md)
- [Auditoria de licenças](../auditoria-licencas-sbom.md)
- [Customização de temas do Keycloak](https://www.keycloak.org/ui-customization/themes)

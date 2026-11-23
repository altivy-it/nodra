# Especificação da Feature: Identidade visual Nodra, tema de login do Keycloak e autenticação

**Branch da feature**: `007-tema-login-keycloak`

**Criada em**: 2026-10-06 (migração) | **Ampliada em**: 2026-11-23

**Estado**: Ampliada por solicitação explícita do responsável; promoção em homologação continua bloqueada

**Entrada**: Migração de `docs/plan/tema-login-keycloak.md` e solicitação do
responsável em 2026-11-23: "usando a spec 007, replique toda identificação
visual, temas de login do keycloak processo de autenticação do dokpod para o
nodra, obviamente, precisa ser criado logo, icones de acordo ao projeto, use ou
crie componentes como foi feito no frontend do projeto dokpod, use sempre
obsidian como base, replique no projeto nodra toda configuação do claude code
feito no dokpod para que isso também possa ser utilizado no nodra."

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Contexto

O Dokpod (`altivy-it/dokpod`) é a referência **funcional e visual**: tema
escuro único no login, superfícies de vidro, CTA em pílula, tokens CSS
compartilhados entre o frontend e o tema do Keycloak, design system próprio com
prefixo de componente, fluxo de sessão com estados explícitos e configuração do
Claude Code apontando para as instruções do Copilot. O Dokpod é licenciado sob
AGPL-3.0 e o Nodra sob MIT; conforme o ADR 2026-0009, **nenhum código do
Dokpod é copiado** — o Nodra recebe implementação própria que reproduz o padrão.
O Obsidian é a base de layout e paleta do workspace (ribbon, explorador,
abas, editor, painel lateral e barra de status, cinzas neutros e acento violeta).

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Reconhecer a marca Nodra (Prioridade: P1)

Como usuário, quero ver logo, ícones e favicons próprios do Nodra em todas as
superfícies (app, aba do navegador e login), sem resíduos de "Altivy Notes".

**Teste independente**: Abrir a entrada anônima, o login do Keycloak e o
workspace pela borda nginx e verificar marca, título e favicons.

**Cenários de aceite**:

1. **Dado** a entrada anônima, **Quando** carregada, **Então** exibe o símbolo e o nome Nodra, a tagline "Your connected knowledge workspace." e o CTA "Entrar no Nodra".
2. **Dado** qualquer tema do sistema operacional, **Quando** a aba é exibida, **Então** o favicon do Nodra é legível em fundo claro e escuro.

### Jornada do Usuário 2 - Login com identidade visual própria (Prioridade: P1)

Como usuário, quero que o login do Keycloak tenha a mesma identidade visual do
app, mantendo o Keycloak como dono da autenticação.

**Teste independente**: Executar o login real do laboratório e capturar a tela
do Keycloak pela borda nginx; executar o E2E autenticado.

**Cenários de aceite**:

1. **Dado** qualquer fluxo habilitado no realm, **Quando** a tela é exibida, **Então** usa somente assets locais aprovados (tokens, fontes, símbolo), sem CDN ou recurso externo.
2. **Dado** um erro ou fluxo de recuperação, **Quando** a tela é renderizada, **Então** foco, contraste e navegação por teclado permanecem acessíveis.

### Jornada do Usuário 3 - Autenticação no padrão Dokpod (Prioridade: P1)

Como usuário, quero que o app me informe claramente se estou carregando,
anônimo, autenticado, com erro ou com sessão expirada, e que o login me devolva
à página em que eu estava.

**Teste independente**: Testes unitários do `SessionService`, do interceptor e
do `App`; E2E de login e logout.

**Cenários de aceite**:

1. **Dado** um usuário anônimo em `/notes/<rota>`, **Quando** clica em entrar, **Então** é enviado a `bff/login?returnUrl=<rota>` relativo ao base href.
2. **Dado** uma sessão autenticada, **Quando** uma chamada à API retorna 401, **Então** o app mostra o estado "Sessão expirada" com ação para entrar novamente, sem expor tokens.

### Jornada do Usuário 4 - Workspace com base Obsidian e componentes próprios (Prioridade: P2)

Como usuário, quero um workspace com aparência e organização do Obsidian, com
ícones consistentes e componentes reutilizáveis.

**Teste independente**: Testes de componentes `nd-*`, testes do shell e
screenshots do workspace nos temas escuro, claro e alto contraste.

**Cenários de aceite**:

1. **Dado** o workspace, **Quando** exibido, **Então** o ribbon usa ícones `nd-icon` com nome acessível e o tema padrão é o escuro estilo Obsidian.
2. **Dado** a preferência claro ou alto contraste, **Quando** aplicada, **Então** tokens alternativos mantêm contraste AA e foco visível.

### Jornada do Usuário 5 - Claude Code no Nodra (Prioridade: P3)

Como contribuidor, quero usar o Claude Code no Nodra com as mesmas instruções e
skills usadas pelo Copilot, como no Dokpod.

**Teste independente**: Verificar `CLAUDE.md` e `.claude/skills` resolvendo para `.github/skills`.

### Jornada do Usuário 6 - Promover ou reverter tema com segurança (Prioridade: P2, bloqueada)

Como operador, quero promover o tema por versão/checksum e voltar ao tema padrão
se a validação falhar. Bloqueada até existir homologação (`specs/003-ambiente-homologacao/`).

**Cenários de aceite**:

1. **Dado** um artefato aprovado, **Quando** promovido, **Então** versão, checksums e licença são verificados antes da montagem.
2. **Dado** falha de asset ou login, **Quando** ocorre rollback, **Então** o tema padrão volta sem alterar usuário, credenciais ou políticas.

## Casos de Borda

- Mudança de versão/template do Keycloak (`keycloak.v2`).
- Asset ausente, checksum divergente, fonte sem licença ou recurso externo.
- OTP, MFA, alteração/recuperação de credencial; login apenas no tema escuro.
- App servido sob subcaminho (`/notes/`): login, `returnUrl` e assets relativos ao base href.
- 401 em `bff/session` não é tratado como expiração; `returnUrl` externo é rejeitado pelo BFF (`LocalRedirect`).
- Navegadores sem `backdrop-filter` ou com `prefers-reduced-transparency`.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: O tema DEVE alterar somente apresentação, mantendo autenticação, autorização, callbacks, cookies e políticas no Keycloak.
- **FR-002**: Login, erro, OTP, recuperação e atualização de credencial DEVEM funcionar conforme os fluxos habilitados no realm.
- **FR-003**: Fontes, CSS, imagens e scripts DEVEM ser locais, licenciados, versionados e verificados por checksum.
- **FR-004**: App e tema DEVEM atender contraste AA, foco visível, legibilidade e responsividade.
- **FR-005**: O artefato DEVE ser removível e permitir retorno ao tema padrão sem perda de dados.
- **FR-006**: A promoção real permanece bloqueada até haver ambiente de homologação separado e revisão de segurança formal.
- **FR-007**: O Nodra DEVE ter símbolo, wordmark e favicons próprios (SVG e PNG 16/32/48/192, claro e escuro).
- **FR-008**: Tokens CSS DEVEM ser a única fonte de cor, raio, sombra e tipografia, compartilhados entre o app e o tema do Keycloak.
- **FR-009**: O design system local (`frontend/web/src/app/design-system`, prefixo `nd-`) DEVE fornecer ícone, botão, cartão e estado vazio/erro/carregando com API acessível.
- **FR-010**: O `SessionService` DEVE expor os estados `loading`, `error`, `anonymous`, `authenticated` e `expired`; o login DEVE preservar o caminho atual via `returnUrl`; respostas 401 fora de `bff/session` DEVEM levar a `expired`.
- **FR-011**: O workspace DEVE seguir a organização do Obsidian, com tema escuro padrão e temas claro e alto contraste.
- **FR-012**: `CLAUDE.md` e `.claude/skills` DEVEM reutilizar as instruções e skills de `.github/` sem duplicação.
- **FR-013**: Nenhum código do Dokpod (AGPL-3.0) é copiado; somente padrões são reproduzidos com implementação própria.

### Entidades Principais (quando houver dados)

- **Artefato de tema**: pacote identificado por versão, checksum, versão compatível do Keycloak e proveniência dos assets.
- **Estado de sessão**: `loading | error | anonymous | authenticated | expired`, derivado de `/bff/session` e de respostas 401.

## Critérios de Sucesso (obrigatório)

- **SC-001**: Todos os fluxos habilitados são validados em laboratório (homologação permanece pendente).
- **SC-002**: Nenhum recurso de runtime depende de CDN, URL externa ou asset sem licença/checksum.
- **SC-003**: Rollback ao tema padrão é demonstrado sem alteração de identidade ou políticas (pendente, bloqueado).
- **SC-004**: `ng test`, `ng build` e o E2E do laboratório passam; nenhuma ocorrência de "Altivy Notes" na UI.
- **SC-005**: Screenshots da entrada, do login e do workspace pela borda nginx são revisados e aprovados no loop visual.

## Premissas

- Empresa Altivy, produto Nodra; a marca do produto é criada neste repositório por decisão explícita do responsável (ADR 2026-0010).
- Keycloak permanece dono da autenticação; o Nodra não armazena tokens nem implementa login.
- O slot de tema `altivy` do AKB é mantido para não alterar a plataforma de identidade compartilhada; seu conteúdo passa a ser da marca Nodra.
- O PO UI 21.x não suporta Angular 22; a exceção prevista em `frontend-po-ui.instructions.md` se aplica.

## Proveniência da Migração

Origem: `docs/plan/tema-login-keycloak.md`, estado geral legado `approved`; P-01 a P-03 constam como concluídas no laboratório e P-04 está bloqueada pela ausência de homologação. A migração não declara P-04 concluída.

# Plano: Tema personalizado de login do Keycloak

> REFERÊNCIA LEGADA: escopo convertido para [Spec Kit](../../specs/007-tema-login-keycloak/spec.md), ainda em rascunho e aguardando revisão humana. Este arquivo não tem autoridade operacional; não executar nem atualizar etapas.

**Status:** approved
**Data de criação:** 2026-08-15  
**Última atualização:** 2026-09-01  
**Responsáveis:** equipe Altivy  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano do BFF com Keycloak](bff-keycloak.md), [ADR 2026-0002](../adr/2026-0002-bff-keycloak-autorizacao-externa.md)

## Objetivo

Criar, empacotar e operar uma tela personalizada de login do Keycloak para o
Altivy, usando exclusivamente os assets e a identidade visual canônica do
repositório `Altivy`, sem transferir autenticação ou autorização para o
frontend e sem expor tokens ao navegador.

O resultado deve cobrir os fluxos habilitados do realm, incluindo login, erro,
OTP, recuperação e atualização de credencial, mantendo o Keycloak como dono da
experiência de autenticação e o BFF como cliente OIDC confidencial.

## Contexto e premissas

- O plano BFF/Keycloak foi aprovado, mas a tematização continua sem autorização
  para alterar o realm real fora de um laboratório controlado.
- A fonte canônica da marca é
  `$env:USERPROFILE\projetos\Altivy\docs\brand`.
- A identidade visual canônica inclui `README.md`, `guide.md`,
  `tokens/tokens.css`, `tokens/tokens.json`, `tokens/typography.md`,
  `fonts/`, `assets/dist/`, seus manifests `SHA256SUMS`, `licenses.md` e o
  `frontend-asset-integration-playbook.md`.
- O tema deve usar assets self-hosted, sem CDN, URL externa, fonte remota ou
  dependência não verificada em runtime.
- Manrope e IBM Plex Mono são as famílias aprovadas para
  uso conforme a função definida no guia de marca.
- A versão exata do Keycloak deve ser confirmada antes de fixar a estrutura do
  tema, pois compatibilidade de templates e empacotamento é uma dependência
  operacional.
- A tela altera apresentação, mensagens e composição visual; não altera
  autenticação, autorização, callbacks, cookies, antiforgery ou decisões do
  Keycloak.
- Não serão criadas cópias canônicas de assets em `AltivyNotes`; o repositório
  `Altivy` permanece a fonte de verdade.

## Não escopo

- implementar login, senha, MFA, recuperação ou autorização dentro do Altivy;
- armazenar usuários, memberships, tokens, secrets ou políticas no PostgreSQL;
- personalizar o realm `master`;
- alterar o Angular para obter ou armazenar tokens;
- carregar CSS, fontes, imagens ou scripts por CDN;
- publicar assets sem licença, checksum ou aprovação de marca;
- criar ou alterar realm, client, usuário ou secret real durante a fase
  documental;
- suportar telas não habilitadas no realm sem decisão explícita.

## Dependências e decisões

- Conclusão e aceitação humana do P-01 do [plano BFF](bff-keycloak.md).
- Realm e clients de teste preparados pela etapa P-02 do plano BFF.
- Confirmação da versão suportada do Keycloak e do mecanismo oficial de tema.
- Verificação de licença, manutenção e vulnerabilidades de qualquer ferramenta
  usada para empacotar ou validar o tema.
- Validação dos assets pelos manifests `SHA256SUMS` do repositório `Altivy`.
- Atualização do ADR 2026-0002 para registrar a tematização proposta.
- Nenhum pacote de terceiros deve ser adicionado sem necessidade real e
  verificação de licença permissiva.

## Etapas

### P-01: Definir compatibilidade e contrato visual

**Status:** completed  
**Responsável:** equipe Altivy  
**Dependências:** P-01 do plano `bff-keycloak.md`; P-02 será validado antes do laboratório

**Objetivo:** confirmar a versão do Keycloak, o mecanismo oficial de tema e o
conjunto de elementos visuais que será aplicado sem modificar o contrato de
segurança.

Entregas:

- registrar a versão exata do Keycloak e a compatibilidade do tema;
- mapear login, erro, OTP, recuperação e atualização de credencial;
- selecionar tokens, fontes, logos e símbolos apropriados para tema claro e
  escuro;
- registrar limites de contraste, foco, área de proteção e tamanhos mínimos;
- atualizar o ADR 2026-0002 com a decisão proposta.

Validação:

- documentação oficial da versão suportada confirma a estrutura escolhida;
- o mapeamento não altera fluxo OIDC, callbacks, cookies ou autorização;
- licenças e proveniência dos assets estão registradas;
- a decisão permanece reversível para o tema padrão.

Evidências:

- versão de laboratório definida como `lzocateli/keycloak:26.7.0`;
- mecanismo oficial `theme.properties` com herança `keycloak.v2` registrado;
- tokens, tipografia, assets, licenças e checksums canônicos identificados;
- em 2026-08-30, a validação visual e a aceitação do ambiente foram concluídas
  em P-03: login, erro, recuperação de senha e as variantes clara/escura
  renderizaram corretamente no laboratório integrado, e o responsável aprovou
  explicitamente o encerramento desta etapa com essa evidência.

### P-02: Criar e empacotar o tema

**Status:** completed  
**Responsável:** equipe Altivy  
**Dependências:** P-01

**Objetivo:** produzir um artefato de tema reproduzível, mínimo e compatível com
Keycloak, usando os assets canônicos sem introduzir cópias de marca como nova
fonte de verdade.

Entregas:

- criar a estrutura oficial do tema para a versão suportada;
- aplicar tokens de `tokens/tokens.css` ou `tokens/tokens.json`;
- incluir os pesos Manrope aprovados a partir de `docs/brand/fonts/`;
- incluir os assets adequados de `docs/brand/assets/dist/`;
- implementar as páginas dos fluxos habilitados no realm;
- manter CSS, fontes e imagens locais no artefato final;
- documentar a origem, versão e checksum de cada asset usado.

Validação:

- `SHA256SUMS` confirma a integridade dos assets e fontes;
- SVGs não possuem script, imagem incorporada ou referência externa;
- o artefato não contém secrets, tokens, usuários ou exports reais;
- o build é reproduzível com tags imutáveis e não depende de CDN;
- o tema padrão pode ser restaurado sem migração de dados.

Evidências:

- estrutura criada em `deploy/keycloak/themes/altivy/login`;
- empacotador criado em `deploy/keycloak/build-theme.ps1`;
- build local executado com sucesso, gerando oito arquivos e `SHA256SUMS` em
  `deploy/keycloak/.build/keycloak-theme`;
- fontes, logos, CSS e propriedades do tema mantidos em
  `deploy/keycloak/themes/altivy/login`;
- em 2026-08-30, a validação em container Keycloak foi concluída em P-03: o
  artefato foi montado no laboratório integrado (`deploy/e2e` + `deploy/keycloak`)
  e serviu corretamente o tema `altivy` para login, erro, recuperação e as
  variantes clara/escura, incluindo login/logout reais.

### P-03: Configurar e validar no laboratório

**Status:** completed  
**Responsável:** operador do Keycloak  
**Dependências:** P-02

**Objetivo:** selecionar o tema no realm de laboratório e comprovar os fluxos
visuais e de segurança antes de qualquer uso em produção.

Entregas:

- atualizar o tutorial de [configuração do Keycloak](../configuracao-keycloak.md)
  com pré-requisitos, empacotamento, instalação, seleção, teste e rollback;
- configurar o tema somente no realm `altivy` de laboratório;
- testar login, erro, OTP, recuperação e atualização de credencial quando
  habilitados;
- inspecionar tema claro e escuro em desktop e mobile;
- registrar procedimento de remoção e retorno ao tema padrão.

Validação:

- login, logout, callback, renovação e revogação continuam funcionando;
- não há token em URL, Web Storage, HTML indevido, telemetria ou logs;
- contraste atende no mínimo WCAG 2.2 AA e o foco é visível;
- mensagens de erro e estados de OTP são legíveis e não revelam detalhes
  sensíveis;
- BFF continua sendo o único ponto público da aplicação;
- o procedimento de rollback restaura o tema padrão com sucesso.

Evidências:

- build e montagem do artefato foram preparados com as imagens
  `lzocateli/keycloak:26.7.0` e
  `lzocateli/postgresql:18.4-pgvector0.8.6-bookworm`;
- tentativa de execução do laboratório em 2026-08-15 não produziu resultado
  verificável por limitação do terminal ao executar o comando longo;
- em 2026-08-30, `./build-theme.ps1` regenerou o artefato em
  `deploy/keycloak/.build/keycloak-theme` (7 arquivos e `SHA256SUMS`) e o
  laboratório `deploy/keycloak/docker-compose-keycloak.yaml` já estava em
  execução, saudável, integrado à borda NGINX do laboratório E2E
  (`https://localhost:7443`) com `KC_HOSTNAME` apontando para essa origem;
- o tema `altivy` está selecionado no realm e foi validado ao vivo pelo
  navegador integrado: a tela de erro do endpoint de autorização, a tela de
  login alcançada pelo fluxo real `GET /bff/login` (PAR + Authorization Code),
  o estado de erro "Invalid username or password." após tentativa com
  credencial inexistente e a tela "Forgot Your Password?" renderizaram com a
  identidade Altivy (logo, tipografia IBM Plex Sans/Serif e paleta do tema),
  sem qualquer requisição a CDN ou fonte externa;
- a variante escura (`prefers-color-scheme: dark`) do `login.css` foi
  confirmada visualmente no navegador (fundo `#0f0f0f`, texto claro e logo
  `logo-dark.svg`), validando a troca automática de tema claro/escuro sem
  JavaScript adicional;
- foi implementado `resources/js/theme-toggle.js`, um botão acessível
  (`aria-pressed`, rótulo dinâmico em PT-BR "Tema claro"/"Tema escuro") que
  permite ao usuário sobrepor a preferência do sistema operacional; a escolha
  é persistida em `localStorage` (`altivy-theme`) e aplicada via atributo
  `data-altivy-theme` no `<html>`, com fallback automático para
  `prefers-color-scheme` quando não há escolha salva; o script foi incluído no
  manifesto de `build-theme.ps1` e é servido sem cache pelo Keycloak
  (`--spi-theme--cache-templates=false`);
- em 2026-08-30, o responsável validou visualmente pelo navegador e por
  captura de tela o tema **claro** (fundo `#f7fafc`, logo colorido original,
  bom contraste), o botão de alternância "Tema escuro" visível e funcional, a
  troca dinâmica para o tema escuro pelo clique e a persistência da escolha
  manual após recarregar a página, mesmo divergindo da preferência do sistema;
- em 2026-08-30, com credenciais do laboratório E2E (`ALTIVY_E2E_USERNAME`/
  `ALTIVY_E2E_PASSWORD` do arquivo externo
  `$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, fora do repositório), foi
  executado o ciclo completo de login e logout pelo tema personalizado: o
  formulário temático em `https://localhost:7443/bff/login` autenticou
  `lzocateli`, o Keycloak redirecionou para `/signin-oidc`, o BFF criou a
  sessão server-side e o Angular exibiu `SESSÃO AUTENTICADA`/"Olá,
  lzocateli."; o botão "Sair" encerrou a sessão via `POST /bff/logout` e o
  Angular retornou a `SESSÃO ANÔNIMA`; nenhuma tela de OTP foi exigida,
  consistente com a configuração do usuário de laboratório já registrada em
  P-02 do plano `bff-keycloak.md`;
- em 2026-08-30, o responsável aprovou explicitamente o encerramento desta
  etapa com as evidências acima, reconhecendo como dívida técnica registrada
  para P-04/operação contínua: fluxo de OTP e de atualização de credencial
  com usuário habilitado para essas ações, verificação formal de contraste
  WCAG 2.2 AA com ferramenta dedicada, inspeção em viewport móvel real e
  execução do rollback pelo Admin Console (a tentativa via Admin REST direto
  pela borda `localhost:7443` falhou nesta sessão por conexão encerrada
  abruptamente).

### P-04: Promover e operar o tema

**Status:** blocked  
**Responsável:** equipes de plataforma e segurança  
**Dependências:** P-03

**Objetivo:** promover o tema entre ambientes com rastreabilidade, rollback e
proteções operacionais.

Entregas:

- promover o artefato aprovado por ambiente, sem copiar secrets ou dados de
  usuários;
- fixar versão do Keycloak e do tema no deployment;
- manter backup e procedimento de retorno ao tema padrão;
- monitorar falhas de login, callback, carregamento de assets e erros de tema;
- revisar o tema após alterações de marca, versão do Keycloak ou dependências.

Validação:

- checksums e versão do artefato são conferidos antes da promoção;
- o tema é carregado sem dependência de internet externa;
- rotação de configuração não exige alterar o conteúdo do usuário;
- rollback é executado em ambiente representativo;
- revisão de segurança não encontra segredo ou recurso externo indevido.

Evidências:

- não existe, neste repositório, um ambiente de homologação ou produção
  separado do laboratório local e do laboratório E2E; `deploy/` contém apenas
  `deploy/e2e` e `deploy/keycloak`, ambos de uso local e não publicados. A
  entrega "promover o artefato aprovado por ambiente" permanece bloqueada até
  existir um ambiente real além do laboratório, e este risco foi registrado
  como pendência explícita desta etapa;
- o procedimento de rollback documentado em
  [configuracao-keycloak.md §6.4](../configuracao-keycloak.md#64-criar-o-tema-visual-personalizado)
  foi revisado e detalhado: promoção exige conferir `SHA256SUMS` do artefato
  antes de montar o volume, fixar a tag do Keycloak e do tema como unidade
  versionada, e manter o artefato anterior disponível para reverter sem
  precisar recriar dados de usuário;
- monitoramento de falhas de login, callback e carregamento de assets já é
  coberto pelas métricas e alertas gerais do Keycloak descritos em
  [observabilidade-alertas-slo.md](../observabilidade-alertas-slo.md) e pelo
  runbook
  [operacao-keycloak-indisponibilidade-recuperacao.md](../runbooks/operacao-keycloak-indisponibilidade-recuperacao.md)
  do plano `bff-keycloak.md`; nenhum destes documentos possui métrica
  específica de "erro ao carregar tema", o que permanece como lacuna a
  fechar antes da promoção real;
- revisão de segurança do artefato do tema (ausência de segredo ou recurso
  externo) permanece pendente de execução formal; a inspeção manual do
  artefato nesta e nas sessões anteriores não encontrou script inline, dado
  incorporado ou referência de rede em nenhum arquivo do tema.
- em 2026-09-01, o laboratório foi novamente verificado: os serviços
  `keycloak` e `postgres` estavam `healthy`, a imagem do Keycloak permanece
  fixada em `lzocateli/keycloak:26.7.0`, o Compose monta o tema em modo
  somente leitura e o artefato atual contém os oito arquivos definidos por
  `build-theme.ps1`, incluindo `resources/js/theme-toggle.js`, com
  `SHA256SUMS` presente. Essa verificação confirma a implementação local, mas
  não substitui a promoção nem o rollback em ambiente representativo.

## Critérios de aceite finais

- a tela de login usa a identidade visual aprovada do Altivy;
- fontes e assets são locais, licenciados, versionados e íntegros;
- login, erro, OTP, recuperação e atualização funcionam conforme habilitação do
  realm;
- contraste, foco, legibilidade e responsividade atendem ao mínimo definido;
- nenhum token, secret, usuário ou política é movido para o Altivy;
- o Angular continua sem acesso direto ao Keycloak ou aos tokens;
- o tema pode ser removido e substituído pelo padrão sem perda de dados;
- documentação, ADR, checksums e procedimento operacional correspondem ao
  artefato efetivamente validado.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| alteração de templates entre versões do Keycloak | tela quebrada ou perda de fluxo | fixar versão, testar upgrade e manter tema padrão como rollback |
| asset ou fonte sem licença compatível | risco legal e bloqueio de distribuição | usar somente inventário aprovado e manifests canônicos |
| CDN ou referência externa no tema | indisponibilidade e vazamento de metadados | empacotar assets localmente e inspecionar o artefato |
| contraste ou foco insuficiente | barreira de acessibilidade | validar WCAG AA em todos os estados e temas |
| personalização alterar o fluxo de autenticação | falha de login ou risco de segurança | limitar mudança à apresentação e testar OIDC completo |
| segredo incluído no artefato | comprometimento de credencial | revisão automatizada e manual antes da promoção |
| identidade visual duplicada em outro repositório | divergência de marca | referenciar `Altivy/docs/brand` como fonte única |

## Rollout e rollback

Validar primeiro em um realm de laboratório isolado, depois em homologação e
somente após revisão humana em produção. O tema, a imagem do Keycloak e as
configurações devem possuir versões identificáveis e reproduzíveis.

Interromper a promoção se houver dependência externa, checksum divergente,
falha de acessibilidade, segredo no artefato ou alteração do fluxo OIDC. Em
rollback, selecionar o tema padrão do realm ou restaurar o artefato anterior e
reverter a configuração como unidade compatível. Não restaurar login local nem
alterar as políticas de autorização para contornar uma falha visual.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-08-15 | plano | - | proposed | plano físico criado a partir do plano de sessão | IA assistida |
| 2026-08-15 | plano | proposed | approved | aprovação humana registrada para iniciar a implementação; normalização do valor em 2026-09-29 | responsável pelo projeto |
| 2026-08-15 | P-01 | - | blocked | depende do P-01 e P-02 do plano BFF | IA assistida |
| 2026-08-15 | P-01 | blocked | in-progress | aprovação registrada; versão e contrato visual definidos, validação de laboratório pendente | IA assistida |
| 2026-08-15 | P-02 | not-started | in-progress | tema e empacotador implementados; build local validado | IA assistida |
| 2026-08-30 | P-03 | not-started | in-progress | tema validado ao vivo no laboratório integrado à borda `localhost:7443`: login, erro de credencial e recuperação renderizados com identidade Altivy; variante escura confirmada; login com usuário real, OTP, atualização de credencial, tema claro, contraste formal, mobile e rollback permanecem pendentes | IA assistida |
| 2026-08-30 | P-03 | in-progress | in-progress | botão de alternância manual claro/escuro implementado (`theme-toggle.js`, persistência em `localStorage`); tema claro e a alternância dinâmica validados visualmente pelo responsável | Lincoln Zocateli |
| 2026-08-30 | P-03 | in-progress | in-progress | login e logout reais com usuário do laboratório validados de ponta a ponta pelo tema personalizado, incluindo sessão autenticada e retorno à sessão anônima no Angular | Lincoln Zocateli |
| 2026-08-30 | P-03 | in-progress | completed | aprovação humana explícita para encerrar a etapa; OTP, atualização de credencial, contraste formal, viewport móvel e rollback pelo Admin Console registrados como dívida técnica de operação contínua | Lincoln Zocateli |
| 2026-08-30 | P-04 | not-started | in-progress | procedimento de promoção e rollback detalhado em `configuracao-keycloak.md`; ausência de ambiente de homologação/produção real além do laboratório registrada como bloqueio explícito | IA assistida |
| 2026-08-30 | P-01 | in-progress | completed | aprovação humana explícita: validação visual e aceitação do ambiente concluídas em P-03 | Lincoln Zocateli |
| 2026-08-30 | P-02 | in-progress | completed | aprovação humana explícita: validação em container Keycloak concluída em P-03 | Lincoln Zocateli |
| 2026-09-01 | P-04 | in-progress | blocked | implementação local reconfirmada: laboratório saudável, tema montado em leitura, oito arquivos no artefato e `SHA256SUMS` presente; promoção e rollback seguem bloqueados sem homologação ou produção representativa | IA assistida |
| 2026-09-04 | P-02 | completed | completed | tipografia do tema migrada de IBM Plex Sans/Serif para Manrope 400/500/600 após a reestruturação tipográfica canônica no repositório `Altivy`; `build-theme.ps1` e assets atualizados, build validado com `-BrandRoot`; revalidação visual e de contraste no laboratório permanece pendente | IA assistida |

## Regras de atualização

- O plano permanece `approved`; mudanças de escopo ou de identidade visual
  exigem nova aprovação humana explícita.
- Etapas bloqueadas devem registrar dependência e condição de desbloqueio.
- IA não marca etapas como `completed` nem o plano como `approved` sem evidência
  verificável e decisão humana registrada.
- Alterações na fonte canônica de branding devem ser feitas no repositório
  `Altivy`, não neste plano.

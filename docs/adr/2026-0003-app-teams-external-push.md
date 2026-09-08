# ADR 2026-0003: Plataforma de plugins e primeiro plugin Microsoft Teams

**Status:** proposed
**Data:** 2026-08-31
**Responsáveis:** equipe Altivy
**Origem:** IA assistida
**Revisor humano:** Lincoln Zocateli
**Relacionado:** [docs/plan/app-altivy-notes-teams-external-push.md](../plan/app-altivy-notes-teams-external-push.md)

## Contexto

O Altivy precisa permitir integrações opcionais sem acoplar protocolos,
dependências e configuração de cada fornecedor ao núcleo. O app Microsoft
Teams será o primeiro plugin: por ação explícita, o usuário envia uma mensagem
ou conteúdo digitado para um documento Markdown em um vault autorizado, sem
permissão de leitura no Microsoft Graph e sem observação silenciosa do Teams
ou do painel Loop. O contrato `external-push` será a primeira capacidade
reutilizável da plataforma, autenticada, idempotente e durável.

O Altivy é self-hosted, monólito modular (`Api`, `Bff`, `Worker` sobre
bibliotecas de domínio/aplicação/infraestrutura) e usa o Keycloak como única
autoridade de identidade e autorização. Markdown no filesystem permanece fonte
canônica; PostgreSQL guarda apenas estado técnico, inbox, jobs e projeções
reconstruíveis. Esta decisão é necessária antes de definir contratos de
plugin, registrar o app no Teams, criar OpenAPI executável ou adicionar SDK.

"Plugin" neste ADR significa um módulo confiável, versionado e instalado pelo
operador como parte de uma distribuição compatível do Altivy. Não significa
compatibilidade com a API do Obsidian, marketplace aberto, download de código
em runtime nem execução arbitrária de JavaScript, assemblies ou scripts de
terceiros. Plugins não confiáveis e sandboxed continuam sendo uma possível
evolução da Fase 5 e exigirão outro ADR e threat model.

Uma **conexão** é a configuração administrada de uma instância do plugin para
um tenant, conta ou serviço externo. Assim, uma distribuição contém o código
do plugin, enquanto o operador pode criar zero ou mais conexões, cada uma com
credenciais referenciadas, capacidades, destinos, agenda e estado próprios.
Google Calendar e calendários do Outlook/Microsoft Graph são exemplos futuros
desse modelo, mas sua adoção, permissões e contratos não são aprovados por este
ADR.

## Forças de decisão

- entregar a experiência sem exigir permissões Microsoft Graph nem introduzir
  sincronização automática de conteúdo do Teams/Loop;
- manter o Keycloak como única autoridade de identidade e autorização; nome,
  e-mail ou identificadores do Teams não podem, isoladamente, conceder acesso
  a um vault;
- manter o Altivy como monólito modular, sem introduzir microsserviço para o
  app do Teams ou para `external-push`;
- permitir que novas integrações sejam adicionadas por contratos estáveis,
  sem alterar os módulos de Notes, Vaults ou Indexing;
- tornar instalação, ativação, atualização, desativação e diagnóstico de cada
  plugin explícitos, reversíveis e administrados pelo operador;
- impedir que um plugin amplie privilégios silenciosamente: capacidades,
  endpoints, jobs, configuração e scopes devem ser declarados;
- suportar semântica at-least-once do `external-push` com idempotência,
  conflito e reconciliação auditável, sem sobrescrita silenciosa de conteúdo
  local;
- reduzir superfície de exposição HTTPS pública ao mínimo necessário para o
  protocolo do bot e para o recurso `external-push`;
- permitir integrações que operem apenas com conexões de saída, sem transformar
  endpoint público em requisito de toda a plataforma de plugins;
- preservar reversibilidade: o recurso é candidato de fase futura e não pode
  bloquear ou degradar o MVP.

## Opções consideradas

### Opção A: plugins confiáveis in-process com contratos e manifesto

Cada plugin é um módulo .NET versionado, composto explicitamente pela API e/ou
worker a partir de um catálogo permitido no build. Um manifesto declarativo
informa identidade, versão, compatibilidade com o host, capacidades, endpoints,
jobs, configuração, secrets referenciados e requisitos de autorização. O host
oferece somente contratos estáveis de aplicação; plugins não acessam o
`DbContext`, filesystem, sessão BFF ou internals de outros módulos diretamente.

Vantagens: preserva o monólito modular, permite testes e SBOM reproduzíveis,
mantém DI e observabilidade convencionais e evita carregar código desconhecido
em runtime. Desvantagens: instalar ou atualizar código de plugin exige imagem
ou distribuição do Altivy que o contenha; isolamento é lógico, não um sandbox
de segurança contra código malicioso.

### Opção B: plugins em processo externo

Cada integração executaria em container/processo próprio e falaria com o
Altivy por API versionada. Vantagens: isolamento de falhas e dependências.
Desvantagens: cria topologia distribuída, autenticação serviço a serviço,
deploy, health, compatibilidade e custo operacional desproporcionais para o
primeiro plugin; aproxima a solução de microsserviços antes de evidência.

### Opção C: carregamento dinâmico de pacotes ou scripts de terceiros

O operador instalaria pacotes sem reconstruir a distribuição. Vantagens:
experiência semelhante a um marketplace. Desvantagens: execução remota de
código, resolução de dependências, assinatura, sandbox, supply chain, upgrade
e suporte tornam-se parte crítica da plataforma. O .NET não fornece isolamento
de segurança dentro do mesmo processo para assemblies não confiáveis.

Para a plataforma, adotar a Opção A. As opções B e C somente podem ser
reavaliadas por ADR próprio com evidência de demanda e controles operacionais.

## Opções do primeiro plugin Teams

### Opção T1: message extension baseada em bot com action command

Um app do Teams registra uma action command **Salvar no Altivy Notes**,
disponível sobre uma mensagem existente e na área de composição. O Teams invoca
um bot hospedado pelo Altivy via Bot Framework; o bot abre um dialog para
confirmar título, destino e conteúdo, e envia a atividade autenticada ao
backend.

Vantagens: suporta invocação sobre mensagem e composição livre no mesmo
manifesto; dialogs nativos cobrem edição de título/destino sem UI adicional;
Bot Framework tem SDK oficial para .NET, autenticação de atividade documentada
e é o único mecanismo, hoje, que cobre os dois pontos de entrada exigidos pelo
plano. Desvantagens: exige um endpoint de callback do protocolo do bot
alcançável publicamente, além de registro do bot e do canal Teams no Azure.

### Opção T2: API-based message extension (sem bot)

O Teams chama diretamente um endpoint HTTP do Altivy para a action command, sem
intermediar por um bot registrado no Bot Framework.

Vantagens: menos peças de infraestrutura (sem registro de bot/canal).
Desvantagens: na data deste ADR, o suporte de API-based message extension para
action command sobre mensagem existente com dialog completo não está
comprovado como equivalente ao baseado em bot; o plano trata esta opção como
reavaliação de POC, não como baseline, para não presumir paridade funcional
sem evidência.

### Opção T3: bot conversacional dedicado

Um bot conversacional processaria comandos de texto livre em vez de action
command estruturada.

Vantagens: não depende de manifesto de message extension. Desvantagens: pior
descoberta (usuário precisa saber o comando), sem integração nativa com o menu
de contexto de uma mensagem, maior superfície de parsing de linguagem livre e
maior risco de ambiguidade na captura de destino/conteúdo.

### Opção T4: Outgoing Webhook do Teams

Um Outgoing Webhook por equipe, acionado por `@mention`, envia a mensagem para
um endpoint HTTP.

Vantagens: configuração simples, sem publicação de app. Desvantagens: exige
`@mention`, funciona apenas em canal público, é configurado por equipe (não
organizacional) e não oferece dialog, confirmação estruturada nem a
experiência completa do app. Adequado apenas como fallback manual limitado
para produtores `external-push`, não como baseline do app Altivy Notes.

## Decisão

Adotar plugins confiáveis in-process com dois níveis de contrato:

1. `Plugin.Abstractions`: contratos mínimos e versionados do host, sem ASP.NET
  Core, EF Core, filesystem ou SDK de fornecedor;
2. capacidades de aplicação, começando por `external-push`, que recebem DTOs
  neutros e retornam resultados explícitos sem conhecer Teams.

Cada plugin possui identificador global estável, versão SemVer, intervalo de
compatibilidade com o host, capacidades e configuração declaradas em
`PluginManifest`. O catálogo é allowlist gerada no build/composition root; a
ativação é administrativa e persiste somente estado técnico. Registro deve ser
determinístico, idempotente e falhar no startup quando houver ID duplicado,
versão incompatível ou capacidade desconhecida. Plugin desabilitado não mapeia
rotas, agenda jobs, abre conexões externas nem mantém readiness obrigatório.

As capacidades do host são independentes do fornecedor e começam com esta
taxonomia:

- `external-push/v1`: um produtor autorizado entrega conteúdo ao Altivy;
- `external-pull/v1`: o worker consulta alterações incrementais no provedor e
  mantém cursor, retry e reconciliação duráveis;
- `subscription-webhook/v1`: um callback externo apenas valida e persiste o
  sinal; o worker busca e processa o conteúdo posteriormente;
- `external-action/v1`: o Altivy executa uma ação explícita de saída, com
  autorização, idempotência e escopo restrito.

Cada plugin declara somente as capacidades que implementa. Adicionar uma nova
capacidade ou ampliar dados e permissões exige revisão de contrato, threat
model e consentimento administrativo; não confere acesso direto ao vault.

O host fornece portas estreitas para autorização, inbox durável, auditoria,
telemetria, secrets por referência e publicação de jobs. O plugin não recebe
acesso direto ao vault ou banco. A gravação Markdown permanece em caso de uso
do núcleo, que normaliza paths, aplica ETag, resolve conflitos e reconcilia
filesystem/PostgreSQL. Migrações pertencentes a uma capacidade usam namespace
de schema/tabelas documentado e são aplicadas pelo migrator do Altivy, nunca
executadas livremente pelo plugin no startup.

O **plugin `altivy.integrations.microsoft-teams`** implementará inicialmente a
capacidade `external-push/v1`. Para ele, adotar a **Opção T1** como baseline,
com T2 reavaliada na POC apenas se comprovar paridade sem Microsoft Graph. T4
permanece fallback e T3 é descartada. Código e assets específicos ficam sob
uma fronteira própria de plugin; SDK Teams não pode vazar para abstrações ou
casos de uso comuns.

Fronteiras HTTP são três contratos distintos e isolados: (1) callback do
protocolo do bot, autenticado pelo Bot Framework; (2) recurso público
`external-push`, autenticado pelo Keycloak para produtores de serviço; (3)
recursos administrativos/de consulta do usuário, servidos via BFF. O callback
do bot nunca grava arquivo nem executa conversão no caminho síncrono: after
validar autenticidade, autorização, tamanho e formato, persiste a entrega na
inbox e responde dentro do limite da plataforma; o processamento durável ocorre
no worker.

Há dois perfis operacionais compatíveis com a plataforma:

1. **privado por saída:** o Altivy não publica ingressos de plugin; o worker
  usa `external-pull` com polling incremental, egress restrito e cursores
  duráveis. É a baseline para Google Calendar e Outlook quando latência de
  polling for aceitável;
2. **callback público isolado:** somente as rotas explicitamente habilitadas
  por conexão são publicadas pelo proxy, com validação específica do provider,
  allowlist de tenant/app, limites de payload, rate limiting, dedupe e inbox
  durável. É obrigatório para a experiência interativa do bot Teams e
  opcional para notificações de mudança de calendários.

OAuth não implica, por si só, webhook público: o redirecionamento interativo é
feito pelo navegador para uma URL do Altivy que o usuário consiga alcançar, ou
pode ser substituído por fluxo de dispositivo quando oficialmente suportado e
aprovado. Requisitos de redirect URI, consentimento e publicação de cada
provider devem ser comprovados em POC própria. O Altivy nunca atua como
provedor de identidade e armazena somente referência protegida à credencial.
Authorization Code exige `state` imprevisível, PKCE, redirect URI exata,
expiração, uso único e vínculo com a sessão administrativa, conexão e provider.
Tokens gerados e rotacionados em runtime ficam em secret store gravável, com
proteção em repouso, revogação e exclusão; o banco persiste apenas referência
opaca.

O envelope `external-push` usa `sourceEventId`/`Idempotency-Key` para
idempotência lógica, `sourceDocumentId` opaco para upsert estável,
`destinationAlias` para indireção de destino (nunca path físico), e `mode`
restrito a `create`/`upsert` (nunca `delete`) na primeira versão.

Identidade e autorização continuam exclusivamente no Keycloak: o token do Bot
Framework prova origem da atividade, mas não concede acesso a vault; produtores
`external-push` usam credencial de serviço e scopes do Keycloak; a API consulta
a decisão vigente antes de expor destinos ou aceitar uma entrega. Vinculação da
identidade do Teams ao usuário Altivy é explícita e revogável (comparação entre
OAuth interativo e código de uso único fica para a POC em P-02).

Instalação e lifecycle na primeira versão:

- **disponível:** plugin consta no catálogo da distribuição e passou por
  licença, vulnerabilidades, SBOM e testes de compatibilidade;
- **habilitado:** operador aceitou capacidades e forneceu configuração válida;
- **degradado:** dependência externa falhou, sem degradar o núcleo;
- **desabilitado:** ingressos e jobs do plugin estão fechados, preservando
  documentos e estado conforme retenção;
- **removido:** nova distribuição não contém o plugin; dados técnicos ficam
  inertes até limpeza administrativa explícita e reversível.

Esta decisão não aprova carregamento dinâmico, schema final do manifesto Teams,
versão de SDK nem OpenAPI completo. Esses itens seguem no plano com evidência.

## Consequências

### Positivas

- experiência nativa no menu de contexto de mensagens e na composição, sem
  exigir Microsoft Graph;
- reaproveita o monólito modular existente sem criar um serviço novo;
- cria uma extensão reutilizável para novas integrações sem dependência do
  protocolo Teams no núcleo;
- permite que conectores de calendário operem por polling somente com tráfego
  de saída, preservando uma implantação sem ingress público quando esse modo
  atender ao SLO;
- catálogo de build e manifesto tornam compatibilidade, capacidades, SBOM e
  operação auditáveis;
- separa claramente autenticação de protocolo (Bot Framework) de autorização
  de recurso (Keycloak), evitando "confused deputy" entre os dois;
- caminho síncrono do bot fica curto (persistência na inbox), reduzindo risco
  de timeout da plataforma e mantendo o processamento pesado no worker.

### Negativas e trade-offs

- a experiência interativa escolhida para Teams exige registro de bot e app no
  Azure/Teams e endpoint HTTPS público
  alcançável pela infraestrutura do Teams, aumentando a superfície exposta em
  relação a uma instalação totalmente privada; isso não é requisito de plugins
  que usem apenas polling ou ações de saída;
- polling reduz superfície exposta, mas aumenta latência, consumo de quota e
  responsabilidade por cursores, backoff e reconciliação;
- dependência de SDK do Bot Framework/Teams cujo licenciamento, manutenção e
  compatibilidade com .NET 10 ainda precisam ser comprovados (P-02/P-03);
- plugins in-process compartilham processo e recursos; portanto são código
  confiável da distribuição, não uma fronteira de segurança;
- a primeira versão exige rebuild/release para adicionar código de plugin e
  não oferece marketplace ou instalação arbitrária em runtime;
- se a Opção B (API-based) se mostrar viável na POC, parte deste desenho de
  callback pode precisar ser revisto; a decisão é explicitamente reversível
  até a conclusão de P-03.

## Segurança e privacidade

Threat model preliminar (elaboração completa é entrega de P-01 do plano):

- **Supply chain do plugin:** somente plugins presentes na allowlist de build,
  com versão fixa, licença permitida, SBOM, scan e proveniência, podem entrar na
  distribuição; não há upload de assembly ou script pela UI.
- **Capability escalation:** habilitação exige capacidades declaradas e scopes
  Keycloak próprios; capacidade nova ou ampliada exige nova versão, revisão e
  consentimento administrativo.
- **Isolation failure:** contratos não expõem `DbContext`, filesystem, tokens
  de usuário ou secret bruto quando uma referência/cliente limitado bastar;
  orçamento de timeout, concorrência e payload é aplicado por plugin.

- **Spoofing:** atividades do bot são validadas pela assinatura/token do Bot
  Framework; produtores `external-push` autenticam por credencial de serviço
  Keycloak. Nenhum claim de identidade do Teams (nome, e-mail, team/channel ID)
  é usado isoladamente como prova de autorização.
- **Replay:** `sourceEventId`/`Idempotency-Key` deduplicam tentativas; mesma
  chave com conteúdo diferente é tratada como conflito, não sobrescrita.
- **Tampering:** o callback do bot persiste a entrega antes de qualquer
  conversão; a gravação no vault ocorre apenas no worker, após validação de
  autorização e integridade do payload.
- **Information disclosure:** o produtor nunca envia path físico, apenas alias
  opaco de destino; conteúdo de entregas não é registrado em logs.
- **Denial of service:** limite de tamanho de payload, timeout e resposta
  `202 Accepted` isolam o caminho síncrono de processamento pesado.
- **Elevation of privilege / confused deputy:** a API sempre reconsulta a
  decisão vigente do Keycloak para o destino solicitado antes de aceitar ou
  processar uma entrega; o token do bot ou a credencial de serviço provam
  apenas a origem do protocolo, nunca a permissão sobre o vault.
- Secrets do bot e chaves de providers `external-push` residem em arquivos
  montados ou secret store operacional; nunca em banco, manifesto, imagem ou
  Git.
- **Egress e OAuth:** conexões de saída usam HTTPS, hosts e portas exatos,
  menor escopo, timeout, limite de resposta e proteção contra SSRF. Toda URL
  opaca, inclusive cursor, discovery e redirect, é revalidada após resolução
  DNS; loopback, redes privadas, link-local e metadata são bloqueados, redirects
  são limitados e `Authorization` nunca cruza origem. Refresh tokens ou
  certificados permanecem em secret store e nunca são devolvidos à UI.
- **Callbacks:** notificações são sinais não confiáveis até a validação
  disponível no provider. Segredo de subscription, IDs de canal/recurso e,
  quando houver, assinatura JWT são verificados; o conteúdo sempre é buscado
  posteriormente pela API autenticada do provider.
- O perfil privado não publica rotas técnicas. No perfil público, o proxy expõe
  flags independentes para callback de bot, callback de subscription e
  `external-push`; BFF administrativo, API interna, worker, PostgreSQL e vault
  permanecem inacessíveis diretamente pela internet.

### Modelo de ameaça verificável da P-01

| Elemento | Definição e proteção exigida |
| --- | --- |
| Ativos | conteúdo Markdown, versões em conflito, aliases e mappings, estado da inbox e jobs, vínculos técnicos de identidade, referências de segredo, configuração do plugin e decisões de autorização do Keycloak; nenhum token, segredo ou corpo de entrega entra em logs, auditoria ou telemetria. |
| Atores confiáveis | operador que habilita conexões, usuário autenticado no Altivy, Keycloak, host e worker distribuídos pelo operador. |
| Atores não confiáveis | usuário do Teams, tenant/app Teams, produtor `external-push`, payload, headers, aliases, IDs, URLs, cursor e qualquer callback recebido. |
| Fronteiras de confiança | Teams/Bot Framework para callback; produtor de serviço para `external-push`; browser/BFF; API/Keycloak; API/worker/PostgreSQL/filesystem; plugin/portas do host; host/provider externo. |
| Fluxos críticos | atividade Teams -> inbox; produtor autorizado -> inbox; inbox -> worker -> Markdown; sessão administrativa -> configuração e vínculo; worker -> provider por egress. Cada fluxo autentica na sua fronteira, autoriza antes de revelar destinos e é correlacionado sem registrar conteúdo. |

| Ameaça | Cenário de abuso | Controle e evidência mínima |
| --- | --- | --- |
| Spoofing | callback ou produtor forja identidade, tenant ou conexão | validação de assinatura, issuer, audience, expiração e allowlist; teste negativo para token, tenant e conexão inválidos. |
| Replay | produtor ou plataforma reenvia uma entrega | chave de idempotência, `sourceEventId`, hash e unicidade transacional; mesmo payload converge e hash divergente conflita. |
| Adulteração | payload, alias ou destino é alterado entre recepção e escrita | validação de schema e hash; alias pré-configurado; worker resolve destino e revalida autorização antes da escrita. |
| Exfiltração | conteúdo ou segredo aparece em logs, resposta, banco técnico ou egress | minimização de campos, referências opacas a segredo, redaction e testes de log; egress com origem, host e redirect restritos. |
| Confused deputy | token do bot ou claim Teams é usado como autorização de vault | vinculação explícita e revogável; decisão vigente do Keycloak por recurso e scope antes de aceitar e processar. |
| Elevação horizontal | produtor escolhe vault, path ou mapping de outro contexto | endpoint aceita somente alias autorizado; paths físicos não fazem parte do contrato; respostas negadas não revelam existência. |
| Indisponibilidade | payload grande, alta frequência ou provider lento esgota o host | limite de corpo antes do processamento, rate limit, timeout, inbox durável, budgets por plugin e processamento assíncrono. |
| Supply chain | plugin ou dependência comprometida é incluído na distribuição | allowlist no build, versões fixas, licença verificada, SBOM, scan e proveniência antes da distribuição. |

### Gates de evolução

O modo sem Microsoft Graph somente é aceitável quando a POC comprovar, com
tráfego sintético, que a action command recebe o conteúdo explicitamente
selecionado ou digitado, não faz chamadas Graph e não requer scopes Graph. A
matriz de evidências deve registrar contextos suportados e recusados, limites
de payload e timeout, versão do schema de manifesto e política do tenant.

Uma evolução para Loop ou outra sincronização automática exige ADR e threat
model próprios antes de qualquer código. A proposta deve demonstrar API e
permissões Graph mínimas, consentimento administrativo, classificação e
retenção de dados, cursor/reconciliação, exclusões, comportamento em conflito,
limites de quota e rollback que preserve o vault. A importação manual desta
decisão não pode ser anunciada como sincronização até esses gates serem
aprovados por humano.

## Dados, compatibilidade e migração

- Nenhuma migração de dado existente é necessária; o recurso introduz uma
  inbox e jobs novos no PostgreSQL, projeções reconstruíveis a partir do
  Markdown gravado no vault.
- O manifesto da plataforma e seus contratos são versionados. Mudança aditiva
  preserva compatibilidade; remoção ou alteração semântica exige versão major,
  matriz de compatibilidade e estratégia expand-contract.
- Estado técnico usa `pluginId` e versão como proveniência. Desinstalação não
  apaga documentos; limpeza de inbox/configuração exige comando administrativo
  separado, retenção e auditoria.
- Campos desconhecidos no envelope `external-push` seguem política de
  compatibilidade a ser documentada no contrato OpenAPI (P-04), sem quebrar
  produtores existentes.
- O documento Markdown resultante registra apenas proveniência técnica no
  frontmatter, sem token, secret ou identificador pessoal desnecessário.

## Observabilidade e operação

- Estado da entrega (recebida, processada, rejeitada, em conflito) deve ser
  consultável pelo usuário via BFF.
- A administração exibe plugins disponíveis, versão, compatibilidade,
  capacidades, estado, dependências e diagnóstico sem revelar secrets.
- Health/readiness do núcleo não falha por indisponibilidade de plugin opcional;
  cada plugin expõe diagnóstico separado e métricas com `plugin.id` de baixa
  cardinalidade.
- Métricas e logs cobrem taxa de aceitação/rejeição, conflito e latência do
  processamento assíncrono, sem registrar conteúdo de entrega.
- Retenção de payload da inbox e de versões em conflito depende de aprovação
  de segurança e privacidade (dependência aberta do plano).

## Validação

- ADR permanece `proposed` até revisão humana explícita.
- testes de arquitetura devem provar que abstrações não dependem de plugins,
  que plugins não acessam Infra.Data/filesystem e que SDK Teams permanece na
  implementação específica;
- testes de contrato devem carregar um plugin sintético compatível, rejeitar
  ID duplicado/versão incompatível/capacidade desconhecida e comprovar que um
  plugin desabilitado não registra rotas ou jobs;
- Threat model completo (P-01 do plano) deve cobrir spoofing, replay,
  adulteração, exfiltração, confused deputy, elevação de privilégio e
  indisponibilidade antes de qualquer registro de app ou dependência nova.
- POC em tenant de teste (P-02) deve comprovar ausência de chamada e permissão
  Microsoft Graph, e a matriz de contextos suportados (chat pessoal, group
  chat, canal público/privado/compartilhado, reunião).
- testes do host devem comprovar que plugin apenas com `external-pull` não
  registra ingress HTTP e que desabilitá-lo interrompe polling e egress;
- futuras POCs de calendário devem comparar polling incremental e webhook,
  permissões mínimas, fluxo OAuth e limites de quota antes de criar um plugin.
- Gate G1 (P-03) exige decisão humana registrada antes de fixar a forma final
  do app.

## Rollout e rollback

- Nenhum código ou dependência é criado a partir deste ADR isoladamente; ele
  habilita as etapas seguintes do plano.
- Se a POC (P-02) ou o gate G1 (P-03) invalidarem a Opção A, este ADR deve ser
  atualizado ou substituído antes de qualquer implementação, preservando esta
  versão como histórico com status `superseded`.
- Rollback do plugin desabilita primeiro seus ingressos e jobs, remove o app do
  Teams e depois pode usar uma distribuição sem o pacote. Vaults, documentos e
  o restante do Altivy permanecem funcionais; estado técnico não é apagado
  automaticamente. Rollback dos contratos do host respeita a matriz de
  compatibilidade e mantém ao menos a versão anterior durante a transição.

## Referências

- [docs/plan/app-altivy-notes-teams-external-push.md](../plan/app-altivy-notes-teams-external-push.md)
- [docs/plan/integracao-teams-notas-reuniao.md](../plan/integracao-teams-notas-reuniao.md) (plano relacionado, ainda `proposed`)
- [ADR 2026-0002](2026-0002-bff-keycloak-autorizacao-externa.md) (Keycloak como autoridade de identidade e autorização)

# Plano: Integração de notas de reunião do Microsoft Teams

> REFERÊNCIA LEGADA: escopo convertido para [Spec Kit](../../specs/006-teams-loop-notes-sync/spec.md), ainda em rascunho e aguardando revisão humana. Este arquivo não tem autoridade operacional; não executar nem atualizar etapas.

**Status:** blocked
**Data de criação:** 2026-08-15  
**Última atualização:** 2026-10-05
**Responsáveis:** não atribuídos  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** ADR de integração com Microsoft Graph pendente e
[app Altivy Notes para Teams com external-push](app-altivy-notes-teams-external-push.md)

## Objetivo

Entregar uma integração opcional que detecte alterações no documento Microsoft
Loop usado pelo painel **Notes** de reuniões do Microsoft Teams, converta seu
conteúdo para Markdown e mantenha um documento gerenciado em um vault do
Altivy, sem sobrescrita silenciosa e sem tornar o funcionamento local dependente
do Microsoft 365.

O resultado esperado é um espelho unilateral Teams/Loop → Altivy. A agenda, as
notas e as tarefas de acompanhamento registradas na reunião devem aparecer em um
único documento Markdown rastreável, pesquisável e indexado pelo Altivy.

## Contexto e premissas

- A viabilidade é condicionalmente positiva e depende da prova de conceito
  descrita neste plano.
- O painel atual de notas colaborativas do Teams usa Microsoft Loop. Componentes
  `.loop` criados no Teams são armazenados no OneDrive do criador.
- O Microsoft Graph v1.0 representa arquivos `.loop` como `driveItem` e permite
  convertê-los para HTML com `GET .../content?format=html`.
- As change notifications de OneDrive são registradas na raiz do drive, não no
  conteúdo específico das notas da reunião. A notificação funciona como sinal
  para consultar `driveItem: delta` e filtrar os itens mapeados.
- Uma instalação sem endpoint HTTPS público pode executar `driveItem: delta`
  periodicamente no worker.
- O delta polling é a baseline self-hosted/local-first. Change notifications
  por webhook são uma otimização opcional para instalações que aceitem publicar
  uma rota técnica isolada e necessitem menor latência.
- A principal incerteza é localizar de forma estável e suportada o arquivo
  `.loop` correspondente a uma reunião e acessá-lo com privilégio aceitável.
- Caso a descoberta automática não seja comprovada, a primeira versão usará
  vinculação assistida por URL ou seleção do arquivo Loop.
- Teams/Loop será a origem externa autoritativa para reconciliação da área
  importada. O Markdown continua sendo a representação canônica persistida no
  Altivy e será gerenciado e somente leitura na interface.
- Uma ocorrência de reunião gera um documento Markdown. Reuniões recorrentes
  preservam identificadores da série e da ocorrência para evitar colisões.
- O fluxo reutilizará os casos de uso de Notes, a gravação segura no filesystem,
  ETags, jobs duráveis, reconciliação e indexação definidos no plano mestre.
- A integração é candidata da Fase 5 do produto e não deve bloquear o MVP nem
  antecipar uma API genérica de webhooks.

## Não escopo

- sincronização Altivy → Teams ou edição bidirecional;
- importação de chat, transcrição, gravação ou resumo do Copilot na primeira
  entrega;
- criação ou atualização de tarefas no Planner ou Microsoft To Do;
- uso de Microsoft Graph `/beta`, scraping do Teams, automação do cliente ou
  engenharia reversa do formato interno de arquivos Loop;
- relay SaaS obrigatório, Azure Event Grid, Azure Event Hubs, broker externo,
  novo microsserviço ou nova fila fora do PostgreSQL;
- app Altivy Notes para Teams e ingestão `external-push`, cobertos pelo
  [plano complementar](app-altivy-notes-teams-external-push.md);
- exclusão automática de documentos Markdown quando a origem for removida.

## Dependências e decisões

- Criar um ADR proposto para integração com Microsoft Graph, fonte canônica,
  autenticação, permissões, webhook versus polling, retenção, conflito e
  rollback.
- Atualizar o threat model antes da implementação para cobrir conteúdo externo,
  callbacks públicos, replay, excesso de privilégio e comprometimento da
  credencial Microsoft 365.
- Usar apenas APIs estáveis do Microsoft Graph v1.0. A necessidade de API beta
  bloqueia a etapa correspondente.
- Obter consentimento administrativo explícito para as permissões Graph
  comprovadamente necessárias.
- Preferir autenticação da aplicação por certificado montado e rotacionável.
- Verificar licença permissiva, versão, manutenção e vulnerabilidades antes de
  adicionar qualquer dependência de conversão HTML → Markdown.
- Descrever recursos administrativos em OpenAPI antes da implementação da API.
- Manter decisões de autorização de usuários e vaults no Keycloak. A API do
  Altivy permanece como Policy Enforcement Point.
- Implementar `polling` como baseline funcional e avaliar `webhook` no gate G2
  somente quando o SLO e a política de exposição da instalação o exigirem.

## Em espera

Lincoln Zocateli determinou em 2026-10-05 que esta integração não deve ser
executada agora. O plano permanece bloqueado antes de P-01; todas as etapas
continuam `not-started`. Não iniciar prova de conceito, consentimento Microsoft
Graph, implementação ou provisionamento até nova autorização humana explícita.

## Etapas

### P-01: Registrar a decisão arquitetural

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** nenhuma

**Objetivo:** estabelecer os limites arquiteturais, de segurança e operacionais
da integração antes da prova de conceito.

Entregas:

- ADR proposto em `docs/adr/` para a integração com Microsoft Graph;
- definição da fonte canônica e do comportamento do documento gerenciado;
- opções de autenticação, permissões, detecção de mudanças e topologia;
- critérios para interromper a iniciativa sem contaminar o núcleo do produto;
- threat model inicial da nova fronteira externa.

Validação:

- ADR segue `.github/ADR_TEMPLATE.md` e permanece `proposed`;
- revisão humana confirma que opções, trade-offs e condições de reversão estão
  documentados;
- threat model cobre credenciais, callback, replay, HTML não confiável,
  autorização horizontal e indisponibilidade do Graph.

Evidências:

- pendente.

### P-02: Provar descoberta e leitura do Loop

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-01

**Objetivo:** confirmar, com Microsoft Graph v1.0, que o arquivo Loop das notas
de uma reunião pode ser localizado ou vinculado e exportado com fidelidade
suficiente.

Entregas:

- tenant Microsoft 365 de teste com reunião e conteúdo sintético;
- identificação de `tenantId`, `driveId`, `driveItemId`, `eTag`, `cTag`, URL e
  relação disponível com evento, reunião ou chat;
- experimento de descoberta automática usando apenas contratos documentados;
- experimento de vinculação assistida por URL ou seleção do arquivo;
- exportação HTML de agenda, notas e tarefas;
- matriz de permissões delegadas e de aplicação efetivamente necessárias;
- relatório de fidelidade para headings, listas, checklists, menções, links,
  tabelas e tarefas.

Validação:

- repetir o experimento com pelo menos três reuniões e dois organizadores;
- localizar ou vincular sem scraping e sem endpoint `/beta`;
- alterar cada seção no Teams e observar mudança de `eTag` ou delta;
- obter HTML pelo Graph v1.0 e comparar seu conteúdo com a interface;
- demonstrar a menor permissão funcional e registrar permissões que falharam.

Evidências:

- pendente.

### P-03: Executar o gate G1 de viabilidade

**Status:** not-started  
**Responsável:** revisor humano pendente  
**Dependências:** P-02

**Objetivo:** decidir se a integração pode avançar e qual modo de vinculação é
suportado.

Entregas:

- decisão entre descoberta automática e vinculação assistida;
- decisão sobre aceitabilidade das permissões Graph;
- registro de limitações de fidelidade da conversão;
- atualização do ADR e deste plano com a decisão humana.

Validação:

- descoberta automática somente é aceita se for documentada, estável e
  reproduzível;
- vinculação assistida é o fallback quando não existir elo público confiável
  entre reunião e arquivo Loop;
- a iniciativa fica `blocked` se exportação HTML ou permissões forem
  inaceitáveis;
- transcrição ou chat somente podem ser avaliados em outro plano.

Evidências:

- pendente; exige decisão humana registrada.

### P-04: Definir contratos administrativos

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-03

**Objetivo:** definir os recursos HTTP usados para configurar e observar a
integração sem expor detalhes internos do Graph ou do filesystem.

Entregas:

- recursos OpenAPI para `integrations`, `meeting-note-mappings` e `sync-runs`;
- operações de criação, consulta, desativação, remapeamento e retry;
- callback técnico do Graph separado dos recursos públicos de Notes;
- erros `application/problem+json`, IDs opacos e timestamps UTC;
- requisitos de autorização Keycloak por recurso e scope;
- documentação de paginação, idempotência e respostas `409`/`412`.

Validação:

- lint e testes de contrato OpenAPI;
- rotas usam substantivos e semântica HTTP, sem verbos ou RPC;
- exemplos não contêm tokens, IDs reais, URLs pré-autenticadas ou conteúdo de
  notas;
- clientes gerados compilam sem edição manual.

Evidências:

- pendente.

### P-05: Modelar domínio e persistência

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-04

**Objetivo:** representar mappings, subscriptions, cursores e execuções de forma
durável, idempotente e independente do provider.

Entregas:

- plugin `altivy.integrations.microsoft-graph`, compatível com o Plugin Host,
  declarando capacidades, endpoints, jobs e scopes no manifesto;
- modelos `MicrosoftGraphConnection`,
  `ExternalDocumentMapping`, `GraphSubscription`, `GraphDeltaCursor`,
  `InboundNotification` e `SyncRun`;
- SDK e tipos do Microsoft Graph confinados ao plugin; o núcleo recebe apenas
  contratos neutros de aplicação;
- persistência de tenant, drive, item, vault, path, source eTag, hash, estado,
  lease, retry e chave de idempotência;
- migrations expand-contract para PostgreSQL;
- referência a secrets externos, sem armazenar credenciais no banco;
- estados explícitos para ativo, desativado, conflito, origem indisponível,
  retry e dead letter.

Validação:

- testes de arquitetura impedem que Domain, Application e outros plugins
  dependam do plugin Microsoft Graph ou de seu SDK;
- testes unitários das invariantes e transições de estado;
- testes de integração com PostgreSQL real para concorrência, lease e dedupe;
- teste de migration de ida e compatibilidade com a versão anterior;
- inspeção confirma ausência de token, certificado privado e URL de download.

Evidências:

- pendente.

### P-06: Definir o contrato Markdown gerenciado

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-04

**Objetivo:** produzir documentos portáveis e previsíveis sem permitir que a
sincronização sobrescreva alterações locais silenciosamente.

Entregas:

- convenção de path configurável, por exemplo
  `Reunioes/Teams/AAAA/MM/AAAA-MM-DD - assunto.md`;
- normalização de nome e resolução de colisões por identificador opaco;
- frontmatter com provider, IDs técnicos, URL de origem, horário, source eTag,
  sincronização e estado gerenciado;
- seções estáveis `Agenda`, `Notas da reunião` e
  `Tarefas de acompanhamento`;
- política de read-only na UI e de conflito para alterações externas;
- regra de remoção da origem sem exclusão automática do Markdown.

Validação:

- fixtures cobrem Windows, Linux, caracteres Unicode, nomes reservados,
  traversal, reuniões recorrentes e títulos iguais;
- o documento permanece legível fora do Altivy;
- alterações locais produzem conflito recuperável e preservam as duas versões;
- frontmatter não contém secret nem dado pessoal desnecessário.

Evidências:

- pendente.

### P-07: Implementar autenticação e cliente Graph

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-03, P-05

**Objetivo:** acessar somente o tenant e os recursos autorizados, com menor
privilégio, resiliência e telemetria segura.

Entregas:

- porta em `Altivy.Backend.Application`, adapter específico no plugin
  `altivy.integrations.microsoft-graph` e apenas primitivas HTTP genéricas em
  `Altivy.Backend.Infra.Http`;
- autenticação preferencial por certificado montado e rotacionável;
- restrições de tenant, audience, endpoints e scopes;
- validação central de URLs opacas, inclusive `@odata.nextLink` e
  `@odata.deltaLink`, com HTTPS, host/porta exatos, resolução DNS, bloqueio de
  loopback, redes privadas, link-local e metadata, além de redirects limitados
  sem propagação de `Authorization` entre origens;
- timeout, cancelamento, retry com `Retry-After`, backoff e limite de resposta;
- classificação de erros transitórios, permanentes e de autorização;
- métricas e traces sem corpo, token ou URL pré-autenticada.

Validação:

- testes com servidor HTTP controlado para timeout, throttling e retry;
- testes negativos para tenant, audience, token expirado e scope ausente;
- secret scanning e inspeção de logs;
- revisão da licença e versão de qualquer SDK adicionado.

Evidências:

- pendente.

### P-08: Converter Loop para Markdown seguro

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-06, P-07

**Objetivo:** transformar o HTML exportado pelo Graph em Markdown determinístico
sem executar ou preservar conteúdo ativo perigoso.

Entregas:

- leitura condicional do `driveItem` por eTag;
- exportação por `content?format=html`;
- parser HTML estruturado e sanitização central por allowlist;
- conversão de headings, parágrafos, listas, checklists, links e tabelas;
- fallback legível e warning para estruturas desconhecidas;
- limites configuráveis de tamanho, profundidade e tempo de processamento.

Validação:

- testes com fixtures coletadas na POC e dados sintéticos;
- saída idêntica para a mesma entrada;
- bloqueio de script, iframe, event handlers, `javascript:` e data URLs não
  permitidas;
- testes de payload excessivo, HTML malformado e estruturas profundas;
- comparação automática entre seções esperadas e Markdown gerado.

Evidências:

- pendente.

### P-09: Implementar pipeline durável de espelhamento

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-05, P-08

**Objetivo:** transformar notificações ou polling em atualizações idempotentes
de Notes sem acoplar callback, worker e filesystem.

Entregas:

- inbox durável para eventos externos;
- job com lease, retry, backoff e dead letter no PostgreSQL;
- filtro de mappings, leitura, conversão e chamada ao caso de uso de Notes;
- reautorização no Keycloak do principal técnico e do recurso antes da busca e
  novamente antes da gravação; revogação ou indisponibilidade falha fechada e
  coloca o mapping em quarentena observável;
- gravação por arquivo temporário no mesmo filesystem e replace seguro;
- uso de source eTag, hash e ETag local para dedupe e conflito;
- enfileiramento da indexação após gravação bem-sucedida;
- reconciliação periódica de mappings ativos.

Validação:

- testes com PostgreSQL e filesystem reais;
- replays e execuções concorrentes não duplicam documentos ou versões;
- falha entre filesystem e banco converge por reconciliação;
- edição local concorrente entra em conflito sem perda de conteúdo;
- dead letter preserva IDs técnicos e não armazena conteúdo integral em logs.

Evidências:

- pendente.

### P-10: Provar detecção por webhook opcional

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-05, P-09

**Objetivo:** comprovar, sem torná-la requisito da integração, a detecção por
change notifications em instalações que aceitem endpoint HTTPS público.

Entregas:

- endpoint de validação do `validationToken`;
- endpoints de change notification e lifecycle notification;
- validação em tempo constante de `clientState`, tenant e subscription;
- tratamento do callback como sinal não confiável: notificações básicas usam
  `clientState` aleatório por subscription; rich notifications, se adotadas,
  também validam assinatura, issuer, audience, tenant e caller do JWT;
- assinatura da raiz do drive com `changeType=updated`;
- criação, renovação antecipada, reautorização, remoção e reconciliação das
  subscriptions;
- persistência rápida e disparo de delta query pelo worker;
- configuração de proxy, TLS e restrição de rede aplicável.

Validação:

- validação responde `200 text/plain` em até 10 segundos;
- callback válido é persistido e responde `202` em até 3 segundos;
- callback inválido não agenda trabalho;
- subscriptions expiradas, removidas e com reautorização necessária são
  recuperadas;
- teste de carga mantém o endpoint fora dos estados `slow` e `drop` do Graph.

Evidências:

- pendente.

### P-11: Implementar detecção por delta polling

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-05, P-09

**Objetivo:** oferecer sincronização funcional em instalações privadas sem
callback público.

Entregas:

- job agendado no worker para `driveItem: delta`;
- persistência opaca de `@odata.deltaLink`;
- paginação completa, replay seguro, jitter e backoff;
- tratamento de `410 Gone` com ressincronização completa controlada;
- intervalo configurável e métricas de atraso;
- mesma chave de idempotência usada pelo modo webhook.

Validação:

- alteração do Loop é detectada dentro do intervalo configurado;
- reinício do worker retoma o cursor sem perder ou duplicar atualização;
- reset do delta executa full sync e converge;
- throttling respeita `Retry-After`;
- nenhum endpoint público é necessário para esse modo.

Evidências:

- pendente.

### P-12: Executar o gate G2 de topologia

**Status:** not-started  
**Responsável:** revisor humano pendente  
**Dependências:** P-11; P-10 somente quando webhook for candidato ao rollout

**Objetivo:** selecionar o modo operacional suportado por instalação e definir
se haverá fallback automático.

Entregas:

- critérios documentados para selecionar `webhook` ou `polling`;
- decisão sobre manter somente polling ou adicionar webhook como aceleração;
- SLO de atualização para cada modo;
- atualização do ADR, configuração e runbook.

Validação:

- webhook é recomendado apenas com proxy HTTPS público confiável;
- polling é a baseline suportada para rede privada;
- os dois modos não processam o mesmo evento como produtores independentes sem
  dedupe compartilhado;
- mudança de modo não exige recriar mappings nem documentos.

Evidências:

- pendente; exige decisão humana registrada.

### P-13: Implementar administração e experiência de conflito

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-04, P-05, P-12

**Objetivo:** permitir configuração e diagnóstico sem expor credenciais,
conteúdo bruto ou caminhos físicos.

Entregas:

- tela Angular protegida por scopes Keycloak;
- cadastro da referência da credencial e validação de conectividade;
- seleção de tenant, drive, item, vault, path e modo de detecção;
- estado de subscription ou cursor, última sincronização, atraso e erro;
- ações de desativar, remapear e solicitar nova sincronização;
- indicação read-only no documento gerenciado;
- fluxo explícito para resolver conflito e preservar versões.

Validação:

- testes de componente e Playwright para sucesso, erro, vazio, conflito e
  indisponibilidade;
- navegação por teclado e estados de foco atendem WCAG 2.2 AA;
- autorização horizontal impede configurar outro vault;
- tokens, HTML bruto e paths físicos não aparecem na UI nem nas respostas.

Evidências:

- pendente.

### P-14: Preparar observabilidade e operação

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-09, P-12

**Objetivo:** tornar atraso, falha e recuperação observáveis sem registrar
conteúdo de notas.

Entregas:

- métricas de callbacks, rejeições, delta pages, lag, exportações, no-op,
  conflitos, retries e dead letters;
- traces de notification → delta → fetch → write → index;
- readiness que distingue falha opcional do Graph da saúde do núcleo;
- configuração de proxy, TLS, secrets e certificado montado;
- runbooks de consentimento, rotação, expiração, reautorização, `410 Gone`,
  revogação, remapeamento e remoção da integração;
- alertas recomendados para atraso e falha persistente.

Validação:

- falha no Graph não impede CRUD e busca local;
- dashboards mostram atraso e estado sem conteúdo integral;
- rotação de certificado e recuperação de subscription são ensaiadas;
- pessoa fora da implementação executa os runbooks em ambiente de teste.

Evidências:

- pendente.

### P-15: Automatizar a qualidade da integração

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-08, P-09, P-11, P-13, P-14; P-10 quando webhook for
selecionado em P-12

**Objetivo:** demonstrar comportamento, segurança, desempenho e recuperação nos
níveis proporcionais ao risco da integração.

Entregas:

- testes unitários de conversão, naming, path, dedupe e conflito;
- testes de integração com PostgreSQL e filesystem reais;
- testes de contrato OpenAPI e, quando habilitados, callbacks Graph;
- testes negativos de segurança;
- testes Playwright da administração e do documento gerenciado;
- cenário E2E em tenant Microsoft 365 de teste;
- teste de desempenho da fila e, quando habilitado, do callback;
- teste de recuperação de subscription e delta cursor.

Validação:

- editar agenda, notas e tarefas atualiza exatamente um Markdown;
- replay, retry e concorrência não duplicam documentos;
- tenant, subscription ou `clientState` inválidos são rejeitados;
- traversal, XSS, payload excessivo e nomes reservados são tratados;
- webhook, quando selecionado, atende os limites de resposta do Graph;
- polling atende o SLO configurado;
- logs, métricas e traces passam por verificação de dados sensíveis.

Evidências:

- pendente.

### P-16: Executar rollout incremental e rollback

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-15

**Objetivo:** liberar a integração de forma reversível e baseada em evidências.

Entregas:

- feature flag desabilitada por padrão;
- piloto inicial com um tenant, um drive, um vault e reunião sintética;
- piloto posterior com grupo autorizado;
- acompanhamento de fidelidade, latência, conflito, throttling e permissões;
- procedimento de desativação que preserva documentos, cursores, auditoria e
  revisões;
- critérios de expansão, pausa e cancelamento.

Validação:

- desativar a integração interrompe novas sincronizações;
- rollback não exclui nem altera documentos existentes;
- mappings podem ser reativados sem perder idempotência;
- expansão só ocorre após revisão humana das métricas e riscos;
- remoção definitiva de arquivo exige ação explícita e autorizada.

Evidências:

- pendente.

## Critérios de aceite finais

- A POC localiza automaticamente ou vincula de forma assistida o arquivo
  `.loop` correto e o exporta por Microsoft Graph v1.0, sem APIs beta.
- As permissões mínimas estão documentadas e aprovadas; nenhuma permissão de
  escrita é solicitada sem necessidade comprovada.
- Editar agenda, notas e tarefas no Teams atualiza exatamente um Markdown no
  destino configurado.
- O SLO inicial é de até 2 minutos no modo webhook e de até
  `intervalo de polling + 2 minutos` no modo privado.
- Replays, retries, notificações duplicadas e concorrência não geram arquivos
  duplicados nem versões desnecessárias.
- Uma alteração manual concorrente nunca é sobrescrita; o mapping entra em
  conflito e preserva o conteúdo existente e a versão importada.
- Revogação, subscription expirada, `410 Gone`, throttling e indisponibilidade
  geram estado observável e recuperação documentada.
- Revogar o scope entre agendamento, fetch e gravação impede o commit e coloca
  o mapping em quarentena observável.
- Callback, quando habilitado, rejeita tenant, subscription e `clientState`
  inválidos e atende os limites de resposta do Graph.
- HTML malicioso, links perigosos, payload excessivo, traversal e nomes
  reservados são rejeitados ou sanitizados antes da escrita.
- Logs, métricas e traces não contêm conteúdo integral, tokens, URLs
  pré-autenticadas ou dados pessoais desnecessários.
- A falha ou desativação da integração não impede CRUD, busca, backup ou
  reconstrução local do Altivy.
- Desativar a integração preserva os documentos já importados.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Não existir elo público estável entre reunião e Loop | descoberta automática inviável | vinculação assistida por URL ou item como baseline; gate G1 |
| Permissão Graph ampla para conversão | rejeição pelo tenant ou excesso de privilégio | medir na POC, usar menor privilégio e bloquear sem aprovação |
| Webhook observar somente a raiz do drive | alto volume de sinais irrelevantes | usar webhook como wake-up, delta cursor e filtro por mapping |
| Endpoint privado não receber callback | modo webhook inviável | oferecer delta polling sem exposição pública |
| Formato do HTML exportado mudar | regressão na conversão | fixtures versionadas, parser tolerante e canal preview |
| Documento gerenciado ser alterado localmente | risco de perda de conteúdo | read-only na UI, hash/ETag, conflito e revisão recuperável |
| Subscription expirar ou callback ser perdido | atraso de sincronização | renewal, lifecycle notifications, delta e fallback controlado |
| Dono do Loop sair da organização | origem ficar indisponível | estado explícito, alerta e remapeamento sem mover o `.loop` |
| Rótulo de sensibilidade ou retenção bloquear acesso | falha de sync ou risco de compliance | respeitar políticas, falhar fechado e documentar responsabilidade |
| Conteúdo externo malicioso | XSS, exfiltração ou path injection | parser estruturado, allowlist, limites e validação de path |
| Throttling do Microsoft Graph | atraso e acúmulo de jobs | `Retry-After`, backoff, jitter, métricas e limitação por conexão |
| Secret ou certificado ser comprometido | acesso indevido ao Microsoft 365 | certificado rotacionável, menor privilégio, revogação e auditoria |

## Rollout e rollback

O rollout inicia com feature flag desabilitada e avança por quatro anéis:

1. POC isolada em tenant de teste, sem dados reais;
2. integração interna com uma reunião sintética;
3. piloto autorizado com poucas reuniões e um vault dedicado;
4. disponibilidade opcional após revisão de segurança e operação.

Sinais de sucesso incluem fidelidade do Markdown, atraso dentro do SLO, ausência
de duplicatas, zero sobrescrita silenciosa, baixa taxa de dead letter e
permissões aceitas pelo administrador. O rollout deve ser pausado diante de
perda de conteúdo, necessidade de API beta, excesso de privilégio, vazamento em
logs, conflito não recuperável ou throttling persistente.

O rollback desativa mappings e subscriptions e interrompe o polling. Documentos
Markdown já criados, revisões, cursores e auditoria são preservados. Nenhum
rollback remove arquivos automaticamente. A credencial Graph é revogada ou
rotacionada conforme o motivo da reversão.

## Referências

- [Plano mestre](../plano-mestre.md)
- [App Altivy Notes para Teams e ingestão external-push](app-altivy-notes-teams-external-push.md)
- [Evolução do produto](../evolucao-produto.md)
- [Implementação do backend](../backend.md)
- [Implementação do frontend](../frontend.md)
- [Microsoft Teams meeting notes](https://support.microsoft.com/en-us/office/take-meeting-notes-in-microsoft-teams-3eadf032-0ef8-4d60-9e21-0691d317d103)
- [Loop components e armazenamento `.loop`](https://learn.microsoft.com/en-us/microsoft-365/loop/loop-components-teams)
- [Conversão de `driveItem` para HTML](https://learn.microsoft.com/en-us/graph/api/driveitem-get-content-format?view=graph-rest-1.0)
- [Change notifications](https://learn.microsoft.com/en-us/graph/change-notifications-overview)
- [Entrega de notifications por webhook](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-webhooks)
- [Delta query](https://learn.microsoft.com/en-us/graph/delta-query-overview)

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-08-15 | plano | - | proposed | análise do plano mestre, arquitetura do Altivy e documentação oficial Microsoft | IA assistida |
| 2026-08-31 | plano/P-10/P-12/P-15 | approved/not-started | approved/not-started | polling definido como baseline privada e webhook mantido como aceleração opcional condicionada ao gate G2 | IA assistida |
| 2026-10-05 | plano | approved | blocked | Lincoln Zocateli colocou a integração Teams/Loop em espera; nenhuma etapa deve ser executada até nova autorização explícita | Lincoln Zocateli |

## Regras de atualização

- O status geral usa `draft`, `proposed`, `approved`, `in-progress`, `blocked`,
  `completed`, `cancelled` ou `superseded`.
- Cada etapa usa `not-started`, `in-progress`, `blocked`, `completed` ou
  `skipped`.
- IA pode manter este plano no máximo como `proposed` sem aprovação humana
  explícita.
- `approved` registra responsável e referência da aprovação humana.
- `completed` exige etapas obrigatórias concluídas ou justificadamente
  ignoradas, critérios finais atendidos e evidências registradas.
- `skipped` exige justificativa e aprovação humana.
- Toda alteração de status geral ou de etapa adiciona uma entrada ao histórico.

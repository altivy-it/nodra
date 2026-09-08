# Plano: Workspace frontend inspirado no Obsidian

**Status:** in-progress  
**Data de criação:** 2026-09-01  
**Última atualização:** 2026-09-02  
**Responsáveis:** Lincoln Zocateli  
**Origem:** IA assistida a partir da solicitação do produto  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [plano mestre](../plano-mestre.md), [implementação do frontend](../frontend.md), [evolução do produto](../evolucao-produto.md) e [plataforma de plugins](app-altivy-notes-teams-external-push.md)

## Objetivo

Evoluir o frontend para um workspace denso e persistente, inspirado na organização
espacial do Obsidian sem copiar sua identidade visual, no qual o usuário possa:

- alternar entre módulos pela barra vertical de atividades à esquerda;
- organizar notas e pastas em um explorador hierárquico redimensionável;
- trabalhar com documentos na área central sem perder contexto;
- consultar conta e notificações no canto superior direito;
- abrir uma central de notificações com eventos operacionais relevantes;
- abrir uma ajuda integrada por menu ou teclado, sem depender de documentação
  externa;
- usar um painel contextual à direita para grafo, calendário, propriedades,
  tags e futuras integrações confiáveis.

O resultado deve preservar Markdown como fonte canônica, oferecer navegação
completa por teclado e manter o shell utilizável durante carregamento, falha de
rede ou indisponibilidade de uma integração opcional.

## Contexto e premissas

- O frontend atual concentra sessão, lista de notas e edição no componente raiz;
  a evolução deve migrar esse comportamento em etapas, sem reescrever os serviços
  funcionais já disponíveis.
- A arquitetura frontend já prevê `core`, `shell`, `features`, `data-access` e
  `design-system`; o novo layout concretiza essa separação.
- A inspiração no Obsidian limita-se ao modelo mental de workspace com barras,
  painéis e área de documento. Tokens, tipografia, iconografia e interação devem
  manter identidade própria do Altivy.
- A primeira versão desktop usa quatro regiões persistentes: barra de atividades,
  painel primário, conteúdo central e painel contextual. Uma barra superior e uma
  barra de status atravessam as regiões necessárias.
- O estado de dimensões, painéis abertos, módulo ativo e abas pertence ao
  workspace. Preferências não sensíveis são persistidas no storage do browser
  por usuário e vault, mas conteúdo e estado operacional continuam vindo do
  servidor.
- Tema, densidade, filtros, ordenação, painéis visíveis, dimensões e módulo ativo
  devem sobreviver ao fechamento e à reabertura da aplicação no mesmo perfil do
  browser.
- A central de notificações exibe eventos de produto e operação destinados ao
  usuário. Ela não é um visualizador de logs HTTP, payloads SignalR, conteúdo
  integral de notas, tokens ou secrets.
- Toasts continuam efêmeros; notificações persistentes possuem identificador,
  origem, categoria, severidade, instante, estado de leitura e ação segura
  opcional.
- Atualizações em tempo real usam SignalR como aceleração. Consulta paginada à
  API é a fonte para reconstruir a central após reconexão.
- O painel direito oferece slots controlados para features instaladas na
  distribuição. Ele não carrega JavaScript arbitrário, plugins do vault nem
  componentes remotos em runtime.
- Grafo e propriedades são features nativas; calendários e outras integrações
  permanecem opcionais e dependem de contratos, autorização e planos próprios.
- A ajuda integrada é conteúdo Markdown empacotado com a aplicação, somente
  leitura e separado do vault. Não aparece no explorador, na busca de notas, no
  grafo, no histórico ou na indexação do conteúdo do usuário.
- Angular CDK e Lucide Angular são as referências já definidas, mas versões e
  licenças exatas devem ser verificadas antes de qualquer inclusão no projeto.

## Modelo de layout proposto

```text
┌────────┬──────────────────────┬────────────────────────────┬──────────────────────┐
│        │                      │ Barra superior             │ Notificações | Conta │
│ Barra  ├──────────────────────┼────────────────────────────┴──────────────────────┤
│ de     │ Painel primário      │ Abas e conteúdo central    │ Painel contextual    │
│ ativi- │                      │                            │                      │
│ dades  │ Arquivos             │ Editor / preview / busca   │ Grafo / calendário  │
│        │ Busca                │ ou central de notificações │ propriedades / tags  │
│        │ Tags / favoritos     │                            │                      │
├────────┴──────────────────────┴────────────────────────────┴──────────────────────┤
│ Barra de status: salvamento | conexão | indexação | contexto                    │
└──────────────────────────────────────────────────────────────────────────────────┘
```

- Barra de atividades: largura estável entre 44 e 52 px, com ícones, tooltip,
  estado ativo e ordem previsível.
- Painel primário: largura inicial próxima de 280 px, redimensionável por mouse e
  teclado, recolhível e com limites que não comprimam a área central.
- Área central: única região flexível; mantém abas, seleção e scroll por documento.
- Painel contextual: largura inicial próxima de 320 px, redimensionável,
  recolhível e associado à aba ou ao contexto atual.
- Barra superior: título/contexto à esquerda; sino, estado de conexão, avatar e
  menu da conta à direita, sem expor dados além dos fornecidos pela sessão.
- Barra de status: altura estável e mensagens curtas, sem provocar deslocamento de
  layout.

Os valores finais devem ser confirmados em protótipo responsivo e registrados em
tokens, não espalhados como valores isolados nos componentes.

## Persistência local de preferências

A primeira versão usa `localStorage` para configurações pequenas, serializáveis e
não sensíveis. O acesso fica encapsulado em um serviço de preferências; features
não leem nem escrevem diretamente no storage. IndexedDB somente será considerada
se o volume ou a estrutura ultrapassar o uso apropriado de `localStorage`.

Preferências previstas:

- tema `system`, `light`, `dark` ou `high-contrast`;
- densidade compacta ou confortável;
- larguras, visibilidade e ferramenta ativa dos painéis;
- módulo ativo e estado de expansão do explorador;
- filtros e ordenações de arquivos, busca, tags, grafo e notificações;
- opções de visualização que não alterem conteúdo ou autorização.

O documento persistido deve possuir versão de schema e ser separado por escopo
opaco de usuário e identificador de vault. Preferências globais, como tema, ficam
no escopo do usuário; filtros relacionados ao conteúdo ficam no escopo do usuário
e vault. A aplicação deve aplicar defaults quando não houver sessão, quando o
storage estiver indisponível ou quando o valor estiver corrompido, incompatível ou
fora da allowlist.

Não podem ser persistidos nesse mecanismo:

- access token, refresh token, cookie, antiforgery token ou credencial;
- conteúdo, título, trecho, path ou rascunho de nota;
- payload de notificação, resposta de API, log ou dado de plugin;
- permissão, decisão de autorização ou flag capaz de liberar funcionalidade
  protegida.

O storage do browser é entrada não confiável. Filtros recuperados devem ser
validados e normalizados antes do uso, e a API continua responsável por validar
filtros, limites e autorização. O logout limpa estado sensível em memória, mas
mantém preferências não sensíveis no escopo do usuário para o próximo login. O
menu da conta deve oferecer uma ação explícita para restaurar padrões e remover
as preferências locais daquele usuário no dispositivo.

O escopo de usuário usa exclusivamente o claim OIDC `sub`, por ser opaco e
estável. `username`, nome e e-mail podem ser alterados no Keycloak e não podem
compor a chave de persistência. O formato conceitual é
`altivy:preferences:v<schema>:<sub>:<vault-id>`; preferências globais omitem o
identificador de vault.

## Metadados, classificação e suporte a IA

Toda nota criada pelo Altivy recebe frontmatter YAML mínimo e válido. Ele é parte
do arquivo Markdown canônico, permitindo indexação e portabilidade fora da
aplicação. A criação não pode ser bloqueada por falta de preenchimento manual:
os valores iniciais são `doc_type: note`, `classification: unclassified` e
`tags: [inbox]`. O usuário pode alterá-los na tela de Propriedades antes ou depois
de salvar.

O schema inicial deve usar campos namespaced para evitar colisão com frontmatter
já usado por outras ferramentas:

```yaml
---
altivy:
  metadata_version: "1.0"
  classification: unclassified
  doc_type: note
  tags: [inbox]
---
```

`classification` é um rótulo de organização e busca; não concede acesso nem
substitui a autorização decidida pelo Keycloak. A taxonomia inicial e fechada é
`unclassified`, `public`, `internal`, `restricted` e `confidential`; cada vault
pode posteriormente acrescentar valores por política versionada. `doc_type`
começa com `note`, `reference`, `decision`, `meeting`, `runbook` e `task`.
`tags` deve conter ao menos uma tag normalizada, sendo `inbox` o default seguro.

Campos como `last_reviewed`, `audience` e `keywords` são opcionais. O backend
indexa título, caminho, headings, corpo, links, tags e propriedades presentes;
ausência de palavras-chave ou data de revisão não torna a nota não indexável.
Arquivos Markdown criados ou alterados externamente continuam válidos: o parser
os indexa com os dados disponíveis, marca a ausência de classificação como estado
visível e nunca injeta ou reordena frontmatter silenciosamente.

Hints para IA fazem sentido somente como orientação opcional e declarativa. Em
vez de comentário HTML, devem ficar no frontmatter `altivy.agent` para que sejam
parseáveis, versionáveis, visíveis e editáveis pela mesma tela de Propriedades:

```yaml
---
altivy:
  metadata_version: "1.0"
  classification: internal
  doc_type: reference
  tags: [arquitetura, camadas, clean-architecture, dotnet]
  keywords: [dependency rule, Api, Application, Domain, Infra, acoplamento]
  last_reviewed: "2026-07-13"
  audience: [developers, architects]
  agent:
    intent_examples:
      - "Qual a direção de dependência permitida entre camadas?"
      - "Domain pode referenciar Entity Framework?"
    answer_style: "regra objetiva + sinais de alerta + exemplos de código"
    canonical_section_order: ["Visão Geral", "Regra Obrigatória", "Referências"]
    strict_citations: false
---
```

O frontmatter não aparece no preview renderizado, mas nunca é oculto do usuário:
o modo fonte o exibe e o painel Propriedades deve fornecer uma seção avançada
**Orientação para IA**, com visualização, edição, remoção e explicação breve de
que esses campos são opcionais. Hints não são instruções confiáveis: não podem
alterar autorização, executar ações, suplantar o conteúdo da nota, dispensar
citações ou forçar comportamento de um modelo. Enquanto uma capacidade de IA não
existir ou estiver desabilitada, o Altivy apenas preserva e indexa esses campos
sem enviá-los a qualquer serviço externo.
## Ajuda integrada

O Altivy inclui uma central de ajuda em Markdown versionada junto do frontend e
servida como asset estático da aplicação. Ela usa a mesma política de
renderização segura aplicada ao preview, mas é sempre somente leitura: não abre
no editor, não aceita alteração no browser e não pode ser escrita no vault.

A ajuda abre como módulo ou rota lazy loaded na área central por item da barra de
atividades, menu da conta ou atalho configurável. Ao fechá-la, o usuário retorna
à aba ou rota anterior sem perder o estado do workspace. A primeira versão inclui
tutoriais curtos e pesquisáveis sobre:

- criar, organizar, mover, renomear e recuperar notas e pastas;
- usar abas, editor, preview, atalhos, busca, backlinks e painel contextual;
- selecionar tema, densidade, painéis, filtros e restaurar preferências locais;
- classificar documentos, atribuir tags e editar propriedades e orientação para
  IA;
- abrir **Gerenciar conta** e alterar dados de perfil, credenciais, sessões ou
  MFA exclusivamente no Keycloak Account Console, sujeitos à política do realm;
- identificar estado de salvamento, conflito, sincronização, notificação, acesso
  negado e indisponibilidade de plugin;
- limites de segurança: Markdown como fonte canônica, classificação não é
  autorização e conteúdo de plugins somente opera quando permitido.

O conteúdo deve usar headings estáveis, links internos, sumário e linguagem
acessível. Atualizações do Help acompanham a release da aplicação; mudanças que
afetem fluxos descritos exigem atualização no mesmo change set. Documentação de
operação, credenciais ou decisões administrativas permanece fora do Help quando
não for apropriada ao usuário final.

## Não escopo

- copiar CSS, assets, marca, atalhos ou código do Obsidian;
- obter paridade completa com o Obsidian ou compatibilidade com seus plugins;
- marketplace, instalação dinâmica ou execução de código de terceiros;
- implementar calendário, integrações externas e grafo global completo neste
  plano; esta entrega prepara suas superfícies e integra features quando prontas;
- transformar logs internos, traces ou requisições em notificações para usuários;
- criar chat genérico entre frontend e backend;
- transformar a ajuda integrada em nota editável, conteúdo do vault ou canal de
  suporte operacional;
- carregar documentação remota, scripts ou HTML não confiável dentro da ajuda;
- exigir palavras-chave, data de revisão ou hints de IA para salvar uma nota;
- tratar classificação do documento como mecanismo de autorização;
- ocultar metadados de usuários capazes de editar a nota;
- enviar conteúdo ou metadados a um provedor de IA sem recurso opcional,
  consentimento, política de privacidade e decisão arquitetural próprios;
- administração de usuários, senha, MFA ou políticas de autorização no Altivy;
- redesenhar contratos de notas sem necessidade demonstrada pelo shell;
- substituir a estratégia de estado por um store global.

## Dependências e decisões

- Mudanças HTTP devem partir de OpenAPI e usar o cliente gerado; DTOs de
  notificação não serão duplicados manualmente no frontend.
- A API deve filtrar notificações pela identidade e pelas permissões emitidas pelo
  Keycloak. O browser usa apenas a sessão same-origin do BFF.
- A sessão deve fornecer um identificador opaco e estável para separar
  preferências locais: o claim OIDC `sub`. Nome, e-mail, `username`, token e
  outras credenciais não podem compor a chave.
- Preferências locais não exigem endpoint backend nem sincronização entre
  dispositivos nesta entrega. A ausência ou limpeza do storage apenas restaura os
  valores padrão.
- O contrato Markdown deve preservar campos de frontmatter desconhecidos e
  validar o namespace `altivy`, sua versão, tipos, valores permitidos e
  normalização de tags sem remover conteúdo do usuário.
- A tela de Propriedades edita frontmatter por parser YAML seguro, nunca por
  concatenação de strings, e usa a mesma gravação com ETag das demais alterações
  de nota.
- A classificação é indexável e filtrável, mas autorização de leitura e escrita
  continua exclusivamente no Keycloak e na API como PEP.
- Metadados de orientação para IA exigem capacidade opcional, threat model e ADR
  antes de qualquer processamento por modelo ou envio a terceiros.
- O Help é um asset versionado e somente leitura; sua rota, conteúdo e links não
  podem depender da disponibilidade da API. A busca do Help ocorre somente nesse
  corpus empacotado e nunca consulta ou mistura notas do vault.
- Nome, e-mail, `username`, credenciais, sessões e MFA pertencem exclusivamente
  ao Keycloak. O Altivy não cria endpoints, banco ou formulários para alterar
  esses dados; o menu de conta apenas redireciona ao Keycloak Account Console
  configurado pelo operador.
- A permissão para editar perfil, e-mail, `username` ou configurar MFA é política
  do realm. O frontend deve ocultar ou desabilitar o redirecionamento quando o
  Account Console não estiver configurado, sem tentar reproduzir suas regras.
- O contrato mínimo da central deve definir paginação por cursor, filtros,
  marcação de leitura idempotente e política de retenção. Ação associada usa rota
  interna ou comando permitido, nunca URL arbitrária fornecida por integração.
- O envelope SignalR deve transportar apenas o resumo necessário para atualizar a
  interface; detalhes são consultados pela API quando autorizados.
- A retenção, auditoria e classificação de notificações operacionais precisam ser
  alinhadas ao módulo `Operations`. Notificações de plugins devem declarar origem
  e capacidade, sem conceder acesso adicional ao vault.
- A região extensível do shell pode ser implementada antes da plataforma de
  plugins porque registra componentes nativos em build time. Integrações externas
  permanecem condicionadas às decisões e ADRs da plataforma de plugins.
- Angular CDK, Lucide Angular, CodeMirror e Cytoscape.js somente entram nas etapas
  que os utilizam, com versão fixada, licença permissiva verificada e SBOM
  atualizado.

## Etapas

### P-01: Validar arquitetura de informação e protótipo

**Status:** in-progress  
**Responsável:** produto, UX e frontend  
**Dependências:** nenhuma

**Objetivo:** confirmar hierarquia, densidade e comportamento responsivo antes de
fragmentar o componente raiz.

Entregas:

- mapa dos módulos e comandos disponíveis na barra de atividades;
- wireframes para desktop, tablet e celular nos estados autenticado, vazio,
  carregando, offline, proibido e erro;
- inventário de atalhos, ordem de foco e comportamento de recolhimento;
- definição dos limites de largura e prioridade de cada região;
- taxonomia inicial de classificação, tipos de documento, convenção de tags e
  regras de compatibilidade para Markdown externo;
- decisão de produto para metadados de orientação à IA, incluindo se serão
  preservados apenas localmente ou consumidos por uma capacidade futura;
- decisão explícita sobre a central de notificações como rota na área central,
  mantendo o sino apenas como ponto de entrada e resumo.

Validação:

- walkthrough das jornadas de abrir nota, criar pasta, alternar módulo, consultar
  notificação e retornar ao documento sem perder o estado;
- aprovação humana registrada para a arquitetura de informação e os wireframes.

Evidências:

- pendente; referenciar protótipo aprovado, issue ou ata de decisão.

### P-02: Criar fundação visual e shell persistente

**Status:** in-progress  
**Responsável:** frontend  
**Dependências:** P-01

**Objetivo:** substituir o layout monolítico por regiões independentes, estáveis e
acessíveis.

Entregas:

- tokens de superfície, borda, texto, foco, seleção, dimensão e densidade;
- componentes de shell para barra superior, barra de atividades, painel primário,
  área central, painel contextual e barra de status;
- serviço de workspace com Signals para módulo ativo, visibilidade e dimensões;
- serviço de preferências que encapsule `localStorage`, schema, escopo, validação,
  migração e fallback quando o storage não estiver disponível;
- split views redimensionáveis por ponteiro e teclado, com limites responsivos;
- persistência versionada de tema, densidade, layout e filtros, separada por
  usuário e vault, com fallback seguro quando dados locais estiverem inválidos;
- temas claro, escuro e alto contraste sem cards decorativos ou elevação fora de
  overlays.

Validação:

- `ng test --watch=false` e `ng build --configuration production` em
  `frontend/web`;
- testes de componente para composição, foco, redimensionamento, recolhimento e
  restauração de preferências;
- testes do serviço com storage vazio, indisponível, corrompido, schema antigo e
  preferências pertencentes a outro usuário ou vault.

Evidências:

- Implementados `WorkspaceShell`, `WorkspaceService` e `PreferencesService` em
  `frontend/web/src/app/shell/`.
- O shell autenticado possui barra superior, barra de atividades, explorador,
  conteúdo central, painel contextual e barra de status, com temas `system`,
  `light`, `dark` e `high-contrast`.
- Os favicons e ícones de toque agora usam variantes oficiais claras e escuras,
  escolhidas pelo `prefers-color-scheme` e sincronizadas com o tema explícito do
  workspace.
- Preferências são versionadas por `sub` e vault, com allowlist, normalização de
  dimensões, fallback para defaults e tolerância a storage corrompido.
- O módulo ativo também é validado por allowlist, restaurado por usuário/vault e
  persistido junto ao layout; módulos desconhecidos são ignorados.
- Painéis primário e contextual usam separadores operáveis por ponteiro e
  teclado, com `aria-valuenow`, limites anunciados e persistência de largura.
- Suíte executada em `frontend/web` pelo terminal integrado: 3 arquivos e 10
  testes aprovados, incluindo sessão, realtime e composição da aplicação.
- Spec focado de preferências: 3 testes aprovados.
- Build de produção concluído; permanece aviso de orçamento do CSS do shell
  (`5.00 kB` contra `4.00 kB`).
- Pendências: execução confirmada do teste de separadores, capturas dos três
  temas e validação visual desktop/mobile. O asset
  inicial `frontend/web/public/help.md` foi versionado, mas ainda aguarda
  integração com um renderer Markdown seguro.

### P-03: Implementar barra de atividades e navegação modular

**Status:** in-progress  
**Responsável:** frontend  
**Dependências:** P-02

**Objetivo:** oferecer acesso previsível aos módulos sem acoplá-los ao componente
raiz.

Entregas:

- registro tipado de itens nativos com identificador, ícone, label, rota, ordem,
  permissão e painel associado;
- entradas iniciais para arquivos, busca, tags, grafo, notificações e configurações;
- entrada de ajuda com ícone, label acessível e atalho, disponível antes de o
  usuário abrir uma nota;
- estado ativo coerente com a rota e suporte a tooltip e label acessível;
- atalhos para alternar regiões e devolver foco ao conteúdo central;
- ocultação de itens indisponíveis conforme capacidade e autorização, sem inferir
  permissões apenas no cliente.

Validação:

- testes de unidade do registro, ordenação, autorização visual e sincronização com
  o roteador;
- teste Playwright de navegação por teclado entre todos os módulos disponíveis.
- teste Playwright para abrir a ajuda pelo menu e pelo atalho e voltar à aba
  anterior preservando foco e estado.

Evidências:

- Criado `ActivityRegistry` tipado com identificador, label, símbolo, ordem e
  disponibilidade controlada em build time.
- A barra de atividades do `WorkspaceShell` passou a renderizar somente itens
  disponíveis, ordenar pelo registro e derivar o estado ativo do workspace.
- A barra possui foco roving e navegação por teclado com setas, `Home` e `End`,
  mantendo o módulo ativo sincronizado com o foco.
- Rotas componentless `/workspace/:module` foram adicionadas; seleção, atalho de
  ajuda e abertura direta de uma URL sincronizam o módulo ativo do workspace.
- O registro de atividades declara também a rota e o painel associado (`primary`,
  `context` ou `central`) de cada módulo.
- Atalhos `Ctrl+1` a `Ctrl+6` alternam módulos disponíveis e devolvem o foco à
  área central; campos de edição não são interceptados.
- A atividade selecionada expõe `aria-current="page"`, complementando o foco
  roving e o label acessível da barra.
- Diagnóstico TypeScript limpo nos arquivos do registro e do shell.
- Pendente: execução confirmada do spec do registro e teste Playwright de ajuda
  por atalho/menu com preservação de foco.

### P-04: Evoluir o explorador de pastas e arquivos

**Status:** not-started  
**Responsável:** frontend e backend Notes/Vaults  
**Dependências:** P-02 e contratos de notas e pastas

**Objetivo:** transformar a lista atual em uma árvore adequada à organização de um
vault.

Entregas:

- árvore virtualizada e carregada sob demanda, com expansão persistida por vault;
- criação de nota e pasta no nó selecionado;
- rename, move, exclusão contextual e drag and drop com validação de destino;
- seleção simples, navegação por setas, menu de contexto e comandos equivalentes
  por teclado;
- indicadores de alteração, conflito, processamento e link não resolvido;
- indicação de nota sem classificação ou sem metadados mínimos, sem reescrever
  arquivos externos automaticamente;
- atualização externa incremental sem fechar nós ou deslocar foco sem necessidade;
- mensagens claras para raiz vazia, acesso proibido, offline e falha parcial.

Validação:

- testes de árvore para teclado, lazy loading, operações otimistas reversíveis e
  falhas de API;
- Playwright cobrindo criar pasta, criar nota, renomear, mover e reabrir a árvore;
- cenário de referência com 20 mil entradas mantendo interação fluida.

Evidências:

- pendente; registrar testes, medição e fixture utilizada.

### P-05: Integrar abas, editor e barra de status

**Status:** not-started  
**Responsável:** frontend  
**Dependências:** P-02 e P-04

**Objetivo:** preservar o trabalho do usuário ao navegar entre documentos e
módulos.

Entregas:

- abas com título, estado de alteração, conflito, fechamento e reabertura recente;
- restauração de seleção, scroll e contexto por aba;
- criação de nota com frontmatter `altivy` mínimo, valores default indexáveis e
  preservação de campos desconhecidos quando a nota já existir;
- estados de salvamento `clean`, `dirty`, `saving`, `saved`, `conflict` e `error`;
- barra de status com salvamento, conexão SignalR, indexação e contexto do editor;
- proteção contra fechamento com alteração não persistida e recuperação de
  rascunho conforme a estratégia existente;
- migração incremental dos fluxos atuais de abrir, criar, salvar e excluir nota.

Validação:

- testes de transição de estado, troca de abas e conflito de ETag;
- testes de criação, atualização e preservação de frontmatter, incluindo YAML
  inválido, campo desconhecido e arquivo externo sem metadados;
- Playwright cobrindo edição em duas abas, falha de rede e retorno sem perda de
  conteúdo.

Evidências:

- pendente; referenciar testes e cenário de conflito executado.

### P-06: Implementar barra superior e menu da conta

**Status:** not-started  
**Responsável:** frontend e BFF  
**Dependências:** P-02 e contrato de sessão existente

**Objetivo:** concentrar contexto da aplicação, acesso à conta e estado resumido
no topo sem misturar identidade com autorização local.

Entregas:

- título do vault e contexto atual com truncamento acessível;
- botão de notificações com label, estado não lido e anúncio não intrusivo;
- avatar com fallback por iniciais ou ícone neutro, nome de exibição e menu;
- ação **Gerenciar conta** que redireciona ao Keycloak Account Console para
  alterações permitidas de perfil, e-mail, `username`, credenciais, sessões e
  MFA, sem implementar esses fluxos no Altivy;
- retorno seguro da gestão de conta para rota same-origin previamente configurada,
  seguido de recarga da sessão para refletir claims atualizadas;
- ações de sessão suportadas pelo BFF, incluindo logout e retorno de erro;
- navegação por teclado, gerenciamento de foco e fechamento por `Escape` nos
  overlays.

Validação:

- testes para sessão carregando, autenticada, expirada e indisponível;
- testes para URL ausente ou inválida do Account Console, redirecionamento seguro
  e atualização de sessão após o retorno;
- auditoria de contraste, nome acessível e operação completa sem mouse.

Evidências:

- pendente; registrar testes e relatório de acessibilidade.

### P-07: Entregar central de notificações operacionais

**Status:** not-started  
**Responsável:** frontend, backend Operations e contratos  
**Dependências:** P-03, P-06 e contrato OpenAPI/SignalR aprovado

**Objetivo:** fornecer histórico confiável e acionável de eventos relevantes ao
usuário, incluindo eventos futuros emitidos por plugins permitidos.

Entregas:

- rota lazy loaded renderizada na área central, aberta pelo sino sem destruir abas
  de documentos;
- lista virtualizada e paginada por cursor, agrupada por período;
- filtros por leitura, origem, categoria e severidade;
- ações para marcar uma, várias ou todas como lidas, com atualização idempotente;
- estados de vazio, carregamento, erro, offline, reconexão e evento removido;
- badge limitado visualmente, com texto acessível para contagens altas;
- adapter entre contrato gerado e modelo de tela;
- atualização SignalR com deduplicação por identificador e reconciliação pela API;
- origem identificável para núcleo, indexação, sincronização e plugin;
- política que proíba conteúdo integral de notas, credenciais, payloads técnicos e
  informações não autorizadas nas mensagens.

Validação:

- testes de paginação, filtros, deduplicação, leitura idempotente e reconexão;
- testes de contrato garantindo isolamento por usuário e permissão;
- Playwright cobrindo recebimento em tempo real, abertura pelo sino, marcação como
  lida e retorno à aba anterior;
- revisão de segurança sobre conteúdo, ações e isolamento das notificações.

Evidências:

- pendente; referenciar contrato, testes e revisão de segurança.

### P-08: Criar painel contextual e slots extensíveis

**Status:** not-started  
**Responsável:** frontend e arquitetura  
**Dependências:** P-02, P-03 e decisões da plataforma de plugins

**Objetivo:** reservar uma região útil para contexto do documento e integrações
futuras sem permitir extensão arbitrária do browser.

Entregas:

- registro tipado em build time para painéis com identificador, título, ícone,
  ordem, contexto aceito, capacidade exigida e componente lazy loaded;
- tabs ou seletor compacto para outline, propriedades, backlinks, tags e grafo
  local quando as features estiverem disponíveis;
- editor de Propriedades para classificação, tipo, tags, audiência, palavras-chave
  e data de revisão, com validação e salvamento pela infraestrutura de notas;
- seção avançada **Orientação para IA** para visualizar, editar ou remover
  `altivy.agent`, sempre opcional e sem ocultar o conteúdo do usuário;
- estados de indisponível, não configurado, sem permissão, carregando e degradado;
- descarte e cancelamento de assinaturas ao trocar documento ou fechar painel;
- isolamento de falha para que um painel opcional não derrube editor ou shell;
- slot reservado para calendário, exibido somente quando um plugin confiável e
  uma conexão autorizada estiverem habilitados;
- persistência da ferramenta selecionada por contexto, sem persistir conteúdo do
  provider no browser além do necessário.

Validação:

- testes do registro, lazy loading, troca de contexto, autorização e isolamento de
  erro;
- testes de propriedades para valores obrigatórios default, taxonomia, validação
  YAML, preservação de campos desconhecidos e visibilidade dos hints de IA;
- Playwright cobrindo abrir, redimensionar, trocar e fechar painéis sem perder foco
  ou conteúdo da nota;
- teste arquitetural impedindo dependência direta entre features e implementações
  concretas de plugins.

Evidências:

- pendente; registrar testes e decisão arquitetural relacionada.

### P-09: Adaptar responsividade, acessibilidade e preferências

**Status:** in-progress  
**Responsável:** frontend e qualidade  
**Dependências:** P-03 a P-08

**Objetivo:** manter os fluxos principais utilizáveis em desktop, tablet e celular
sem sobreposição ou perda de contexto.

Entregas:

- desktop com painéis simultâneos conforme espaço disponível;
- tablet com apenas um painel lateral aberto e área central preservada;
- celular com barras laterais em overlays mutuamente exclusivos e foco contido;
- seletor de tema entre sistema, claro, escuro e alto contraste;
- preferência de densidade compacta ou confortável;
- persistência dos filtros e ordenações configurados pelo usuário em cada feature,
  com comando local para limpar o filtro atual;
- atalhos remapeáveis e comando para restaurar layout;
- ação para restaurar todas as preferências locais do usuário no dispositivo;
- suporte a zoom de 200%, redução de movimento, alto contraste e leitores de tela;
- anúncios controlados para salvamento, notificações e mudanças de conexão, sem
  ruído excessivo.

Validação:

- Playwright em viewports desktop, tablet e celular, incluindo screenshots de
  regressão das regiões críticas;
- axe ou verificação equivalente aprovada para WCAG 2.2 AA;
- teste manual de teclado, zoom de 200% e leitor de tela em jornadas prioritárias.
- teste de fechar e reabrir a aplicação confirmando tema, layout e filtros, sem
  reaproveitar as preferências ao alternar para outro usuário ou vault.

Evidências:

- Tela de configurações integrada ao módulo `settings`, com seleção de tema,
  densidade e restauração explícita das preferências locais.
- Shell possui comportamento responsivo para transformar painéis laterais em
  overlays em telas estreitas e respeita `prefers-reduced-motion`.
- Pendentes: screenshots desktop/tablet/mobile, auditoria WCAG/axe, foco contido
  nos overlays, zoom de 200% e validação manual com leitor de tela.

### P-10: Consolidar desempenho, observabilidade e rollout

**Status:** not-started  
**Responsável:** frontend, qualidade e operações  
**Dependências:** P-04 a P-09

**Objetivo:** liberar o novo workspace com métricas, limites e recuperação
compatíveis com uso prolongado.

Entregas:

- lazy loading efetivo das features e painéis opcionais;
- orçamento de bundle e medição de interação do shell, árvore e notificações;
- telemetria sem conteúdo de notas ou dados pessoais desnecessários;
- indicadores de falha por região para diagnóstico sem bloquear o workspace;
- migração versionada das preferências antigas e opção de restaurar defaults;
- flag de rollout para alternar entre o layout atual e o novo durante validação;
- remoção do layout anterior somente após evidência de estabilidade e aprovação.

Validação:

- `npm test -- --watch=false` e `npm run build` em `frontend/web`;
- suíte Playwright dos fluxos principais em Chromium, Firefox e WebKit;
- shell interativo em até 2,5 segundos após cache frio em rede local de referência;
- troca entre notas abertas em até 100 ms e nenhuma tarefa acima de 50 ms durante
  digitação comum;
- ausência de erros de console, vazamentos de subscriptions e regressões críticas
  de layout nos viewports suportados.

Evidências:

- pendente; registrar relatórios de testes, bundle, desempenho e rollout.

### P-11: Entregar ajuda integrada somente leitura

**Status:** in-progress  
**Responsável:** frontend, produto e documentação  
**Dependências:** P-03, P-05, P-06, P-08 e P-09

**Objetivo:** tornar as principais capacidades do workspace compreensíveis sem
converter a documentação de uso em parte editável do vault.

Entregas:

- arquivo Markdown de ajuda como asset versionado, com estrutura de headings,
  sumário, links internos e conteúdo em português brasileiro;
- rota ou feature lazy loaded para renderizar o Help com a política única de
  sanitização e sem controles de edição;
- acesso pelo item de ajuda na barra de atividades, menu da conta e atalho
  configurável, com retorno à aba ou rota anterior;
- busca local no corpus da ajuda, com destaque e navegação por heading;
- tutoriais de perfil e MFA que encaminham ao Keycloak Account Console, sem
  instruir o usuário a manipular tokens ou configurar autenticação no Altivy;
- revisão editorial a cada alteração de fluxo descrito e indicação de versão da
  ajuda compatível com a release.

Validação:

- testes de componente para modo somente leitura, sanitização, navegação por
  heading, busca e recuperação da rota anterior;
- Playwright para abrir o Help pelo teclado e pelo menu, verificar ausência de
  edição e confirmar que o conteúdo não aparece no explorador ou na busca do
  vault;
- revisão de acessibilidade para headings, links, foco, contraste e leitor de
  tela;
- build de produção verificando que o asset é empacotado e acessível offline após
  o carregamento inicial da aplicação.

Evidências:

- Módulo `help` disponível na barra de atividades e renderizado como conteúdo
  somente leitura na área central, sem aparecer no explorador de notas.
- Fonte editorial inicial versionada em `frontend/web/public/help.md`, incluída
  como asset estático do build.
- Criado parser restrito em `frontend/web/src/app/shell/help-markdown.ts` para
  estruturar headings, listas e parágrafos sem gerar HTML; scripts e HTML são
  tratados como texto.
- Criado `HelpService` para carregar `public/help.md` sob demanda, com cache em
  memória, estados de carregamento/erro e nova tentativa após falha; o shell
  inicia esse carregamento quando o módulo Ajuda é selecionado.
- Criado `HelpPanel` com template próprio, que usa o parser restrito para
  renderizar headings, parágrafos e listas por interpolação Angular, mantendo a
  ajuda somente leitura e sem `innerHTML`.
- O `HelpPanel` oferece busca local por termo, limitada aos blocos do `help.md`
  carregado em memória, com estado explícito para resultado vazio.
- Ação explícita **Fechar ajuda** retorna ao último módulo não-Ajuda e devolve o
  foco à área central, sem alterar a nota selecionada ou seu conteúdo.
- O `WorkspaceShell` passou a usar o template separado `workspace-shell-v2.html`
  e renderiza o `HelpPanel` no módulo de ajuda; o asset `public/help.md` é
  carregado sob demanda pelo serviço local.
- O template antigo `workspace-shell.html` foi removido para evitar duas fontes
  de verdade e manter a composição do shell em um único arquivo.
- Spec do parser cobre estrutura Markdown e conteúdo potencialmente executável;
  diagnóstico TypeScript/template limpo. Pendente cobrir a jornada no browser,
  busca local, links internos e retorno de foco/rota.
- Conteúdo inicial orienta abertura de notas, preservação de contexto e limites
  de conta/segurança, sem instruir manipulação de tokens.
- Pendentes: mover o conteúdo para asset Markdown versionado, busca local,
  retorno de foco/rota, links internos, revisão editorial e testes Playwright.

## Critérios de aceite finais

- a barra de atividades alterna módulos e informa o item ativo por visão, texto
  acessível e foco;
- o usuário cria pastas e notas, reorganiza a árvore e executa os comandos
  essenciais sem mouse;
- cada nota criada pelo Altivy contém frontmatter `altivy` mínimo com versão,
  classificação, tipo e ao menos uma tag, sem impedir criação rápida;
- notas externas sem frontmatter continuam indexáveis e recebem indicação para
  classificação, sem alteração silenciosa do arquivo;
- o painel Propriedades permite consultar e editar todo metadado `altivy`,
  inclusive orientação opcional para IA, que permanece ausente do preview mas
  nunca inacessível ao usuário;
- painéis esquerdo e direito são redimensionáveis, recolhíveis e restaurados sem
  comprometer a área central;
- conta e notificações permanecem acessíveis no canto superior direito em desktop;
- o menu de conta encaminha a gestão de perfil e MFA ao Keycloak Account Console,
  e a sessão é atualizada após o retorno sem alterar as preferências vinculadas ao
  mesmo `sub`;
- a ajuda integrada abre por menu e teclado, apresenta tutoriais atualizados e
  retorna ao contexto anterior sem permitir edição ou alterar o vault;
- o sino representa corretamente o estado não lido e abre a central de
  notificações;
- a central reconstrói seu histórico pela API, recebe atualizações SignalR sem
  duplicação e não expõe conteúdo ou dados sensíveis;
- eventos de plugins identificam sua origem e respeitam permissões, mesmo quando a
  integração estiver degradada ou for desabilitada;
- o painel contextual hospeda features nativas e slots permitidos sem importar
  implementações concretas entre features;
- trocar módulo, abrir notificação ou fechar painel não descarta texto não salvo,
  seleção, scroll ou abas;
- desktop, tablet e celular não apresentam sobreposição incoerente, texto cortado
  ou controles inacessíveis;
- os temas claro, escuro e alto contraste atendem contraste AA;
- tema, densidade, layout, filtros e ordenações não sensíveis sobrevivem ao
  fechamento do browser e permanecem isolados por usuário e vault;
- storage ausente, bloqueado, corrompido ou de versão incompatível não impede a
  abertura da aplicação e resulta em defaults seguros;
- nenhuma credencial, conteúdo de nota, payload operacional ou decisão de
  autorização é gravada junto às preferências;
- testes unitários, build, Playwright, acessibilidade e metas de desempenho do
  recorte estão verdes e possuem evidências registradas.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| excesso de regiões reduzir a área de edição | editor estreito e fadiga | limites mínimos, recolhimento rápido, persistência e comportamento responsivo |
| inspiração virar cópia visual do Obsidian | perda de identidade e risco jurídico | usar apenas o modelo espacial e aplicar tokens, marca e interações próprias |
| central virar espelho de logs técnicos | vazamento, ruído e baixa utilidade | taxonomia de eventos, allowlist de campos, retenção e revisão de segurança |
| eventos SignalR perdidos ou duplicados | badge e histórico inconsistentes | API como fonte reconstruível, IDs estáveis, cursor e deduplicação |
| plugin opcional quebrar o shell | perda do workspace principal | registro controlado, lazy loading, error boundary e estado degradado isolado |
| estado de layout crescer sem dono | bugs de restauração e acoplamento | serviço de workspace versionado com Signals e contratos pequenos por feature |
| preferência local corrompida ou manipulada | falha de inicialização ou filtro inválido | schema versionado, allowlist, validação, normalização e fallback para defaults |
| preferências vazarem entre usuários no mesmo browser | exposição de hábitos e interface incorreta | chave por escopo opaco de usuário e vault, sem nome, e-mail ou token |
| `localStorage` indisponível ou sem espaço | preferências não restauradas | captura de erro, operação em memória e restauração dos defaults sem bloquear o shell |
| Altivy duplicar fluxos de perfil ou MFA | inconsistência de identidade e superfície de segurança indevida | redirecionar somente ao Keycloak Account Console e manter política no realm |
| perfil alterado no Keycloak não aparecer na interface | contexto de conta desatualizado | retorno same-origin configurado e recarga da sessão pelo BFF |
| metadados obrigatórios criarem atrito para notas rápidas | abandono ou conteúdo incompleto | defaults indexáveis, edição posterior e validação progressiva |
| frontmatter ser corrompido ao editar propriedades | perda ou alteração indevida de Markdown | parser YAML seguro, ETag, teste de preservação e nenhuma reordenação silenciosa |
| classificação ser confundida com controle de acesso | exposição indevida por falsa sensação de proteção | Keycloak e API permanecem a única decisão de autorização |
| hints de IA ficarem obsoletos ou induzirem respostas incorretas | baixa qualidade e risco operacional | campos opcionais, edição visível, data de revisão e conteúdo da nota como autoridade |
| ajuda divergir do comportamento entregue | orientação incorreta e perda de confiança | manter Help versionado e atualizar no mesmo change set de mudanças de fluxo |
| ajuda ser confundida com uma nota do usuário | edição ou indexação indevida | asset somente leitura fora do vault, rota própria e exclusão explícita de CRUD e busca |
| árvore grande degradar interação | navegação lenta em vaults reais | lazy loading, virtualização, atualização incremental e benchmark com fixture |
| atalhos conflitarem com navegador ou editor | comandos imprevisíveis | mapa central, escopo de contexto, remapeamento e testes nos navegadores-alvo |
| retenção de notificações crescer sem limite | custo e lentidão | paginação por cursor, política de retenção e índices definidos com Operations |

## Rollout e rollback

O rollout será incremental por etapas e protegido por configuração de runtime. O
novo shell entra primeiro para um grupo de validação, mantendo o layout anterior
disponível enquanto árvore, editor e notificações estabilizam. Métricas de erro,
tempo de interação, falhas de SignalR, abandono de ações e feedback de
acessibilidade orientam a ampliação.

O rollout deve validar a migração de schema das preferências em perfis existentes
e a inicialização com storage bloqueado ou corrompido. O rollback reativa o layout
anterior e ignora chaves de versão mais nova sem apagá-las automaticamente.

O rollout deve ser interrompido se houver perda de conteúdo, quebra de
autorização, regressão relevante de teclado, erro recorrente do shell ou metas de
desempenho não atendidas. O rollback reativa o layout anterior e ignora com
segurança preferências de layout de versão mais nova; nenhuma reversão pode
descartar notas, rascunhos ou estado persistido no servidor. A remoção da flag e
do layout anterior exige aprovação humana e evidência dos critérios finais.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-09-01 | plano | - | draft | criação a partir da visão de workspace solicitada | IA assistida |
| 2026-09-02 | plano | approved | in-progress | implementação do P-02 iniciada após aprovação humana registrada | IA assistida |
| 2026-09-02 | P-02 | not-started | in-progress | shell, estado Signals, preferências locais, temas e testes iniciais implementados; pendem validações visuais e redimensionamento completo | IA assistida |
| 2026-09-02 | P-03 | not-started | in-progress | registro tipado de atividades integrado ao shell; validação automatizada e roteamento ainda pendentes | IA assistida |
| 2026-09-02 | P-09 | not-started | in-progress | configurações de tema/densidade/restauração e responsividade inicial integradas; auditoria de acessibilidade pendente | IA assistida |
| 2026-09-02 | P-11 | not-started | in-progress | ajuda somente leitura inicial integrada ao módulo próprio; asset Markdown, busca e retorno de foco pendentes | IA assistida |
| 2026-09-02 | P-03 | in-progress | in-progress | navegação por teclado com foco roving e asset de ajuda inicial adicionados; validação final do runner pendente | IA assistida |
| 2026-09-02 | P-02 | in-progress | in-progress | variantes oficiais de ícones para tema claro/escuro integradas ao índice e sincronizadas pelo shell | IA assistida |
| 2026-09-03 | P-02 | in-progress | in-progress | separadores dos painéis implementados para ponteiro e teclado, com teste de componente e build de produção aprovados; resumo do teste focado pendente | IA assistida |
| 2026-09-03 | P-03 | in-progress | in-progress | atividade ativa passou a expor aria-current para leitores de tela; testes e Playwright ainda pendentes | IA assistida |
| 2026-09-02 | P-03 | in-progress | in-progress | rotas `/workspace/:module` e sincronização inicial do módulo ativo adicionadas; Playwright ainda pendente | IA assistida |
| 2026-09-02 | P-03 | in-progress | in-progress | registro ampliado com rota/painel e atalhos Ctrl+1..6 com foco central; execução final do runner ainda pendente | IA assistida |
| 2026-09-02 | P-11 | in-progress | in-progress | HelpService carrega `public/help.md` sob demanda com retry e cache; renderer e Playwright ainda pendentes | IA assistida |
| 2026-09-02 | P-11 | in-progress | in-progress | HelpPanel integrado ao template do shell com parser seguro; busca, retorno de foco e Playwright ainda pendentes | IA assistida |
| 2026-09-02 | P-11 | in-progress | in-progress | template antigo removido e shell passou a ter composição única com HelpPanel; validação executável final ainda pendente | IA assistida |
| 2026-09-02 | P-11 | in-progress | in-progress | busca local restrita ao corpus da ajuda adicionada; validação de componente e jornada Playwright ainda pendentes | IA assistida |
| 2026-09-03 | P-11 | in-progress | in-progress | fechamento da ajuda retorna ao módulo anterior e preserva contexto; teste unitário criado, execução e Playwright pendentes | IA assistida |

## Regras de atualização

- O status geral usa `draft`, `proposed`, `approved`, `in-progress`, `blocked`,
  `completed`, `cancelled` ou `superseded`.
- IA pode criar e atualizar o plano, mas não define status acima de `proposed`
  sem aprovação humana explícita.
- `approved` registra responsável e referência da aprovação humana.
- `completed` exige todas as etapas obrigatórias em `completed` ou `skipped`,
  critérios finais atendidos e evidências registradas.
- `skipped` exige justificativa e aprovação humana; bloqueio registra causa e
  condição de desbloqueio.
- Toda alteração de status geral ou de etapa adiciona uma entrada no histórico.
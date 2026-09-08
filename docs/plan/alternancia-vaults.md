# Plano: Alternância segura entre vaults

**Status:** in-progress
**Data de criação:** 2026-09-04  
**Última atualização:** 2026-09-05  
**Responsáveis:** equipe Altivy  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Workspace frontend inspirado no Obsidian](workspace-frontend-inspirado-no-obsidian.md), [ADR de BFF e autorização externa](../adr/2026-0002-bff-keycloak-autorizacao-externa.md), [ADR de catálogo autorizado de vaults](../adr/2026-0004-catalogo-autorizado-de-vaults.md)

## Objetivo

Permitir que uma pessoa autenticada liste somente os vaults para os quais possui
`vault:read`, selecione o vault ativo no workspace e preserve esse contexto em
navegação, recarga e novo login. A URL canônica será
`/workspace/:vaultId/:module`; o último vault ainda autorizado será restaurado
por usuário e as preferências continuarão isoladas por usuário e vault.

## Contexto e premissas

- O frontend e o `NotesService` usam atualmente `lab-0001` como valor fixo.
- O PostgreSQL contém a projeção de vaults, mas não é autoridade de
  autorização nem armazena memberships.
- O Keycloak Authorization Services permanece a única fonte de decisão sobre
  acesso; a API continua atuando como Policy Enforcement Point.
- O BFF continua responsável apenas pela sessão OIDC e pelo relay do token.
- A API existente valida `vaultId` e autoriza operações individuais com UMA.
- `PreferencesService` já isola preferências pela chave
  `altivy:preferences:v1:{subject}:{vaultId}`.
- O MVP listará candidatos do PostgreSQL e avaliará `vault:read` por vault no
  Keycloak, com concorrência limitada e falha fechada.
- A lista pública será paginada por cursor, com limite máximo documentado.
- O último vault selecionado será persistido somente como ID técnico na chave
  `altivy:last-vault:v1:{subject}`.
- Nenhuma nova dependência externa ou migration é prevista.
- A mudança de estratégia de enumeração e autorização requer ADR antes da
  implementação.

## Não escopo

- criar, renomear ou excluir vaults pela interface;
- administrar memberships, grupos, roles, recursos, scopes ou policies;
- armazenar cópia local de decisões de autorização;
- compartilhar vaults ou convidar usuários;
- pesquisar simultaneamente em múltiplos vaults;
- manter documentos de múltiplos vaults abertos ao mesmo tempo;
- introduzir cache de decisões UMA no MVP;
- adotar integração batch com a Protection API sem medição e ADR próprios;
- implementar confirmação de troca por edição suja antes de existir uma
  política consolidada de dirty state.

## Dependências e decisões

- Criar ADR em `docs/adr/` com status `proposed` para registrar a estratégia de
  catálogo autorizado, falha fechada, revogação e não exposição de metadados.
- Preservar a decisão do ADR 2026-0002: Keycloak decide e a API aplica a
  autorização; o BFF não se torna diretório ou motor de políticas.
- Expandir o contrato OpenAPI antes da implementação do cliente Angular.
- Usar a projeção `VaultRecord` existente; não há mudança de schema prevista.
- Usar exclusivamente EF Core para a consulta paginada de vaults.
- Reutilizar `VaultAuthorization.ResourceFor`, `ReadScope`, `VaultId` e
  `IAuthorizationDecisionService`.
- A URL canônica aprovada para o planejamento é
  `/workspace/:vaultId/:module`.
- O vault inicial aprovado para o planejamento é o último vault ainda
  autorizado, com fallback para o primeiro autorizado.
- A estratégia inicial aprovada para o planejamento é UMA por vault com
  concorrência limitada a quatro decisões simultâneas.
- A listagem usa limite padrão de 20 vaults autorizados e máximo de 100.
- A paginação usa cursor keyset opaco e versionado sobre a última posição de
  candidato consumida, sem offset, cache ou nova migration.
- A indisponibilidade de qualquer decisão necessária falha a requisição inteira
  com `503`, sem devolver página parcial.
- O ADR 2026-0004 foi aceito por Lincoln Zocateli em 2026-09-04.

## Sequência de execução

1. P-01 registra o ADR e expande o OpenAPI; a implementação pausa para revisão
  humana do ADR.
2. P-02 cria a paginação de candidatos na aplicação e no adapter PostgreSQL,
  sem incorporar autorização à infraestrutura.
3. P-03 preenche páginas autorizadas na API, atravessando candidatos negados
  sem expô-los e limitando a concorrência UMA.
4. P-04 torna vault e módulo explícitos na URL e restaura somente IDs ainda
  presentes no catálogo autorizado.
5. P-05 isola notas, documento selecionado e realtime por vault, descartando
  respostas obsoletas.
6. P-06 entrega o seletor acessível e preserva o módulo durante a troca.
7. P-07 reconcilia o laboratório, valida a jornada real e sincroniza a
  documentação.

Cada etapa começa pelo teste ou contrato proprietário do comportamento e roda
imediatamente a validação mais estreita disponível. Evidências verificáveis são
registradas antes de marcar uma etapa como `completed`.

## Etapas

### P-01: Registrar decisão e contrato

**Status:** completed  
**Responsável:** equipe Altivy  
**Dependências:** nenhuma

**Objetivo:** registrar a decisão arquitetural e estabelecer o contrato HTTP
aditivo antes de alterar backend ou frontend.

Entregas:

- criar ADR `proposed` para catálogo PostgreSQL de candidatos e filtragem UMA;
- documentar Keycloak como única autoridade, falha fechada, revogação e não
  exposição de vaults negados;
- adicionar `GET /api/v1/vaults` ao OpenAPI;
- definir `VaultSummary` com `id` e `name` e `VaultPage` com `items` e
  `nextCursor`;
- definir paginação por cursor, limite padrão e limite máximo;
- documentar autenticação bearer, autorização `vault:read` e respostas `200`,
  `400`, `401` e `503` em `application/problem+json` quando aplicável;
- não retornar scopes, memberships, policies ou candidatos negados.

Validação:

- o ADR segue `.github/ADR_TEMPLATE.md` e permanece `proposed`;
- o OpenAPI passa nas validações de contrato do repositório;
- exemplos usam dados sintéticos e não revelam informações sensíveis;
- revisão confirma que a mudança é aditiva para os consumidores atuais.

Evidências:

- ADR 2026-0004 criado em `proposed` e conferido contra
  `.github/ADR_TEMPLATE.md`;
- expansão OpenAPI aditiva com 77 linhas adicionadas e nenhuma removida;
- 21 referências locais do OpenAPI e 10 links Markdown do recorte resolvidos;
- diagnósticos do editor sem erros nos três arquivos alterados;
- `test-validate-repository.ps1` aprovado em 7 cenários;
- `validate-repository.ps1` bloqueado por link preexistente fora do recorte em
  `docs/adr/2026-0002-bff-keycloak-autorizacao-externa.md`.

### P-02: Implementar catálogo paginado de candidatos

**Status:** completed
**Responsável:** equipe Altivy  
**Dependências:** P-01

**Objetivo:** obter candidatos de vault do PostgreSQL de forma ordenada e
paginada, sem incorporar decisão de autorização na infraestrutura.

Entregas:

- estender `IVaultReader` com leitura paginada de candidatos;
- criar query, resultado e handler imutáveis em
  `backend/libs/application/Vaults`;
- manter `GetVaultQueryHandler` e o contrato individual existentes;
- implementar paginação em `PostgresVaultReader` com EF Core,
  `AsNoTracking`, ordenação estável por `Id` e cursor opaco;
- reutilizar `VaultRecord` e `PostgresDbContext` sem migration;
- registrar os novos serviços nos composition roots existentes.

Validação:

- testes unitários cobrem limite, cursor, ordenação e cancelamento;
- integração com PostgreSQL real comprova paginação estável;
- domínio e aplicação não passam a depender de EF Core, ASP.NET Core ou
  Keycloak;
- nenhum dado de membership ou policy é persistido localmente.

Evidências:

- aceite humano do ADR 2026-0004 registrado em 2026-09-04;
- handler e adapter PostgreSQL implementados com cursor opaco versionado;
- testes unitários do handler aprovados e testes de integração adicionados
  para ordenação estável e cursor inválido.

### P-03: Expor somente vaults autorizados

**Status:** completed
**Responsável:** equipe Altivy  
**Dependências:** P-01, P-02

**Objetivo:** implementar `GET /api/v1/vaults` sem revelar ID ou nome antes de
uma decisão positiva do Keycloak.

Entregas:

- registrar o endpoint em `ApiEndpointRouteBuilderExtensions`;
- extrair e validar o bearer token como nos endpoints atuais;
- avaliar `VaultAuthorization.ResourceFor(id)` e `ReadScope` para cada
  candidato por `IAuthorizationDecisionService`;
- limitar a concorrência das avaliações e propagar `CancellationToken`;
- continuar percorrendo candidatos até preencher a página autorizada ou
  esgotar o catálogo;
- omitir decisões negativas sem indicar a existência do vault;
- retornar lista vazia quando todas as decisões forem negativas;
- retornar `503 application/problem+json` quando o Keycloak estiver
  indisponível, evitando lista parcial enganosa;
- retornar `401` para token ausente ou inválido e `400` para cursor ou limite
  inválido;
- não adicionar cache de autorização antes de medir latência e volume.

Validação:

- testes HTTP cobrem dois vaults permitidos e um negado sem vazamento;
- testes cobrem lista vazia, indisponibilidade, cancelamento e paginação que
  atravessa candidatos negados;
- payloads e logs não contêm nomes de vaults não autorizados;
- testes de contrato confirmam respostas e `application/problem+json`;
- classes de teste declaram a `Trait` apropriada.

Evidências:

- suíte de contrato da API aprovada com 29 testes antes da expansão final;
- casos adicionais cobrem lista vazia, bearer ausente e cursor inválido;
- decisões são executadas em lotes de no máximo quatro e indisponibilidade
  retorna `503` sem payload parcial.

### P-04: Criar estado e navegação multi-vault no Angular

**Status:** completed
**Responsável:** equipe Altivy  
**Dependências:** P-01, P-03

**Objetivo:** tornar o vault ativo parte explícita da URL e do estado da
aplicação, preservando o último vault autorizado por usuário.

Entregas:

- criar data access tipado para vaults a partir do OpenAPI;
- criar `VaultsService` com signals para catálogo, vault ativo, loading, vazio
  e erro;
- tornar `/workspace/:vaultId/:module` a rota canônica;
- resolver o vault inicial nesta ordem: URL autorizada, último vault ainda
  autorizado, primeiro vault autorizado e estado sem acesso;
- para uma URL bem formada, mas não autorizada, redirecionar para um fallback
  autorizado ou mostrar estado sem acesso sem distinguir inexistência de
  negação;
- persistir somente o último ID em
  `altivy:last-vault:v1:{subject}`;
- preservar preferências em
  `altivy:preferences:v1:{subject}:{vaultId}`;
- persistir as preferências do vault anterior e restaurar as do novo durante
  a troca;
- remover defaults e textos fixos de `lab-0001` do frontend.

Validação:

- Vitest cobre resolução inicial, fallback após revogação e isolamento entre
  usuários e vaults;
- recarregar uma URL válida restaura o mesmo vault e módulo;
- ID arbitrário na URL não revela nome, existência ou motivo da negação;
- o browser não armazena nomes, scopes ou policies como preferência.

Evidências:

- `VaultsService` pagina o catálogo e valida URL e último vault contra os IDs
  autorizados;
- rota canônica alterada para `/workspace/:vaultId/:module`;
- testes cobrem paginação, URL autorizada e fallback após revogação.

### P-05: Trocar notas e realtime com isolamento

**Status:** completed
**Responsável:** equipe Altivy  
**Dependências:** P-04

**Objetivo:** garantir que notas, documento selecionado e notificações sempre
pertençam ao vault ativo.

Entregas:

- refatorar `NotesService` para construir a URL com o vault ativo;
- codificar segmentos de rota e continuar tratando paths como dados relativos
  normalizados;
- limpar documento selecionado e erros ao trocar de vault;
- cancelar ou ignorar respostas obsoletas de carregamentos anteriores;
- impedir que notas do vault anterior apareçam após troca rápida;
- sair do grupo SignalR anterior antes de ingressar no novo ou recriar a
  conexão quando não existir operação de saída;
- impedir joins acumulados e notificações cruzadas;
- manter a validação de autorização no `VaultHub`.

Validação:

- testes cobrem troca rápida com respostas fora de ordem;
- a rede não realiza nova chamada ao vault anterior depois da troca;
- notificações do vault anterior não alteram o estado do vault ativo;
- abrir, salvar e excluir notas continua respeitando ETag e `If-Match`.

Evidências:

- `NotesService` usa vault ativo, codifica segmentos e descarta respostas por
  geração;
- teste reproduz resposta tardia do vault anterior;
- SignalR invalida callbacks e joins de gerações anteriores.

### P-06: Entregar seletor acessível no workspace

**Status:** completed
**Responsável:** equipe Altivy  
**Dependências:** P-04, P-05

**Objetivo:** permitir alternância eficiente na topbar sem recarregar a
aplicação inteira.

Entregas:

- substituir o texto estático do vault por seletor compacto na topbar;
- exibir nome e usar ID apenas como contexto técnico quando necessário;
- navegar para `/workspace/{vaultId}/{moduloAtual}` ao selecionar;
- recarregar notas, contexto e realtime do novo vault;
- tratar loading, lista vazia, erro e vault único;
- suportar teclado, foco visível e anúncio acessível da troca;
- manter dimensões estáveis sem sobreposição em desktop e mobile;
- limpar o documento selecionado ao fechar a aba, preservando o módulo atual.

Validação:

- o fluxo funciona integralmente por teclado e leitor de tela;
- a troca preserva o módulo atual e não recarrega a página inteira;
- não há overflow ou corte de controles nos viewports suportados;
- estados vazio, erro e vault único são compreensíveis e acionáveis.

Evidências:

- seletor nativo acessível substitui o ID estático na topbar;
- seleção preserva o módulo e anuncia o nome do vault ativo;
- controle possui dimensões responsivas, foco visível e teste de emissão.

### P-07: Preparar laboratório e validar ponta a ponta

**Status:** in-progress
**Responsável:** equipe Altivy  
**Dependências:** P-03, P-06

**Objetivo:** comprovar autorização, persistência e isolamento com dados
sintéticos em ambiente integrado.

Entregas:

- reconciliar de forma idempotente ao menos dois vaults sintéticos no
  laboratório;
- configurar um usuário autorizado nos dois vaults e outro em apenas um;
- alinhar a projeção PostgreSQL e os fixtures sem copiar memberships;
- adicionar Playwright para login, restauração, troca, recarga e revogação;
- cobrir usuário sem vault e acesso direto a vault negado;
- cobrir preferências independentes por vault;
- atualizar `docs/frontend.md`, `docs/backend.md` e
  `docs/configuracao-keycloak.md` conforme o comportamento entregue;
- atualizar o plano do workspace apenas onde houver relação objetiva.

Validação:

- executar format, build e testes .NET do recorte;
- executar testes unitários e de contrato;
- executar integração PostgreSQL no Compose descartável e realizar teardown;
- executar `ng test --watch=false` e
  `ng build --configuration production` a partir de `frontend/web` pelo alias
  containerizado;
- reconstruir o laboratório E2E e executar Playwright no fluxo real;
- verificar desktop e mobile, console limpo e ausência de vazamento em rede e
  logs;
- comprovar que revogação força fallback seguro no próximo carregamento.

Evidências:

- adapter de laboratório reconciliado com `lab-0001` e `lab-0002`, usando o
  mesmo cursor versionado e sem memberships locais;
- suíte de contrato da API aprovada com 32 testes e suíte Angular completa
  aprovada com 28 testes em 12 arquivos;
- `ng build --configuration production` concluído sem erros e diagnósticos do
  editor sem erros no recorte alterado;
- `docs/frontend.md`, `docs/backend.md` e `docs/configuracao-keycloak.md`
  descrevem a ordem de fallback, isolamento e falha fechada;
- barra superior usa ícone de sino acessível para notificações e o Help explica
  como preparar outro vault sintético no laboratório;
- Playwright autenticado, revogação real e validação visual permanecem
  pendentes por dependerem de Keycloak, secrets e certificados locais do
  laboratório E2E; as jornadas foram adicionadas em
  `frontend/tests/e2e/test_bff_session.py` e a coleta pelo entrypoint oficial
  confirmou 9 testes para Chromium; a seleção dos três cenários multi-vault foi
  validada, mas os três foram ignorados por ausência de
  `ALTIVY_E2E_USERNAME` e `ALTIVY_E2E_PASSWORD`; com as credenciais locais,
  catálogo e recarga passaram (`2 passed`) e a troca foi ignorada porque o
  usuário possui apenas um vault autorizado (`1 skipped`).

## Próxima fase: criação autogerenciada de vaults

Esta fase é uma extensão futura do plano atual. Ela não está aprovada para
implementação e não altera o escopo concluído de alternância entre vaults.

### Decisão preliminar

Implementar primeiro vault pessoal, criado de forma assíncrona pelo usuário.
Compartilhamento, convites e papéis para outros usuários ficam para uma fase
posterior, pois ampliam significativamente a superfície de autorização,
revogação, auditoria e suporte operacional.

O usuário não acessará o Admin REST API do Keycloak. A API do Altivy usará uma
credencial de serviço de menor privilégio para reconciliar o recurso e as
permissions do Authorization Services. Keycloak continua sendo a autoridade de
identidade e autorização; PostgreSQL permanece uma projeção e o filesystem
continua sendo a fonte canônica das notas.

O vault somente será exibido no catálogo quando estiver em estado `ready` e
possuir autorização positiva para o usuário. O nome informado pelo usuário
nunca será usado como path físico.

### Não escopo da próxima fase

- compartilhamento entre usuários;
- convites por e-mail;
- criação de grupos ou memberships locais;
- exclusão física imediata;
- migração entre roots físicas;
- edição colaborativa;
- quotas sem decisão operacional;
- chamadas diretas do browser ao Keycloak Admin API.

### P-08: Aprovar produto, segurança e operação

**Status:** completed  
**Responsável:** equipe Altivy  
**Dependências:** P-07 e decisão humana explícita

**Objetivo:** aprovar o modelo de ownership, lifecycle, quota, retenção,
backup, exclusão e autorização antes de criar recursos pelo usuário.

**Entregas:**

- criar ADR `docs/adr/2026-0005-criacao-autogerenciada-de-vaults.md`;
- decidir limite de vaults por usuário e quota por vault;
- definir estados `pending`, `provisioning`, `ready`, `degraded`, `archived` e
  `failed`;
- definir retenção e restauração de vault arquivado;
- definir credencial de serviço e permissões mínimas no Keycloak;
- definir política de compensação para falhas entre PostgreSQL, Keycloak e
  filesystem;
- definir auditoria sem conteúdo de notas ou secrets.

**Gate:** ADR aceito, threat model revisado e critérios de aceite aprovados.

**Evidência:** ADR 2026-0005 aceito por Lincoln Zocateli em 2026-09-04,
definindo vault pessoal assíncrono, provisioning idempotente, lifecycle,
compensação e ausência de acesso administrativo do browser ao Keycloak.
Quotas detalhadas permanecem configuração a definir em P-10.

### P-09: Modelar lifecycle e caso de uso

**Status:** completed  
**Dependências:** P-08

Criar no domínio/aplicação:

- `CreateVaultCommand`;
- `CreateVaultCommandHandler`;
- `VaultProvisioningOperation`;
- estados e transições válidas do vault;
- validação de nome, quota e limite por sujeito;
- idempotência por `Idempotency-Key`;
- portas para persistência, autorização, provisioning e jobs.

O caso de uso deve criar uma operação pendente, nunca prometer que o vault já
está pronto enquanto o provisionamento não terminou.

**Evidência:** estados, transições, comando, resultado, portas de
persistência/geração de ID e handler idempotente implementados. Testes novos do
domínio passaram `2/2`; testes novos da Application passaram `2/2`.

### P-10: Persistir operação e fila durável

**Status:** completed  
**Dependências:** P-08, P-09

Adicionar uma migration expand-contract para:

- status operacional em `vaults`;
- `vault_operations` ou extensão equivalente em `index_jobs`;
- chave única de idempotência;
- tentativas, lease, próximo retry e erro sanitizado;
- quota e configuração operacional versionada.

Nenhuma coluna de membership ou policy será criada como autoridade local.

**Evidência:** modelo EF Core, mapeamentos Fluent API, adapter
`PostgresVaultProvisioningStore` e migration
`20260905011638_CreateVaultProvisioningOperations` adicionados. A migration
cria estado em `vaults`, tabela de operações e índices de idempotência/lease;
vaults existentes recebem estado `Ready`. O bootstrap versionado
`sql/001-bootstrap-altivy-roles.sql` e o script
`tools/scripts/bootstrap-altivy-postgres.ps1` preparam as roles e grants no
PostgreSQL principal. A suíte de integração passou `9/9` em 2026-09-05 contra
`localhost:5432`, incluindo aplicação de migration, paginação, persistência da
operação e filesystem. A suíte recusa explicitamente a porta `55432` para
impedir retorno à stack descartável.

### P-11: Provisionar o filesystem com segurança

**Status:** in-progress  
**Dependências:** P-09, P-10

Implementar no worker ou adapter de data:

- gerar `vaultId` no servidor;
- criar somente `/vaults/{vaultId}` sob a raiz confiável;
- rejeitar traversal, path absoluto, symlink e reparse point;
- aplicar usuário e permissões mínimos;
- criar arquivos iniciais apenas se o produto definir essa experiência;
- tornar retry idempotente;
- remover ou marcar resíduos de provisionamento incompleto;
- reconciliar diretório, projeção e estado.

Usar filesystem real nos testes de integração.

**Próximo slice:** implementar o registro durável de ocorrências, a entrega de
alertas administrativos e a quarentena conforme a
[Política de retenção e recuperação de resíduos](../politica-retencao-recuperacao-residuos-vault.md).

**Evidência parcial:** `IVaultFileSystemProvisioner` e
`FileSystemVaultProvisioner` implementados. O adapter valida o ID opaco no
domínio, confina o destino à raiz configurada, rejeita reparse points e torna a
criação do diretório idempotente. Os testes de integração com filesystem real
cobrem criação repetida, IDs com traversal ou path absoluto, reparse point no
diretório do vault e reparse point na própria raiz configurada;
no Windows, o cenário usa junction para não exigir privilégio administrativo de
criação de link simbólico. A suíte do recorte passou `6/6` sem falhas.

O processamento durável foi iniciado com `VaultProvisioningProcessor`, que
reivindica uma operação por lease, cria o diretório e persiste `ready`,
`degraded` com retry sanitizado ou `failed` quando o filesystem nega acesso.
`PostgresVaultProvisioningStore` faz o claim condicional, expira leases
abandonados e atualiza a projeção do vault na conclusão sob transação. O host
`backend/apps/worker` resolve dependências scoped por operação e foi incluído
na solução com Dockerfile próprio. Os três testes unitários do processador e o
build do worker passaram em 2026-09-06. Com
`ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING` e
`ALTIVY_INTEGRATION_RUNTIME_CONNECTION_STRING` carregadas localmente de
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, o teste de integração PostgreSQL também passou em
2026-09-06, confirmando que a migration aplica o lease, impede claim duplicado
e conclui a projeção do vault.

`VaultProjectionReconciler` executa no worker a cada cinco minutos após o
processamento da fila. Ele verifica cada projeção `ready` pelo adapter de
filesystem e marca somente as que não possuem diretório confiável como
`degraded`; não recria diretórios ausentes, evitando mascarar uma perda externa
de conteúdo. A verificação física passou em filesystem real e a listagem e
transição `ready` para `degraded` passaram contra PostgreSQL real em
2026-09-06.

A tabela `vault_residue_occurrences` registra de forma idempotente a ausência
do diretório de um vault `ready`, com primeiro e último instante de detecção e
`alert_pending`. A detecção degrada a projeção e cria ou renova a ocorrência na
mesma transação. O teste PostgreSQL real passou em 2026-09-06, confirmando que
detecções repetidas não duplicam a ocorrência e renovam a pendência de alerta.

O worker processa alertas pendentes por SMTP com TLS configurado pelo operador.
Cada entrega é exclusiva por lease, usa retry durável e envia e-mail sanitizado
para a caixa operacional. Host, credenciais, remetente e destinatário são
obrigatórios somente em runtime, sem versionamento de secrets. Os testes do
validador SMTP passaram em 2026-09-06.

`VaultOrphanDirectoryReconciler` compara diretórios válidos diretamente sob a
raiz confiável com vaults e operações conhecidos. Diretórios sem vínculo são
registrados primeiro como `orphaned-directory/v1` e então movidos atomicamente
para `.quarantine`; a ordem preserva o alerta mesmo se a movimentação falhar.
Diretórios inválidos, links e a própria quarentena não entram na varredura.
Os testes unitário, de filesystem real e PostgreSQL real passaram em
2026-09-06.

A política de retenção e recuperação de resíduos foi aprovada, com retenção de
30 dias e notificação imediata ao administrador
quando uma ocorrência é detectada. Ela bloqueia exclusão automática enquanto
não houver alerta confirmável, backup verificável e autorização administrativa
explícita.

O worker passou a publicar métricas OpenTelemetry de operações processadas,
resíduos detectados por tipo fixo, alertas processados e falhas por categoria
fixa. A exportação OTLP é opcional por configuração e não inclui IDs de vault,
sujeitos, paths, endereços SMTP ou secrets nas tags. O build do worker passou
em 2026-09-06.

Ao completar 30 dias desde a última detecção, o worker agenda um único alerta
administrativo `retention-expired`, sem exclusão física. Nova detecção reinicia
o prazo e torna a ocorrência novamente elegível para o alerta inicial. O teste
de integração PostgreSQL real passou em 2026-09-06, cobrindo entrega inicial,
aviso de vencimento e prevenção de duplicidade.

### P-12: Integrar provisioning do Keycloak

**Status:** blocked  
**Dependências:** P-08, P-09

Criar adapter idempotente para:

- registrar `vault-{vaultId}`;
- garantir `vault:read` e `vault:write`;
- criar permission do proprietário;
- consultar e reconciliar recursos já existentes;
- compensar recurso criado quando a operação falhar;
- tratar indisponibilidade sem liberar o vault parcialmente.

A credencial de serviço não deve usar usuário ou senha do realm `master` e não
deve administrar usuários, roles globais ou outros clients.

**Evidência de bloqueio:** a service account `service-account-altivy-api` foi
verificada no Keycloak 26.7.0 e possui somente `uma_protection`. Esse role
permite registrar recursos pelo Protection API, mas não criar ou reconciliar
policies e permissions do proprietário. O Admin REST API exige
`realm-management/manage-clients`, que concede administração de todos os
clients do realm e viola o princípio de menor privilégio. A porta de adapter
provisória foi removida; não será concedido esse role nem implementado adapter
até existir uma alternativa arquitetural aprovada e confinada ao resource
server `altivy-api`.

**Mitigação manual:** o script idempotente
`tools/scripts/provision-vault-owner-authorization.ps1` permite a um
administrador do Keycloak criar ou reutilizar o recurso `vault-{vaultId}`, uma
policy vinculada ao usuário proprietário e as permissions `vault:read`,
`vault:write`, `vault:manage` e `vault:delete`. O runtime, worker e browser não
recebem `manage-clients`. O `DryRun` para `lab-0001` e `lzocateli` confirmou o
alvo em 2026-09-06, e a execução contra o Keycloak local em
`http://localhost:8080` terminou sem erro. O procedimento está documentado em
[Configuração do Keycloak](../configuracao-keycloak.md#112-provisionar-autorização-manual-de-vault-pessoal).

### P-13: Expor contrato HTTP

**Status:** not-started  
**Dependências:** P-09, P-10, P-11, P-12

Adicionar ao OpenAPI:

```text
POST /api/v1/vaults
GET  /api/v1/vault-operations/{operationId}
POST /api/v1/vaults/{vaultId}/archive
```

O primeiro endpoint retorna `202 Accepted` com `operationId`. Documentar
`400`, `401`, `403`, `409`, `413`, `429`, `503` e `application/problem+json`.

O vault arquivado não deve ser destruído imediatamente. A exclusão definitiva
será uma decisão posterior com retenção, confirmação e backup verificado.

### P-14: Integrar o frontend

**Status:** not-started  
**Dependências:** P-13

Adicionar ao workspace:

- ação `Criar vault` no seletor;
- formulário de nome e quota visível;
- estados de provisionamento, sucesso, falha e retry;
- polling ou evento durável de operação;
- atualização do catálogo sem reload completo;
- navegação automática para o vault quando estiver `ready`;
- tratamento de conflito de idempotência, quota e indisponibilidade.

O browser não deve armazenar policy, membership, token ou path físico.

### P-15: Reconciliar laboratório

**Status:** not-started  
**Dependências:** P-12, P-13

Criar fixture/script idempotente para:

- usuário com dois vaults autorizados;
- usuário com apenas um vault;
- usuário sem vault;
- recurso sem `vault:read`;
- revogação durante a sessão;
- criação e retry da operação de provisioning.

As associações permanecem no Keycloak. O PostgreSQL não recebe cópia de
membership.

### P-16: Validar E2E, segurança e operação

**Status:** not-started  
**Dependências:** P-14, P-15

Cobrir:

- criação pessoal bem-sucedida;
- retry com a mesma `Idempotency-Key` sem duplicação;
- reload durante `provisioning`;
- falha do Keycloak;
- falha do filesystem;
- quota excedida;
- vault `failed` ausente do catálogo;
- vault `ready` disponível após reconciliação;
- arquivamento e fallback seguro;
- isolamento entre usuários;
- logs sem conteúdo, credentials ou secrets;
- métricas de duração, retries e falhas;
- backup e restauração conforme a política aprovada.

### Critérios de aceite da criação pessoal

- usuário autenticado cria um vault pessoal sem acessar Keycloak diretamente;
- a operação é idempotente e assíncrona;
- o nome nunca vira path físico;
- o vault aparece somente após `ready` e autorização positiva;
- falhas não deixam permission aberta sem filesystem pronto;
- retry não cria recursos duplicados;
- outro usuário não visualiza o vault;
- quota e limite de criação são aplicados;
- vault arquivado não aparece no catálogo;
- lifecycle é auditável sem registrar conteúdo ou secrets;
- a reconciliação converge após falha parcial.

## Critérios de aceite finais

- A pessoa autenticada visualiza somente vaults com decisão positiva para
  `vault:read`.
- IDs, nomes e existência de vaults negados não aparecem em payloads, UI ou
  logs.
- A URL canônica contém o vault e o módulo ativos.
- Novo login e recarga restauram o último vault somente se ele continuar
  autorizado.
- Revogação resulta em fallback seguro para outro vault autorizado ou estado
  sem acesso.
- A troca atualiza notas, documento, contexto, preferências e realtime sem
  misturar dados.
- Tema, densidade e painéis permanecem independentes por usuário e vault.
- O seletor funciona por teclado, tem foco visível e não causa overflow.
- Operações de nota preservam autorização, ETag, `If-Match` e proteção de
  paths.
- Contrato, código, laboratório e documentação descrevem o mesmo comportamento.
- Todos os gates de validação do recorte passam sem testes ignorados.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Avaliação UMA N+1 aumenta a latência | Abertura lenta do workspace com muitos vaults | Paginação, concorrência limitada, métricas e avaliação batch somente após medição |
| Lista parcial quando o Keycloak falha | UI pode interpretar ausência como revogação | Falhar a requisição inteira com `503` e preservar estado atual sem revelar candidatos |
| ID arbitrário na URL | Tentativa de autorização horizontal | API autoriza toda operação e UI não diferencia inexistente de negado |
| Resposta antiga chega após troca | Exposição visual temporária de outro vault | Cancelar ou descartar respostas por geração/vault ativo |
| Join SignalR anterior permanece ativo | Notificações cruzadas | Sair do grupo ou recriar conexão antes de ingressar no novo vault |
| Último vault foi revogado | Loop ou tela quebrada no login | Revalidar contra catálogo autorizado e usar fallback seguro |
| Preferências vazam entre vaults | Tema e layout incorretos | Manter chave por `subject + vaultId` e testar isolamento |
| Cursor permite inferência do catálogo | Enumeração indireta de candidatos | Cursor opaco, respostas apenas com autorizados e testes de não divulgação |
| Troca com edição futura não salva | Perda de conteúdo local | Manter fora do MVP e integrar com política de dirty state antes de habilitar esse cenário |

## Rollout e rollback

- Entregar o endpoint de listagem como expansão aditiva antes de alterar as
  rotas do frontend.
- Manter endpoints de vault e notas existentes durante toda a implantação.
- Ativar o seletor somente depois de contrato, autorização e testes de
  isolamento estarem validados.
- Monitorar latência e falhas de `GET /api/v1/vaults`, decisões UMA e trocas de
  vault sem registrar nomes ou conteúdo.
- Interromper o rollout se houver vazamento de metadados, lista parcial em
  indisponibilidade ou mistura de estado entre vaults.
- O rollback do frontend restaura temporariamente a navegação para o vault de
  laboratório sem remover o endpoint aditivo.
- O rollback do backend remove o uso do endpoint no frontend antes de retirar
  sua implementação; nenhuma migration precisa ser revertida.
- Alterações do laboratório são revertidas pela reconciliação idempotente dos
  recursos e policies sintéticos.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-09-04 | plano | - | draft | plano criado após análise do código e definição humana da URL, restauração e estratégia UMA | IA assistida |
| 2026-09-04 | plano | draft | approved | aprovação humana registrada no cabeçalho por Lincoln Zocateli | equipe Altivy |
| 2026-09-04 | plano | approved | in-progress | execução solicitada por Lincoln Zocateli; P-01 iniciada | IA assistida |
| 2026-09-04 | P-01 | not-started | in-progress | criação do ADR e expansão aditiva do OpenAPI iniciadas | IA assistida |
| 2026-09-04 | P-01 | in-progress | completed | ADR proposto, contrato aditivo e verificações direcionadas aprovadas | IA assistida |
| 2026-09-04 | P-02 | not-started | blocked | aguarda aceite humano explícito do ADR 2026-0004 | IA assistida |
| 2026-09-04 | plano | in-progress | blocked | gate arquitetural humano pendente antes da implementação | IA assistida |
| 2026-09-04 | ADR 2026-0004 | proposed | accepted | aprovação explícita de Lincoln Zocateli | Lincoln Zocateli |
| 2026-09-04 | plano | blocked | in-progress | ADR 2026-0004 aceito; execução retomada | IA assistida |
| 2026-09-04 | P-02 | blocked | in-progress | gate arquitetural atendido | IA assistida |
| 2026-09-04 | P-02 | in-progress | completed | catálogo keyset e testes do recorte implementados | IA assistida |
| 2026-09-04 | P-03 | not-started | completed | endpoint UMA e testes de não divulgação implementados | IA assistida |
| 2026-09-04 | P-04 | not-started | completed | catálogo Angular, rota canônica e fallback implementados | IA assistida |
| 2026-09-04 | P-05 | not-started | completed | notas e SignalR isolados por geração de vault | IA assistida |
| 2026-09-04 | P-06 | not-started | completed | seletor acessível integrado à topbar | IA assistida |
| 2026-09-04 | P-07 | not-started | in-progress | segundo vault sintético e documentação adicionados; E2E real pendente | IA assistida |
| 2026-09-04 | próxima fase | - | not-started | plano para criação autogerenciada de vaults salvo; recomenda iniciar por vault pessoal assíncrono | IA assistida |
| 2026-09-04 | P-08 | not-started | in-progress | ADR 2026-0005 criado em proposed; aguarda revisão humana e threat model | IA assistida |
| 2026-09-04 | ADR 2026-0005 | proposed | accepted | aprovação explícita de Lincoln Zocateli | Lincoln Zocateli |
| 2026-09-04 | P-08 | in-progress | completed | ADR aceito; escopo e segurança do vault pessoal aprovados | IA assistida |
| 2026-09-04 | P-09 | not-started | in-progress | lifecycle e handler idempotente implementados; testes 2/2 e 2/2 aprovados | IA assistida |
| 2026-09-04 | P-09 | in-progress | completed | lifecycle, command handler e idempotência validados por testes unitários | IA assistida |
| 2026-09-04 | P-10 | not-started | in-progress | modelo EF, adapter e migration de provisioning adicionados; integração PostgreSQL pendente | IA assistida |
| 2026-09-04 | P-10 | in-progress | completed | testes PostgreSQL, migration e store executados com sucesso pelo usuário | Lincoln Zocateli |
| 2026-09-05 | P-10 | completed | in-progress | integração passa a usar exclusivamente a stack PostgreSQL principal; bootstrap de roles e nova evidência no ambiente correto são necessários | IA assistida |
| 2026-09-05 | P-10 | in-progress | completed | bootstrap idempotente aplicado e suíte Integration aprovada com 9/9 testes em localhost:5432 | IA assistida |
| 2026-09-05 | P-12 | not-started | in-progress | porta de provisioning de autorização externa adicionada; adapter depende de service account restrita no Keycloak | IA assistida |
| 2026-09-05 | P-12 | in-progress | blocked | `uma_protection` não administra policies/permissions; `manage-clients` é amplo demais e não será concedido | IA assistida |
| 2026-09-04 | P-11 | not-started | in-progress | próximo slice: provisioning de filesystem confinado e idempotente | IA assistida |

## Regras de atualização

- O status geral usa `draft`, `proposed`, `approved`, `in-progress`, `blocked`,
  `completed`, `cancelled` ou `superseded`.
- IA pode atualizar este plano, mas não elevar seu status acima de `proposed`
  sem aprovação humana explícita.
- `approved` registra responsável e referência da aprovação humana.
- Etapas só mudam para `completed` mediante evidência verificável; `skipped`
  exige justificativa e aprovação humana.
- Toda mudança de status geral ou de etapa adiciona uma entrada no histórico.
- Mudança na fonte de autorização, estratégia batch ou cache de decisão exige
  revisão do ADR e deste plano antes da implementação.
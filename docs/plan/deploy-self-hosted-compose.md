# Plano: Deploy self-hosted por Docker Compose

**Status:** in-progress  
**Data de criação:** 2026-10-02  
**Última atualização:** 2026-10-02  
**Responsáveis:** Lincoln Zocateli  
**Origem:** decisão humana registrada na conversa de 2026-10-02  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [ADR 2026-0008](../adr/2026-0008-deploy-self-hosted-compose.md), [plano mestre](../plano-mestre.md), [CI/CD e segurança](../ci-cd-seguranca.md)

## Objetivo

Entregar e validar uma instalação self-hosted no host escolhido pelo operador,
subida por Docker Compose, com PostgreSQL persistente, gateway HTTPS e os hosts
web, BFF, API, worker e migrations. A instalação não depende de uma VPS ou de
NGINX/PostgreSQL compartilhados do projeto.

## Contexto e premissas

- A decisão humana de 2026-10-02 determina lançamento self-hosted local em
  containers.
- Keycloak permanece a autoridade externa obrigatória e pode operar localmente
  ou em infraestrutura alcançável; esta composição não cria realm, client,
  usuário, credencial ou política.
- O operador configura issuer, redirect URIs, certificados, secrets, SMTP,
  diretórios persistentes e host de publicação.
- O bind do gateway é loopback por padrão. Publicação em LAN/internet é escolha
  explícita do operador e exige TLS, firewall e hostname compatíveis.
- Secrets locais ficam exclusivamente no arquivo externo aprovado; nenhum
  `.env` real ou cópia entra no repositório.
- `deploy/production/` e os documentos de VPS ficam apenas como histórico até a
  migração operacional ser concluída; workflows de deploy remoto deixam de ser
  acionáveis.
- Releases continuam sendo construídas/publicadas separadamente com versão e
  digest. A primeira Compose também permite build local para validar o fluxo.

## Não escopo

- hospedar ou administrar Keycloak;
- configurar DNS, firewall ou emitir certificados para o operador;
- incluir dados de usuário ou secrets em fixtures ou no repositório;
- declarar release estável antes de validar OIDC, backup/restore e atualização;
- adicionar serviços compartilhados, broker ou novo datastore.

## Dependências e decisões

- ADR 2026-0008 aceito com a decisão humana de usar Compose self-hosted;
- Keycloak com client confidencial `altivy-bff`, issuer TLS e Authorization
  Services configurados pelo operador;
- PostgreSQL 18 com pgvector na imagem definida pelo plano mestre;
- certificados TLS do gateway e certificado PFX dedicado à proteção de dados do
  BFF;
- bootstrap seguro das roles `altivy_migrator` e `altivy_runtime`;
- workflow de release mantém publicação de imagens, SBOM, scan e proveniência;
  deployment passa a ser iniciado pelo operador no próprio host.

## Etapas

### P-01: Decidir e substituir a topologia de VPS

**Status:** completed  
**Responsável:** Lincoln Zocateli  
**Dependências:** nenhuma

**Objetivo:** registrar a decisão humana e impedir que automações continuem
promovendo para a VPS anterior.

Entregas:

- aceitar ADR 2026-0008;
- marcar ADR 2026-0007 e plano `deploy-producao-vps.md` como `superseded`;
- remover os workflows que executavam SSH/deploy de aplicação e infraestrutura
  na VPS, mantendo CI e publicação de release.

Validação:

- ADR e histórico registram a decisão humana;
- nenhum workflow ativo mantém secrets SSH para deploy remoto.

Evidências:

- Lincoln Zocateli determinou em 2026-10-02 que o lançamento da stack deve ser
  local/self-hosted em containers; a decisão foi registrada em ADR 2026-0008;
- ADR 2026-0007 e plano de VPS apontam para esta decisão;
- workflows de deploy remoto removidos; CI e publicação GHCR permanecem separados.

### P-02: Criar Compose e gateway self-hosted

**Status:** in-progress  
**Responsável:** equipe Altivy  
**Dependências:** P-01

**Objetivo:** fornecer uma composição local persistente, com fronteiras de rede
seguras e um único ponto HTTPS publicado.

Entregas:

- Compose em `deploy/self-hosted/` com gateway NGINX não-root, web, BFF, API,
  worker, PostgreSQL e perfil exclusivo de migrations;
- bind mounts documentados para vault, PostgreSQL, chaves do BFF e certificado
  de Data Protection;
- publicação apenas do gateway, em loopback por padrão;
- configuração de hostname, origem, proxy confiável e issuer sem valores
  sensíveis versionados;
- documentação de instalação e lista de variáveis seguras de exemplo.

Validação:

- `docker compose config --quiet` com placeholders não inicia serviços;
- apenas a porta HTTPS do gateway é publicada;
- NGINX valida a configuração e inicia não-root/read-only;
- imagens constroem em container e health checks ficam saudáveis.

Evidências:

- Compose, configuração NGINX e guia inicial criados em `deploy/self-hosted/`;
- `docker compose config --quiet` passou em 2026-10-02 com env-file temporário
  fora do repositório contendo somente placeholders; nenhum serviço foi iniciado;
- build containerizado em 2026-10-02 construiu `altivy-web`, `altivy-bff`,
  `altivy-api`, `altivy-worker` e `altivy-migrations` com sucesso (5/5);
- `nginx -t` passou no container não-root `nginxinc/nginx-unprivileged` com
  certificado autoassinado descartável; a chave temporária foi removida após o
  teste;
- execução da stack, health checks integrados, OIDC real e persistência ainda
  pendentes.

### P-03: Provisionar e validar primeiro boot autenticado

**Status:** in-progress  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-02

**Objetivo:** provar a instalação completa com Keycloak e PostgreSQL configurados
pelo operador.

Entregas:

- criar diretórios persistentes e aplicar permissões compatíveis com UIDs dos
  containers;
- provisionar database e roles separadas de runtime/migrations por processo
  administrativo seguro;
- cadastrar callbacks OIDC para a origem escolhida;
- aplicar migrations e iniciar os serviços;
- validar sessão anônima, login, logout, autorização e gravação de nota pelo
  gateway HTTPS.

Validação:

- `docker compose up --wait` conclui com health checks saudáveis;
- OIDC e chamadas protegidas passam pelo proxy, sem acesso direto ao BFF/API;
- reiniciar/recriar containers mantém vault, banco e chaves de sessão.

Evidências:

- A stack E2E existente foi iniciada em 2026-10-03 sobre o gateway NGINX e o
  Keycloak compartilhados, sem recriar esses serviços. Web, BFF e API ficaram
  saudáveis; `/bff/session` respondeu sessão anônima pelo gateway.
- A API e o worker aplicam override do host PostgreSQL somente em
  `Development`. Testes passaram: API 61/61, host override do worker 4/4 e
  suíte completa de testes unitários do worker 7/7. O Compose E2E injeta a
  connection string runtime existente e conecta API e worker à rede
  `identity-data`.
- As migrations pendentes foram aplicadas ao schema `altivy` com a role de
  migration. A conexão runtime foi validada por
  `PostgresVaultReaderReadsProjectedVaultWithRuntimeRole` (1/1); o cleanup
  confirmou zero registros de fixture `integration-*`.
- Testes reais de filesystem para `FileSystemNoteStore` passaram 9/9, cobrindo
  criação, update, conflito de ETag, move e isolamento em diretório temporário.
- O fixture isolado `lab-write-e2e` foi criado em `Ready`, com diretório no
  volume E2E e autorização UMA somente para `lzocateli`; nenhum grant de
  `lab-0001` ou `lab-0002` foi alterado. A jornada Playwright pelo gateway
  `https://localhost:7443/notes` passou: POST, GET com ETag, PUT com `If-Match`
  e leitura final do conteúdo atualizado. Duas execuções criaram duas notas
  sintéticas, mantidas apenas nesse vault; a limpeza da lixeira não foi feita.
- Playwright real via `https://localhost:7443/notes`: smoke anônimo aprovado;
  oito jornadas positivas autenticadas passaram, incluindo login/logout,
  catálogo/leitura autorizada e SignalR. O teste de negação de `lab-0002`
  falhou porque a conta atual tem grant nesse vault; nenhum grant foi alterado.
- A suíte completa de integração teve 25 aprovados e 11 falhas `57P03` enquanto
  o PostgreSQL compartilhado entrou em recovery. Depois disso, o serviço voltou
  a aceitar conexões sem restart do container e o teste PostgreSQL focado passou.
  Não repetir a suíte inteira até estabilizar a causa operacional da recuperação.
- O worker E2E foi reconstruído após a falha transitória de PostgreSQL. O loop
  agora repete `TimeoutException` e `NpgsqlException.IsTransient` com backoff
  limitado de 5 a 60 segundos; o teste `RetriesTransientDatabaseFailureAndKeepsRunning`
  passou e a suíte unitária completa passou 7/7. O container permaneceu ativo,
  executou consultas às filas e ao catálogo no PostgreSQL healthy; a fila de
  indexação estava vazia, portanto um job real ainda não foi comprovado. Os
  logs também registraram aviso não fatal de `libgssapi_krb5.so.2` ausente.
  Em Development, a ausência total de SMTP seleciona
  `DevelopmentAdministrativeNotifier`, que registra metadados sanitizados sem
  enviar e-mail; Production continua exigindo SMTP completo. Nenhum valor
  externo foi lido ou exibido.
- O worker concluiu quatro jobs de indexação do fixture (state `Completed`, uma
  tentativa cada) e materializou duas projeções. O container permaneceu
  `running`, com zero restarts; PostgreSQL ficou `healthy`. Um aviso não fatal de
  `libgssapi_krb5.so.2` ausente continua visível nos logs. Reinício com
  persistência e validação da stack self-hosted completa seguem pendentes.

### P-04: Backup, restore, upgrade e recuperação

**Status:** not-started  
**Responsável:** operação Altivy  
**Dependências:** P-03

**Objetivo:** tornar upgrades locais seguros e reversíveis sem downgrade
destrutivo de schema.

Entregas:

- procedimento de backup coordenado do vault e PostgreSQL;
- restore em diretório e database limpos, com reconstrução da projeção quando
  necessário;
- atualização usando imagem imutável/digest e migrations compatíveis;
- rollback de imagens sem downgrade automático do banco;
- documentação de logs, health, rotação de secrets e recuperação.

Validação:

- backup e restore completos executados em host descartável;
- upgrade e rollback comprovados sem perda de arquivos Markdown;
- operador recupera a instalação seguindo somente o runbook.

Evidências:

- pendente.

## Critérios de aceite finais

- a stack inicia em Docker Engine/Docker Desktop sem exigir runtime de aplicação
  instalado no host;
- PostgreSQL, vault e chaves do BFF sobrevivem à recriação de containers;
- somente gateway HTTPS publica portas, com bind seguro e TLS válido;
- login, logout e autorização são decididos pelo Keycloak e aplicados pela API;
- migrations usam role dedicada; API/worker não recebem privilégios de DDL;
- backup/restore e upgrade/rollback têm evidência executada;
- documentação não contém secrets e descreve diferenças entre instalação local,
  LAN e host público.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Keycloak inacessível ou issuer diferente | login/callback falha | exigir issuer idêntico no BFF/API, navegador e Discovery; smoke OIDC antes do uso |
| porta publicada fora de loopback sem proteção | exposição não intencional | loopback por padrão; exigir decisão explícita, TLS e firewall para LAN/Internet |
| volumes criados com owner incompatível | falha de escrita ou risco de privilégio | documentar UID, owner e preflight antes de `up` |
| perda parcial entre banco e vault | projeção divergente | backup conjunto, jobs idempotentes e reconciliação |
| segredo de bootstrap exposto em argumentos | comprometimento de credencial | provisionar roles por procedimento seguro, sem echo, history ou logs |
| imagens locais diferentes do digest de release | comportamento não reproduzível | manter build local para avaliação e usar digest para promoção suportada |

## Rollout e rollback

O operador prepara secrets, issuer, callbacks, certificados e bind mounts; inicia
PostgreSQL; provisiona roles; executa migrations; sobe os serviços e valida o
smoke test pelo gateway. A instalação é interrompida se OIDC, readiness ou
persistência falhar. Rollback retorna à imagem/configuração anterior e não
reverte migrations destrutivas. Restore de banco e vault é uma operação
coordenada documentada em P-04.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-10-02 | plano | - | approved | direção self-hosted local aprovada explicitamente por Lincoln Zocateli na conversa | Lincoln Zocateli |
| 2026-10-02 | plano | approved | in-progress | Compose e gateway iniciais implementados; config validado com placeholders | IA assistida |
| 2026-10-02 | P-01 | not-started | completed | decisão humana registrada no ADR 2026-0008 | Lincoln Zocateli |
| 2026-10-02 | P-02 | not-started | in-progress | Compose, gateway e guia criados; validação de config aprovada; build/startup ainda pendentes | IA assistida |
| 2026-10-03 | P-03 | not-started | in-progress | migrations aplicadas, PostgreSQL runtime 1/1, filesystem 9/9, E2E positivo 8/8 e worker iniciado em Development; processamento de job, escrita E2E e restore pendentes | IA assistida |

## Regras de atualização

- não marcar o plano como concluído antes de P-03 e P-04 possuírem evidências
  operacionais;
- não ler ou versionar secrets externos;
- manter workflows de release separados do ato de iniciar a stack;
- atualizar o índice em `docs/plano-mestre.md` junto com a inclusão do plano.

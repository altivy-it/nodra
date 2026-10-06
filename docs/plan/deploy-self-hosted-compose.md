# Plano: Deploy self-hosted por Docker Compose

**Status:** in-progress  
**Data de criação:** 2026-10-02  
**Última atualização:** 2026-10-06
**Responsáveis:** Lincoln Zocateli  
**Origem:** decisão humana registrada na conversa de 2026-10-02  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [ADR 2026-0008](../adr/2026-0008-deploy-self-hosted-compose.md), [plano mestre](../plano-mestre.md), [CI/CD e segurança](../ci-cd-seguranca.md)

## Objetivo

Entregar e validar a stack `altivy-notes` no host escolhido pelo operador, com
web, BFF, API e worker em Docker Compose. Gateway, PostgreSQL e Keycloak são
compartilhados na stack `altivy-identity`, pertencente ao AKB; migrations são
operações transitórias.

## Contexto e premissas

- A decisão humana de 2026-10-02 determina lançamento self-hosted local em
  containers.
- Keycloak permanece a autoridade externa obrigatória e pode operar localmente
  ou em infraestrutura alcançável; esta composição não cria realm, client,
  usuário, credencial ou política.
- O operador configura issuer, redirect URIs, certificados, secrets, SMTP,
  diretórios persistentes e host de publicação.
- A stack do produto não publica portas; toda navegação entra pelo gateway
  compartilhado em `/notes`.
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

**Objetivo:** fornecer uma composição de produto persistente, consumida pelo
gateway e PostgreSQL compartilhados por redes externas restritas.

Entregas:

- Compose em `deploy/self-hosted/` somente com web, BFF, API e worker;
- bind mounts documentados para vault, chaves do BFF e certificado
  de Data Protection;
- nenhuma porta publicada pela stack `altivy-notes`;
- configuração de hostname, origem, proxy confiável e issuer sem valores
  sensíveis versionados;
- documentação de instalação e lista de variáveis seguras de exemplo.

Validação:

- `docker compose config --quiet` com placeholders não inicia serviços;
- aliases `notes-web` e `notes-bff` resolvem na rede `identity-global`;
- API e worker alcançam PostgreSQL somente pela rede `identity-client`;
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
- A solução `Altivy.slnx` continha dez referências cruzadas para projetos do
  Dokpod ausentes deste repositório. As referências foram removidas; o build
  Release com analyzers passou com zero warnings/erros e os testes backend fora
  de Integration passaram 181/181. O Vitest passou 99/99 e o build Angular de
  produção foi gerado.
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
  e leitura final do conteúdo atualizado. As notas sintéticas permanecem
  apenas nesse vault; a limpeza da lixeira não foi feita.
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
  executou consultas às filas e ao catálogo no PostgreSQL healthy. A
  comprovação posterior de quatro jobs concluídos e duas projeções consta abaixo.
  Os logs também registraram aviso não fatal de `libgssapi_krb5.so.2` ausente.
  Em Development, a ausência total de SMTP seleciona
  `DevelopmentAdministrativeNotifier`, que registra metadados sanitizados sem
  enviar e-mail; Production continua exigindo SMTP completo. Nenhum valor
  externo foi lido ou exibido.
- O worker concluiu quatro jobs de indexação do fixture (state `Completed`, uma
  tentativa cada) e materializou duas projeções. Após os testes adicionais, o
  estado final foi oito jobs concluídos e quatro projeções. O container
  permaneceu `running`, com zero restarts; PostgreSQL ficou `healthy`.
- A recriação de web/BFF/API/worker inicialmente deixou o BFF unhealthy porque
  `localhost` apontava para o IP antigo `172.31.0.10`. O Compose agora exige o
  IP e a sub-rede atuais de `identity-global`, obtidos por inspeção do gateway;
  a configuração passou e os quatro consumidores ficaram healthy. Após a
  recriação, a leitura autenticada de uma nota preexistente passou (1/1),
  provando a persistência do volume do vault. O diretório de Data Protection
  contém uma chave; a sessão anônima pelo gateway respondeu como esperado.
- A suíte E2E autenticada pós-recriação passou 10/10. O único cenário excluído
  continua sendo a negação de `lab-0002`, pois o usuário real possui grant; não
  alteramos essa autorização. O runner em volume somente leitura emite avisos
  não bloqueantes do pytest cache.
- O aviso não fatal de `libgssapi_krb5.so.2` ausente continua visível nos logs.
  A validação isolada com PostgreSQL próprio foi supersedida pela decisão de
  consumir exclusivamente o PostgreSQL compartilhado; backup, restore, upgrade
  e rollback pertencem à etapa P-04.
- Em 2026-10-04, Lincoln Zocateli autorizou o reinício do PostgreSQL
  compartilhado. `docker restart --timeout 60` preservou as contagens antes e
  depois: três vaults, oito jobs do fixture, oito concluídos e quatro projeções.
  O banco voltou a aceitar conexões e ficou healthy; o worker manteve zero
  restarts. Sete cenários Playwright via gateway passaram, incluindo login,
  leitura da nota preexistente, negociação SignalR e logout. Isso comprova
  persistência após restart, não após remoção/recriação do container do banco.
- Após o ensaio coordenado de backup, escrita/autosave e leitura persistida
  passaram 2/2 via gateway. A fila de indexação chegou a dez jobs concluídos e
  cinco projeções no vault sintético, comprovando reconexão e nova gravação.
- Em 2026-10-05, o preflight histórico da composição isolada, com project name
  hoje obsoleto `altivy-selfhosted-validation` e arquivo externo aprovado, falhou na
  interpolação. Faltam configuração específica de origem/rede/paths, identidade
  do banco e credenciais Production; a lista operacional está no
  [guia self-hosted](../../deploy/self-hosted/README.md). Esse bloqueio foi
  resolvido posteriormente pelo script versionado e configuração externa; não
  descreve o estado atual da instalação.
- Em 2026-10-05, o fluxo `start-self-hosted.ps1` passou por `-ValidateOnly`,
  validando TLS/CA, SANs, correspondência de chaves, PFX e Compose sem alterar
  serviços. O boot completo provisionou roles sem privilégios administrativos
  para runtime, aplicou migrations, sincronizou callbacks sem apagar os
  anteriores e iniciou PostgreSQL/API/BFF/web/gateway healthy, com worker ativo,
  em `https://localhost:8443`.
- O fluxo completo repetido com `-ValidationVaultId lab-write-e2e` criou apenas
  fixture sintética opt-in, sem criar grants. A suíte Playwright no runner
  containerizado, acessando somente o gateway, passou 5/5: anônimo, challenge
  SignalR anônimo, login/logout, autosave POST/GET/PUT com ETag/If-Match e
  negociação SignalR autenticada. Depois, `-RecreateConsumers` recriou
  web/BFF/API/worker/gateway sem recriar PostgreSQL; a leitura autenticada da
  nota persistida passou 1/1. A instalação terminou online e healthy.
- `new-local-production-certificates.ps1 -ValidateOnly` passou em container
  OpenSSL. O emissor versionado recusa sobrescrever destinos; confia numa CA
  local já instalada, não instala uma CA e não substitui certificados públicos.
- A regressão Angular do estado autenticado sem vault, com ação de logout,
  passou 4/4. O build Production da web e os builds de API/worker/BFF/migrations
  concluíram no boot completo. O aviso não fatal `libgssapi_krb5.so.2` permanece
  nos logs do migrator.
- Restore completo, upgrade/rollback entre versões e revisão humana dos gates
  ainda não foram concluídos; por isso P-03 volta a `in-progress`, sem marcar a
  etapa como concluída automaticamente.

### P-04: Backup, restore, upgrade e recuperação

Os registros abaixo são cronológicos. Os bloqueios temporários de P-03 foram
resolvidos pelas evidências atuais da etapa anterior. O escopo P-04 foi concluído
com backup/restore isolado, roles de menor privilégio e upgrade/rollback do API;
os ensaios de rollback autenticado, downgrade e migrations `Down` foram
explicitamente dispensados por decisão humana registrada no histórico.

Em 2026-10-05, as credenciais existentes foram encontradas nas variáveis de
integração. Connection strings foram construídas em memória por parser
estruturado, preservando senhas e roles e ajustando host/database para o banco
próprio. PostgreSQL Production iniciou em bind mount exclusivo; roles runtime
e migrator foram provisionadas sem flags administrativos. Backup pré-migration
foi capturado fora do repositório; as 12 migrations concluíram com sucesso.

A stack histórica `altivy-selfhosted-production`, posteriormente substituída
pelo padrão `altivy-notes`, iniciou no perfil Production em
`https://localhost:8443`. Foram corrigidos HTTPS interno obrigatório do BFF,
permissões dos tmpfs NGINX e colisão do IP de gateway. O override local de
identidade preserva a plataforma compartilhada. Banco/API/BFF/web/gateway
ficaram healthy e worker ativo; somente o gateway próprio publica 8443 em
loopback. Sessão anônima HTTPS respondeu corretamente com CA validada.

Naquele estágio, o gate funcional seguia bloqueado por OIDC, não por credenciais ausentes:
tentativas de adicionar callbacks Production ao client existente foram
rejeitadas com HTTP 400 e não confirmadas; a borda de identidade mascara o
corpo do erro. No Playwright Production, a tela anônima passou e negociação
SignalR falhou com 500 durante challenge/PAR. Não houve desativação de TLS,
PKCE ou PAR. Login, logout e escrita autenticada ainda precisavam de evidência
naquele estágio; foram validados posteriormente conforme registrado em P-03.

Nesse preflight de Production em 2026-10-05, a combinação dos arquivos
externos da plataforma de identidade e do AltivyNotes resolveu as variáveis
administrativas centralizadas, sem copiar valores. Restaram somente
`ALTIVY_POSTGRES_RUNTIME_CONNECTION_STRING` e
`ALTIVY_POSTGRES_MIGRATOR_CONNECTION_STRING`. O script de bootstrap disponível
é exclusivo do laboratório e aponta diretamente para `keycloak`; não foi
executado. Production ainda não havia sido iniciado nesse ponto; as roles e
connection strings foram configuradas antes da aplicação das migrations.

Em 2026-10-05, por solicitação de Lincoln Zocateli, os parâmetros
administrativos PostgreSQL passaram a usar `POSTGRES_ADMIN_USERNAME` e
`POSTGRES_ADMIN_PASSWORD`, como na plataforma de identidade. Compose, exemplo
e guia foram alinhados; `config --quiet` passou com dados sintéticos. Nenhum
valor real foi copiado durante essa alteração.

O preflight de início de Production em 2026-10-05 resolveu TLS e PFX usando
`certs-prd`. O PFX foi validado com a senha do arquivo externo, tem chave
criptografada AES-256-CBC e validade até 2028-10-04. Parâmetros não secretos da
instalação local isolada foram definidos somente na sessão do terminal.
Restam `POSTGRES_ADMIN_USERNAME`, `POSTGRES_ADMIN_PASSWORD`,
`ALTIVY_POSTGRES_RUNTIME_CONNECTION_STRING`,
`ALTIVY_POSTGRES_MIGRATOR_CONNECTION_STRING` e `ALTIVY_SMTP_TO`. Nenhum serviço
Production havia sido iniciado naquele ponto. A stack compartilhada seguiu
operacional, sem promoção ou mudança das credenciais existentes.

Em 2026-10-05, Lincoln Zocateli autorizou reutilizar credenciais/certificados em
Production. A composição passou a aceitar aliases externos de secret BFF e
paths TLS existentes, com prioridade dos nomes próprios de Production. Testes
Compose sintéticos de fallback/precedência passaram; o preflight real resolveu
esses aliases, mas ainda restava configuração Production naquele ponto.
PFX dedicado/senha e destinatário SMTP não são substituídos pelo TLS. Não houve
promoção dos consumidores nem mudança na topologia ou nas credenciais reais.

**Status:** completed
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

- Em 2026-10-04, BFF/API/worker foram parados graciosamente durante a captura
  coordenada e retomados em `finally`. O dump custom-format inclui somente o
  schema `altivy`, sem owners/grants; o tar inclui apenas `lab-write-e2e`.
  Nenhum schema de identidade ou credencial foi exportado. Os artefatos e
  diretórios restaurados ficaram fora do repositório, sob
  `%LOCALAPPDATA%/AltivyNotes/Recovery/`, com ACL restrita ao usuário atual.
- `pg_restore --exit-on-error` passou em PostgreSQL limpo com a mesma imagem por
  ID, sem rede/portas, usando bind mount novo. O baseline restaurado foi
  `3|8|8|4` (vaults, jobs, concluídos, projeções). As quatro notas restauradas em
  diretório limpo tiveram hashes SHA-256 idênticos aos da origem.
- O container de destino foi removido/recriado preservando seu bind mount; o
  baseline permaneceu `3|8|8|4`. Os dois containers auxiliares foram removidos
  depois do teste; o banco compartilhado nunca foi alvo de restore.
- Procedimento, diagnóstico e limites estão no
  [runbook de recuperação](../runbooks/recuperacao-self-hosted.md). O ensaio de
  2026-10-04 era parcial; a evidência mais recente abaixo amplia a cobertura ao
  dump PostgreSQL e a `vaults/` + `bff-keys/`, mas não restaura PFX,
  roles/grants de produção ou identidade.
- Em 2026-10-05, o boot coordenado passou a capturar, antes de migrations,
  dump PostgreSQL, archive de `vaults/` e `bff-keys/`, ID imutável da imagem
  PostgreSQL, checksum de dump/archive/metadata e manifesto SHA-256 por arquivo.
  O snapshot foi restaurado em
  PostgreSQL efêmero da mesma imagem, em container `--network none`, sem portas;
  a restauração passou para 1 vault e 276 tabelas, e o manifesto confirmou
  integridade dos arquivos do vault e do keyring. A camada descartável foi
  removida ao fim do teste.
- O PFX e o estado Keycloak/UMA continuam sob backup externo do operador e da
  plataforma de identidade; não são conteúdo do archive Altivy. O ensaio
  reprovisionou roles sintéticas de menor privilégio e comprovou restore de
  schema/keyring. O smoke da API anterior foi liveness, sem rollback autenticado.
- O cache local contém imagens antigas de consumidores, mas não um conjunto
  pareado de migrations/imagens por digest para rollback. Não foram executadas
  migrations `Down` nem downgrade.
- Em 2026-10-05, o harness `test-self-hosted-upgrade.ps1` foi executado do commit
  `69431f0` ao bundle atual em PostgreSQL descartável `--network none`. Aplicou
  11 migrations até `20260928234000_AddTrashAuditEvents`, inseriu uma linha
  sintética, aplicou `20260930123851_ReconcileCurrentModel`, confirmou 12
  migrations, schema inalterado e fixture preservada. O API antigo por digest
  `1cb343...` ficou healthy e respondeu `/health/live` pelo NGINX allowlist-only.
  Todos os containers e o bundle temporário foram removidos pelo harness.
- Esse rollback validou somente a imagem da API antiga e sua liveness; não cobre
  BFF/web/worker, endpoints autenticados, escrita/leitura da projeção nem
  proveniência assinada vinculando imagem e commit. Lincoln Zocateli dispensou
  explicitamente esses ensaios autenticados, downgrade de banco e migrations
  `Down` na decisão registrada no histórico de 2026-10-05.
- Em 2026-10-05, `test-self-hosted-recovery.ps1` restaurou o snapshot em
  PostgreSQL isolado, aplicou o bootstrap versionado e transferiu ownership dos
  objetos do schema para `altivy_migrator`. O bundle atual foi executado com essa
  role; `altivy_runtime` leu o catálogo, manteve NOSUPERUSER/NOCREATEDB/
  NOCREATEROLE/NOBYPASSRLS e recebeu negação ao tentar DDL. Senhas foram
  geradas no container descartável e não sobreviveram ao teardown. Isso prova
  restore de schema/roles e least privilege, mas não restaura secret/PFX ou
  grants do Keycloak.
- Em 2026-10-05, Compose passou a aceitar os cinco refs do `images.env` de release
  por digest; `start-self-hosted.ps1 -ReleaseImageManifest ... -NoBuild` valida
  checksum, allowlist de nomes e formato GHCR e baixa refs antes de parar writers.
  Preflight local sem manifesto continuou aprovado. Manifesto sintético passou pelo
  preflight; ausência de `-NoBuild` e checksum adulterado foram rejeitados. Não
  houve pull nem deploy de release oficial, então promoção/rollback entre releases
  seguem pendentes.

Conclusão de P-04: backup e restore do database, vault e keyring foram executados
em destino efêmero; ownership/migrations e least privilege foram validados; a
compatibilidade expand da migration atual e liveness da API anterior passaram.
Lincoln Zocateli autorizou dispensar os ensaios de rollback autenticado, downgrade
de schema e migrations `Down`; esses itens são tratados como concluídos por decisão
humana, não como testes executados. PFX e identidade continuam em mecanismos
externos documentados e não fazem parte do backup Altivy.

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
| 2026-10-04 | P-04 | not-started | in-progress | continuação autorizada por Lincoln Zocateli; restart PostgreSQL preservou baseline e sete testes E2E passaram; ensaio isolado de restore iniciado | IA assistida |
| 2026-10-04 | P-03 | in-progress | blocked | preflight self-hosted falhou por variáveis Production ausentes no ambiente/arquivo externo; nenhum serviço isolado foi iniciado | IA assistida |
| 2026-10-05 | P-03 | blocked | in-progress | boot reprodutível, health checks, OIDC, Playwright autosave e leitura após recriação dos consumidores passaram; restore/upgrade e revisão humana permanecem pendentes | IA assistida |
| 2026-10-05 | P-04 | in-progress | completed | backup/restore com roles, upgrade 11→12 e liveness da API anterior passaram em containers isolados; Lincoln Zocateli dispensou rollback autenticado, downgrade e migrations Down explicitamente na conversa de 2026-10-05 | Lincoln Zocateli |

## Regras de atualização

- não marcar o plano como concluído antes de P-03 e P-04 possuírem evidências
  operacionais;
- não ler ou versionar secrets externos;
- manter workflows de release separados do ato de iniciar a stack;
- atualizar o índice em `docs/plano-mestre.md` junto com a inclusão do plano.

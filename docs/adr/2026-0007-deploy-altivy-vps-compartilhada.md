# ADR 2026-0007: Deploy do Altivy Notes em VPS com infraestrutura compartilhada

**Status:** superseded  
**Data:** 2026-09-29  
**Responsáveis:** equipe Altivy  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano de deploy de produção em VPS](../plan/deploy-producao-vps.md), [CI/CD e segurança](../ci-cd-seguranca.md)

> Supersedido pela decisão humana de distribuição self-hosted local registrada no [ADR 2026-0008](2026-0008-deploy-self-hosted-compose.md).

## Contexto

O Altivy Notes precisa de deploy automatizado e reversível em uma VPS dedicada,
mas o NGINX, PostgreSQL e Keycloak já operam como infraestrutura compartilhada.
O domínio público da aplicação será `https://notes.altivy.it`; o Keycloak
compartilhado permanece em `https://credential.altivy.it`.

Os workflows atuais não publicam aplicações nem configuram os serviços
compartilhados. A aplicação já possui imagens Docker para web, API, BFF e
worker, além de migrations EF Core. O frontend E2E usa o subpath `/notes/`,
enquanto a instalação de produção usa a raiz do domínio.

## Forças de decisão

- não reiniciar nem substituir containers compartilhados durante deploy da aplicação;
- evitar credenciais permanentes de registry e alterações destrutivas no PostgreSQL;
- permitir promoção independente de frontend e backend;
- manter imagem, configuração e migrações identificáveis e recuperáveis;
- limitar o escopo de DNS, TLS, secrets e persistência por aplicação.

## Opções consideradas

### Stack completa isolada

Subir cópias próprias de NGINX, PostgreSQL e Keycloak no Compose do produto.
Foi descartada para esta VPS porque essas infraestruturas já existem e são
compartilhadas.

### Deploy sobre serviços compartilhados

Manter somente web, BFF, API, worker e migration bundle na stack Altivy; usar
redes Docker externas e entregar a configuração de virtual host em workflow
independente. O workflow PostgreSQL entrega o SQL de bootstrap, mas não cria
roles, executa SQL administrativo nem reinicia o container. Esta opção foi
selecionada pelo responsável do projeto em 2026-09-29.

## Decisão

- Criar imagens OCI SemVer no GHCR, produzir CycloneDX, scan Trivy e
  proveniência BuildKit, e promover imagens exclusivamente por digest.
- Gerar changelog e tags SemVer com Release Please após CI verde e merge humano
  da release PR.
- Usar GitHub Environment `production` e SSH com host key previamente fixada;
  o token GHCR de leitura é enviado por stdin e removido ao final da operação.
- Separar deploy de `frontend`, `backend` e `all`; migrations rodam antes da
  promoção de API/BFF/worker com role própria de migration.
- Guardar Compose e scripts em `/userapps/altivynotes`, arquivos de runtime em
  `/userapps/configs/altivynotes`, vault e chaves BFF em
  `/userapps/var/altivynotes`, e virtual host em
  `/userapps/configs/nginx/altivynotes`.
- Reutilizar o certificado existente em `/userapps/ssl/altivy.it.pem` e
  `/userapps/ssl/altivy.it.key`; nenhuma chave TLS é publicada no repositório.
- Não configurar nem alterar o Keycloak compartilhado pelo workflow. O realm
  `altivy` deve autorizar os callbacks da origem `https://notes.altivy.it`.
- O workflow de infraestrutura instala/testa/recarrega apenas o virtual host
  Altivy e entrega o SQL PostgreSQL. Ele não reinicia o NGINX/PostgreSQL nem
  altera o bind mount compartilhado `/userapps/var/`.
- Rollback reverte imagens e configuração do serviço, mas nunca tenta desfazer
  migrations PostgreSQL automaticamente.

## Consequências

### Positivas

- mudança frontend pode ser promovida sem reciclar o backend;
- aplicação não possui container PostgreSQL próprio nem publica portas internas;
- release aponta para digests verificáveis e pode recuperar a versão anterior;
- workflows de app e infraestrutura têm permissões e efeitos separados.

### Negativas e trade-offs

- primeiro provisionamento de rede, database, roles, arquivos externos e
  acesso SSH continua sendo uma tarefa explícita do operador;
- usuário de deploy com acesso ao Docker do host tem poder equivalente a root;
  SSH, Environment protegido, host key e chave dedicada são controles
  obrigatórios, mas não eliminam esse risco;
- atualizações de schema devem seguir expand-contract para permitir rollback da
  aplicação sem downgrade de banco;
- uso do PostgreSQL/NGINX compartilhados preserva riscos de contenção e raio de
  impacto entre serviços do host.

## Segurança e privacidade

Secrets não são versionados nem enviados como build args. Os arquivos de
ambiente no host têm permissões restritas. Os jobs não imprimem connection
strings, chaves, tokens ou conteúdo dos vaults. A imagem da aplicação não
monta Docker socket. O workflow de infraestrutura não recebe token GHCR nem
credencial PostgreSQL administrativa.

## Dados, compatibilidade e migração

O database `altivy` e as roles `altivy_migrator`/`altivy_runtime` são
provisionados pelo operador no PostgreSQL existente. `altivy_runtime` recebe
somente DML; migrations utilizam `altivy_migrator`. Os arquivos Markdown e
anexos continuam em bind mount próprio da aplicação.

## Observabilidade e operação

O deploy exige readiness de containers, smoke test HTTPS em
`https://notes.altivy.it/bff/session`, logs do Compose e rollback de serviço.
Backup do vault e PostgreSQL, teste de restore, alertas e procedimento de
recuperação são pré-requisitos operacionais antes do primeiro release estável.

## Validação

- CI executa testes Angular/.NET, integração com PostgreSQL e build das imagens;
- publicação bloqueia vulnerabilidades `CRITICAL` corrigíveis e anexa SBOM,
  hashes e manifesto de digests à GitHub Release;
- deploy exige SSH com host key validada, redes externas presentes, arquivos
  de configuração protegidos e smoke test verde;
- workflow NGINX exige `nginx -t` e comprova que o virtual host está incluído
  antes do reload.

## Rollout e rollback

Instalar primeiro o virtual host e entregar o SQL PostgreSQL; provisionar o
database/roles e atualizar os callbacks do realm; depois promover a aplicação.
Deploy de backend executa backup operacional conforme política do host,
migrations compatíveis, atualização dos serviços e smoke test. Falha restaura
as imagens anteriores, sem downgrade de schema. NGINX possui cópia da última
configuração funcional e reload separado.

## Referências

- [Plano de deploy de produção em VPS](../plan/deploy-producao-vps.md)
- [Runbook de deploy](../runbooks/deploy-producao-vps.md)
- [Plano mestre](../plano-mestre.md)
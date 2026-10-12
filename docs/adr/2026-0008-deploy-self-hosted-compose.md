# ADR 2026-0008: Distribuição self-hosted por Docker Compose

**Status:** accepted  
**Data:** 2026-10-02, revisada em 2026-10-06
**Responsáveis:** Lincoln Zocateli  
**Origem:** decisão humana registrada na conversa de 2026-10-02  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano de deploy self-hosted](../plan/deploy-self-hosted-compose.md), [ADR 2026-0007 supersedido](2026-0007-deploy-altivy-vps-compartilhada.md), [plano mestre](../plano-mestre.md)

## Contexto

O Altivy Notes é um produto self-hosted distribuído por containers. Gateway,
PostgreSQL e Keycloak são aplicações independentes, compartilhadas com outros
produtos e operadas pelo repositório `architecture-knowledge-base`. Misturá-las
ao Compose do produto duplica ownership e acopla ciclos de vida distintos.

A aplicação já tem containers para web, BFF, API, worker e migrations. O plano
mestre exige Docker Compose, PostgreSQL persistente, vault em bind mount e
Keycloak como autoridade externa de identidade e autorização. O produto deve
consumir a infraestrutura compartilhada por redes Docker externas sem controlá-la.

## Forças de decisão

- instalação reproduzível em Docker Engine ou Docker Desktop;
- persistência explícita de vault e chaves de Data Protection no produto;
- nenhuma porta publicada pela stack do produto;
- nenhum secret ou arquivo `.env` real é versionado;
- Keycloak e políticas de autorização continuam sob controle do operador;
- instalações locais, corporativas e isoladas usam as mesmas imagens e variam
  somente configuração.

## Opções consideradas

### Opção A: infraestrutura compartilhada e stack de produto separada

Centraliza gateway, PostgreSQL e Keycloak em `altivy-identity`, sob ownership do
AKB. O produto mantém `altivy-notes` somente com web, BFF, API e worker.

### Opção B: infraestrutura duplicada no Compose do produto

Inicia gateway TLS, PostgreSQL e consumidores no mesmo projeto Compose. Foi
validada temporariamente, mas mistura aplicações independentes e dificulta o
compartilhamento com outros produtos.

## Decisão

Adotar a Opção A. O deployment de referência fica em `deploy/self-hosted/` e é
iniciado como projeto Compose `altivy-notes`, contendo exclusivamente `web`,
`bff`, `api` e `worker`. Gateway, PostgreSQL e Keycloak permanecem no Compose
`altivy-identity` do AKB. Migrations executam em container transitório e não
integram a stack permanente.

O produto não publica portas. Web e BFF expõem aliases restritos na rede externa
`identity-global`; API e worker alcançam o banco pela rede `identity-client`.
O gateway compartilhado termina TLS e encaminha `/notes` aos aliases do produto.

Keycloak não é provisionado nem configurado pelo Compose do produto. O operador
fornece um realm dedicado e o issuer TLS alcançável pelo browser e pelos
containers. A API continua aplicando as decisões do Keycloak como PEP.

Build da fonte é permitido para avaliação inicial. A distribuição de releases
continua usando imagens imutáveis, digests, SBOM, scan e proveniência; workflows
de publicação permanecem separados da instalação local. Os workflows que
executavam deploy por SSH em VPS deixam de fazer parte do fluxo ativo.

## Consequências

### Positivas

- o operador controla localização, rede, domínio, secrets e ciclo de vida;
- infraestrutura compartilhada tem ownership e ciclo de vida únicos;
- uma composição documentada permite instalação local e em servidor próprio;
- build e release continuam independentes do ato de executar a stack.

### Negativas e trade-offs

- a aplicação depende da disponibilidade e das redes da stack `altivy-identity`;
- bootstrap das roles PostgreSQL ainda requer procedimento seguro de operador;
- Compose inicial constrói as imagens localmente e exige validar a promoção por
  digest antes de ser considerado fluxo de release;
- primeira inicialização autenticada depende de issuer OIDC alcançável e
  callbacks configurados externamente.

## Segurança e privacidade

- a stack do produto não publica portas no host;
- secrets locais permanecem no arquivo externo aprovado do operador ou em
  mecanismo equivalente, nunca no repositório;
- o vault e as chaves do BFF usam bind mounts do produto; PostgreSQL pertence à
  plataforma compartilhada;
- o PostgreSQL não publica porta; API, worker e BFF também não;
- não são adicionados Keycloak, usuários, senha local nem política de autorização
  ao domínio do Altivy;
- TLS encerra no gateway compartilhado; forwarded headers são aceitos somente
  do IP descoberto na rede `identity-global`.

## Dados, compatibilidade e migração

Não há mudança de schema causada pela topologia. A aplicação continua aplicando
migrations por container dedicado com role `altivy_migrator`; API e worker usam
a role `altivy_runtime`. O vault permanece em arquivos Markdown e anexos sob um
bind mount. PostgreSQL e vault precisam ser copiados e restaurados como um
conjunto operacionalmente consistente.

## Observabilidade e operação

O Compose do produto define health check da API, políticas de restart e shutdown.
Health de PostgreSQL, Keycloak e gateway pertence ao AKB. Antes de declarar uma instalação pronta para produção, o plano exige
smoke OIDC real, backup do vault e PostgreSQL, restore em ambiente limpo,
upgrade compatível e rollback de aplicação.

## Validação

- `docker compose config --quiet` com valores de exemplo, sem iniciar serviços;
- teste de inicialização limpa, health/readiness e persistência após recriação;
- login/logout e autorização com Keycloak real;
- teste de backup/restore e upgrade/rollback;
- inspeção da stack para confirmar que nenhum serviço do produto publica porta.

## Rollout e rollback

O operador inicia e valida primeiro `altivy-identity`, configura callbacks,
certificados internos, paths, secrets, SMTP e roles do banco. Depois aplica
migrations por container transitório e inicia `altivy-notes`. Rollback restaura as imagens/configuração
anteriores, sem downgrade destrutivo de schema. O operador mantém backups do
vault e PostgreSQL antes da promoção de versões.

## Referências

- [Plano self-hosted](../plan/deploy-self-hosted-compose.md)
- [Plano da VPS supersedido](../plan/deploy-producao-vps.md)
- [Compose self-hosted](../../deploy/self-hosted/compose.yaml)
- [Plataforma compartilhada](https://github.com/altivy-it/architecture-knowledge-base/blob/main/platforms/identity/README.md)

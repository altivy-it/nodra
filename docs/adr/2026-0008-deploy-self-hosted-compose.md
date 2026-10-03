# ADR 2026-0008: Distribuição self-hosted por Docker Compose

**Status:** accepted  
**Data:** 2026-10-02  
**Responsáveis:** Lincoln Zocateli  
**Origem:** decisão humana registrada na conversa de 2026-10-02  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano de deploy self-hosted](../plan/deploy-self-hosted-compose.md), [ADR 2026-0007 supersedido](2026-0007-deploy-altivy-vps-compartilhada.md), [plano mestre](../plano-mestre.md)

## Contexto

O Altivy Notes é um produto self-hosted distribuído por containers. A topologia
anterior planejava publicar o serviço em uma VPS específica e depender de
NGINX, PostgreSQL e Keycloak compartilhados. Essa decisão não atende à direção
humana atual: o operador deve iniciar a stack no host sob seu controle.

A aplicação já tem containers para web, BFF, API, worker e migrations. O plano
mestre exige Docker Compose, PostgreSQL persistente, vault em bind mount e
Keycloak como autoridade externa de identidade e autorização. A stack E2E atual
não é reutilizável como distribuição porque depende da rede global de identidade
e do gateway de laboratório.

## Forças de decisão

- instalação reproduzível em Docker Engine ou Docker Desktop;
- persistência explícita de vault, PostgreSQL e chaves de Data Protection;
- somente a borda HTTPS é publicada no host;
- nenhum secret ou arquivo `.env` real é versionado;
- Keycloak e políticas de autorização continuam sob controle do operador;
- instalações locais, corporativas e isoladas usam as mesmas imagens e variam
  somente configuração.

## Opções consideradas

### Opção A: VPS dedicada com serviços compartilhados

Reduz duplicação de infraestrutura, mas fixa domínio, redes e paths da aplicação
a uma VPS operada pelo projeto e cria workflows remotos com acesso SSH. Foi
substituída pela decisão humana registrada neste ADR.

### Opção B: Stack Compose self-hosted no host do operador

Inicia gateway TLS, web, BFF, API, worker e PostgreSQL em redes locais. O
operador fornece endpoint Keycloak, certificados, secrets, storage e configuração
SMTP. Evita dependência de serviços compartilhados e atende à distribuição
self-hosted.

## Decisão

Adotar a Opção B. O deployment de referência fica em `deploy/self-hosted/` e é
iniciado pelo operador com Docker Compose. A stack inclui PostgreSQL com a
imagem versionada definida no plano mestre e um gateway NGINX sem privilégios;
web, BFF, API, worker, PostgreSQL e migrations não publicam portas no host.

O gateway publica HTTPS, com bind em `127.0.0.1` como default conservador. O
operador pode escolher outro endereço de bind e hostname, desde que o certificado
TLS e os callbacks OIDC correspondam exatamente à origem publicada. O BFF
confia somente no gateway da rede isolada do proxy.

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
- não há dependência de VPS, DNS, NGINX ou PostgreSQL compartilhados;
- uma composição documentada permite instalação local e em servidor próprio;
- build e release continuam independentes do ato de executar a stack.

### Negativas e trade-offs

- cada operador provisiona e mantém Keycloak, certificados, SMTP, storage e
  backups;
- bootstrap das roles PostgreSQL ainda requer procedimento seguro de operador;
- Compose inicial constrói as imagens localmente e exige validar a promoção por
  digest antes de ser considerado fluxo de release;
- primeira inicialização autenticada depende de issuer OIDC alcançável e
  callbacks configurados externamente.

## Segurança e privacidade

- a porta pública padrão é somente HTTPS do gateway e está limitada a loopback;
- secrets locais permanecem no arquivo externo aprovado do operador ou em
  mecanismo equivalente, nunca no repositório;
- o vault, os dados PostgreSQL e as chaves do BFF usam bind mounts persistentes;
- o PostgreSQL não publica porta; API, worker e BFF também não;
- não são adicionados Keycloak, usuários, senha local nem política de autorização
  ao domínio do Altivy;
- TLS encerra no gateway; forwarded headers são aceitos somente do gateway
  confiável.

## Dados, compatibilidade e migração

Não há mudança de schema causada pela topologia. A aplicação continua aplicando
migrations por container dedicado com role `altivy_migrator`; API e worker usam
a role `altivy_runtime`. O vault permanece em arquivos Markdown e anexos sob um
bind mount. PostgreSQL e vault precisam ser copiados e restaurados como um
conjunto operacionalmente consistente.

## Observabilidade e operação

O Compose define health check de PostgreSQL, API e gateway, políticas de restart
e shutdown. Antes de declarar uma instalação pronta para produção, o plano exige
smoke OIDC real, backup do vault e PostgreSQL, restore em ambiente limpo,
upgrade compatível e rollback de aplicação.

## Validação

- `docker compose config --quiet` com valores de exemplo, sem iniciar serviços;
- teste de inicialização limpa, health/readiness e persistência após recriação;
- login/logout e autorização com Keycloak real;
- teste de backup/restore e upgrade/rollback;
- inspeção da rede para confirmar que somente o gateway publica porta.

## Rollout e rollback

O operador configura primeiro endpoint Keycloak, callbacks, certificado TLS,
paths, secrets, SMTP e roles do banco. Depois aplica migrations pelo perfil
`operations` e inicia a aplicação. Rollback restaura as imagens/configuração
anteriores, sem downgrade destrutivo de schema. O operador mantém backups do
vault e PostgreSQL antes da promoção de versões.

## Referências

- [Plano self-hosted](../plan/deploy-self-hosted-compose.md)
- [Plano da VPS supersedido](../plan/deploy-producao-vps.md)
- [Compose self-hosted](../../deploy/self-hosted/compose.yaml)
- [Imagem NGINX unprivileged](https://github.com/nginx/docker-nginx-unprivileged), licença Apache-2.0

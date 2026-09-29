# Plano: Deploy de produção em VPS

**Status:** in-progress  
**Data de criação:** 2026-09-29  
**Última atualização:** 2026-09-29  
**Responsáveis:** Lincoln Zocateli  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [ADR 2026-0007](../adr/2026-0007-deploy-altivy-vps-compartilhada.md), [CI/CD e segurança](../ci-cd-seguranca.md)

## Objetivo

Preparar release SemVer, geração automática de changelog e deploy independente
de frontend/backend para `https://notes.altivy.it` em VPS dedicada, sem recriar
ou reiniciar o NGINX, PostgreSQL ou Keycloak compartilhados.

## Contexto e premissas

- `notes.altivy.it` é o ambiente de produção; não está sendo criado um ambiente
  de homologação separado neste plano.
- O Keycloak compartilhado permanece acessível em
  `https://credential.altivy.it`; o realm `altivy` e os callbacks são
  configurados pelo operador.
- O host possui PostgreSQL e NGINX em containers com bind mounts.
- O NGINX usa `/userapps/ssl/altivy.it.pem` e
  `/userapps/ssl/altivy.it.key` e carregará a configuração da pasta
  `/userapps/configs/nginx/altivynotes`.
- Compose/scripts do produto ficam em `/userapps/altivynotes`; configuração
  externa fica em `/userapps/configs/altivynotes`; dados persistentes ficam em
  `/userapps/var/altivynotes`.
- O banco/roles são provisionados pelo operador. Workflow PostgreSQL entrega
  SQL, mas não o executa. Migrations EF Core são aplicadas por container
  dedicado com role de migration.
- A configuração de GitHub App, Environment, secrets/variables, DNS, networks,
  containers compartilhados e permissões do host não é criada pelo código.

## Não escopo

- criar ou atualizar NGINX, PostgreSQL ou Keycloak compartilhados;
- alterar DNS, emitir certificados ou publicar chaves TLS;
- criar credenciais do Keycloak, do PostgreSQL ou da VPS;
- executar deploy real ou publicar imagem durante a implementação deste plano;
- substituir as etapas de homologação/validação da interface por um deploy de
  produção.

## Dependências e decisões

- decisão humana registrada em 2026-09-29: VPS dedicada, produção em
  `notes.altivy.it`, Keycloak em `credential.altivy.it`, PostgreSQL/NGINX
  compartilhados e deploy separado por frontend/backend;
- [ADR 2026-0007](../adr/2026-0007-deploy-altivy-vps-compartilhada.md), ainda
  `proposed` até aprovação formal do registro arquitetural;
- GitHub App instalada no repositório com apenas `Contents: write` e
  `Pull requests: write` para criação de release PRs;
- Environment `production` protegido e pacote GHCR acessível ao
  `GITHUB_TOKEN`;
- VPS x86-64 por padrão ou variável `ALTIVY_VPS_PLATFORM` ajustada à arquitetura
  real;
- conta SSH dedicada, host key fixada, Docker Compose v2 e redes externas
  NGINX/PostgreSQL já operacionais;
- arquivo de configuração compartilhada NGINX carrega a pasta
  `altivynotes` e monta os certificados nos paths esperados.

## Etapas

### P-01: Registrar topologia e limites operacionais

**Status:** completed  
**Responsável:** Lincoln Zocateli  
**Dependências:** nenhuma

**Objetivo:** fixar domínio, caminhos do host, separação de workflows e
responsabilidade pelos serviços compartilhados.

Entregas:

- decisão de produção/VPS e DNS registrada;
- escopo de alteração NGINX/PostgreSQL/Keycloak delimitado;
- estratégia de deploy independente aprovada.

Validação:

- correspondência entre decisão humana, ADR e runbook.

Evidências:

- pedido e aprovação humana de 2026-09-29 registrados neste plano;
- ADR criado como `proposed`, sem presumir sua aceitação formal.

### P-02: Automatizar CI, SemVer e artefatos

**Status:** in-progress  
**Responsável:** equipe Altivy  
**Dependências:** P-01

**Objetivo:** bloquear release sem gates e produzir changelog/imagens
reproduzíveis por digest.

Entregas:

- testes e builds containerizados no CI;
- Release Please para PR de versão, tag SemVer e changelog;
- imagens web, API, BFF, worker e migrations no GHCR;
- SBOM CycloneDX, scanner Trivy e proveniência BuildKit;
- tag candidate reutilizável por commit, aliases SemVer somente após todos os
  scans e retry manual validando release/ancestry/CI.

Validação:

- `actionlint` sem diagnósticos;
- workflows CI/release executados no GitHub;
- pacote de release contém `images.env`, checksum e SBOMs.

Evidências:

- workflows executam `actionlint` sem erros;
- testes Angular 99/99 e build de produção passam;
- Domain 26/26, BFF 87/87 e Worker 2/2 passam;
- CI backend ainda é bloqueado por falha em `ApiContractTests` (espera 500,
  recebe 503), implementação faltante em `MovingFolderStore` e analyzer
  `CA1305` em `NoteProjectionBuilderTests`; nenhuma dessas falhas foi
  mascarada ou alterada neste plano;
- CI documental passa nos 7 cenários sintéticos; a varredura completa encontra
  uma referência local inválida preexistente para
  `architecture-knowledge-base/platforms/identity/README.md` em
  `backend/libs/infrastructure/data/README.md`;
- execução dos workflows no GitHub ainda pendente.

### P-03: Implementar deploy da aplicação

**Status:** in-progress  
**Responsável:** equipe Altivy  
**Dependências:** P-02

**Objetivo:** promover frontend, backend ou ambos por digest, com migrations
controladas, readiness, smoke test e rollback de serviço.

Entregas:

- Compose de produção sem PostgreSQL próprio;
- operador remoto restrito ao diretório do produto;
- primeiro deploy exige `all`; deploys seguintes e rollback operam por
  componente via Environment `production`.

Validação:

- Compose resolvido com as redes/arquivos externos do servidor;
- deploy frontend-only e backend-only exercitados em ambiente controlado;
- rollback de aplicação e smoke HTTPS comprovados.

Evidências:

- Dockerfile de migrations construído em container;
- build E2E mantém `base-href /notes/` e build de produção usa `/`;
- Compose de produção resolve sintaticamente com perfil `operations`;
- deploy remoto agora exige backup lógico de `altivy` e arquivo compactado do
  vault antes de executar migrations; backups falhos interrompem a promoção;
- teste remoto ainda pendente.

### P-04: Entregar arquivos da infraestrutura compartilhada

**Status:** in-progress  
**Responsável:** equipe Altivy  
**Dependências:** P-01

**Objetivo:** instalar somente o virtual host Altivy no NGINX e entregar o
bootstrap SQL sem alterar PostgreSQL compartilhado.

Entregas:

- workflow manual independente com targets NGINX/PostgreSQL;
- `nginx -t`, verificação de include e reload, com rollback;
- SQL versionado entregue em `/userapps/altivynotes/postgres/` sem execução.

Validação:

- target NGINX aplica e reverte apenas `notes.altivy.it.conf`;
- target PostgreSQL não executa SQL nem reinicia o container.

Evidências:

- `nginx -t` passou em container com a configuração atual;
- certificado do NGINX é verificado para o SAN `notes.altivy.it` antes do reload;
- execução na VPS ainda pendente.

### P-05: Provisionar e validar primeiro deploy

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-02, P-03, P-04

**Objetivo:** configurar o host, GitHub, redes, database/roles, realm e
arquivos externos; executar deploy real e comprovar recuperação.

Entregas:

- secrets/variables de `production` configurados fora do repositório;
- redes Docker e aliases confirmados;
- database `altivy`, roles e connection strings provisionados;
- callbacks OIDC de `notes.altivy.it` cadastrados no realm compartilhado;
- primeiro deploy, backup e teste de restore registrados.

Validação:

- `https://notes.altivy.it/bff/session` retorna sessão anônima válida;
- login/logout OIDC e autorização de vault funcionam;
- restore e rollback de imagem/configuração foram testados.

Evidências:

- pendente; esta execução não acessou a VPS nem configurou secrets.

## Critérios de aceite finais

- PR de release gera changelog automático e tag SemVer após checks verdes e
  merge humano;
- imagens publicadas possuem digest, SBOM, scan e proveniência;
- frontend/backend podem ser promovidos separadamente por GitHub Environment;
- NGINX e PostgreSQL recebem apenas seus arquivos/configurações atribuídos;
- migrations usam role dedicada e rollback não executa downgrade destrutivo;
- serviço responde por HTTPS e há evidência de backup/restore e rollback.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| SSH user com acesso Docker equivale a root | impacto nos serviços compartilhados | conta dedicada, chave exclusiva, Environment protegido, host key pinning e sem encaminhamento SSH |
| NGINX não inclui a pasta nova ou não vê TLS | deploy incompleto | workflow falha antes do reload; preflight e validação de include/certificados |
| migration incompatível | indisponibilidade ao voltar imagem | expand-contract, backup e nenhum downgrade automático |
| DNS/Keycloak/network sem alcance | login ou readiness falha | preflight de DNS/TLS e smoke após deploy |
| tag/release publicada sem CI | artefato não validado | release job só após workflow CI verde; proteção de `main`/tags necessária |

## Rollout e rollback

Aplicar virtual host, entregar SQL, provisionar database/roles e atualizar
callbacks OIDC antes do deploy. Backend aplica migrations antes de iniciar a
versão nova. Smoke test falho restaura o manifesto e Compose anteriores; a
restauração do PostgreSQL é procedimento separado. NGINX mantém um backup de
configuração e é recarregado somente após `nginx -t` e confirmação de include.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-09-29 | plano | - | proposed | criado a partir do requisito de deploy em VPS | IA assistida |
| 2026-09-29 | plano | proposed | approved | aprovação humana explícita para implementar a arquitetura descrita | Lincoln Zocateli |
| 2026-09-29 | plano | approved | in-progress | implementação e validação local iniciadas; deploy remoto permanece pendente | IA assistida |
| 2026-09-29 | P-01 | not-started | completed | topologia e limites definidos pelo responsável no pedido | Lincoln Zocateli |

## Regras de atualização

- não declarar o plano concluído até P-05 ter evidências de execução real;
- não guardar secrets, connection strings, chaves TLS ou arquivos `.env` no Git;
- tratar `deploy/plan/ambiente-homologacao.md` como plano separado: produção
  não cria homologação isolada nem satisfaz seus gates.
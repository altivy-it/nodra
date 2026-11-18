# ADR 2026-0009: Padronização de CI/CD com Altivy.Groundwork

**Status:** accepted
**Data:** 2026-11-11
**Responsáveis:** Administração da Altivy
**Origem:** IA assistida, no fluxo speckit-plan e na tarefa T003
**Revisor humano:** Administradores da Altivy
**Registro do aceite:** aprovação explícita do usuário nesta conversa em 2026-11-11: "ADR aprovado, siga coma speckit".
**Relacionado:** [feature 011](../../specs/011-padronizacao-cicd/spec.md), [plano](../../specs/011-padronizacao-cicd/plan.md), [tarefa T003](../../specs/011-padronizacao-cicd/tasks.md), PR 31 e [ADR 2026-0008](2026-0008-deploy-self-hosted-compose.md)

## Contexto

O usuário solicitou alinhar os workflows e os ambientes GitHub do Nodra ao
comportamento do Dokpod. A especificação foi aprovada para elaboração de plano
e tarefas, e o namespace `ghcr.io/altivy-it/nodra-*` foi escolhido explicitamente.
Essas autorizações não constituem aceite deste ADR nem aprovação de execução
do plano, publicação, distribuição ou deploy.

A referência funcional é `altivy-it/dokpod`, commit
`01fe5b0c526742edb420541cdd9a0b4e9fa2e74e`, identificado como AGPL-3.0.
Copiar seus workflows ou scripts é incompatível com a política de dependências
do Nodra. A [pesquisa da feature](../../specs/011-padronizacao-cicd/research.md)
registra a consulta autenticada pela API GitHub ao catálogo canônico separado,
`altivy-it/altivy-groundwork`, privado, na tag `v0.11.3`, SHA
`c848afb4fa0b43cb9227abd2c347ab4ecccd6296`, cujo `LICENSE` foi verificado como
MIT. O manifesto do Dokpod confirma `toolVersion` e `catalogVersion` `0.11.3`.
Esta redação referencia a evidência registrada; não afirma nova consulta remota.

A decisão é transversal: define a origem dos componentes gerenciados, os limites
entre CI, release e operação, e o contrato de promoção de artefatos self-hosted.
O [ADR 0008](2026-0008-deploy-self-hosted-compose.md) mantém Compose e
infraestrutura compartilhada separados e retirou workflows de deploy SSH do
fluxo ativo. Este ADR propõe um contrato opcional de deploy protegido, não sua
reativação. Qualquer aceite deverá explicitar essa evolução, preservando a
topologia e o controle do operador definidos no ADR 0008.

A feature parte de `862ef1d`, head do PR 31 ainda aberto; `origin/main` está em
`fb29ed7`, conforme a pesquisa. Integração exige reconciliação explícita dessa
base, preservando os ajustes de peers da feature 010. Trocar workflows não
resolve por si só o diagnóstico DNS nem comprova homologação operacional.

## Forças de decisão

- Reproduzir comportamento e proteções, não apenas nomes de workflows/ambientes.
- CI confiável sem secrets, inclusive forks e Dependabot, com um único `Result`.
- Revisão humana de Release PR, candidata em `quality` e gates de publicação,
  distribuição e deploy separados.
- Rastreabilidade entre fonte, versão, digests, manifesto, SBOM, scan e atestações,
  sem sobrescrita de versões ou recompilação silenciosa de artefatos aprovados.
- Preservar dados, identidade, configuração externa e operação em containers.
- Reduzir drift sem incorporar código AGPL, infraestrutura ou capacidades alheias
  ao Nodra; manter reversibilidade e falhar fechado quando faltar suporte.

## Opções consideradas

### Opção A: copiar os workflows do Dokpod

Ofereceria proximidade imediata com a referência, mas incorporaria código
AGPL-3.0 e pressupostos de outro produto. É proibida pela política de licenças
do Nodra e foi descartada; Dokpod serve somente como referência funcional.

### Opção B: manter e evoluir um pipeline próprio

Preserva autonomia e evita o acesso ao catálogo privado, mas duplica lógica de
promoção, revisão, políticas e segurança. Aumenta manutenção e drift em relação
ao padrão solicitado. É reversível, porém menos simples para manter equivalência.

### Opção C: adotar o catálogo canônico MIT com sementes Nodra

Reutiliza componentes da origem permissiva fixada e mantém as particularidades
do produto em sementes adaptáveis. Reduz duplicação, mas exige acesso privado,
verificação de dependências, disciplina sobre arquivos gerenciados e adaptação
dos consumidores existentes. É a recomendação condicionada aos gates abaixo.

## Decisão

**Opção C aceita pela administração da Altivy**, mediante a aprovação explícita
registrada acima. A continuidade pelo Spec Kit autoriza a implementação local
do plano e das tarefas da feature 011, preservando todos os gates descritos.
Não autoriza publicação, deploy, cadastro de secrets ou alteração implícita de
proteções remotas; o dry-run dessas políticas permanece sujeito a revisão.

O escopo proposto limita-se aos oito componentes do Altivy.Groundwork `0.11.3`:
`ci`, `release`, `release-preview`, `publish-containers`,
`publish-github-assets`, `deploy`, `dependency-updates` e `repository-policy`.
Não selecionar `core`, `speckit`, `secret-scanning` ou `--all`. Verificar também
as licenças das dependências da ferramenta antes de executá-la e preservar os
avisos legais aplicáveis. Licença MIT do catálogo não valida automaticamente
todas as suas dependências.

Arquivos `gw-*` e scripts gerenciados pertencem ao catálogo; sementes de CI,
publicação, pós-release, deploy e configuração pertencem ao Nodra. A geração
deve usar a ferramenta oficial fixada, em container, com manifesto e hashes
reais. Conflitos `.groundwork-new` exigem reconciliação e `resolve` oficial,
sem editar hashes ou sobrescrever customizações. Preservar constituição, specs,
overrides, hooks e `secrets_dir_name=Altivy.Notes`.

O [contrato da feature](../../specs/011-padronizacao-cicd/contracts/cicd.md)
delimita o fluxo:

- CI em `pull_request`, `merge_group`, `push` em `main` e `workflow_dispatch`,
  sem secrets, publicação ou deploy. Todo gate obrigatório alimenta `Result`;
  falha, cancelamento ou omissão indevida não pode produzir sucesso agregado.
- Concorrência preserva o check obrigatório de PR/merge group e serializa
  publicação e deploy no mesmo destino, sem cancelar uma release em andamento.
- Release PR mantém revisão humana; candidata passa por `quality` antes da
  divulgação final autorizada. Aprovações de release/distribuição não autorizam
  deploy automaticamente. **Auto-deploy permanece DESATIVADO.**
- Imagens futuras usam exclusivamente `ghcr.io/altivy-it/nodra-api`,
  `ghcr.io/altivy-it/nodra-bff`, `ghcr.io/altivy-it/nodra-worker`,
  `ghcr.io/altivy-it/nodra-web` e `ghcr.io/altivy-it/nodra-migrations`.
  A escolha do namespace não publica, move ou sobrescreve imagens existentes.
- O manifesto associa versão/tag e SHA fonte aos artefatos identificados por
  `kind`, `name`, `reference` e digest `sha256`, com SBOM, proveniência e
  atestações verificáveis. Ausência ou falha da evidência exigida bloqueia a
  promoção; não se presume suporte nem se reduz o gate.
- Reexecução é idempotente: não sobrescreve versão/digest, não troca o manifesto
  aprovado nem reconstrói silenciosamente o artefato. Modos `preflight` e
  `publish_only` sem implementação testada devem ser recusados explicitamente.
- Deploy aceita somente release publicada, origem permitida e manifesto
  validado; não aceita imagem/digest livre. Jobs que atuam no destino usam o
  ambiente protegido correspondente. Configuração ausente é recusada antes de
  acessar o host.

As políticas propostas são:

| Ambiente | Papel/origem | Aprovação |
| --- | --- | --- |
| `ci` | Eventos de CI; sem secrets de publicação/produção | Sem revisor obrigatório |
| `copilot` | Automação reservada; sem secrets de deploy | Sem revisor obrigatório |
| `quality` | Candidata; tags `v*` | Administradores da Altivy |
| `release` | Preparação/publicação autorizada; `main` e tags `v*` | Administradores da Altivy |
| `distribution` | Divulgação autorizada; `main` e tags `v*` | Administradores da Altivy |
| `production` | Deploy explicitamente autorizado; tags `v*` | Administradores da Altivy |

A grafia real do ambiente é `copilot`. Nos quatro ambientes protegidos,
`prevent_self_review=false` reproduz a referência: o revisor pode aprovar a
própria execução. Isso não equivale a revisão independente por duas pessoas;
alterar esse parâmetro ou o revisor exige decisão explícita. Política remota
somente pode ser aplicada após dry-run e aprovação humana do diff. Não remover
nem substituir regras legadas implicitamente, inclusive por `--replace-legacy`,
e não reduzir checks ou regras de assinatura para desbloquear integração.

Por solicitação posterior do usuário, a responsabilidade deixa de ser pessoal
e passa à administração da Altivy. A referência do Dokpod para administradores
nos rulesets é `RepositoryRole` com `actor_id=5`; seus aprovadores de Environment
ainda são individuais. Esse papel não é aceito como required reviewer de
Environment nem como entrada de CODEOWNERS. Identificar e validar a equipe
administrativa real, seu ID e suas permissões antes de migrar essas proteções.
Se a equipe não estiver disponível, manter a operação bloqueada, sem um
identificador fictício, fallback pessoal ou remoção da aprovação.

### Identidade das contribuições

As contribuições desta manutenção e da automação configurada devem usar autor
e committer `lunadev00 <luna@altivy.it>`. Autoria não confere autoridade de
aprovação e não representa assinatura criptográfica GPG/SSH. Não reescrever
commits publicados, reatribuir trabalho de terceiros ou suprimir avisos legais
obrigatórios. Referências técnicas reais de fornecedores não devem ser trocadas
por endereços inexistentes para remover um nome da apresentação do projeto.

## Consequências

### Positivas

- Origem MIT fixada e separação entre catálogo e sementes tornam a manutenção
  auditável, com menor duplicação e drift.
- Release e deploy passam a ter contratos verificáveis, aprovações explícitas
  e rastreabilidade de artefatos imutáveis.
- A distribuição continua self-hosted; nenhum novo serviço de runtime ou
  dependência obrigatória de nuvem é introduzido no núcleo da aplicação.

### Negativas e trade-offs

- Catálogo privado cria dependência operacional de acesso à API/CLI ou pacote;
  a evidência de acesso via API não comprova instalação pela CLI.
- Plano GitHub, permissões de App, atestações e revisores podem impedir a
  equivalência. Nesses casos a operação fica bloqueada, não parcialmente aprovada.
- Sementes e consumidores de distribuição precisam de adaptação e testes; não
  basta instalar componentes para obter paridade ou homologação OIDC.
- O hook externo exige manutenção pelo operador. O aceite deste ADR não fornece
  credenciais, instala o hook nem torna ambientes prontos para deploy.
- A dependência do PR 31 permanece; integração prematura pode perder correções
  de peers ou produzir evidência sobre uma base diferente da efetivamente usada.

## Segurança e privacidade

Permissões mínimas por job, actions fixadas por SHA e isolamento entre código
não confiável e credenciais são obrigatórios. Não executar código de PR com
secrets via `pull_request_target`. Runners, builds e testes rodam em containers;
testes HTTP acessam exclusivamente nginx também em container.

O inventário GitHub registra apenas nomes, escopos e presença, nunca valores:

| Nome | Tipo e escopo propostos | Finalidade |
| --- | --- | --- |
| `RELEASE_APP_ID` | Variável do repositório | Identificar o App da automação de release |
| `RELEASE_APP_PRIVATE_KEY` | Secret do repositório | Autenticar o App com privilégio mínimo |
| `DEPLOY_SSH_TARGET` | Variável de cada ambiente `quality`/`production` | Destino autorizado |
| `DEPLOY_HOOK` | Variável de cada ambiente `quality`/`production` | Hook externo autorizado |
| `DEPLOY_SSH_KEY` | Secret de cada ambiente `quality`/`production` | Acesso SSH do respectivo destino |
| `DEPLOY_SSH_KNOWN_HOSTS` | Secret de cada ambiente `quality`/`production` | Verificação da identidade SSH do host |

A pesquisa não encontrou cadastros diretos dessas configurações no repositório
ou nos dois ambientes; herança da organização não foi confirmada. O operador
precisa confirmar disponibilidade e permissões efetivas, sem fornecer valores
ao assistente. Orientar a transição do nome legado `RELEASE_APP_CLIENT_ID` sem
sobrescrita ou exclusão automática. Não copiar credenciais do Dokpod, criar
chaves ou publicar para testar acesso. Secrets nunca entram em logs, artifacts,
imagens, chat ou arquivos no repositório; o arquivo externo existente não é lido
nem alterado por esta decisão.

## Dados, compatibilidade e migração

Não há migração de dados ou schema. Markdown permanece canônico; PostgreSQL é
projeção reconstruível. Preservar vaults, bind mounts, paths externos, variáveis
`ALTIVY_*`, `AltivyPostgres`, schema `altivy`, realm/clientes Keycloak, key ring
e a responsabilidade externa por identidade e autorização.

Os impactos ficam em automação/configuração de `.github`, scripts de suporte
e consumidores de distribuição em `deploy`, com documentação correspondente.
Backend/frontend preservam comportamento e versões de dependências; somente
metadados de versão já previstos integram a preparação de releases.

Consumidores ativos de `images.env` precisam de adaptação explícita ao manifesto:
o adaptador deve usar somente referências/digests previamente validados,
preservar o contrato necessário aos consumidores legados e recusar divergências,
sem fallback para tags livres ou resolução mutável. Não tratar um `images.env`
isolado como prova de release validada. A adaptação de namespace e formato é
versionada e testada, sem migrar dados, imagens ou configuração de hosts remotos.

`DEPLOY_HOOK` define uma fronteira externa com comandos `apply`/`check`.
O adaptador Compose recebe o manifesto validado, aplica somente as imagens
autorizadas e verifica versão e saúde pela borda nginx. O operador configura
e instala esse hook separadamente; não se reutiliza host/hook Dokpod, não se
instala hook implicitamente e não se provisiona infraestrutura compartilhada.

## Observabilidade e operação

Registrar, sem conteúdo sensível, origem/SHA, versão, digest do manifesto e das
imagens, execução, ambiente, aprovações e resultados dos gates e do smoke.
Artifacts têm retenção explícita e excluem vaults, dumps e arquivos de secrets.
Manter evidências de status/hashes gerenciados, diferenças do dry-run e políticas
efetivas consultadas após eventual aplicação autorizada. Falhas indicam o gate
e a configuração ausente, sem expor credenciais.

Nenhuma imagem foi publicada ou implantação executada por esta atividade.
Validação documental não comprova disponibilidade do host, OIDC, upgrade,
backup/restore ou prontidão para produção.

## Validação

A hipótese é que catálogo MIT fixado mais sementes Nodra alinham o processo sem
copiar AGPL, enfraquecer controles ou alterar instalações. Os seguintes critérios
devem poder refutá-la nas tarefas oficiais, com execução em containers:

- Status e verificação de arquivos gerenciados, actionlint e testes dos contratos
  devem passar sem editar hashes ou introduzir pipeline duplicado.
- CI sem secrets deve cobrir PR/fork/Dependabot e merge group; simular falha,
  cancelamento e omissão indevida de cada gate deve impedir sucesso de `Result`.
- Guards devem rejeitar falta de aprovação, origem/ambiente não permitido,
  manifesto adulterado, digest divergente, atestação inválida e configuração
  ausente antes de acessar o destino. Nenhum teste pode publicar ou fazer deploy.
- Simulações devem provar idempotência e recusa de sobrescrita/rebuild silencioso,
  além da compatibilidade do adaptador `images.env`/manifesto com Compose.
- Dry-run revisado deve corresponder aos seis ambientes, revisores, origens e
  `prevent_self_review=false`, sem remoção implícita de regras legadas. Limites
  do plano GitHub ou permissões impedem declarar paridade e bloqueiam a operação.
- Inventário por nomes deve identificar lacunas sem ler valores; revisão deve
  confirmar preservação de dados, identidade, configuração externa e Spec Kit.

O aceite humano deste ADR e a continuidade do plano/tarefas foram registrados
na aprovação acima. Permanecem pendentes a verificação das dependências e o
acesso CLI ao catálogo, a capacidade efetiva de
proteções/atestações, configuração/herança GitHub e reconciliação do PR 31.
Esses gates não foram executados pela redação do documento. Se a adoção exigir
licença proibida, perda de integridade ou relaxamento dos gates, interromper e
reavaliar a recomendação, sem aprovar alternativa por inferência.

## Rollout e rollback

Este ADR é insumo de decisão, não um plano paralelo: a execução pertence
exclusivamente às [tarefas da feature](../../specs/011-padronizacao-cicd/tasks.md).
Aceite humano e revisão antecedem as alterações dependentes; substitutos devem
ter equivalência validada antes da retirada de workflows legados. Sincronização
remota exige aprovação do dry-run, sem autorizar publicação ou deploy.

Se uma adoção futura falhar, reverter somente as alterações de automação e
consumidores introduzidas pela feature, preservando trabalho alheio, dados e
configuração externa. Políticas remotas exigem diff inverso e autorização
específica, sem exclusão automática de ambientes/regras. Não restaurar um fluxo
legado que contorne os gates: manter publicação/deploy bloqueados até correção.
Artefatos já publicados nunca são reescritos. Rollback de aplicação, se um deploy
vier a ser autorizado em outra atividade, seleciona manifesto anterior validado
e compatível com o schema, sem downgrade destrutivo nem restauração implícita.

## Referências

- [Plano mestre](../plano-mestre.md)
- [ADR 0008: distribuição self-hosted por Docker Compose](2026-0008-deploy-self-hosted-compose.md)
- [Especificação aprovada para planejamento](../../specs/011-padronizacao-cicd/spec.md)
- [Plano em revisão humana](../../specs/011-padronizacao-cicd/plan.md)
- [Pesquisa: evidências de licença, catálogo, ambientes e base Git](../../specs/011-padronizacao-cicd/research.md)
- [Modelo de dados operacionais](../../specs/011-padronizacao-cicd/data-model.md)
- [Contratos de CI/CD](../../specs/011-padronizacao-cicd/contracts/cicd.md)
- [Tarefas oficiais, incluindo T003](../../specs/011-padronizacao-cicd/tasks.md)
- [Template de ADR](../../.github/ADR_TEMPLATE.md)
# Guia de Validacao e Configuracao GitHub

## Estado Inicial

Feature local 011 criada sobre o head 862ef1d do PR 31. Nenhum workflow,
Environment, secret ou variavel foi alterado durante a pesquisa. Duas branches
integradas foram excluidas com leases; main e a branch do PR 31 foram preservadas.

Plano/tarefas e ADR devem ser revisados antes de implementar. A toolchain
Groundwork deve vir da origem MIT v0.11.3 e executar em container. Nao instalar
core/Spec Kit ou executar --all para esta tarefa. Nenhum comando de validacao
deve publicar imagens ou implantar aplicacoes.

**Atualizacao:** ADR aceito e implementacao local autorizada pelo usuario em
2026-11-11: "ADR aprovado, siga coma speckit". As evidencias abaixo nao
representam homologacao remota nem autorizacao de publicacao/deploy.

## O Que Cadastrar no GitHub

Os nomes abaixo sao o contrato planejado; conferir as seeds finais antes de
cadastrar. Nenhum valor sensivel deve ser enviado pelo chat.

| Nome | Tipo | Escopo | Finalidade |
| --- | --- | --- | --- |
| RELEASE_APP_ID | Variable | Repositorio | Client ID ou App ID numerico do GitHub App de release instalado e autorizado no Nodra |
| RELEASE_APP_PRIVATE_KEY | Secret | Repositorio ou organizacao com acesso restrito ao Nodra | Chave privada do mesmo App; nunca incluir no repo, comando literal ou chat |
| ALTIVY_ADMIN_REVIEWER | Variable | Repositorio | Login do reviewer User do Dokpod, validado como owner da Altivy e com acesso ao Nodra |
| DEPLOY_SSH_TARGET | Variable | quality e production | Destino usuario@host revisado do ambiente Nodra |
| DEPLOY_HOOK | Variable | quality e production | Caminho absoluto do hook Nodra revisado que implementa apply/check |
| DEPLOY_SSH_KEY | Secret | quality e production | Chave SSH dedicada, com menor privilegio para o host de cada ambiente |
| DEPLOY_SSH_KNOWN_HOSTS | Secret | quality e production | Fingerprint de host verificado por canal confiavel; nao confiar cegamente em ssh-keyscan |

Os aprovadores de Environment nao exigem secret. O gate de publicacao usa a
variable ALTIVY_ADMIN_REVIEWER com o login do reviewer User ja configurado no
Dokpod e validado como owner da organizacao e com permissao no Nodra. O manifesto
Groundwork local registra o mesmo login nos parametros
`release_reviewers`/`deploy_reviewers`, mas `.github/repository-policy.json`
nao foi regenerado e nao representa a configuracao remota atual. Nao usar
RepositoryRole/actor_id 5 como reviewer.

`RELEASE_APP_CLIENT_ID` e o nome legado encontrado no workflow atual. O novo
contrato usa RELEASE_APP_ID; nao apagar ou renomear valores existentes ate
conferir a transicao. Nas consultas iniciais, nao havia entradas no repositorio
nem nos ambientes quality/production; a consulta posterior da organizacao esta
registrada abaixo. Nao inventar PAT, NUGET_API_KEY ou secrets de producao para
fazer o CI passar; estes jobs usam dados sinteticos e GITHUB_TOKEN de permissoes
limitadas.

O parametro `release_app_id` do manifesto e o App ID numerico usado no ruleset
de tags; nao confundi-lo com o Client ID. O GitHub recomenda o Client ID para
`RELEASE_APP_ID`, mas aceita tambem o App ID numerico como emissor do JWT.
A emissao real do token ainda nao foi verificada com a chave privada.

### Cadastro Seguro

1. Em Settings → Secrets and variables → Actions do Nodra, criar a Variable
   RELEASE_APP_ID com o Client ID ou App ID numerico correto do App. Confirmar
   que o App esta instalado no Nodra e possui apenas as permissoes de
   contents/pull requests necessarias ao fluxo, conforme a configuracao final.
2. Cadastrar RELEASE_APP_PRIVATE_KEY em Secrets, ou conceder ao Nodra acesso
   restrito ao secret da organizacao correspondente. Nao copiar a chave entre
   arquivos nem compartilha-la com o assistente.
3. Em Settings → Environments, conferir ci, copilot, quality, release,
   distribution e production; revisar as protecoes do contrato antes do sync.
4. Para quality/production, cadastrar as duas vars e dois secrets SSH com
   valores do ambiente Nodra, nao os do Dokpod. Se o host/hook nao estiver
   preparado, deixar o deploy bloqueado em vez de usar placeholders operacionais.
5. Caso sejam necessarias permissoes para instalar pacote privado Groundwork,
   configurar acesso fora do modelo pelo terminal seguro e remover credenciais
   temporarias apos o uso. Nao armazenar PAT no repositorio ou NuGet.Config.

## Validacao Local

- Confirmar origem, SHA, licenca MIT e versao exata da ferramenta e dependencias.
- Revisar a aplicacao local dos componentes e .groundwork-new; preservar fontes existentes.
- Executar teste gerenciado, status do catalogo, actionlint e validadores de contratos em containers.
- Verificar CI sem secrets com cenarios de PR/merge_group, falha de job obrigatorio e entradas de release/deploy invalidas.
- Conferir manifests, digests, SBOM, proveniencia e compatibilidade de consumidores sem executar publicacao.
- Executar groundwork github --sync --dry-run, revisar as mutacoes propostas e aplicar somente o aprovado, sem --secrets nem --replace-legacy implicitos.
- Consultar novamente ambientes/politicas apos qualquer sync; erro de permissao ou protecao indisponivel nao significa sucesso.

## Administracao e Autoria

Os ambientes protegidos do Dokpod usam um reviewer do tipo User por ambiente,
com a mesma identidade nos quatro. Em 2026-11-18 a API confirmou essa conta como
owner da organizacao e com permissao admin no Nodra. Usar o mesmo login nos
quatro environments e no gate de publicacao; nao converter RepositoryRole/actor_id
5 em reviewer nem atribuir CODEOWNERS automaticamente.

Para as contribuicoes desta manutencao e da automacao configurada, a identidade
do clone e:

```bash
git config --local user.name lunadev00
git config --local user.email luna@altivy.it
git var GIT_AUTHOR_IDENT
git var GIT_COMMITTER_IDENT
```

A configuracao foi aplicada e os identificadores efetivos foram conferidos.
Nao altera configuracao global, historico, autoria de terceiros ou assinatura
GPG/SSH. Nao criar coautoria ficticia nem substituir avisos legais obrigatorios.

## Gates Remotos

CI remoto em PR pode ser executado apos push autorizado. Os workflows de
release/distribuicao/deploy nao serao disparados nesta validacao. A configuracao
remota nao resolve a falha DNS de BuildKit analisada no PR 31 nem substitui
homologacao OIDC/upgrade/recuperacao da feature 010.

## Limites e Recuperacao

Sem publicacao/deploy, rollback da definicao de pipeline e revisao de arquivos
versionados e politicas aprovadas; nao alterar dados da aplicacao. Remocao de
regras remotas existentes exige revisao especifica. Se uma configuracao estiver
ausente, usar os nomes acima e cadastrar pelo GitHub; nao enviar o valor ao chat.

## Resultados Locais — 2026-11-11

Ferramentas executadas em containers, na raiz do repositorio:

| Verificacao | Resultado |
| --- | --- |
| Groundwork 0.11.3 status | 37 arquivos gerenciados integros |
| Inventario `.github/ci-cd.json` | seis ambientes; quatro com aprovacao administrativa requerida; nomes/escopos de variables e secrets validados, sem valores |
| Test-ManagedFiles com os oito componentes | 37 hashes conferidos |
| actionlint 1.7.12 de todos os workflows | aprovado |
| test-cicd-contracts.ps1 | aprovado: eventos/permissoes, Result verde/failure/cancelled/skipped/vazio, reviewer User correto/ausente/incorreto/misto/equipe sintetica, guard inicial e manifestos invalidos |
| Manifesto para images.env | cinco imagens corretas; origem/atestacao/digest/quantidade/duplicacao invalidos e sobrescrita recusados |
| Scan de release em dry-run | digests remotos exatos, sem rebuild, sem push e sem expor credenciais |
| --help dos tres scripts novos | codigo 0, sem dependencias opcionais nem rede |
| test-validate-repository.ps1 | 10 cenarios aprovados, incluindo fence indentada em lista |
| validate-repository.ps1 | 231 documentos verificados |
| API, BFF, worker, web e migrations | cinco builds locais de candidata 0.1.0-rc.1 concluidos; os cinco labels de versao conferidos |
| Sintaxe/ajuda dos consumidores self-hosted | aprovadas; nenhum arquivo externo de secrets lido |
| Politica github --sync --dry-run | recusada antes da API: distribution exige revisores e lista vazia |

Comandos de validacao (dentro do container SDK
docker.io/lzocateli/dotnet-sdk:10.0.403-noble-v1, com o repositorio em /workspace):

```powershell
pwsh -NoProfile -File tools/scripts/test-cicd-contracts.ps1
pwsh -NoProfile -Command '& ./tools/scripts/ci/Test-ManagedFiles.ps1 -Components @("ci","release","release-preview","publish-containers","publish-github-assets","deploy","dependency-updates","repository-policy")'
pwsh -NoProfile -File tools/scripts/test-validate-repository.ps1
pwsh -NoProfile -File tools/scripts/validate-repository.ps1
```

O teste YAML requer o build Release existente da API/YamlDotNet e PowerShell
7.6/.NET 10; o SDK aprovado fornece esse runtime. O primeiro ajuste do baseline
falhou na fixture e foi corrigido; o resultado final acima e o que foi aprovado.

## Pendencias Impeditivas

1. Disponibilizar `RELEASE_APP_ID` e `RELEASE_APP_PRIVATE_KEY` do App ja
   identificado e cadastrar `ALTIVY_ADMIN_REVIEWER` com o login validado;
   nao solicitar nem registrar valores sensiveis.
2. T018 permanece bloqueada pelo contrato do Groundwork 0.11.3: o template nao
   gera `ci`/`copilot`, inclui fallback `release-pr` e pode configurar `release`
   ou `quality` sem reviewer ao receber HTTP 422. Com
   `preview_acceptance=deploy`, o reviewer de `quality` e gerado, mas nao e
   exigido pelo sincronizador em caso de falha. O manifesto local registra o
   App e os revisores, mas a politica gerenciada nao foi regenerada. Solicitar
   suporte compativel no catalogo antes de alterar arquivos gerenciados; nao
   editar hashes nem sincronizar a politica remota.
3. Reconciliar a documentacao gerenciada en/es com a regra pt-BR na origem,
   preservando hashes e avisos legais. Nao aceitar excecao implicitamente.
4. Executar BuildKit, scanner real e testes remotos de atestacao/imutabilidade
   em validacao autorizada. Builds locais Podman nao comprovam esses gates.
5. Homologar hook/SSH e laboratorio OIDC/upgrade/recuperacao separadamente;
   nenhuma instalacao foi acessada ou modificada nesta feature.
6. Reconciliar PR 31 antes de integrar em main. Em 2026-11-11, commit e push
   da 011 aguardavam autorizacao; veja a atualização abaixo.

## Atualização — normalização do CI em 2026-11-18

Foram autorizados commits e pushes para normalizar as branches, mantendo os
PRs separados e em rascunho. Merge final, publicação e deploy não foram
autorizados.

O workflow Gitleaks legado permanece em paralelo ao fluxo gerenciado para
preservar a varredura de histórico sem rede e as referências dos relatórios
da 010. Ambos executam verificações de segredos; o fluxo gerenciado continua
obrigatório no resultado da CI.

O job `Container Images` usa um Docker Engine 29.9.3 isolado no runner efêmero,
com `containerd-snapshotter` e sem `lazy-pull`. O build conserva `--load`,
`--provenance=mode=max` e SBOM; o gerador
`docker/buildkit-syft-scanner:1.12.1` é fixado pelo digest do índice
multiplataforma. O Trivy continua lendo os arquivos locais, sem socket Docker,
e compartilha um volume de cache descartável entre os scans. O CI valida SBOM e
proveniência na imagem carregada sem imprimir seus corpos.

A prova isolada em [Docker real no GitHub Actions](https://github.com/altivy-it/nodra/actions/runs/44365783571)
gerou as duas atestações de uma imagem sintética e executou os dois scans Trivy
sobre o arquivo exportado, sem publicar imagens. Ela não substitui a execução
do job nas cinco imagens do projeto, tampouco a validação de publicação,
imutabilidade e republish de T033, que permanece pendente.

Licenças verificadas nas versões exatas: a
[setup-docker-action 5.7.0](https://github.com/docker/setup-docker-action/blob/v5.7.0/LICENSE),
o [Docker Engine 29.9.3](https://github.com/moby/moby/blob/docker-v29.9.3/LICENSE)
e o [gerador Syft 1.12.1](https://github.com/docker/buildkit-syft-scanner/blob/v1.12.1/LICENSE)
declaram Apache-2.0. O [Syft 1.51.0](https://github.com/anchore/syft/blob/v1.51.0/LICENSE)
incluído no gerador também declara Apache-2.0.

### Validação local das imagens — 2026-11-18

As cinco imagens foram construídas em formato Docker e analisadas por Trivy
0.72.0: cada uma gerou SBOM CycloneDX e não apresentou achados corrigíveis
`HIGH,CRITICAL` no gate aplicado. O bundle de migrations incorporou runtime
.NET 10.0.15, aplicou 12 migrations com a role dedicada e foi reaplicado sem
alterações em PostgreSQL 18.4 descartável. O worker conectou com a role
runtime; API, BFF e web responderam pela borda nginx, incluindo o fallback da
SPA. As licenças das bases estão registradas em
[CI/CD e segurança](../../docs/ci-cd-seguranca.md).

Esses builds locais com Podman não comprovam as atestações no Docker Engine
do GitHub; veja a validação remota abaixo. Publicação e `republish` continuam
pendentes; T033 não foi concluída.

### Validação remota das imagens — 2026-11-18

O [CI do commit `ff3c72a`](https://github.com/altivy-it/nodra/actions/runs/44376361969)
construiu e escaneou as cinco imagens, verificou SBOM e proveniência
`mode=max` de cada uma e concluiu `Container Images` e `Result` com sucesso.
O artifact `image-security` contém 15 relatórios de build, SBOM e scan.

As verificações dos bugs
[cache do Trivy](../../.specify/bugs/ci-trivy-cache-permissions/test.md) e
[imagens vulneráveis](../../.specify/bugs/ci-imagens-vulneraveis/test.md)
registram o estado local anterior ao envio; a execução acima acrescenta a
evidência do CI real. Ela não valida publicação, imutabilidade ou `republish`.

### T015 — integração dos workflows de release e preview

Os workflows gerenciados `gw-release.yml`, `gw-preview.yml`,
`gw-container-images.yml` e `gw-github-assets.yml` estão alinhados ao manifesto
Groundwork. O workflow legado `release.yml` não está mais versionado. A
configuração declara preview obrigatório para release, aceite por deploy e
`quality` como ambiente; os workflows condicionam publicação à aprovação no
Environment `distribution` e encaminham a candidata para `quality`.

O fluxo de publicação conecta as entradas de digest à proveniência, verifica
os digests publicados antes de montar os assets e inclui os relatórios SBOM e
de segurança. O envio de assets não usa `--clobber`. A validação estrutural
local de `test-cicd-contracts.ps1` passou; no CI do commit
[`0504617`](https://github.com/altivy-it/nodra/actions/runs/44409369557),
`Actionlint`, contratos CI/CD e arquivos gerenciados passaram. Os 37 hashes do
manifesto também conferiram após aplicar os finais de linha canônicos de
`.gitattributes`.

Essa evidência valida a integração estática, não uma publicação real. A
identidade do reviewer User foi validada para T018; gerar a política local e
revisar a diferença permanecem pendentes em T018/T019;
publicação, `republish` e validação operacional de imutabilidade continuam
pendentes em T033 e T039/T040. Nenhuma publicação ou implantação foi executada.

### T017 — manifesto, digests e falhas de atestação

`test-cicd-contracts.ps1` passou em container. Além dos casos de manifesto
inválido, digest inconsistente e atestação sintética inválida, foram executados
cenários isolados com comandos `docker`, `gh` e `git` simulados:

- Uma imagem existente foi reutilizada com o digest publicado; o publisher não
  executou build/push e indicou `pushed=false`.
- Um asset existente foi registrado com o digest publicado; o publisher não
  executou `gh release upload`.
- O guard recusou um manifesto cujo commit divergia do commit da tag.
- Uma falha de `gh attestation verify` impediu o deploy antes de acessar o host.

Todos os comandos GitHub e Docker desses cenários foram mocks locais. Não foi
consultado o entitlement real de atestações do plano GitHub, nem houve release,
publicação ou deploy. O gate trata falha do comando de verificação como recusa;
a disponibilidade remota e o uso real permanecem para qualificação separada.

O teste confirma que a repetição não substitui artefatos existentes, mas não
prova a proveniência deles. Em particular, `Publish-ContainerImage.ps1` aceita
o digest retornado pelo registry sem demonstrar sua ligação à tag e ao commit
aprovados, e `Publish-ReleaseAssets.ps1` preserva o asset existente sem exigir
que seu digest corresponda ao arquivo recém-gerado fora de `republish`. Essa
lacuna está separadamente rastreada em T039 e requer correção na origem
canônica Groundwork, sem editar arquivos gerenciados ou seus hashes neste
repositório.

### T018 — verificacao do bloqueio do catalogo — 2026-11-18

`test-cicd-contracts.ps1`, `actionlint 1.7.12` para `publish.yml` e
`git diff --check` passaram. O `Test-ManagedFiles` limitado a
`repository-policy` confirmou os dois arquivos desse componente. A verificacao
dos oito componentes, porem, falhou com 17 divergencias de hash em arquivos
gerenciados dos componentes `ci`, `release`, `release-preview`, `deploy`,
`publish-containers` e `publish-github-assets`; nenhum hash foi alterado
manualmente. A politica `.github/repository-policy.json` continua inalterada,
e nenhuma sincronizacao remota foi tentada.

### T018/T019 — revisao do candidato canonico — 2026-11-19

O Nodra foi confirmado publico; reviewers de Environment e atestacoes GitHub
nao estao mais bloqueados pela visibilidade no plano Team. Isso nao comprova
configuracao ou execucao operacional.

Uma geracao isolada com Groundwork 0.11.3 e os parametros de politica do
manifesto produziu `release`, `distribution`, `quality` e `production`, com
o reviewer validado e o ID do App. O candidato nao gerou `ci`/`copilot`,
manteve `reviewersFallback=release-pr` e permitiria que o sincronizador
tentasse novamente sem reviewer em `release`/`quality`; nao foi copiado para
o repositorio nem aplicado ao GitHub.

O `groundwork github --sync --dry-run` desse candidato retornou codigo 2 e
propos alterar as quatro opcoes de merge, habilitar secret scanning e push
protection, criar `gw-main-integrity`, `gw-main-review` e `gw-release-tags`,
criar `release`/`distribution` e ajustar reviewers e origens de
`quality`/`production`. O plano nao incluiu remocao da protecao classica.
Ele identificou `RELEASE_APP_ID` e `RELEASE_APP_PRIVATE_KEY` ausentes nas
configuracoes diretas e da organizacao acessiveis ao repositorio.

Esse dry-run e evidencia parcial, nao aprova um contrato incompleto e nao
conclui T019. A ferramenta considera `allowed_actions=all` conforme; ela nao
propos a allowlist nem a exigencia de SHA do Dokpod. Nao aplicar a allowlist
literal sem incluir o SHA de `docker/setup-docker-action` usado pelo Nodra.
Nenhuma configuracao remota, secret, release ou deploy foi alterado.

A verificacao em memoria dos arquivos gerenciados identificou 20 hashes
exatos e 17 correspondencias apos conversao para CRLF conforme
`.gitattributes`, sem outras divergencias. Os bytes do checkout nao foram
alterados e a verificacao oficial ampla ainda falha nesse checkout.

Na revisao final, `test-cicd-contracts.ps1`, `actionlint 1.7.12` para
`publish.yml`, `Test-ManagedFiles` para `repository-policy` e
`validate-repository.ps1` passaram em containers; este ultimo verificou 255
documentos. `git diff --check` tambem passou.

### T018–T020 — candidato local corrigido — 2026-11-19

Uma correção do catálogo Groundwork foi implementada em checkout separado,
baseado no commit `55af195b3a278a70ca20d7bdeb7576f2f097eacf` da `main`.
No momento do ensaio, ela ainda não tinha commit ou publicação; a última
release disponível era `v0.11.3`. Os parâmetros novos são opt-in:
`unrestricted_environments=ci,copilot` e
`strict_environment_reviewers=true`. O modo estrito exige revisores em
`release`, `distribution` e nos ambientes de deploy e não tenta configurá-los
sem reviewer após HTTP 422. Ele não admite `manual-dispatch`.

Uma geração isolada, com os demais parâmetros reais do manifesto do Nodra,
produziu exatamente seis ambientes: `ci` e `copilot` sem reviewers ou
restrições de refs; `release` e `distribution` com `main` e tags `v*`;
`quality` e `production` somente com tags `v*`. Os quatro protegidos exigem
o User `lzocateli`, com `preventSelfReview=false` e sem fallback. O usuário
foi confirmado como administrador ativo da organização e administrador do
repositório. O ruleset de tags usa o App ID `5441343`. O manifesto e os
arquivos gerenciados desse candidato ficaram isolados; os dois hashes de
`repository-policy` passaram em `Test-ManagedFiles`.

Os 52 testes direcionados de políticas passaram. Na suíte completa offline,
280 testes passaram e um teste de consulta ao feed externo foi ignorado.
`dotnet format --verify-no-changes` também passou, em container. A base local
declara licença MIT; as versões exatas das dependências resolvidas no ensaio
declaram MIT, Apache-2.0 ou BSD-2-Clause. Essa verificação não substitui a da
versão que vier a ser disponibilizada.

O `groundwork github --sync --dry-run` executado com esse candidato e a fonte
local retornou código 2 e apresentou o seguinte plano, ainda não aplicado:

- alterar `allow_rebase_merge` de `true` para `false`,
  `squash_merge_commit_title` de `COMMIT_OR_PR_TITLE` para `PR_TITLE`,
  `squash_merge_commit_message` de `COMMIT_MESSAGES` para `PR_BODY` e
  `allow_update_branch` de `false` para `true`;
- habilitar secret scanning e push protection, atualmente desativados;
- criar `gw-main-integrity`, `gw-main-review` e `gw-release-tags`;
- criar `copilot` sem restrições e `release`/`distribution` com reviewer e
  origens `main`/`v*`; `ci` já estava conforme;
- em `quality`, substituir a origem atual `main` por `tag:v*`; em
  `production`, remover `main` e manter `tag:v*`; adicionar o reviewer User
  aos dois ambientes, atualmente sem revisores.

A proteção clássica de `main` foi identificada, mas não consta como remoção
no plano. A leitura confirmou os checks obrigatórios `Result` e
`Full History`, com modo estrito; assinaturas obrigatórias estão desativadas e
o plano não prevê alterá-las. O ruleset legado `Protect release tags`, que
bloqueia atualização e exclusão de `v*`, também permanece. Continuam ausentes
os pré-requisitos
`RELEASE_APP_ID` e `RELEASE_APP_PRIVATE_KEY`; uma consulta separada também
não encontrou `ALTIVY_ADMIN_REVIEWER` no repositório ou nas configurações de
organização acessíveis. Somente os nomes foram consultados. O sync não é
atômico, portanto uma falha futura exige nova consulta do estado antes de
qualquer repetição.

Um `groundwork update --dry-run` adicional, com o catálogo de desenvolvimento
e o manifesto atual sem os novos parâmetros, avaliou 47 arquivos e retornou
código 0. O plano preservou as sementes e os 17 arquivos identificados como
modificações locais; atualizaria a documentação de estratégia e de deploy e os
dois arquivos de política. Esse resultado não inclui os seis ambientes
pretendidos e não resolve as divergências de finais de linha ou substitui
`Test-ManagedFiles`.

Esse ensaio não substitui uma origem canônica imutável nem conclui T018 ou
T019. T020 também permanece aberta: é necessário integrar e disponibilizar a
correção do catálogo com autorização explícita, executar a atualização oficial
do Nodra, repetir e revisar o dry-run e somente então aplicar e verificar o
contrato aprovado. Os artefatos da feature ainda fixam a versão `0.11.3`;
antes da atualização, a nova origem, versão, licença e dependências devem ser
verificadas e reconciliadas no Spec Kit. A política gerenciada do Nodra e as
configurações remotas não foram alteradas neste ensaio; não houve criação de
secrets, publicação ou deploy.

### Integração do catálogo — 2026-11-19

A correção foi enviada no commit `2d73985b6e5f6135b7f42fb67f99b71925bec1dd`
e no [PR 74 do Groundwork](https://github.com/altivy-it/altivy-groundwork/pull/74).
O [CI do PR](https://github.com/altivy-it/altivy-groundwork/actions/runs/44526682391)
passou, incluindo `Result`, testes .NET, Gitleaks e validação dos workflows do
catálogo.

Na verificação anterior à integração, a conta autenticada era autora do PR e o
GitHub recusou sua autoaprovação. A revisão técnica foi registrada como
comentário, não como aprovação, e uma revisão independente foi solicitada. O
PR permanecia aberto: a regra exigia revisão ou o bypass de administrador
configurado. O possível efeito de publicação de snapshot ainda não havia sido
autorizado, e aquela verificação não fez merge nem alterou proteções.

T018–T020 continuam abertas até a integração da origem, reconciliação do pin da
feature, novo dry-run e verificação do contrato remoto aprovado.

### T018–T020 — origem integrada e versão pendente — 2026-11-23

O [PR 74 do Groundwork](https://github.com/altivy-it/altivy-groundwork/pull/74)
foi integrado em 2026-11-19 no commit
`548e510c3660161b2d49fa6f3eaa3c020b9a7b1a`. O
[PR 73](https://github.com/altivy-it/altivy-groundwork/pull/73), de localização
do Spec Kit, também foi integrado no commit
`d8a358fd71a636aac12da9389491982517038aa8`.

O [CI](https://github.com/altivy-it/altivy-groundwork/actions/runs/44528849721)
e o [workflow Release](https://github.com/altivy-it/altivy-groundwork/actions/runs/44528850059)
associados à integração do PR 74 terminaram com sucesso. Nesse workflow,
`Release PR` executou e `Publish` foi ignorado; o resultado não comprova a
disponibilidade de um pacote com a correção.

A árvore do commit integrado, `5105bb741c2e381c8933959bbb0c9766063a6e7b`,
é idêntica à do commit `2d73985b6e5f6135b7f42fb67f99b71925bec1dd` do
candidato testado acima. O código integrado identifica ferramenta e catálogo
como `0.12.0`; o manifesto do Nodra registra os números das versões, mas não o
commit de origem. Portanto, trocar apenas o número da versão não identifica de
forma reproduzível o código usado em um eventual build da fonte.

Na consulta de 2026-11-23, a última release estável listada era
[`v0.11.3`](https://github.com/altivy-it/altivy-groundwork/releases/tag/v0.11.3),
publicada em 2026-11-07, antes da correção. A listagem dos pacotes
NuGet da organização retornou HTTP 403 por falta do escopo `read:packages`;
a existência de um snapshot utilizável não foi verificada.

T018 permanece aberta: antes de atualizar componentes ou gerar a política,
é necessário reconciliar no Spec Kit uma origem e versão imutáveis compatíveis
e verificar sua licença e dependências. T019 exige novo dry-run e revisão do
diff; T020 depende da aprovação explícita do contrato remoto. Nesta consulta,
nenhuma política, configuração remota, secret, release ou deploy foi alterado.

# Pesquisa: padrao CI/CD do Dokpod

## Origem e Licenca

**Decisao**: Usar o catalogo canonico Altivy.Groundwork 0.11.3, nao arquivos
copiados do Dokpod. Fonte: altivy-it/altivy-groundwork, tag v0.11.3, commit
c848afb4fa0b43cb9227abd2c347ab4ecccd6296. O LICENSE da tag foi consultado pela
API autenticada e confirma MIT, com o aviso legal da origem a preservar.

**Justificativa**: O Dokpod declara AGPL-3.0, enquanto o catalogo independente
MIT permite o alinhamento sem incorporar codigo copyleft. O manifesto do
Dokpod confirma toolVersion/catalogVersion 0.11.3.

**Alternativas**: Copiar gw-* do Dokpod e proibido. Reimplementar o catalogo
criaria drift desnecessario. Uma pesquisa inicial nao confirmou a licenca;
a consulta direta posterior ao repositorio canonico resolveu a duvida.

## Contrato de Fluxo

**Decisao**: CI sem secrets, Result unico agregador, Release PR humano,
candidata quality, publicacao por digest e manifesto, deploy guardado na tag.
Separar arquivos gerenciados e seeds do projeto. Auto-deploy fica desativado
nesta adocao; o usuario nao autorizou executar implantacao.

**Justificativa**: Corresponde aos papeis observados em .github/CI-CD.md,
ci.yml, publish.yml e deploy.yml do Dokpod, referencia funcional
01fe5b0c526742edb420541cdd9a0b4e9fa2e74e.

**Alternativas**: Manter publicacao monolitica e deploy arbitrario nao alinha
o processo. Habilitar preflight/publish_only sem implementacao seria falso
gate; deve haver implementacao testada ou recusa explicita.

## Ambientes

**Decisao**: ci/copilot sem aprovacoes; quality, release, distribution e
production com aprovacao da administracao da Altivy. quality/production aceitam tags v*; release e
distribution aceitam main e tags v*. prevent_self_review=false na referencia.

**Justificativa**: Politicas reais consultadas pela API. Em 2026-11-18, Nodra
tinha ci, quality e production sem required reviewers; release e distribution
ainda nao existiam no repositorio.

**Alternativas**: Criar apenas nomes nao reproduz a protecao; enfraquecer a
politica quando indisponivel nao e aceitavel.

**Esclarecimento aprovado pelo usuario em 2026-11-18**: usar exatamente os
revisores User configurados nos quatro environments protegidos equivalentes
do Dokpod, em vez de exigir uma equipe. A API confirmou um reviewer por
ambiente e a mesma identidade nos quatro. A consulta de membership confirmou
que essa conta e admin/owner da organizacao; a consulta de permissao do Nodra
confirmou permissao admin no repositorio. O login nao e reproduzido aqui.
RepositoryRole/actor_id 5 continua sendo um ator de ruleset e nao substitui o
reviewer User de Environment. CODEOWNERS permanece separado.

## Namespace e Configuracoes

**Decisao**: O usuario escolheu ghcr.io/altivy-it/nodra-* em 2026-11-11.
Guia exige RELEASE_APP_ID, RELEASE_APP_PRIVATE_KEY e configuracao SSH por
ambiente quando deploy for habilitado. O nome legado do workflow Nodra e
RELEASE_APP_CLIENT_ID, que exige orientacao de migracao sem sobrescrever dados.

**Justificativa**: Variaveis e secrets foram consultados apenas por nome.
Nao foram encontrados cadastros diretos no repositorio ou em quality/production.
Configuracoes herdadas da organizacao precisam ser confirmadas pelo operador;
ausencia nas listas locais nao prova ausencia de toda heranca.

**Alternativas**: Copiar valores do Dokpod ou pedir tokens/chaves pelo chat
violaria custodia e isolamento de produtos.

## Branches

**Decisao e evidencia**: Excluidas somente 009-migracao-prompts-skills-revisao
(a40ae67) e chore/adotar-spec-kit (bf7cf41), apos is-ancestor contra origin/main
e leases pelos SHAs completos. ls-remote confirmou que ficaram main e
010-renomeacao-nodra. O PR 31 continua aberto. A feature 011 local descende de
862ef1d; origin/main esta em fb29ed7 e ainda nao contem essa feature.

## Gates de Arquitetura e Operacao

**Decisao**: ADR proposto para adocao de catalogo e contrato de distribuicao,
com aceite humano antes das tarefas dependentes. Nenhum SQL, schema,
identidade, arquivo externo de secrets ou ambiente instalado sera alterado.
Manter o diagnostico DNS do PR 31 como trabalho rastreado, sem presumir que
trocar os workflows resolve conectividade ou homologacao.

## Evidencias de Implementacao Local — 2026-11-11

- Fonte canonica obtida no SHA aprovado e compilada fora do repositorio, em
	container SDK .NET 10; build com zero avisos e zero erros. Restore somente
	pela fonte publica NuGet. Scriban 7.5.3 BSD-2-Clause e System.CommandLine
	2.0.14 MIT verificados nos nuspecs oficiais; ambos sem dependencias para o
	target aplicavel. Aviso MIT original preservado em THIRD-PARTY-NOTICES.md.
- Executados init --dry-run e init oficiais com os oito componentes do plano.
	Nenhum core, speckit, secret-scanning, native-aot ou publish-nuget aplicado.
	Seeds existentes foram preservadas pelo gerador e adaptadas conscientemente;
	nao houve conflito gerenciado a resolver nem hashes editados manualmente.
- Status oficial e Test-ManagedFiles com os oito componentes confirmaram os
	37 arquivos integros. Auto-deploy vazio, snapshots false e attestations true.
- post-release.yml nao distribui NuGet nem outros registries; agentes Windows
	e mecanismos de licenciamento do Dokpod nao foram copiados.
- A branch 011 continua dependente de PR 31/010. Antes de merge, reconciliar
	explicitamente a main fb29ed7 sem restaurar o bump incompativel do router;
	o relatorio .specify/bugs/pr31-build-mergeability/assessment.md foi preservado.

## Bloqueios Confirmados do Catalogo e da Plataforma

O dry-run anterior `groundwork github --sync --dry-run` recusou o contrato local
antes de qualquer acesso ou mutacao remota porque `distribution` exigia
revisores e a lista estava vazia. O seed editavel `.github/ci-cd.json` declara
os seis ambientes, os quatro requisitos de aprovacao e o inventario de nomes e
escopos sem valores. Os parametros `release_reviewers` e `deploy_reviewers` do
manifesto agora registram o reviewer validado, mas `.github/repository-policy.json`
permanece sem essa atualizacao.

A fonte fixada em `c848afb4fa0b43cb9227abd2c347ab4ecccd6296` confirma que o
template 0.11.3 so gera `release`/`distribution` quando `release` esta
selecionado e ambientes de deploy conforme `deploy_environments`; nao gera
`ci` nem `copilot`. Tambem inclui `reviewersFallback: release-pr` em `release`
e define `quality.requireReviewers=false` quando `preview_acceptance=deploy`.
Correcao: isso nao impede configurar reviewers em `quality`; com a lista
preenchida, o gerador os inclui, como no Dokpod. Entretanto, em `release` e
`quality`, o sincronizador pode repetir uma operacao rejeitada com HTTP 422
sem reviewers, pois `requireReviewers` nao e verdadeiro. Isso nao atende ao
fail-closed exigido por T018. O dry-run local de `groundwork update` tambem
propoe alteracoes em documentos gerenciados fora do escopo da tarefa. Nenhuma
geracao local ampla, edicao manual de hashes ou sincronizacao remota foi
aplicada; T018 permanece bloqueada ate haver suporte canonico compativel.

Em 2026-11-19, o App `altivy-release`, ID `5441343`, foi confirmado na
organizacao com instalacao para todos os repositorios e permissoes de escrita
em contents e pull requests. O ID foi preenchido em `release_app_id` no
manifesto local; a politica gerenciada ainda nao foi regenerada. A variable
`RELEASE_APP_ID` e o secret `RELEASE_APP_PRIVATE_KEY` nao apareceram nos
cadastros diretos nem nas listas de configuracoes da organizacao disponiveis
ao Nodra. Nenhum valor de secret foi lido ou alterado. O reviewer User foi
validado como owner da organizacao e com permissao admin no Nodra; a
configuracao local do manifesto nao cria nem altera ambientes no GitHub.
Na mesma data, o usuario solicitou configurar o ambiente como o Dokpod; a
solicitacao nao elimina os gates de T018/T019 nem autoriza publicar, implantar
ou compartilhar secrets entre produtos.

O `release_app_id` do manifesto e o App ID numerico exigido pelo ruleset. A
action `actions/create-github-app-token` fixada em `64ab729a6294000f5aa4a4bc0b73d1f32bc45510`
encaminha `client-id` e o antigo `app-id` pelo mesmo caminho de autenticacao.
O [GitHub](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app)
aceita Client ID ou App ID como emissor do JWT e recomenda o primeiro. A
emissao real de token nao foi testada, pois a chave privada nao foi acessada.

Na validacao de T018 em 2026-11-18, `test-cicd-contracts.ps1`,
`actionlint 1.7.12` para `publish.yml` e `git diff --check` passaram.
`Test-ManagedFiles` para `repository-policy` confirmou dois arquivos. A
verificacao ampla dos oito componentes reportou 17 divergencias de hash em
arquivos gerenciados de `ci`, `release`, `release-preview`, `deploy`,
`publish-containers` e `publish-github-assets`; nenhum hash foi ajustado
manualmente. Em 2026-11-19, uma verificacao somente em memoria confirmou que
essas 17 divergencias correspondem aos finais de linha CRLF previstos em
`.gitattributes`; os demais 20 hashes coincidem diretamente. O checkout nao
foi alterado e `Test-ManagedFiles` continua falhando nos bytes atuais.

O catalogo tambem gera documentos gerenciados em ingles e espanhol. A regra
pt-BR e a integridade dos arquivos gerenciados precisam de reconciliacao na
origem antes do fechamento; nao traduzir localmente nem alterar hashes.

O scan por digest foi implementado na semente publish.yml, depois das entradas
canonicas das cinco imagens, antes de anexar assets/concluir a release. SBOM
CycloneDX e relatorios passam pelo publisher atestado. O dry-run offline
comprova selecao dos digests sem rebuild; execucao real do scanner/BuildKit e
atestacao GitHub permanecem pendentes, sem publicar para testar.
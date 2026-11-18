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

**Justificativa**: Politicas reais consultadas pela API. Nodra atualmente tem
somente quality e production, sem required reviewers nas respostas obtidas.

**Alternativas**: Criar apenas nomes nao reproduz a protecao; enfraquecer a
politica quando indisponivel nao e aceitavel.

**Revisao posterior solicitada pelo usuario**: retirar a atribuicao pessoal e
usar os administradores da Altivy. Nova consulta ao Dokpod confirmou papel
RepositoryRole/actor_id 5 nos rulesets, mas aprovadores de Environment ainda
individuais. Nao ha equipe administrativa identificada nas equipes visiveis da
organizacao consultadas. Como Environment e CODEOWNERS exigem usuario/equipe
concretos, a proposta nao deve fingir que um papel administrativo e um Team ID;
resolver e validar a equipe na implementacao antes de aplicar protecoes.

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

O dry-run oficial `github --sync --dry-run` recusou o contrato local antes
de qualquer acesso ou mutacao remota: distribution exige revisores e a lista
esta vazia. O seed editavel `.github/ci-cd.json` agora declara os seis ambientes,
os quatro requisitos de aprovacao e o inventario de nomes/escopos sem valores.
A politica gerenciada ainda possui apenas release/distribution/quality/
production; ci/copilot faltam. quality esta com requireReviewers false e release
com fallback release-pr. O inventario local nao e aplicado pelo Groundwork 0.11.3
e nao significa que os ambientes existem remotamente. Nao editar hashes. A equipe
administrativa e o App ainda precisam ser identificados.

O catalogo tambem gera documentos gerenciados em ingles e espanhol. A regra
pt-BR e a integridade dos arquivos gerenciados precisam de reconciliacao na
origem antes do fechamento; nao traduzir localmente nem alterar hashes.

O scan por digest foi implementado na semente publish.yml, depois das entradas
canonicas das cinco imagens, antes de anexar assets/concluir a release. SBOM
CycloneDX e relatorios passam pelo publisher atestado. O dry-run offline
comprova selecao dos digests sem rebuild; execucao real do scanner/BuildKit e
atestacao GitHub permanecem pendentes, sem publicar para testar.
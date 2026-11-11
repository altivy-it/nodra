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
| RELEASE_APP_ID | Variable | Repositorio | Identificador do GitHub App de release instalado e autorizado no Nodra |
| RELEASE_APP_PRIVATE_KEY | Secret | Repositorio ou organizacao com acesso restrito ao Nodra | Chave privada do mesmo App; nunca incluir no repo, comando literal ou chat |
| ALTIVY_ADMIN_TEAM | Variable | Repositorio | Slug da equipe administrativa real, validada como Team reviewer nos quatro ambientes protegidos |
| DEPLOY_SSH_TARGET | Variable | quality e production | Destino usuario@host revisado do ambiente Nodra |
| DEPLOY_HOOK | Variable | quality e production | Caminho absoluto do hook Nodra revisado que implementa apply/check |
| DEPLOY_SSH_KEY | Secret | quality e production | Chave SSH dedicada, com menor privilegio para o host de cada ambiente |
| DEPLOY_SSH_KNOWN_HOSTS | Secret | quality e production | Fingerprint de host verificado por canal confiavel; nao confiar cegamente em ssh-keyscan |

`RELEASE_APP_CLIENT_ID` e o nome legado encontrado no workflow atual. O novo
contrato usa RELEASE_APP_ID; nao apagar ou renomear valores existentes ate
conferir a transicao. As consultas por nome retornaram listas vazias no
repositorio e nos ambientes quality/production; heranca da organizacao ainda
deve ser confirmada. Nao inventar PAT, NUGET_API_KEY ou secrets de producao
para fazer o CI passar; estes jobs usam dados sinteticos e GITHUB_TOKEN de
permissoes limitadas.

### Cadastro Seguro

1. Em Settings → Secrets and variables → Actions do Nodra, criar a Variable
   RELEASE_APP_ID com o identificador correto do App. Confirmar que o App esta
   instalado no Nodra e possui apenas as permissoes de contents/pull requests
   necessarias ao fluxo, conforme a configuracao final.
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

Os aprovadores propostos sao os administradores da Altivy, nao uma conta
pessoal. O Dokpod usa o papel RepositoryRole/actor_id 5 nos rulesets, mas isso
nao pode ser usado diretamente como required reviewer de Environment ou como
CODEOWNER. Validar o slug, ID, visibilidade e permissao da equipe administrativa
antes da aplicacao. A consulta atual nao retornou equipe administrativa visivel;
essa pendencia nao autoriza criar grupo, membros ou permissoes implicitamente.

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
| test-cicd-contracts.ps1 | aprovado: eventos/permissoes, Result verde/failure/cancelled/skipped/vazio, equipe ausente/presente sintetica, guard inicial e manifestos invalidos |
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
pwsh -NoProfile -File tools/scripts/ci/Test-ManagedFiles.ps1 -Components ci,release,release-preview,publish-containers,publish-github-assets,deploy,dependency-updates,repository-policy
pwsh -NoProfile -File tools/scripts/test-validate-repository.ps1
pwsh -NoProfile -File tools/scripts/validate-repository.ps1
```

O teste YAML requer o build Release existente da API/YamlDotNet e PowerShell
7.6/.NET 10; o SDK aprovado fornece esse runtime. O primeiro ajuste do baseline
falhou na fixture e foi corrigido; o resultado final acima e o que foi aprovado.

## Pendencias Impeditivas

1. Identificar a equipe administrativa real, permissao/visibilidade e App; nao
   substituir aprovador por RepositoryRole/5 ou uma conta pessoal.
2. Corrigir pela origem canonica a politica dos seis ambientes e protecoes;
   revisar novo dry-run antes de qualquer sync. CODEOWNERS legado aguarda a
   equipe; nao foi reatribuido ficticiamente.
3. Reconciliar a documentacao gerenciada en/es com a regra pt-BR na origem,
   preservando hashes e avisos legais. Nao aceitar excecao implicitamente.
4. Executar BuildKit, scanner real e testes remotos de atestacao/imutabilidade
   em validacao autorizada. Builds locais Podman nao comprovam esses gates.
5. Homologar hook/SSH e laboratorio OIDC/upgrade/recuperacao separadamente;
   nenhuma instalacao foi acessada ou modificada nesta feature.
6. Obter autorizacao explicita para commit/push 011; reconciliar PR 31 antes de
   integrar em main. Nenhum commit/push desta feature foi executado.
# Plano de Implementacao: Padronizacao CI/CD self-hosted

**Branch**: `011-padronizacao-cicd` | **Data**: 2026-11-11 | **Spec**: [spec.md](spec.md)
**Entrada**: Escopo aprovado para planejamento; namespace ghcr.io/altivy-it/nodra-* confirmado pelo usuario.
**Status**: Implementacao local autorizada pelo usuario em 2026-11-11, com aceite explicito do ADR 2026-0009 nesta conversa: "ADR aprovado, siga coma speckit". Publicacao, deploy e mutacoes remotas de protecao permanecem sujeitos aos gates do plano.

## Resumo

Adotar o mesmo catalogo canonico de CI/CD usado pelo Dokpod, mantendo tarefas
especificas do Nodra nas sementes do projeto. A referencia funcional e Dokpod
01fe5b0c526742edb420541cdd9a0b4e9fa2e74e, mas os componentes devem vir de
Altivy.Groundwork 0.11.3, tag v0.11.3, commit
c848afb4fa0b43cb9227abd2c347ab4ecccd6296, cujo LICENSE confirma MIT.

Hipotese local: componentes canonicos mais sementes Nodra permitem alinhar
eventos, gates, ambientes e contratos sem reimplementar o catalogo nem copiar
codigo AGPL. Check discriminante: status/validacao dos arquivos gerenciados,
actionlint e testes dos contratos de sementes e politicas antes de qualquer
sincronizacao remota. Instalar componentes nao e aprovacao de release/deploy.

## Contexto Tecnico

**Linguagem/Versao**: YAML GitHub Actions, JSON, PowerShell 7; aplicacao .NET 10 e Angular 22 existentes.
**Dependencias Principais**: Altivy.Groundwork 0.11.3 MIT; actions ja fixadas e imagens existentes. Verificar dependencias da ferramenta antes de executa-la.
**Persistencia**: Nenhuma mudanca em vault, schema, identidade ou dados; metadados de release e politica sao contratos operacionais versionados.
**Testes**: actionlint em container, testes dos scripts/contratos, validador de repositorio, verificacao gerenciada Groundwork, dry-run de politicas e CI remoto autorizado.
**Plataforma Alvo**: GitHub Actions e distribuicao self-hosted em containers; HTTP de homologacao somente por nginx.
**Tipo de Projeto**: Monorepo existente, sem novos servicos de aplicacao.
**Metas de Desempenho**: Nao alterar desempenho de runtime; limitar concorrencia e timeouts dos jobs.
**Restricoes**: Nenhum secret lido/copiedo, nenhuma publicacao ou deploy, nenhum codigo AGPL importado, nenhum core/Spec Kit sobrescrito.
**Escala/Escopo**: Componentes ci, release, release-preview, publish-containers, publish-github-assets, deploy, dependency-updates e repository-policy; seis ambientes GitHub.

## Verificacao da Constituicao

Gate anterior a pesquisa: principios de integridade, identidade externa,
seguranca, modularidade e qualidade preservados. Gate apos desenho: atende sob
as condicoes abaixo; a implementacao e a decisao arquitetural exigem revisao.

- Conteudo/dados: nao mudar vaults, schema, roles, realm, certificados, key ring ou secrets externos.
- Seguranca: CI nao recebe secrets de release/deploy; permissoes minimas por job; ambientes protegidos com aprovacao humana.
- Licencas: nao copiar arquivos do Dokpod AGPL; verificar MIT no tag canonico e registrar aviso legal necessario dos componentes reutilizados.
- Governanca: core, secret-scanning e speckit nao serao selecionados nesta instalacao; preservar constituicao, specs, overrides, hooks e customizacoes locais.
- Compatibilidade: migrar de images.env para manifesto validado com adaptacao explicita de consumidores ativos, sem alterar instalacoes remotas.
- ADR: a adocao do catalogo e o contrato de manifesto/deploy sao uma decisao transversal de distribuicao. Criar ADR proposto pelo fluxo auxiliar oficial e obter aceite humano antes das tarefas dependentes; nao aceitar em nome do usuario.
- Qualidade: executar ferramentas/builds/testes em containers; aplicar politica remota somente apos dry-run revisado.

## Estrutura do Projeto

### Documentacao da Feature

```text
specs/011-padronizacao-cicd/
  spec.md
  plan.md
  research.md
  data-model.md
  contracts/cicd.md
  quickstart.md
  checklists/requirements.md
  tasks.md
```

### Superficies Afetadas

```text
.groundwork/manifest.json              # criado pelo catalogo; nunca falsificar hashes
.github/workflows/gw-*.yml             # gerenciados pela origem MIT
.github/workflows/{ci,publish,post-release,deploy}.yml
.github/ci-cd.json
.github/repository-policy.json
release-please-config.json
.release-please-manifest.json
tools/scripts/ci/                     # gerenciados
tools/scripts/                        # testes/adaptadores especificos estritamente necessarios
deploy/self-hosted/                   # consumidores e documentacao de distribuicao
docs/ci-cd-seguranca.md
docs/runbooks/
docs/adr/                             # ADR proposto antes do aceite humano
```

**Decisao de Estrutura**: Nao criar pipeline paralelo nem editar gw-* para
contornar o teste gerenciado. Seeds pertencem ao Nodra e mantem seus testes,
imagens e caminhos. Nao copiar agentes Windows, NuGet publishing ou licenciamento
do Dokpod; nao instalar componentes desnecessarios.

## Sequencia de Implementacao

1. Revisar plano/tarefas e decidir a ADR. Preservar o relatorio local do PR 31 e a dependencia da branch 010.
2. Obter o catalogo exclusivamente da origem MIT fixada e executar a ferramenta em container. Se pacote privado exigir PAT adicional, parar e orientar entrada segura no terminal, nunca pelo chat; alternativa de build de fonte exige a mesma verificacao de licencas.
3. Executar aplicacao local dos oito componentes com parametros Nodra, license=MIT, dotnet_solution=Nodra.slnx, version_files existentes, preview quality e auto-deploy desativado. Manter secrets_dir_name=Altivy.Notes e nao modificar versoes de dependencias da aplicacao.
4. Integrar .groundwork-new conscientemente; migrar arquivos seed conflitantes sem sobrescrita silenciosa. Remover workflows legados duplicados somente apos substitutos e testes prontos; resolver conflitos com comando oficial, sem editar hashes.
5. Adaptar CI: PR/merge_group/main/dispatch, hygiene, gitleaks, testes backend/PostgreSQL UTC, frontend, imagens e Result. CI permanece sem secrets e nao depende de configuracao de producao.
6. Adaptar seeds de publicacao e deploy aos cinco artefatos Nodra e GHCR aprovado. Implementar apenas modos realmente suportados; preflight/publish_only nao implementados devem falhar fechado, nao aparentar validacao.
7. Configurar repositorio: dry-run de politicas, revisar diferencas e aplicar os seis ambientes com aprovacao da administracao da Altivy. Identificar e validar a equipe GitHub administrativa antes de alterar required reviewers ou CODEOWNERS; nao usar conta pessoal como fallback. Nao apagar regras legadas com --replace-legacy sem uma autorizacao humana especifica para o diff; nao reduzir gates para destravar PR.
8. Documentar App e SSH por escopo; o operador cadastra secrets no GitHub fora do modelo. Nao habilitar publish/deploy para testar credenciais.
9. Validar arquivos gerenciados, actionlint, contratos e simulacoes nao destrutivas. Publicar branch/PR somente se autorizado; sem merge automatico e sem executar release/deploy.

## Politica dos Ambientes

| Ambiente | Papel | Origem | Protecao |
| --- | --- | --- | --- |
| ci | Validacao | PR/main/merge group conforme job | Sem secrets de publicacao/producao |
| copilot | Automacao reservada | Conforme configuracao do GitHub | Sem secrets de deploy |
| quality | Validacao de candidata | tags v* | Administradores da Altivy |
| release | Preparacao/publicacao autorizada | main e tags v* | Administradores da Altivy |
| distribution | Autorizacao de divulgacao | main e tags v* | Administradores da Altivy |
| production | Implantacao manual de release | tags v* | Administradores da Altivy |

Espelhar a referencia prevent_self_review=false sem alterar para true
silenciosamente. Se o plano GitHub nao permitir uma protecao, registrar bloqueio
e manter a operacao fechada; nao declarar paridade inexistente.

A referencia administrativa do Dokpod nos rulesets e RepositoryRole com ID 5;
isso nao e um required reviewer de Environment nem uma entrada CODEOWNERS.
O inventario nao retornou equipes visiveis: a identificacao e validacao da
equipe administrativa permanece pre-requisito, sem criar um slug ficticio.

## Identidade das Contribuicoes

Contribuicoes desta manutencao e da automacao configurada usam autor/committer
lunadev00 <luna@altivy.it>. A identidade Git foi configurada apenas neste clone;
nao altera usuarios globais, autoria de terceiros, commits publicados ou
assinaturas criptograficas. Nomes pessoais nao devem aparecer como titulares
de aprovacao ou autoria nos novos artefatos. Referencias tecnicas reais de
fornecedores e avisos legais obrigatorios nao sao substituidos por nome ficticio.

## Registro de Complexidade

Nenhuma violacao proposta. Bloqueios de permissao, App, licencas transitivas,
plano GitHub, atestacao ou laboratorio sao reportados; nao justificam remover
controles. Reconciliar a dependencia do PR 31 antes de integrar esta feature em
main. A main tem alerta remoto de vulnerabilidades; correcao geral e trabalho
separado, nao uma atualizacao implicita desta feature.

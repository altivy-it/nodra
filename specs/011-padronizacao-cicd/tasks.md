# Tarefas: Padronizacao CI/CD self-hosted

**Entrada**: spec.md, plan.md, research.md, data-model.md e contracts/cicd.md em specs/011-padronizacao-cicd/.
**Status**: Implementacao local autorizada e ADR aceito pelo usuario em 2026-11-11 nesta conversa. Manter gates de revisao de politicas remotas e de operacoes de publicacao/deploy.
**Pre-requisitos**: Revisao do plano/tarefas e aceite humano da ADR antes das tarefas dependentes.
**Testes**: Obrigatorios para gates, contratos, politicas e modos de falha; executar ferramentas em containers.

## Formato e Caminhos

`- [ ] T### [P] [US#] Descricao com caminho`. P somente para tarefas sem
dependencia ou disputa de arquivos. Arquivos gw-* sao gerenciados pelo catalogo;
seeds e adaptacoes do Nodra permanecem nos caminhos definidos em plan.md.

## Fase 1: Preparacao

- [x] T001 Registrar revisao humana do plano e das tarefas em specs/011-padronizacao-cicd/plan.md, incluindo os seis ambientes, aprovacao pelos administradores da Altivy, autoria lunadev00 <luna@altivy.it>, namespace GHCR aprovado e ausencia de autorizacao de deploy/publicacao.
- [x] T002 Verificar fonte, tag, SHA, LICENSE MIT e dependencias da ferramenta Groundwork 0.11.3 e registrar evidencias em specs/011-padronizacao-cicd/research.md; preservar avisos legais necessarios, sem copiar arquivos do Dokpod AGPL.
- [x] T003 Criar ADR proposto em docs/adr com proxima sequencia livre, usando .github/ADR_TEMPLATE.md e a skill auxiliar oficial; registrar origem e obter aceite humano explicito para catalogo e contrato de manifesto/deploy, sem aceitar por inferencia.
- [x] T004 Validar base Git e dependencia do PR 31 e preservar o relatorio local .specify/bugs/pr31-build-mergeability/assessment.md; registrar em specs/011-padronizacao-cicd/research.md como a feature sera reconciliada antes de merge em main, sem descartar o ajuste de peers.

## Fase 2: Fundacao

**Gate**: Nenhum arquivo operacional e alterado antes da revisao e dos pre-requisitos.

- [x] T005 Preparar execucao containerizada da ferramenta canonica fixada e a aplicacao dos oito componentes previstos em specs/011-padronizacao-cicd/plan.md; se acesso privado exigir credencial adicional, interromper e orientar terminal seguro, sem registrar token.
- [x] T006 Gerar componentes e .groundwork/manifest.json pelo comando oficial com parametros Nodra, license MIT, Nodra.slnx, versionFiles existentes, secrets_dir_name Altivy.Notes, ambientes quality/production e auto-deploy desativado; nao selecionar core, speckit ou --all.
- [x] T007 Integrar conflitos .groundwork-new das seeds e configuracoes .github/ci-cd.json e release-please-config.json, preservando customizacoes; usar resolve oficial, sem alterar hashes gerenciados ou sobrescrever constituicao/specs/hooks.
- [x] T008 Adicionar testes dos contratos em tools/scripts/ reaproveitando o validador existente quando adequado: Result completo, CI sem secrets, permissoes minimas, entradas/ambientes negados e nenhuma publicacao/deploy em PR; scripts novos exigem --help e testes.

## Fase 3: US1 - CI Padronizado (P1)

**Objetivo**: Gates de PR/merge group/main consistentes e executaveis sem secrets.
**Teste independente**: validar um cenario positivo e falha/cancelamento de um job obrigatorio no agregador.

- [x] T009 [US1] Adaptar .github/workflows/ci.yml aos eventos pull_request/merge_group/main/dispatch, concorrencia e permissoes por job do contrato; manter baseline, testes Nodra em containers, PostgreSQL real com sessoes UTC, frontend e imagens.
- [x] T010 [US1] Integrar .github/workflows/gw-pr-hygiene.yml e gw-gitleaks.yml no CI e no needs de Result; remover duplicacao de .github/workflows/gitleaks.yml somente depois de validar o substituto e manter a protecao de historico completo.
- [x] T011 [US1] Atualizar .github/ci-cd.json com scopes aprovados, solucao, imagens e versionFiles do Nodra, sem copiar scopes/licenciamento/stack do Dokpod; garantir Result como unico check obrigatorio no contrato local .github/repository-policy.json, sujeito a revisao remota.
- [x] T012 [US1] Executar actionlint, teste gerenciado e contratos de CI em containers, incluindo PR sem secrets e ausencia de publicacao; registrar evidencia em specs/011-padronizacao-cicd/quickstart.md sem disparar release/deploy.

## Fase 4: US2 - Release e Distribuicao (P1)

**Objetivo**: Release PR, candidata e artefatos imutaveis com gates humanos.
**Teste independente**: entradas invalidas, aprovacao ausente e tentativa de sobrescrita sao recusadas sem publicar.

- [ ] T013 [US2] Integrar a semente release-please-config.json e .release-please-manifest.json com versao atual, backend/Directory.Build.props e frontend/web/package.json, preservando versoes de pacotes; adaptar consumidores legados do arquivo .github/release-please-config.json sem dois fluxos ativos.
- [x] T014 [US2] Implementar .github/workflows/publish.yml com os cinco Dockerfiles Nodra e GHCR ghcr.io/altivy-it/nodra-*, entradas tag/version/republish e release-entries-*; preflight/publish_only nao suportados devem falhar fechado, ou ser implementados com teste antes de habilitar a politica correspondente.
- [ ] T015 [US2] Integrar .github/workflows/gw-release.yml e gw-preview.yml pelo catalogo, com configuracao preview quality e gates humanos; remover o workflow legado .github/workflows/release.yml somente apos validar equivalencia de SBOM, proveniencia, scan e imutabilidade.
- [x] T016 [US2] Preparar .github/workflows/post-release.yml sem fan-out ou publicacao em registries adicionais nao solicitados; documentar a ausencia deliberada de NuGet, agentes Windows e licenciamento em specs/011-padronizacao-cicd/research.md.
- [ ] T017 [US2] Validar manifesto, digests, origem e modos idempotentes em tools/scripts/ e nos testes do catalogo; falhas de atestacao/plano GitHub devem bloquear deploy e ser registradas em specs/011-padronizacao-cicd/quickstart.md, sem publicar release para testar.

## Fase 5: US3 - Ambientes e Configuracao (P1)

**Objetivo**: Os mesmos seis ambientes, com protecoes verificadas e guia de cadastro seguro.
**Teste independente**: consultar politicas efetivas e comprovar ausencia de configuracao sensivel nos arquivos/logs.

- [ ] T018 [US3] Gerar e revisar .github/repository-policy.json para os seis ambientes, identificando e validando a equipe real dos administradores da Altivy como aprovadora dos quatro protegidos antes de aplicar politicas ou migrar .github/CODEOWNERS; distinguir RepositoryRole/actor_id 5 dos IDs de Team/User, preservar prevent_self_review false e as origens aprovadas, sem fallback pessoal ou aprovacao ficticia.
- [ ] T019 [US3] Executar dry-run oficial de politicas e registrar o diff em specs/011-padronizacao-cicd/quickstart.md; nao remover regras legadas, alterar regras de assinatura ou reduzir checks sem autorizacao explicita do diff remoto.
- [ ] T020 [US3] Aplicar somente o contrato remoto aprovado e consultar novamente ambientes/revisores/origens, registrando resultado em specs/011-padronizacao-cicd/quickstart.md; falha de permissao ou limitacao do plano deve ficar como bloqueio, nao sucesso.
- [x] T021 [US3] Criar ou atualizar o guia especifico em docs/ci-cd-seguranca.md com RELEASE_APP_ID, RELEASE_APP_PRIVATE_KEY e DEPLOY_SSH_TARGET/DEPLOY_HOOK/DEPLOY_SSH_KEY/DEPLOY_SSH_KNOWN_HOSTS por ambiente; explicar migracao de RELEASE_APP_CLIENT_ID, heranca da organizacao e cadastro pelo operador sem receber valores.
- [ ] T022 [US3] Verificar apenas presenca e nomes de configuracoes exigidas pelas seeds finais e registrar pendencias em specs/011-padronizacao-cicd/quickstart.md; nao criar chaves, copiar secrets do Dokpod ou sobrescrever valores existentes.

## Fase 6: US4 - Deploy por Manifesto (P2)

**Objetivo**: Somente release validada e ambiente autorizado podem chegar ao host.
**Teste independente**: tentar branch, ambiente, manifesto ou credencial ausente e confirmar recusa antes de acessar o host.

- [ ] T023 [US4] Adaptar .github/workflows/deploy.yml a version/environment/manifest-artifact, environment em todo job que atua no destino e SSH de host verificado; origem vem do guard canonico, sem imagem/digest livre ou token impresso.
- [x] T024 [US4] Definir em docs/runbooks/deploy-projeto.md o adaptador/hook Nodra apply/check e compatibilidade com Compose e configuracao existentes, conforme a ADR aceita; nao executar nem instalar hook no host durante esta feature.
- [ ] T025 [US4] Atualizar consumidores ativos de namespace e manifesto em deploy/self-hosted/README.md, deploy/production/nodra-ops.sh e docs/runbooks pertinentes; manter compatibilidade explicita com images.env e dados/configuracao externa, sem publicar/mover imagens ou editar planos congelados.
- [ ] T026 [US4] Testar os guards e a passagem de manifesto validado em cenarios sinteticos, incluindo rejeicao de origem nao publicada, ambiente nao permitido e configuracao ausente; registrar em specs/011-padronizacao-cicd/quickstart.md sem deploy.

## Fase 7: Refinamento e Fechamento

- [ ] T027 Revalidar status/hashes gerenciados em .groundwork/manifest.json, actionlint e validadores de estrutura/links; confirmar ausencia de workflows duplicados e variaveis/secrets em logs, registrando evidencias em specs/011-padronizacao-cicd/quickstart.md.
- [ ] T028 Auditar .github/workflows e .github/repository-policy.json contra os 16 requisitos e os seis ambientes do contrato; conferir remocao de atribuicoes pessoais ativas e autoria institucional, reportar referencias tecnicas/legais que precisam de preservacao e bloqueios de plataforma/App/equipe/atestacao, sem marcar homologacao operacional como concluida.
- [ ] T029 Executar /speckit-analyze e /speckit-converge conforme a etapa, acrescentando trabalho restante a specs/011-padronizacao-cicd/tasks.md; documentar a limpeza ja realizada das duas branches integradas e preservar main/PRs pendentes.
- [ ] T030 Consolidar commits e publicar a branch 011 somente apos autorizacao explicita; manter a dependencia do PR 31 e nao realizar merge, release, distribuicao ou deploy por efeito colateral; registrar o resultado em specs/011-padronizacao-cicd/quickstart.md.

## Dependencias e Ordem

Preparacao → Fundacao → CI → Release/Distribuicao → Ambientes → Deploy → Auditoria.

- T003 bloqueia tarefas de mudanca de contrato de distribuicao/deploy.
- T006-T007 antecedem qualquer adaptacao de seed; nao modificar arquivos gerenciados para contornar os testes.
- T019 e sua revisao antecedem T020; usuario cadastra secrets fora do modelo e nenhuma tarefa depende de receber seus valores.
- T023-T026 dependem dos contratos e politicas, nao de um deploy real.
- Integracao em main depende da reconciliacao do PR 31; limpeza adicional nunca remove branches pendentes.

## Paralelismo

Nenhuma tarefa marcada P: instalacao, reconciliacao, seeds e politicas compartilham
arquivos e dependencias. Consultas de metadados e testes em ambientes isolados
podem ocorrer em paralelo quando nao modificarem os mesmos artefatos.

## Estrategia de Implementacao

Entregar primeiro CI e testes sem secrets, depois os contratos de release,
ambientes e deploy. Validar o recorte imediatamente apos cada mudanca. Configurar
revisores e explicar credenciais nao e autorizacao para publicar ou implantar.
Parar fechado diante de limite de licenca, permissao ou plano GitHub e registrar
trabalho restante sem inventar evidencias.

## Phase 8: Convergence

Resultado: tasks_appended em 2026-11-11. Avaliacao do codigo contra os 16 FR,
6 SC, quatro historias e decisoes do plano. As tarefas abaixo detalham trabalho
restante, sem substituir ou declarar concluidas as tarefas anteriores.

- [ ] T031 CRITICAL Reconciliar na origem do catalogo a geracao de docs/en/release-integrity.md e docs/es/release-integrity.md com a documentacao pt-BR exigida; aplicar somente atualizacao oficial verificada e repetir status/Test-ManagedFiles, sem traducao manual nem hashes ficticios, per Constitution V e plan: componentes gerenciados (contradicts).
- [ ] T032 CRITICAL Identificar e validar a equipe administrativa real e o App, obter pela origem canonica politica para os seis ambientes com quatro aprovacoes obrigatorias e sem fallback pessoal, migrar CODEOWNERS somente com destinatario valido e repetir dry-run revisado antes de sync; o atual e recusado por revisores vazios, per FR-006, FR-007, FR-016 e SC-002 (partial).
- [ ] T033 HIGH Qualificar offline e em gates remotos autorizados as sementes finais de release, incluindo versionFiles/Release Please, scan e SBOM dos digests publicados, proveniencia/atestacao e republish sem sobrescrita; fixtures/dry-run e builds Podman locais nao comprovam a execucao real do BuildKit/Trivy/GitHub, per FR-008, US2/AC3 e T013/T015/T017 (partial).
- [ ] T034 HIGH Ampliar testes sinteticos dos passos SSH de deploy/smoke para configuracao ausente, origem nao publicada e entradas invalidas, e qualificar o hook Nodra apply/check em laboratorio autorizado quanto a idempotencia, versao e nginx; nao instalar nem acessar host de producao nesta tarefa sem autorizacao separada, per FR-009, US4/AC1, US4/AC2 e T023/T026 (partial).
- [ ] T035 HIGH Testar comportamento dos consumidores self-hosted com os cinco digests institucionais e referencias legadas permitidas, rejeicoes de imagem externa/checksum invalido e pacote extraido com dependencias completas; sintaxe/ajuda e testes do conversor nao substituem esses cenarios de consumidor, per FR-012, FR-015 e T025 (partial).
- [ ] T036 MEDIUM Auditar autoria efetiva da automacao canônica de Release PR/commits e ajustar somente por mecanismo suportado para lunadev00 <luna@altivy.it>, preservando avisos legais, fornecedores e autores de terceiros; a configuracao local do clone nao comprova a autoria produzida pelo GitHub App, per FR-016 e plan: Identidade das Contribuicoes (partial).
- [ ] T037 HIGH Conferir novamente somente presenca/nomes de App, ALTIVY_ADMIN_TEAM e configuracoes SSH apos cadastro seguro pelo operador, concluir a auditoria dos 16 requisitos e obter autorizacao explicita para commit/push/CI remoto da 011; preservar dependencia do PR 31 e nao publicar release, mudar protecoes nem implantar por efeito colateral, per FR-010, FR-014, SC-004, SC-006 e T022/T027/T028/T030 (partial).
- [x] T038 Incluir no seed editavel .github/ci-cd.json o inventario dos seis ambientes, requisitos de revisao e nomes/escopos de variables e secrets sem valores; validar o contrato sem alterar arquivos gerenciados, per FR-006, FR-007, FR-010 e SC-004.
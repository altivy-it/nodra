# Tarefas: Altivy Nodra

**Entrada**: Documentos em `specs/010-renomeacao-nodra/`.
**Pre-requisitos**: spec.md aprovada; plan.md, research.md, data-model.md e contracts/compatibilidade.md revisados antes da implementacao.
**Status**: Plano, tarefas e excecoes aprovados pelo usuario em 2026-11-11 nesta conversa. Execucao autorizada; renomeacao remota somente ao final das verificacoes.

**Testes**: Obrigatorios conforme o risco transversal; reutilizar suites e validadores existentes. Checkboxes representam execucao validada, nao aprovacao humana.

## Formato

`- [ ] T### [P] [US#] Descricao com caminho`. O marcador P indica somente independencias reais; tarefas de preparacao, fundacao e fechamento nao possuem historia.

## Convencoes de Caminhos

Manter `backend/apps`, `backend/libs`, `backend/tests`, `frontend/web`, `frontend/libs`, `frontend/tests`, `contracts`, `deploy` e `tools/scripts`. Renomear apenas segmentos especificos do produto. Nao modificar docs/plan ou outras specs como se fossem tarefas desta feature.

## Fase 1: Preparacao

- [x] T001 Registrar em specs/010-renomeacao-nodra/plan.md a revisao humana do plano, das tarefas e das excecoes de compatibilidade antes de modificar a aplicacao.
- [x] T002 Validar ancestralidade de a9546a5, referencias de origin e estado limpo/alteracoes preexistentes; registrar a base e o inventario de nomes em specs/010-renomeacao-nodra/research.md.
- [x] T003 Registrar os resultados de baseline em specs/010-renomeacao-nodra/quickstart.md, distinguindo o ERESOLVE Angular preexistente de regressoes e sem ler secrets.

## Fase 2: Fundacao

**Gate**: Concluir antes das historias; nenhuma substituicao global sobre identificadores persistidos.

- [x] T004 Atualizar tools/scripts/test-validate-repository.ps1 e tools/scripts/validate-repository.ps1 com testes de nomes esperados Nodra, referencias de projetos/arquivos e excecoes previstas em specs/010-renomeacao-nodra/contracts/compatibilidade.md, preservando a passagem do baseline ate a migracao dos consumidores.
- [x] T005 Confirmar em specs/010-renomeacao-nodra/contracts/compatibilidade.md os literais de banco, identidade, Data Protection, cookies, headers, configuracao, preferencias e secrets que permanecem identicos; adicionar checks nas suites existentes antes de renomear consumidores.

## Fase 3: US1 - Identidade Nodra (P1)

**Objetivo**: Apresentar um produto chamado Nodra, assinado por Altivy, sem prometer novas capacidades entregues.
**Teste independente**: Comparar apresentacao vigente e telas com o registro aprovado; executar expectativas de titulo e marca nas suites existentes.

- [x] T006 [US1] Registrar empresa Altivy, produto Nodra, marca Altivy Nodra, tagline, posicionamento literal e sequencia Notes → Nodes → Connections → Knowledge → AI em README.md, docs/plano-mestre.md e docs/brand/README.md, explicitando IA opcional e nenhuma nova CLI.
- [x] T007 [US1] Atualizar nome do pacote/alvo em frontend/web/package.json, frontend/web/package-lock.json e frontend/web/angular.json sem atualizar versoes de dependencias nem mascarar o bloqueio Angular.
- [x] T008 [US1] Atualizar titulo em frontend/web/src/index.html, titulos de rota em frontend/web/src/app/app.routes.ts e demais rotulos/nomes acessiveis pertinentes sob frontend/web/src/app; preservar chaves de preferencias e protocolos legados.
- [x] T009 [US1] Revisar assets sob docs/brand e frontend/web/public e atualizar somente marcas de produto aplicaveis, preservando a marca corporativa Altivy e sem inventar imagens externas nao licenciadas.
- [x] T010 [US1] Atualizar expectativas unitarias e jornadas existentes em frontend/web/src/app e frontend/tests/e2e para Nodra/Altivy Nodra; validar screenshot desktop e mobile pela topologia deploy/e2e/nginx-mocked.conf quando o gate de dependencias permitir.

## Fase 4: US2 - Codigo e distribuicao renomeados (P1)

**Objetivo**: Compilar e distribuir o mesmo produto com nomes coerentes Nodra, preservando as integracoes existentes.
**Teste independente**: Resolver projetos, compilar/testar em containers e verificar entrypoints e referencias, sem mudar APIs funcionais.

- [x] T011 [US2] Renomear Altivy.slnx para Nodra.slnx e os arquivos csproj de backend/apps e backend/libs para prefixo Nodra, atualizando AssemblyName, RootNamespace, namespaces, usings e ProjectReference; preservar chaves/literais do contrato de compatibilidade.
- [x] T012 [US2] Renomear diretorios/projetos de testes prefixados Altivy em backend/tests e suas referencias para Nodra; atualizar assertions de assembly/namespace sem alterar expectativas de schema, identidade ou dados persistidos.
- [x] T013 [US2] Atualizar Dockerfiles e entradas de assemblies em backend/apps, frontend/web e deploy/production/migrations.Dockerfile, incluindo COPY e comandos de build, para os caminhos Nodra.
- [x] T014 [US2] Atualizar a apresentacao OpenAPI em contracts/openapi/altivy-api.yaml e suas referencias, renomeando o arquivo para nodra-api.yaml sem alterar paths, versoes, DTOs, ETags, erros ou autorizacao.
- [x] T015 [US2] Atualizar .github/workflows/ci.yml, .github/workflows/release.yml e referencias ativas ao repositorio para altivy-it/nodra e aos artefatos Nodra; manter namespace do registry existente salvo decisao explicita, imagens base fixadas e nenhuma publicacao como validacao.
- [x] T016 [US2] Atualizar instrucoes, agentes e auxiliares locais em .github, ferramentas em tools/scripts e referencias de solucao/projetos; preservar tokens e componentes oficiais do Spec Kit e executar auditoria de localizacao somente se esses componentes forem alterados.
- [x] T017 [US2] Atualizar documentacao vigente em docs/backend.md, docs/frontend.md e demais documentos ativos afetados, incluindo links para arquivos renomeados; registrar a preservacao de specs anteriores, ADRs historicos e docs/plan em specs/010-renomeacao-nodra/research.md.
- [ ] T018 [US2] Executar formatador/analisadores, build e suites backend renomeadas em container, com PostgreSQL real quando necessario; registrar evidencia em specs/010-renomeacao-nodra/quickstart.md.
- [x] T019 [US2] Executar instalacao reproduzivel, testes e build frontend em container conforme .github/workflows/ci.yml, sem flags permissivas; registrar falhas preexistentes separadamente em specs/010-renomeacao-nodra/quickstart.md.

## Fase 5: US3 - Compatibilidade operacional (P1)

**Objetivo**: Manter dados e acesso enquanto nomes publicos e internos mudam.
**Teste independente**: Conferir instalacao limpa/atualizacao com dados sinteticos e contratos legados, sem acessar secrets reais.

- [x] T020 [US3] Renomear arquivos e referencias internas de deploy/e2e, deploy/self-hosted, deploy/production e diretorios especificos do produto em deploy/nginx e deploy/postgres quando nao forem bind mounts externos; manter variaveis ALTIVY_* e identificadores persistidos conforme contrato.
- [x] T021 [US3] Revisar deploy/keycloak/realm e deploy/keycloak/themes, atualizando apenas apresentacao de produto e assets aplicaveis; manter realm, clientes, audience e infraestrutura de identidade AKB, sem recriar recursos nem afrouxar validacao.
- [x] T022 [US3] Validar em backend/apps/bff e backend/libs/infrastructure/data os nomes Data Protection, cookies, antiforgery, AltivyPostgres, schema altivy e historico de migrations; executar os testes de compatibilidade preparados em T005.
- [x] T023 [US3] Validar preservacao das chaves do navegador em frontend/web/src/app/core/vaults.service.ts, frontend/web/src/app/shell/preferences.service.ts e frontend/web/src/app/features/explorer/folder-explorer.component.ts, com fixtures sinteticas nas suites existentes.
- [x] T024 [US3] Documentar atualizacao, excecoes e recuperacao em deploy/self-hosted/README.md e deploy/production/README.md, preservando paths externos, configuracao existente e o arquivo Microsoft/UserSecrets/Altivy.Notes/.env sem le-lo ou move-lo.
- [ ] T025 [US3] Validar sintaxe de Compose/scripts, instalacao limpa e continuidade de instalacao sintetica usando deploy/e2e, sempre com borda nginx para HTTP; registrar resultados e limitacoes em specs/010-renomeacao-nodra/quickstart.md.

## Fase 6: Refinamento e Operacao Remota

- [ ] T026 Auditar ocorrencias antigas, referencias quebradas e diff final com tools/scripts/validate-repository.ps1 e registrar as excecoes restantes em specs/010-renomeacao-nodra/research.md; executar os gates definidos, sem marcar bloqueios como sucesso.
- [x] T027 Reconfirmar o momento da renomeacao global, verificar permissao administrativa e ausencia de colisao para altivy-it/nodra; registrar a decisao em specs/010-renomeacao-nodra/quickstart.md antes de alterar o remoto.
- [x] T028 Renomear o repositorio existente para altivy-it/nodra e atualizar origin em .git/config preservando transporte/autenticacao; verificar o mesmo ID do repositorio, historico, issues/PRs e acesso, registrando evidencia em specs/010-renomeacao-nodra/quickstart.md, sem push, deploy ou merge automatico.
- [x] T029 Executar /speckit-converge sobre specs/010-renomeacao-nodra e registrar trabalho restante em tasks.md; informar claramente bloqueios, arquivos alterados, verificacoes executadas e operacoes remotas nao realizadas.

## Dependencias e Ordem

Preparacao → Fundacao → US1/US2/US3 conforme dependencias → Auditoria → Confirmacao remota → Renomeacao remota → Convergencia.

- US1: T006 antes de T007-T010; testes visuais dependem do desbloqueio Angular autorizado.
- US2: T011 antes de T012-T018; T013/T015 acompanham nomes finais de assemblies e arquivos.
- US3: T020-T025 seguem o contrato T005 e usam artefatos renomeados de US2, sem modificar dados para compensar nomes novos.
- T028 depende de T027 e de ausencia de bloqueio que o mantenedor considere impeditivo; nao contornar permissao negada.

## Paralelismo

Sem tarefas marcadas P: o escopo possui referencias cruzadas e nomes de arquivo compartilhados. Depois de estabilizar T011, testes de frontend e backend podem executar separadamente se nao disputarem mounts/artefatos. Nao delegar escrita concorrente nos mesmos caminhos.

## Estrategia de Implementacao

Entregar primeiro a identidade documentada e seus testes, depois nomes de compilacao com consumidores, e por ultimo operacao e remoto. Cada incremento recebe validacao focada imediatamente. Nao considerar a feature concluida apenas porque a marca visivel ou o repositorio remoto foram renomeados. Nenhuma mudanca de dependencia, schema, DNS ou capacidade de IA esta autorizada por estas tarefas.

## Phase 7: Convergence

Convergencia executada em 2026-11-11 apos a implementacao. Repositorio remoto
renomeado com o mesmo ID, marca e projetos Nodra aplicados, builds/testes
funcionais aprovados conforme quickstart.md. As lacunas abaixo sao de
qualificacao e nao autorizam correcoes alheias, alteracao de infraestrutura,
publicacao ou deploy sem os gates e decisoes humanas correspondentes.

- [ ] T030 Revisar as pendencias IDE0005/IDE0058 em backend/tests e o formato dos quatro arquivos frontend registrados em specs/010-renomeacao-nodra/quickstart.md; delimitar eventual correcao no fluxo autorizado e repetir dotnet format e Prettier sem supressoes indevidas, conforme FR-013, SC-006 e T018/T026 (partial).
- [ ] T031 Qualificar os Dockerfiles renomeados, boot limpo, upgrade/recuperacao e OIDC real usando deploy/e2e, deploy/self-hosted e o laboratorio autorizado, sem ler secrets ou publicar imagens; registrar evidencia em specs/010-renomeacao-nodra/quickstart.md conforme FR-010, FR-013, SC-003, SC-006 e T025/T026 (partial).
- [ ] T032 Qualificar a condicao UTC em .github/workflows/ci.yml, deploy/e2e e backend/tests/Nodra.Backend.Infra.Data.Postgres.IntegrationTests/TemporalPartitionMigrationTests.cs antes de propor mudancas de ambiente; preservar SQL publicado e registrar decisao humana para qualquer impacto operacional, conforme FR-010, SC-006 e o contrato de compatibilidade (partial).
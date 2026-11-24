# Tarefas: Vault de exemplo somente leitura

**Entrada**: Artefatos de desenho em `/specs/013-vault-exemplo/`
**Pré-requisitos**: plan.md, spec.md, research.md, data-model.md, contracts/

Formato: `[ID] [P?] [História] Descrição`. `[P]` indica tarefa paralelizável.

## Fase 1: Preparação

- [x] T001 Mover `tests/fixtures/markdown/documento-modelo.md` para `backend/apps/api/SampleVault/documento-modelo.md` com `git mv`, ajustar o link interno para âncora e declarar `EmbeddedResource` em `backend/apps/api/Nodra.Notes.Api.csproj`
- [x] T002 [P] Criar `backend/libs/application/Vaults/SampleVault.cs` com `Id`, `Name`, `DocumentPath` e `IsReadOnly(string)`

## Fase 2: Fundação (bloqueia as histórias)

- [x] T003 Criar migration de dados `backend/libs/infrastructure/data/Migrations/20261123120000_AddSampleVault.cs` (insert idempotente em `altivy.vaults`, `Down` remove)
- [x] T004 [P] Incluir o vault de exemplo em `backend/apps/api/Vaults/LabVaultReader.cs`
- [x] T005 Adicionar `ReadOnly` em `backend/apps/api/Vaults/VaultSummaryResponse.cs` e preenchê-lo em `ListVaultsAsync`; atualizar `contracts/openapi/nodra-api.yaml`

## Fase 3: História 1 — Explorar o vault de exemplo (P1)

- [x] T006 [US1] Criar `backend/apps/api/Vaults/SampleVaultMaterializer.cs` (hosted service, escrita atômica quando ausente ou divergente) e registrar em `ApiServiceCollectionExtensions.cs`
- [x] T007 [P] [US1] Testes de materialização em filesystem real em `backend/tests/Nodra.Notes.Api.UnitTests/Vaults/SampleVaultMaterializerTests.cs`
- [x] T008 [P] [US1] Teste de contrato da listagem com `readOnly` em `backend/tests/Nodra.Notes.Api.UnitTests/Contract/ApiContractTests.cs`
- [x] T009 [US1] Remover o seed do documento modelo de `tools/scripts/start-local-lab.ps1` e chamar `provision-sample-vault-authorization.ps1`
- [x] T010 [P] [US1] Script `tools/scripts/provision-sample-vault-authorization.ps1` com `--help` e `-DryRun`, validado no Keycloak do laboratório
- [x] T011 [US1] Exibir indicação de somente leitura no seletor de vaults (`frontend/web/src/app/core/vaults.service.ts`, shell)

## Fase 4: História 2 — Experimentar sem gravar (P1)

- [x] T012 [US2] Em `frontend/web/src/app/app.ts`, não enviar gravação quando o vault é somente leitura e bloquear criar/excluir/mover/lixeira
- [x] T013 [US2] Aviso persistente e ação "Restaurar original" em `frontend/web/src/app/shell/workspace-shell.ts` e `workspace-shell-v2.html`
- [x] T014 [P] [US2] Testes Vitest do comportamento somente leitura em `frontend/web/src/app/app.spec.ts`

## Fase 5: História 3 — Proteger o vault (P2)

- [x] T015 [US3] Recusar `vault-read-only` em `AuthorizeAsync` de `ApiEndpointRouteBuilderExtensions.cs` e `TrashPurgeEndpoint.cs`
- [x] T016 [P] [US3] Testes de contrato de recusa para PUT/POST/PATCH/DELETE de nota, pasta, lixeira e purge em `ApiContractTests.cs`
- [x] T017 [US3] Ocultar ações de escrita no explorador (`frontend/web/src/app/features/explorer/folder-explorer.component.*`)

## Fase 6: Acabamento

- [x] T018 Atualizar `docs/configuracao-keycloak.md`, `docs/backend.md` e `docs/frontend.md`
- [x] T019 Rebuild do laboratório, remover o `documento-modelo.md` legado de `lab-0001` e validar o quickstart pelo nginx com Playwright e screenshot
- [x] T020 Executar build, formatação e testes do backend e frontend
- [x] T021 Publicar o produto em `https://localhost:7443/nodra` (antes `/notes`): `ForwardedPrefixMiddleware`, `WEB_BASE_HREF`, `ORIGIN`, scripts do laboratório/Keycloak, gateway com redirect `/notes` → `/nodra`, testes e documentação

## Dependências

T001/T002 → T003–T005 → US1, US2, US3. T015 independe de US1/US2 no backend.
T019 depende de todas as demais.

# Plano de Implementação: Vault de exemplo somente leitura

**Branch**: `013-vault-exemplo` | **Data**: 2026-11-23 | **Spec**: [spec.md](./spec.md)

**Entrada**: Especificação da funcionalidade em `/specs/013-vault-exemplo/spec.md`

## Resumo

Criar o vault de sistema `nodra-exemplo` ("Vault de exemplo"), que contém o
documento modelo Markdown versionado com a aplicação. A API materializa o
documento no filesystem na inicialização, registra o vault na projeção
PostgreSQL por migration de dados e recusa toda escrita no vault com o problema
`vault-read-only`. O Keycloak continua decidindo a leitura; um script
administrativo concede `vault:read` a todos os usuários do realm via role
policy em `default-roles-<realm>`. O frontend recebe `readOnly` na listagem de
vaults, permite edição apenas em memória, oculta ações de escrita e oferece
"Restaurar original".

## Contexto Tecnico

**Linguagem/Versão**: C# / .NET 10 (API) e TypeScript / Angular 22 (web); PowerShell 7.4 (script administrativo).

**Dependências Principais**: ASP.NET Core Minimal APIs, EF Core + Npgsql (migrations), Keycloak 26.7 Authorization Services, CodeMirror (editor), PO UI. Nenhuma dependência nova.

**Persistência**: Arquivo Markdown canônico no vault `{VaultStorage:Root}/nodra-exemplo/`, materializado a partir de recurso embutido no assembly da API; linha em `altivy.vaults` como projeção reconstruível.

**Testes**: `Nodra.Notes.Api.UnitTests` (contrato HTTP com `WebApplicationFactory`), `Nodra.Backend.Infra.Data.Postgres.IntegrationTests` (materialização em filesystem real), Vitest do Angular (`ng test`), Playwright pelo nginx do laboratório.

**Plataforma Alvo**: Aplicação self-hosted em containers Podman/Docker, acessada pela borda nginx (`https://localhost:7443/notes`).

**Tipo de Projeto**: Monorepo web (Angular + BFF + API + worker).

**Metas de Desempenho**: "Restaurar original" < 2 s (uma leitura `GET` da nota); materialização na inicialização < 100 ms (arquivo de ~14 KB).

**Restrições**: Sem políticas de autorização próprias que concedam acesso; a regra da API apenas nega escrita. Sem armazenamento no navegador para edições locais. Escrita atômica do arquivo (temporário + rename).

**Escala/Escopo**: Um vault de sistema, um documento; afeta ~10 endpoints de escrita e 4 componentes de UI.

## Verificação da Constituição

*GATE: aprovado antes da Fase 0 e revisado após a Fase 1.*

| Princípio | Atendimento |
|-----------|-------------|
| I. Integridade e Portabilidade dos Dados | O documento modelo continua um arquivo Markdown no vault; a linha no PostgreSQL é projeção recriável pela migration. A materialização só sobrescreve o arquivo de sistema do vault de exemplo, nunca conteúdo de usuário, e grava de forma atômica. Edições locais nunca chegam ao servidor. |
| II. Identidade e Autorização Externas | A leitura segue decidida pelo Keycloak (resource `vault-nodra-exemplo`, scope `vault:read`). A concessão a todos os usuários é feita por policy do próprio Keycloak, provisionada por script administrativo. A API apenas acrescenta uma negação de integridade de conteúdo — ver Registro de Complexidade. |
| III. Segurança e Privacidade | A negação ocorre antes de qualquer acesso ao filesystem; o identificador passa por `VaultId.TryParse`. O script não exibe nem grava secrets. O documento é sintético. |
| IV. Arquitetura Modular e Evolução Compatível | `SampleVault` fica em `backend/libs/application`; o host da API compõe. `readOnly` é campo aditivo no contrato; o problema `vault-read-only` reaproveita 403. Migration de dados com `Down`. Sem novo serviço ou datastore. |
| V. Qualidade Verificável e Documentação em pt-BR | Testes de contrato para recusa e listagem, integração de filesystem para materialização, Vitest para UI e Playwright no laboratório. Documentação em pt-BR. |

**Resultado**: aprovado com uma justificativa registrada. Reavaliado após a Fase 1: sem novas violações.

## Estrutura do Projeto

### Documentação desta Funcionalidade

```text
specs/013-vault-exemplo/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── vault-exemplo-api.md
└── tasks.md
```

### Código-Fonte (raiz do repositório)

```text
backend/
├── apps/api/
│   ├── Endpoints/ApiEndpointRouteBuilderExtensions.cs   # recusa vault-read-only e readOnly na listagem
│   ├── Endpoints/TrashPurgeEndpoint.cs                  # recusa vault-read-only
│   ├── Vaults/VaultSummaryResponse.cs                   # campo ReadOnly
│   ├── Vaults/LabVaultReader.cs                         # inclui o vault de exemplo sem PostgreSQL
│   ├── Vaults/SampleVaultMaterializer.cs                # hosted service de materialização
│   ├── SampleVault/documento-modelo.md                  # recurso embutido (movido de tests/fixtures)
│   └── DependencyInjection/ApiServiceCollectionExtensions.cs
├── libs/application/Vaults/SampleVault.cs               # identidade e regra somente leitura
├── libs/infrastructure/data/Migrations/                 # migration de dados do vault de exemplo
└── tests/
    ├── Nodra.Notes.Api.UnitTests/Contract/ApiContractTests.cs
    └── Nodra.Notes.Api.UnitTests/Vaults/SampleVaultMaterializerTests.cs

frontend/web/src/app/
├── core/vaults.service.ts
├── app.ts
├── shell/workspace-shell.ts / workspace-shell-v2.html
└── features/explorer/folder-explorer.component.*

contracts/openapi/nodra-api.yaml
tools/scripts/provision-sample-vault-authorization.ps1
tools/scripts/start-local-lab.ps1
docs/configuracao-keycloak.md
```

**Decisão de Estrutura**: A identidade do vault de sistema (`SampleVault`) é regra reutilizável e fica em `libs/application`. O recurso embutido e o hosted service ficam no host da API, que é o único processo que serve conteúdo do vault e já conhece `VaultStorage:Root`.

## Registro de Complexidade

| Violação | Motivo da Necessidade | Alternativa Mais Simples Rejeitada Porque |
|-----------|------------|-------------------------------------|
| Negação de escrita na API para `nodra-exemplo` independente da decisão do Keycloak (Princípio II). | FR-005 exige proteção mesmo para usuários com permissões amplas; é uma regra de integridade de conteúdo do produto, não uma política de acesso. Ela só nega — nunca concede — e falha fechada. | Depender só do Keycloak deixaria o vault gravável por qualquer policy ampla (ex.: administradores com acesso a todos os recursos) e por erro de configuração. |

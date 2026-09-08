# Infraestrutura PostgreSQL do Altivy

Este projeto contém o `PostgresDbContext`, as migrations e os adapters de
leitura da projeção PostgreSQL do Altivy.

## Banco e isolamento

O Altivy usa o mesmo **database PostgreSQL** já utilizado pelo laboratório do
Keycloak (`keycloak`), mas não compartilha schema nem permissões:

- Keycloak mantém suas tabelas no schema próprio;
- Altivy usa exclusivamente o schema `altivy`;
- `altivy_migrator` aplica migrations e possui DDL somente durante o rollout;
- `altivy_runtime` é a role da API e recebe apenas DML no schema `altivy`;
- nenhuma role da aplicação administra users, databases, extensões ou o schema
  do Keycloak.

A criação das roles e grants é uma ação explícita do operador, separada da API.
O SQL versionado está em
[`sql/001-bootstrap-altivy-roles.sql`](sql/001-bootstrap-altivy-roles.sql) e é
executado de modo idempotente pelo script abaixo. Ele lê somente as senhas já
presentes nas duas connection strings locais, não as mostra e não usa a senha
do Keycloak como credencial das roles do Altivy:

```powershell
.\tools\scripts\bootstrap-altivy-postgres.ps1
```

O script exige que `$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env` configure
`ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING` e
`ALTIVY_INTEGRATION_RUNTIME_CONNECTION_STRING` para `localhost:5432`, database
`keycloak` e as roles correspondentes. Ele executa somente no container
principal `altivy-postresql`; não execute esse SQL no startup da aplicação.

## Secrets locais e design-time

Toda connection string local fica exclusivamente no arquivo externo
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`. Nunca crie `.env`,
`secrets.json` ou arquivo equivalente dentro do repositório.

Defina nesse arquivo `ALTIVY_DESIGNTIME_CONNECTION_STRING` para operações do EF
Core e `ConnectionStrings__AltivyPostgres` para API e worker. Antes de executar
comandos .NET, importe as variáveis para o processo atual sem exibir valores:

```powershell
$EnvironmentPath = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\.env'
Get-Content -LiteralPath $EnvironmentPath | ForEach-Object {
  if ($_ -match '^\s*(?<Name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<Value>.*)\s*$') {
    [Environment]::SetEnvironmentVariable($Matches.Name, $Matches.Value.Trim('"').Trim("'"), 'Process')
  }
}
```

Em CI e produção, injete `ConnectionStrings__AltivyPostgres` pelo mecanismo de
secrets da plataforma, sem materializar o arquivo local do Windows.

## Criar migration

Execute a partir da raiz, usando a ferramenta versionada em `dotnet-tools.json`:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add NomeDaMigration `
  --project .\backend\libs\backend\infrastructure\data\Altivy.Backend.Infra.Data.Postgres.csproj `
  --output-dir Migrations `
  --configuration Release
```

Antes de versionar a migration:

1. revise o schema, tabelas, índices e operações destrutivas;
2. confirme que o modelo usa Fluent API em `*Configuration.cs`;
3. verifique o SQL gerado e o impacto de locks;
4. registre rollout e rollback no plano ou PR;
5. não inclua connection strings ou secrets no diff.

A migration inicial atual é `Migrations/20260819181928_InitialCreate` e cria
somente `altivy.vaults`.

## Atualizar o banco

A aplicação não aplica migrations automaticamente. Depois do backup e da
aprovação do rollout, execute:

```powershell
dotnet tool run dotnet-ef database update `
  --project .\backend\libs\backend\infrastructure\data\Altivy.Backend.Infra.Data.Postgres.csproj `
  --configuration Release
```

O comando usa `ALTIVY_DESIGNTIME_CONNECTION_STRING`, carregada no processo a
partir do arquivo externo local. Em servidor, configure
`ConnectionStrings__AltivyPostgres` no secret store do ambiente, não no
repositório.

## Gerar script SQL

Para revisão ou execução controlada pelo DBA, gere SQL idempotente entre duas
migrations:

```powershell
dotnet tool run dotnet-ef migrations script `
  --project .\backend\libs\backend\infrastructure\data\Altivy.Backend.Infra.Data.Postgres.csproj `
  --from 0 `
  --to 20260819181928_InitialCreate `
  --output .\artifacts\sql\altivy-initial.sql `
  --configuration Release
```

Revise o arquivo antes de executar. O script SQL não deve ser colocado no
bootstrap do container PostgreSQL, pois migrations são uma etapa explícita do
rollout.

## Scaffolding de banco existente

Scaffolding é apenas uma ferramenta de diagnóstico/migração de legado. O
resultado deve ser revisado e alinhado ao domínio; não substitui as entidades,
portas ou mapeamentos do Altivy.

```powershell
dotnet tool run dotnet-ef dbcontext scaffold `
  'Name=ConnectionStrings:AltivyPostgres' `
  Npgsql.EntityFrameworkCore.PostgreSQL `
  --project .\backend\libs\backend\infrastructure\data\Altivy.Backend.Infra.Data.Postgres.csproj `
  --context PostgresDbContext `
  --schema altivy `
  --output-dir Scaffolding `
  --no-onconfiguring `
  --force
```

Não faça scaffolding do schema do Keycloak. O filtro `--schema altivy` é
obrigatório para manter a fronteira de dados.

## Validação

```powershell
dotnet build .\backend\libs\backend\infrastructure\data\Altivy.Backend.Infra.Data.Postgres.csproj --configuration Release
dotnet test .\Altivy.slnx --configuration Release --filter "Category=Unit"
dotnet test .\Altivy.slnx --configuration Release --filter "Category=Integration"
```

Testes que provam comportamento PostgreSQL devem usar PostgreSQL real, nunca
banco em memória. O teste de integração da migration deve criar um database ou
schema isolado, aplicar a migration, inserir dados sintéticos, consultar pelo
adapter e remover o ambiente ao final.

## Teste de integração no laboratório principal

Os testes de integração usam exclusivamente o PostgreSQL persistente da stack
principal `altivy-keycloak-lab`, definida em
[`deploy/keycloak/docker-compose-keycloak.yaml`](../../../../deploy/keycloak/docker-compose-keycloak.yaml).
O banco é publicado somente em `127.0.0.1:5432`; a porta `55432` da antiga
stack descartável é recusada pela suíte para evitar execução no ambiente errado.

Antes de executar, inicie a stack e confirme que as roles `altivy_migrator` e
`altivy_runtime`, o schema `altivy` e os grants descritos em [Banco e
isolamento](#banco-e-isolamento) foram provisionados pelo script
`tools/scripts/bootstrap-altivy-postgres.ps1`. No arquivo local ignorado
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, configure as duas connection strings de integração para
`Host=localhost;Port=5432` sem registrar valores no Git.

Na raiz do repositório, carregue essas variáveis no terminal e execute:

```powershell
dotnet test .\backend\tests\Altivy.Backend.Infra.Data.Postgres.IntegrationTests\Altivy.Backend.Infra.Data.Postgres.IntegrationTests.csproj `
  --configuration Release --filter 'Category=Integration'
```

Os testes aplicam migrations no schema `altivy` e removem somente os dados
sintéticos que criam. Não interrompa nem destrua a stack principal ao final.

# E2E do BFF

Esta suíte executa em `lzocateli/playwright-e2e:0.1.0`. A imagem já contém
Python 3.12, pytest 8.3.4, Playwright 1.49.0 e browsers; não instale Node,
Playwright ou browsers no host.

## Pré-requisitos

- `altivy-web`, BFF, API e Keycloak acessíveis pela URL pública do ambiente;
- Docker Desktop com containers Linux;
- para cenários autenticados, credenciais de laboratório somente em variáveis
  de ambiente locais.

## Executar

No PowerShell, a partir da raiz do repositório:

```powershell
$env:ALTIVY_E2E_BASE_URL = 'https://localhost:7443'
docker run --rm --network host `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  --volume "${PWD}/artifacts/e2e:/app/reports" `
  lzocateli/playwright-e2e:0.1.0 `
  --base-url=$env:ALTIVY_E2E_BASE_URL
```

Para login, logout e autorização negada, defina os valores locais antes de
executar. Eles não devem ser versionados nem impressos em logs:

```powershell
$env:ALTIVY_E2E_USERNAME = '<usuario-do-laboratorio>'
$env:ALTIVY_E2E_PASSWORD = '<senha-do-laboratorio>'
$env:ALTIVY_E2E_DENIED_VAULT_ID = '<vault-sem-permissao-de-leitura>'
```

No laboratório atual, `lab-0002` é o recurso de negação usado pelo cenário
E2E. Confirme que o usuário configurado não possui `vault:read` nesse recurso
antes de usá-lo como `ALTIVY_E2E_DENIED_VAULT_ID`.

Para executar a jornada de alternância, use um usuário de laboratório com
`vault:read` nos dois recursos sintéticos `lab-0001` e `lab-0002`. Para testar
fallback seguro, use outro usuário com acesso a apenas um deles. As
associações pertencem somente ao Keycloak; não crie memberships no PostgreSQL.

O conjunto completo pode ser executado dentro da imagem oficial, a partir da
raiz do repositório:

```powershell
$env:ALTIVY_E2E_BASE_URL = 'https://localhost:7443'
docker run --rm --network host `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  lzocateli/playwright-e2e:0.1.0 `
  --base-url=$env:ALTIVY_E2E_BASE_URL
```

Para validar apenas o catálogo e a alternância:

```powershell
docker run --rm --network host `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  lzocateli/playwright-e2e:0.1.0 `
  --base-url=$env:ALTIVY_E2E_BASE_URL -m authenticated -k "catalog or switch or reload"
```

## Cobertura e limites

- sessão anônima e ausência de tokens em Web Storage;
- bloqueio de negociação de hubs SignalR (`/hubs/vaults`) para clientes anônimos;
- acesso autenticado a vault autorizado (`GET /api/v1/vaults/lab-0001`);
- negociação e handshake de hubs SignalR autenticado via proxy BFF (`/hubs/vaults/negotiate`);
- login pelo BFF com PKCE e logout seguro com token antiforgery;
- negação de acesso a vault não autorizado (`lab-0002` retornando `403 application/problem+json`);
- validação de revogação imediata de permissões via script automatizado (`tools/scripts/test-keycloak-revocation.ps1`).
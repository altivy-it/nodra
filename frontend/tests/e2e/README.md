# E2E do BFF

Esta suíte executa em `lzocateli/playwright-e2e:0.1.0`. A imagem já contém
Python 3.12, pytest 8.3.4, Playwright 1.49.0 e browsers; não instale Node,
Playwright ou browsers no host.

## Pré-requisitos

- `nodra-web`, BFF, API e Keycloak acessíveis pela URL pública do ambiente;
- Docker Desktop com containers Linux;
- para cenários autenticados, credenciais de laboratório somente em variáveis
  de ambiente locais.

Testes e ferramentas rodam em containers. Quando a aplicação estiver em
execução, o navegador e o runner acessam somente nginx em container, nunca a
porta do Angular, BFF ou API. O laboratório real usa o gateway HTTPS em
`https://localhost:7443/nodra`, conforme `deploy/e2e/README.md`.

## Executar cenários com API simulada

A partir da raiz do repositório no PowerShell 7, inicie uma rede isolada, o
Angular sem porta publicada e o proxy nginx como única borda. A imagem e o
volume de `node_modules` são os mesmos usados pelo alias `ng` do profile.
Não use credenciais ou dados reais nesta suíte.

```powershell
Set-Location $env:USERPROFILE\projetos\nodra
docker network inspect altivy-mocked-e2e *> $null
if ($LASTEXITCODE -ne 0) { docker network create altivy-mocked-e2e }
$webPath = (Resolve-Path frontend/web).Path
$webHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($webPath))).Substring(0, 12).ToLowerInvariant()
docker run --rm --detach --name altivy-mocked-web --network altivy-mocked-e2e --network-alias web `
  --volume "${webPath}:/workspace" `
  --volume "ng-node-modules-${webHash}:/workspace/node_modules:nocopy" `
  --workdir /workspace lzocateli/angular-cli:22.1.0-node24.15.0-bookworm ng serve --host 0.0.0.0
docker run --rm --detach --name altivy-mocked-proxy --network altivy-mocked-e2e `
  --publish 127.0.0.1:8088:8080 `
  --volume "${PWD}/deploy/e2e/nginx-mocked.conf:/etc/nginx/conf.d/default.conf:ro" `
  lzocateli/nginx:1.28.0-bookworm
```

Confira a borda com `Invoke-WebRequest http://localhost:8088/`. Se o Angular
ainda estiver compilando, aguarde a resposta 200 do nginx antes do teste. O
nginx encaminha o Host `localhost` aceito pelo servidor Angular. Na rede
isolada, rode Playwright apontando para **o proxy**, não para `web:4200`:

```powershell
docker run --rm --network altivy-mocked-e2e `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  --volume "${PWD}/artifacts/e2e:/app/reports" `
  lzocateli/playwright-e2e:0.1.0 `
  --base-url=http://altivy-mocked-proxy:8080 -m mocked -p no:cacheprovider
```

O resultado esperado são os testes marcados `mocked` aprovados. Para abrir a
tela sintética no navegador, use somente `http://localhost:8088/`. Ao terminar,
encerre somente os containers criados para esta suíte:

```powershell
docker stop altivy-mocked-proxy altivy-mocked-web
docker network rm altivy-mocked-e2e
```

## Executar

No PowerShell, a partir da raiz do repositório:

```powershell
$env:ALTIVY_E2E_BASE_URL = 'https://localhost:7443/nodra'
docker run --rm --network host `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  --volume "${PWD}/artifacts/e2e:/app/reports" `
  lzocateli/playwright-e2e:0.1.0 `
  --base-url=$env:ALTIVY_E2E_BASE_URL
```

### Production self-hosted

O runner em `deploy/self-hosted/compose.e2e.yaml` publica somente as variáveis
de usuário/senha E2E dentro do container; os demais valores presentes nos
arquivos externos não são encaminhados. Compose usa os arquivos somente para
interpolação, sem imprimir a configuração resolvida. Execute a partir da raiz
do Nodra depois do boot:

```powershell
$identityEnv = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Identity\.env'
$notesEnv = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\.env'
docker compose --env-file $identityEnv --env-file $notesEnv `
  -f deploy/self-hosted/compose.e2e.yaml --profile e2e run --rm browser-tests `
  /app/tests/e2e/test_bff_session.py `
  --base-url=https://localhost:8443 --write-vault-id=lab-write-e2e `
  -k 'anonymous or log_out or autosave or negotiate_signalr' -p no:cacheprovider
```

As credenciais de teste precisam estar disponíveis como
`ALTIVY_E2E_USERNAME` e `ALTIVY_E2E_PASSWORD` no processo ou nos arquivos
externos. Sem elas, os casos autenticados são explicitamente marcados como
skipped. A opção `--write-vault-id` exige um grant já existente no Keycloak;
o runner não altera autorização.

Para login, logout e autorização negada, defina os valores locais antes de
executar. Eles não devem ser versionados nem impressos em logs:

```powershell
$env:ALTIVY_E2E_USERNAME = '<usuario-do-laboratorio>'
$env:ALTIVY_E2E_PASSWORD = '<senha-do-laboratorio>'
$env:ALTIVY_E2E_DENIED_VAULT_ID = '<vault-sem-permissao-de-leitura>'
```

Para validar gravação e autosave, use um vault sintético isolado previamente
provisionado no filesystem, no PostgreSQL e no Keycloak. Não use `lab-0001` ou
`lab-0002`; o teste cria notas com nomes únicos e as mantém nesse vault. Defina
`ALTIVY_E2E_WRITE_VAULT_ID` e execute somente o cenário autenticado de escrita:

```powershell
$env:ALTIVY_E2E_WRITE_VAULT_ID = 'lab-write-e2e'
docker run --rm --network host `
  --env ALTIVY_E2E_USERNAME --env ALTIVY_E2E_PASSWORD `
  --env ALTIVY_E2E_WRITE_VAULT_ID `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  --volume "${PWD}/artifacts/e2e:/app/reports" `
  lzocateli/playwright-e2e:0.1.0 `
  /app/tests/e2e/test_bff_session.py `
  --base-url=https://localhost:7443/nodra -m authenticated -k autosave
```

O teste confirma POST, leitura com ETag, atualização PUT com `If-Match` e
persistência pelo BFF/gateway. Jobs de indexação são processados pelo worker; a
limpeza de notas retidas na lixeira exige um procedimento administrativo
separado.

Para conferir a persistência depois de recriar web/BFF/API/worker, informe o
caminho de uma nota criada pela jornada anterior:

```powershell
$env:ALTIVY_E2E_PERSISTED_NOTE_PATH = '<caminho-retornado-pelo-job-de-teste>'
docker run --rm --network host `
  --env ALTIVY_E2E_USERNAME --env ALTIVY_E2E_PASSWORD `
  --env ALTIVY_E2E_WRITE_VAULT_ID --env ALTIVY_E2E_PERSISTED_NOTE_PATH `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  lzocateli/playwright-e2e:0.1.0 `
  /app/tests/e2e/test_bff_session.py `
  --base-url=https://localhost:7443/nodra -m authenticated -k persisted_note
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
$env:ALTIVY_E2E_BASE_URL = 'https://localhost:7443/nodra'
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

# Laboratório E2E Web-BFF-API

O arquivo canônico desta stack é
`deploy/e2e/docker-compose-nodra.yaml`. Ele cria um ambiente local para
os E2E do BFF. O reverse proxy global não pertence a este Compose: ele roda na
plataforma `architecture-knowledge-base/platforms/identity` e publica
`https://localhost:7443/notes`. BFF, API e Web não possuem portas publicadas neste
Compose; Web e BFF entram na rede `identity-global` para que o gateway encaminhe
as rotas de aplicação e OIDC em uma única origem pública.

## Pré-requisitos

- Docker Desktop com containers Linux;
- a plataforma global de identidade em execução, acessível pela Authority configurada;
- certificado e chave TLS locais confiados pelo navegador;
- client secret de `altivy-bff` fornecido somente pela sessão do terminal.

Este Compose declara `identity-global` como rede externa. Inicie a plataforma
global antes do primeiro `up`:

```powershell
Set-Location ..\architecture-knowledge-base\platforms\identity
./scripts/start-platform.ps1
Set-Location ..\..\..\Nodra
```

O certificado da plataforma global deve ser
legíveis pelo usuário `nginx` da imagem. Mantenha-os fora do repositório e
restrinja a leitura no host ao mínimo necessário para a execução do Docker
Desktop.

## Preparar TLS e segredo

Para desenvolvimento local, gere um certificado confiável para `localhost`
com `mkcert`. Instale-o e registre sua autoridade certificadora local somente
na máquina de laboratório:

```powershell
winget install FiloSottile.mkcert
mkcert -install
$AltivySecretsDirectory = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes'
$AltivyCertsDirectory = Join-Path $AltivySecretsDirectory 'certs'
New-Item -ItemType Directory -Force $AltivyCertsDirectory | Out-Null
mkcert -cert-file (Join-Path $AltivyCertsDirectory 'tls.crt') `
  -key-file (Join-Path $AltivyCertsDirectory 'tls.key') `
  localhost 127.0.0.1 ::1
Copy-Item "$(mkcert -CAROOT)\rootCA.pem" (Join-Path $AltivyCertsDirectory 'rootCA.pem')
New-Item -ItemType Directory -Force .\deploy\e2e\keys | Out-Null
```

Não use esse certificado em produção. Em ambiente compartilhado, monte um
certificado emitido pela autoridade aprovada para o host publicado.

No Admin Console do Keycloak, abra **Clients > altivy-bff > Credentials** e
copie o secret somente para a sessão atual. O procedimento completo de
custódia e rotação está em
[Configuração do Keycloak](../../docs/configuracao-keycloak.md#83-guardar-o-client-secret).

```powershell
$env:ALTIVY_E2E_BFF_CLIENT_SECRET = Read-Host 'Client secret do BFF' -MaskInput
```

## Configurar e iniciar

Na raiz do repositório, defina paths absolutos locais e secretos. Não versione
os valores nem os arquivos montados.

O gateway global é a única origem pública. Configure o Keycloak para anunciar
`https://localhost:7443` e o produto em `https://localhost:7443/notes`:

```powershell
$env:KEYCLOAK_PUBLIC_URL = 'https://localhost:7443'
$env:APP_PUBLIC_URL = 'https://localhost:7443/notes'
$gatewayContainer = docker ps --filter 'name=altivy-identity-gateway-1' --format '{{.ID}}' | Select-Object -First 1
if (-not $gatewayContainer) { throw 'Inicie a plataforma global de identidade antes do Compose E2E.' }
$env:IDENTITY_GATEWAY_IP = docker inspect --format '{{with index .NetworkSettings.Networks "identity-global"}}{{.IPAddress}}{{end}}' $gatewayContainer
if (-not $env:IDENTITY_GATEWAY_IP) { throw 'O gateway não está conectado à rede identity-global.' }
$env:IDENTITY_GATEWAY_SUBNET = docker network inspect identity-global --format '{{(index .IPAM.Config 0).Subnet}}'
if (-not $env:IDENTITY_GATEWAY_SUBNET) { throw 'Não foi possível identificar a sub-rede identity-global.' }
```

Confirme que o issuer retornado é o esperado:

```powershell
curl.exe --noproxy "*" --silent --fail `
  https://localhost:7443/realms/altivy/.well-known/openid-configuration
```

O JSON deve conter `"issuer":"https://localhost:7443/realms/altivy"` quando a
borda estiver ativa.

```powershell
$AltivyCertsDirectory = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\certs'
$env:ALTIVY_E2E_TLS_CERTIFICATE_PATH = Join-Path $AltivyCertsDirectory 'tls.crt'
$env:ALTIVY_E2E_TLS_KEY_PATH = Join-Path $AltivyCertsDirectory 'tls.key'
$env:ALTIVY_E2E_TLS_CA_PATH = Join-Path $AltivyCertsDirectory 'rootCA.pem'
$env:ALTIVY_E2E_DATA_PROTECTION_KEYS_PATH = "$PWD\deploy\e2e\keys"
docker compose -f deploy/e2e/docker-compose-nodra.yaml config --quiet
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-nodra.yaml up --build --wait
```

O IP e a sub-rede são obtidos da rede/container em execução. Não use valores
fixos: a borda pode receber outro endereço após ser recriada, e os containers
Altivy precisam confiar no IP/CIDR atual para alcançar localhost e processar
os forwarded headers.

Neste laboratório, BFF e API usam sempre
`https://localhost:7443/realms/altivy` pelo gateway global. Não defina
`ALTIVY_E2E_KEYCLOAK_AUTHORITY`; ela pertence à topologia anterior.

O Keycloak deve permitir `https://localhost:7443/notes` como redirect URI e web
origin do client `altivy-bff`. O Compose não publica portas da API ou do BFF.
O primeiro comando termina sem saída quando a configuração é válida e não
imprime os valores resolvidos.

### Recriar os consumidores E2E

Após alterar o arquivo externo
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, Dockerfile, proxy ou
configuração do BFF/API/worker, recrie somente os serviços consumidores
`web`, `bff`, `api` e `worker`. O comando abaixo usa esse arquivo externo,
reconstrói as imagens e aguarda os health checks sem interromper Keycloak ou
PostgreSQL:

```powershell
$gatewayContainer = docker ps --filter 'name=altivy-identity-gateway-1' --format '{{.ID}}' | Select-Object -First 1
if (-not $gatewayContainer) { throw 'Inicie a plataforma global de identidade antes do Compose E2E.' }
$env:IDENTITY_GATEWAY_IP = docker inspect --format '{{with index .NetworkSettings.Networks "identity-global"}}{{.IPAddress}}{{end}}' $gatewayContainer
$env:IDENTITY_GATEWAY_SUBNET = docker network inspect identity-global --format '{{(index .IPAM.Config 0).Subnet}}'
if (-not $env:IDENTITY_GATEWAY_IP -or -not $env:IDENTITY_GATEWAY_SUBNET) { throw 'Não foi possível descobrir a rede identity-global atual.' }
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-nodra.yaml up --build --force-recreate --wait web bff api worker
```

No perfil `Development`, quando a seção SMTP estiver completamente ausente, o
worker registra um aviso sanitizado e não envia alertas administrativos. Uma
configuração SMTP parcial continua falhando; Production sempre exige SMTP válido.

O Compose habilita `Bff__AllowInsecureHttpForDevelopment=true` somente para
permitir HTTP na rede Docker interna e alcançar o Keycloak local pelo host.
A opção é desabilitada por padrão e não permite HTTP em ambiente diferente de
`Development`; não a use em deployment compartilhado ou produção.

BFF e API recebem a CA local do `mkcert` em modo somente leitura e usam
`SSL_CERT_FILE` para validar o certificado TLS da borda `localhost`. A CA não
é adicionada à imagem, ao repositório ou ao trust store do host.

Não altere o arquivo hosts. BFF e API acessam a plataforma pelo gateway central em `localhost:7443`.
O Playwright usa `--network host`, para que o Chromium do container alcance a
mesma borda `https://localhost:7443/notes` do navegador no Windows.

O client `altivy-bff` usa somente os callbacks `https://localhost:7443/notes`.
Para um realm já existente, reconcilie-os pela borda:

```powershell
.\tools\scripts\configure-keycloak.ps1 -BaseUrl https://localhost:7443 -BffBaseUrl https://localhost:7443/notes -SkipUser
```

O gateway global fixa `X-Forwarded-Proto=https` e encaminha o host público.
Assim, o BFF gera
o redirect URI OIDC igual ao callback público registrado no Keycloak.

Além de `/bff`, `/api/v1` e `/hubs`, o proxy encaminha `/signin-oidc` e
`/signout-callback-oidc` ao BFF. Esses paths não podem cair no fallback da SPA,
pois são os callbacks que criam ou encerram a sessão server-side.

Após o Compose indicar todos os serviços como `healthy`, confirme a sessão
anônima pelo proxy HTTPS sem passar pelo proxy corporativo:

```powershell
curl.exe --noproxy "*" --insecure https://localhost:7443/notes/bff/session
```

O resultado esperado é `{"authenticated":false,"subject":null,"name":null}`.

## Acessar o Admin Console pela borda

Com a plataforma global saudável e este Compose ativo, o gateway
encaminha `/admin`, `/realms` e `/resources` para o Keycloak pela rede
`identity-global`. Acesse o console em:

- <https://localhost:7443/admin/master/console/> (bootstrap controlado no realm `master`);
- <https://localhost:7443/admin/altivy/console/> (administração do realm `altivy`).

Não use `http://localhost:8080/admin/` enquanto os hostnames apontarem para
`7443`: o Keycloak anunciaria `7443` mas serviria a página por outra origem,
quebrando assets e redirects do console.

Se uma execução anterior tiver falhado antes de o Web iniciar, remova somente
os recursos deste laboratório para recriar os aliases `web`, `bff` e `api`.
Não remova a stack compartilhada do Keycloak/PostgreSQL/NGINX:

```powershell
docker compose -f deploy/e2e/docker-compose-nodra.yaml down
```

## Executar E2E

### Condição Temporal dos Testes PostgreSQL

Os testes de roteamento mensal validam instantes em UTC. Nas conexões
**sintéticas e descartáveis** de integração, configure `Timezone=UTC`, como
nas duas connection strings do job `backend-integration` em
`.github/workflows/ci.yml`. O parâmetro fixa o timezone da sessão Npgsql,
inclusive quando a imagem PostgreSQL usa `America/Sao_Paulo` por padrão.

Essa condição foi validada com PostgreSQL real e a suíte backend completa.
Não exige `ALTER DATABASE`, alteração de migrations/SQL já publicados, leitura
de secrets ou mudança na configuração de ambientes existentes. Não aplique uma
migração operacional de timezone em produção por causa deste teste; qualquer
impacto nesses ambientes exige decisão humana específica.

Com o laboratório ativo, execute a imagem de testes a partir da raiz:

```powershell
$env:ALTIVY_E2E_BASE_URL = 'https://localhost:7443/notes'
docker run --rm --network host `
  --volume "${PWD}/frontend/tests:/app/tests:ro" `
  lzocateli/playwright-e2e:0.1.0 `
  --base-url=$env:ALTIVY_E2E_BASE_URL
```

Para login, logout e negação de vault, defina também as variáveis secretas
descritas em [../../frontend/tests/e2e/README.md](../../frontend/tests/e2e/README.md).

## Validar revogação

Com Keycloak, PostgreSQL e o Compose E2E saudáveis, execute o script abaixo a
partir da raiz. Ele remove temporariamente `lzocateli` de `/altivy/readers`,
valida que `lab-0001` retorna `403` pelo browser e restaura a associação mesmo
quando o teste falhar:

```powershell
.\tools\scripts\test-keycloak-revocation.ps1
```

## Encerrar

```powershell
docker compose -f deploy/e2e/docker-compose-nodra.yaml down
```

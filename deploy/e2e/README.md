# Laboratório E2E Web-BFF-API

O arquivo canônico desta stack é
`deploy/e2e/docker-compose-altivynotes.yaml`. Ele cria um ambiente local para
os E2E do BFF. Apenas `web` publica
`https://localhost:7443`; Keycloak, BFF, API e Web não possuem portas
publicadas. O NGINX de borda também encaminha as rotas OIDC do Keycloak, de modo
que browser e serviços usam uma única origem pública.

## Pré-requisitos

- Docker Desktop com containers Linux;
- o laboratório Keycloak em execução, acessível pela Authority configurada;
- certificado e chave TLS locais confiados pelo navegador;
- client secret de `altivy-bff` fornecido somente pela sessão do terminal.

Este Compose e o [laboratório Keycloak](../keycloak/README.md#criar-a-rede-compartilhada-altivy-edge)
declaram `altivy-edge` como rede externa; nenhum dos dois a cria. Crie-a uma
única vez antes do primeiro `up` (o comando não falha se a rede já existir):

```powershell
if (-not (docker network ls --filter name=^altivy-edge$ --format '{{.Name}}')) {
  docker network create altivy-edge
}
```

O certificado e a chave montados devem ser legíveis pelo usuário `nginx` da
imagem. Mantenha-os fora do repositório e restrinja a leitura no host ao mínimo
necessário para a execução do Docker Desktop.

## Preparar TLS e segredo

Para desenvolvimento local, gere um certificado confiável para `localhost`
com `mkcert`. Instale-o e registre sua autoridade certificadora local somente
na máquina de laboratório:

```powershell
winget install FiloSottile.mkcert
mkcert -install
New-Item -ItemType Directory -Force .\deploy\e2e\certs | Out-Null
mkcert -cert-file .\deploy\e2e\certs\tls.crt `
  -key-file .\deploy\e2e\certs\tls.key `
  localhost 127.0.0.1 ::1
Copy-Item "$(mkcert -CAROOT)\rootCA.pem" .\deploy\e2e\certs\rootCA.pem
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

O NGINX de borda é a única origem pública. Configure o Keycloak para anunciar
`https://localhost:7443`, sem publicar sua porta HTTP no host:

```powershell
$env:ALTIVY_KEYCLOAK_HOSTNAME = 'https://localhost:7443'
$env:ALTIVY_KEYCLOAK_ADMIN_HOSTNAME = 'https://localhost:7443'
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/keycloak/docker-compose-keycloak.yaml up -d --force-recreate keycloak
```

Confirme que o issuer retornado é o esperado:

```powershell
curl.exe --noproxy "*" --silent --fail `
  http://localhost:8080/realms/altivy/.well-known/openid-configuration
```

O JSON deve conter `"issuer":"https://localhost:7443/realms/altivy"` quando a
borda estiver ativa.

```powershell
$env:ALTIVY_E2E_TLS_CERTIFICATE_PATH = "$PWD\deploy\e2e\certs\tls.crt"
$env:ALTIVY_E2E_TLS_KEY_PATH = "$PWD\deploy\e2e\certs\tls.key"
$env:ALTIVY_E2E_TLS_CA_PATH = "$PWD\deploy\e2e\certs\rootCA.pem"
$env:ALTIVY_E2E_DATA_PROTECTION_KEYS_PATH = "$PWD\deploy\e2e\keys"
docker compose -f deploy/e2e/docker-compose-altivynotes.yaml config --quiet
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-altivynotes.yaml up --build --wait
```

Neste laboratório, BFF e API usam sempre
`https://localhost:7443/realms/altivy` pela borda NGINX. Não defina
`ALTIVY_E2E_KEYCLOAK_AUTHORITY`; ela pertence à topologia anterior.

O Keycloak deve permitir `https://localhost:7443` como redirect URI e web
origin do client `altivy-bff`. O Compose não publica portas da API ou do BFF.
O primeiro comando termina sem saída quando a configuração é válida e não
imprime os valores resolvidos.

### Recriar os consumidores E2E

Após alterar o arquivo externo
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, Dockerfile, proxy ou
configuração do BFF/API, recrie somente `web`, `bff` e `api`. O comando abaixo
usa esse arquivo externo, reconstrói as imagens e aguarda os health checks sem
interromper Keycloak ou PostgreSQL:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-altivynotes.yaml up --build --force-recreate --wait
```

O Compose habilita `Bff__AllowInsecureHttpForDevelopment=true` somente para
permitir HTTP na rede Docker interna e alcançar o Keycloak local pelo host.
A opção é desabilitada por padrão e não permite HTTP em ambiente diferente de
`Development`; não a use em deployment compartilhado ou produção.

BFF e API recebem a CA local do `mkcert` em modo somente leitura e usam
`SSL_CERT_FILE` para validar o certificado TLS da borda `localhost`. A CA não
é adicionada à imagem, ao repositório ou ao trust store do host.

Não altere o arquivo hosts. BFF e API acessam `localhost` pela rede da borda.
O Playwright usa `--network host`, para que o Chromium do container alcance a
mesma borda `https://localhost:7443` do navegador no Windows.

O client `altivy-bff` usa somente os callbacks `https://localhost:7443`.
Para um realm já existente, reconcilie-os pela borda:

```powershell
.\tools\scripts\configure-keycloak.ps1 -BaseUrl https://localhost:7443 -SkipUser
```

O proxy fixa `X-Forwarded-Proto=https` e encaminha o host com a porta pública
`:7443`, pois `8443` é apenas a porta interna do container. Assim, o BFF gera
o redirect URI OIDC igual ao callback público registrado no Keycloak.

Além de `/bff`, `/api/v1` e `/hubs`, o proxy encaminha `/signin-oidc` e
`/signout-callback-oidc` ao BFF. Esses paths não podem cair no fallback da SPA,
pois são os callbacks que criam ou encerram a sessão server-side.

Após o Compose indicar todos os serviços como `healthy`, confirme a sessão
anônima pelo proxy HTTPS sem passar pelo proxy corporativo:

```powershell
curl.exe --noproxy "*" --insecure https://localhost:7443/bff/session
```

O resultado esperado é `{"authenticated":false,"subject":null,"name":null}`.

## Acessar o Admin Console pela borda

Com `ALTIVY_KEYCLOAK_HOSTNAME`/`ALTIVY_KEYCLOAK_ADMIN_HOSTNAME` apontando para
`https://localhost:7443` e os dois Composes saudáveis, o NGINX de borda
encaminha `/admin`, `/realms` e `/resources` para o Keycloak pela rede
`altivy-edge`. Acesse o console em:

- <https://localhost:7443/admin/master/console/> (login inicial no realm `master`);
- <https://localhost:7443/admin/altivy/console/> (direto no realm `altivy`).

Não use `http://localhost:8080/admin/` enquanto os hostnames apontarem para
`7443`: o Keycloak anunciaria `7443` mas serviria a página por outra origem,
quebrando assets e redirects do console.

Se uma execução anterior tiver falhado antes de o Web iniciar, remova somente
os recursos deste laboratório para recriar os aliases `web`, `bff` e `api` e
seus IPs estáticos. Não remova o Compose do Keycloak:

```powershell
docker compose -f deploy/e2e/docker-compose-altivynotes.yaml down
```

## Executar E2E

Com o laboratório ativo, execute a imagem de testes a partir da raiz:

```powershell
$env:ALTIVY_E2E_BASE_URL = 'https://localhost:7443'
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
docker compose -f deploy/e2e/docker-compose-altivynotes.yaml down
```

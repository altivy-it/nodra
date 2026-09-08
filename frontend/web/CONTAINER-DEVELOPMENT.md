# Desenvolvimento do Altivy Web

O alias `ng` usa `lzocateli/angular-cli:22.1.0-node24.15.0-bookworm`.
O código é montado em `/workspace` e `node_modules` fica no volume Docker
`ng-node-modules-<hash-do-workspace>`, separado do filesystem Windows.

```powershell
Set-Location .\frontend\web
# Opcional: fallback por variável de ambiente
$env:ARTIFACTORY_USER_TOKEN = '<token-do-cofre>'
ng build --configuration production
ng test
```

O alias monta o diretório atual como `/workspace`. Portanto, não execute `ng`
na raiz de `AltivyNotes`: ela não contém `angular.json` e o Angular CLI retorna
“This command is not available when running the Angular CLI outside a workspace”.

Secret recomendado (arquivo local montado como read-only no container):

```powershell
New-Item -ItemType Directory -Force "$HOME/.secrets" | Out-Null
Set-Content -Path "$HOME/.secrets/artifactory_user_token" -NoNewline -Value '<token-do-cofre>'
```

Opcional: sobrescrever caminho do secret para todos os comandos `ng`:

```powershell
$env:NG_SECRET_FILE = 'D:\segredos\artifactory_user_token'
```

O alias executa `npm ci` no container quando o `package-lock.json` mudou. Não
instale Node, npm ou Angular CLI fisicamente no host.

Em diretórios sem `package.json` e `package-lock.json` o alias não executa
`npm ci`; nesse caso comandos como `ng help` e `ng version` funcionam sem
tentar instalar dependências.

O arquivo [.npmrc](.npmrc) do projeto é a fonte de verdade para o npm dentro do
container. O registry padrão é público:

- `https://registry.npmjs.org/` para pacotes públicos;
- `https://artifacts.cat.com/artifactory/api/npm/cat-brazil-jfrog-npm/` apenas
para pacotes do escopo `@cat`.

O mesmo `.npmrc` mantém proxy, email escopado ao registry privado e
`_authToken=${ARTIFACTORY_USER_TOKEN}` para o cenário corporativo.

`NPM_REGISTRY_URL` não é necessário para o registry padrão. O alias `ng` ainda
pode definir essa variável para compatibilidade com ambientes antigos, mas ela
não deve substituir o registry público sem uma decisão explícita.

O alias `ng` tenta obter o token nesta ordem:

1. variável de ambiente `ARTIFACTORY_USER_TOKEN`
2. arquivo secret montado em `/run/secrets/artifactory_user_token`

Obs.: o secret só é montado automaticamente quando `COMPUTERNAME` começa com
`B8` (fluxo de registry privado).

O alias também calcula automaticamente um volume exclusivo de `node_modules`
por diretório de projeto (hash SHA-256 do caminho atual), evitando conflito ao
usar o mesmo `ng` em repositórios diferentes.

Uso genérico em qualquer projeto Angular:

```powershell
Set-Location .\meu-projeto-angular
ng build --configuration production
```

Comando Docker usado pelo alias `ng` (equivalente):

```powershell
$workspace = (Get-Location).Path
$workspaceBytes = [System.Text.Encoding]::UTF8.GetBytes($workspace)
$workspaceHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($workspaceBytes)).Substring(0, 12).ToLowerInvariant()
$nodeModulesVolume = "ng-node-modules-$workspaceHash"
$isPrivateRegistryMachine = $env:COMPUTERNAME -like 'B8*'
$npmRegistryUrl = if ($isPrivateRegistryMachine) { 'https://artifacts.cat.com/artifactory/api/npm/cat-brazil-jfrog-npm/' } else { 'https://registry.npmjs.org/' }

docker run --rm -it `
 --env NG_CLI_ANALYTICS=false `
 --env "NPM_REGISTRY_URL=$npmRegistryUrl" `
 --env ARTIFACTORY_USER_TOKEN `
 -v "$workspace:/workspace" `
 -v "$nodeModulesVolume:/workspace/node_modules:nocopy" `
 -v "$HOME/.secrets/artifactory_user_token:/run/secrets/artifactory_user_token:ro" ` # somente no cenário B8
 -w /workspace `
 lzocateli/angular-cli:22.1.0-node24.15.0-bookworm `
 /bin/sh -lc 'if [ -z "$ARTIFACTORY_USER_TOKEN" ] && [ -f /run/secrets/artifactory_user_token ]; then ARTIFACTORY_USER_TOKEN=$(cat /run/secrets/artifactory_user_token); export ARTIFACTORY_USER_TOKEN; fi; if [ ! -x node_modules/.bin/ng ] || [ package-lock.json -nt node_modules/.ng-package-lock.stamp ]; then npm ci --no-audit --no-fund || exit $?; touch node_modules/.ng-package-lock.stamp; fi; exec "$@"' -- ng build --configuration production
```

O token detectado durante a investigação foi exposto e deve ser revogado e
rotacionado; não o reutilize. Um erro `E401` indica credencial inválida,
expirada ou formato não aceito pelo Artifactory, e não um problema de proxy.

O frontend consulta somente `/bff/session` e `/bff/login`. Tokens permanecem
no BFF e não são armazenados no browser.

# Instalação self-hosted

## Transicao para Nodra

O produto passa a se chamar **Nodra**, da empresa **Altivy**, com marca completa
**Altivy Nodra** e repositorio `altivy-it/nodra`. A solucao agora e
`Nodra.slnx`; projetos e assemblies usam o prefixo `Nodra`.

A renomeacao nao move vaults, backups, bancos, paths externos ou secrets.
O namespace Compose instalado continua `altivy-notes`, e as variaveis
`ALTIVY_*`, a chave `AltivyPostgres`, o schema `altivy`, os identificadores
Keycloak, cookies e Data Protection permanecem compativeis. Nao recrie o realm,
nao altere issuer/audience nem mova o arquivo externo
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env` para aplicar a marca.

Para atualizar, mantenha a configuracao e os bind mounts existentes, revise os
novos nomes de imagem e execute o mesmo inicializador documentado abaixo.
Imagens novas devem existir localmente ou em release aprovada antes do rollout;
renomear o repositorio nao publica imagens nem autoriza deploy.

Para recuperacao, retorne as referencias de imagens ao manifesto anterior
aprovado e use os mesmos dados/configuracao, sem downgrade de schema ou copia
de secrets. A renomeacao nao requer migracao de conteudo. Consulte o
[contrato de compatibilidade](../../specs/010-renomeacao-nodra/contracts/compatibilidade.md)
para distinguir nomes novos de identificadores legados preservados.

O ambiente self-hosted usa duas stacks independentes:

| Stack | Repositorio proprietario | Servicos permanentes |
| --- | --- | --- |
| `altivy-identity` | `architecture-knowledge-base` | `gateway`, `postgres`, `keycloak` |
| `altivy-notes` (namespace legado do Nodra) | `altivy-it/nodra` | `web`, `bff`, `api`, `worker` |

Gateway, PostgreSQL e Keycloak sao compartilhados com outros produtos e nunca
devem ser copiados para o Compose do Nodra. O migrator e uma operacao
transitoria e tambem nao integra a stack permanente do produto.

> **Importante:** este projeto nao inicia nem administra a stack
> `altivy-identity`. Ela deve estar online antes da subida do Nodra e e
> gerenciada no repositorio `architecture-knowledge-base`, em
> `platforms/identity`. Consulte o README desse diretorio para operacoes da
> infraestrutura compartilhada.

## Comando para subir a aplicacao

Execute o comando abaixo a partir da raiz do repositorio `Nodra`:

```powershell
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 `
  -IdentityEnvironmentFile "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" `
  -SyncOidc
```

Esse e o comando principal de instalacao e atualizacao. Nao execute migrations,
`docker compose up` ou scripts OIDC separadamente: o inicializador aplica essas
etapas na ordem correta e interrompe o fluxo se alguma delas falhar.

## Versoes e imagens

A versao inicial dos programas e `0.1.0`:

- backend: `VersionPrefix` em `backend/Directory.Build.props`;
- frontend: `version` em `frontend/web/package.json`;
- imagens locais: `nodra-web:0.1.0`, `nodra-bff:0.1.0`,
  `nodra-api:0.1.0` e `nodra-worker:0.1.0`;
- migrator transitorio: `nodra-migrations:0.1.0`.

Ao alterar a versao, atualize as duas fontes de compilacao, os defaults do
Compose e os defaults `IMAGE_VERSION` dos Dockerfiles no mesmo change set.
Releases publicadas continuam usando referencias imutaveis por digest no
manifesto `images.env`.

A feature 011 adota `release-manifest.json` atestado e imagens
`ghcr.io/altivy-it/nodra-*` por digest. Depois de verificar a release e a
atestação pelo guard canônico, o operador pode converter o manifesto:

```powershell
pwsh -NoProfile -File tools/scripts/convert-release-manifest.ps1 -ManifestPath /secure/release/release-manifest.json -OutputPath /secure/release/images.env
$hash = (Get-FileHash /secure/release/images.env -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  images.env" | Out-File /secure/release/images.env.sha256 -Encoding utf8NoBOM -NoClobber
```

Execute na raiz extraída do pacote, com PowerShell 7.4+, após validar origem
e atestação. O diretório `/secure/release` deve existir e os dois destinos devem
ser novos. Nenhum comando acima inicia containers. A conversão e o checksum
não autenticam a origem por si; nunca use manifesto de origem não verificada.
O inicializador continua exigindo `-NoBuild`, `images.env.sha256` e exatamente
cinco imagens por digest. Referências pessoais legadas são aceitas apenas para
compatibilidade/recuperação; novas releases usam o namespace institucional.
Nenhuma configuração, dado ou imagem instalada é migrada automaticamente.

## Pre-requisitos

- Docker Engine ou Docker Desktop com Compose v2 e containers Linux;
- PowerShell 7.4 ou superior;
- stack compartilhada `altivy-identity` ja online, gerenciada no repositorio
  `architecture-knowledge-base`, diretorio `platforms/identity`;
- redes externas `identity-global` e `identity-client`;
- database `altivy` e roles `altivy_runtime` e `altivy_migrator` previamente
  provisionados pelo operador da plataforma compartilhada;
- realm `altivy`, client confidencial `altivy-bff` e Authorization Services;
- certificados internos da API, CA confiavel e PFX de Data Protection;
- conta SMTP para alertas do worker.

No Windows, secrets permanecem exclusivamente em:

```text
%APPDATA%\Microsoft\UserSecrets\Altivy.Notes\.env
```

As credenciais administrativas da infraestrutura compartilhada permanecem no
arquivo externo da plataforma de identidade. O inicializador recebe esse
arquivo por `-IdentityEnvironmentFile`, sem copiar ou exibir seu conteudo.

## Redes e roteamento

A stack `nodra` ingressa nas redes externas ja existentes:

- `identity-global`: web, BFF e API alcancam o gateway compartilhado;
- `identity-client`: API e worker alcancam o PostgreSQL compartilhado;
- `app`: comunicacao interna entre os quatro servicos do produto.

Nenhuma porta e publicada diretamente pelo Compose do produto. A borda publica
e o gateway compartilhado, que encaminha `/notes` para os aliases `notes-web` e
`notes-bff`. A URL canonica local e `https://localhost:7443/notes`.

## Sequencia de execucao

### 1. Confirmar o pre-requisito compartilhado

Confirme com o operador da plataforma que `altivy-identity` esta online e que
as redes, o database, as roles e o client OIDC dos pre-requisitos estao
provisionados. Nao inicie essa stack por scripts ou Composes deste repositorio.

### 2. Preparar a configuracao externa

Use [compose.env.example](compose.env.example) como lista dos valores esperados
e configure os valores reais em
`%APPDATA%\Microsoft\UserSecrets\Altivy.Notes\.env`. Mantenha as credenciais
administrativas da plataforma no arquivo externo `Altivy.Identity` informado no
comando. Nao copie esses arquivos para o repositorio e nao coloque secrets na
linha de comando.

Essa preparacao e obrigatoria na primeira instalacao e deve ser revisada quando
origens, certificados, credenciais, SMTP, paths ou versoes forem alterados.

### 3. Validar sem efeitos colaterais

A partir da raiz do repositorio, valide arquivos externos, certificados, redes,
servicos compartilhados e configuracao Compose sem iniciar ou parar containers:

```powershell
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 `
  -IdentityEnvironmentFile "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" `
  -ValidateOnly
```

O comando falha fechado quando um pre-requisito nao esta disponivel e nao
imprime valores de configuracao ou secrets.

### 4. Executar a subida ou atualizacao

Execute o comando principal:

```powershell
pwsh -NoProfile -File tools/scripts/start-self-hosted.ps1 `
  -IdentityEnvironmentFile "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" `
  -SyncOidc
```

O script executa automaticamente, nesta ordem:

1. valida arquivos externos, certificados, redes e servicos compartilhados;
2. valida o Compose e resolve o gateway da stack `altivy-identity`;
3. prepara os diretorios persistentes e suas permissoes;
4. interrompe temporariamente writers existentes e cria um backup verificado;
5. constroi ou baixa a imagem transitoria de migrations e aplica o schema;
6. sincroniza os callbacks OIDC de `/notes` por meio do script pertencente ao
   repositorio `architecture-knowledge-base`;
7. inicia ou atualiza somente `web`, `bff`, `api` e `worker` no projeto Compose
   `nodra` e aguarda os health checks;
8. executa o smoke test de sessao pela borda compartilhada.

Se uma etapa falhar, as seguintes nao sao executadas. Writers que tenham sido
interrompidos antes do backup sao reiniciados pelo bloco de recuperacao do
script. O processo nao inicia, para, recria nem remove servicos de
`altivy-identity`.

Use `-NoBuild` para reutilizar as imagens locais `0.1.0`. Para uma release
publicada, informe `-ReleaseImageManifest` junto com `-NoBuild`; o manifesto e
seu checksum devem conter referencias imutaveis por digest.

`-RecreateConsumers` recria somente web, BFF, API e worker.

## Verificacao e recuperacao

Confirme a sessao anonima pela borda compartilhada:

```powershell
Invoke-RestMethod -Uri 'https://localhost:7443/notes/bff/session'
```

O retorno esperado antes do login informa `authenticated: false`. Para estado e
logs, filtre os containers pelo projeto `nodra`; nao use comandos que
removam volumes ou bind mounts. Backup, restore isolado e rollback estao no
[runbook de recuperacao](../../docs/runbooks/recuperacao-self-hosted.md).

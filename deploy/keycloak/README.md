# Tema Keycloak do AltivyNotes

Este diretório é proprietário apenas do tema versionado do realm `altivy` e
do script que gera seu artefato local. A infraestrutura de Keycloak,
PostgreSQL e gateway TLS é fornecida pela plataforma central em
`../architecture-knowledge-base/platforms/identity`.

Não existe Compose de Keycloak neste produto. Não crie uma stack local
paralela: o PostgreSQL central preserva o bind mount legado
`%APPDATA%\keycloak\postgres-data` e o Keycloak monta o tema publicado de
`KEYCLOAK_THEMES_ROOT/altivy` como somente leitura.

## Gerar e publicar o tema

Na raiz do AltivyNotes, gere o artefato a partir dos assets canônicos:

```powershell
./deploy/keycloak/build-theme.ps1
```

Em seguida, publique-o no bind mount de temas da plataforma. Execute o
comando a partir de `architecture-knowledge-base/platforms/identity`, com
`KEYCLOAK_THEMES_ROOT` definido pelo ambiente externo da plataforma:

```powershell
./scripts/publish-product-theme.ps1 `
  -Product altivynotes `
  -SourcePath ../../AltivyNotes/deploy/keycloak/.build/keycloak-theme/altivy
```

Use `-DryRun` para validar a origem e o destino sem alterar arquivos. O script
publica em staging no mesmo volume e promove o diretório de forma atômica.

## Operar a identidade central

A configuração externa da plataforma fica exclusivamente em
`%APPDATA%\Microsoft\UserSecrets\Altivy.Identity\.env`. Inicialize, inicie
e valide a stack a partir do diretório da plataforma:

```powershell
./scripts/initialize-local-platform.ps1
./scripts/start-platform.ps1
./scripts/test-platform.ps1
```

O gateway central publica Keycloak em `https://localhost:7443`. Use esse
endpoint para discovery OIDC, console administrativo e scripts de
reconciliação do AltivyNotes. Consulte a documentação de operação da
plataforma antes de fazer backup, restauração, reinicialização ou alteração de
credenciais.

O realm, clients, roles, policies, usuários e o manifesto
`realm/altivy-realm.json` continuam pertencendo ao AltivyNotes. A
reconciliação deve usar credenciais de client restritas ao realm `altivy`;
runtimes e scripts cotidianos não recebem a credencial do realm `master`.

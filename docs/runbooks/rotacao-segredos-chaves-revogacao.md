# Runbook: Rotação de Segredos, Certificados e Revogação de Acessos

Este runbook estabelece os passos operacionais para rotação de credenciais
confidenciais, renovação de chaves criptográficas e revogação imediata de sessões
e privilégios no ecossistema Altivy.

## 1. Rotação de Client Secret do BFF (`altivy-bff`)

O BFF opera como um cliente OpenID Connect confidencial. O segredo compartilhado
com o Keycloak não deve ser versionado no código e precisa ser rotacionado sem
interrupção de serviço.

### 1.1 Gerar e aplicar novo segredo no Keycloak

1. Acesse o Keycloak Admin Console (`https://localhost:7443/admin` ou console do ambiente).
2. Navegue até **Clients > altivy-bff > Credentials**.
3. Clique em **Regenerate Secret**.
4. Copie o novo valor somente para o cofre seguro ou variável de ambiente da sessão.

### 1.2 Atualizar o BFF

```powershell
# 1. Definir o novo segredo na sessão de deploy ou no cofre
$env:ALTIVY_E2E_BFF_CLIENT_SECRET = '<novo-secret-gerado>'

# 2. Reiniciar o serviço do BFF aplicando a nova variável
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-altivynotes.yaml up --no-build --force-recreate --detach bff

# 3. Validar a prontidão do serviço
curl.exe --noproxy "*" --insecure --silent --include https://localhost:7443/health/ready
```

**Resultado esperado:** HTTP 200 com status `Healthy`.

---

## 2. Rotação do Key Ring de ASP.NET Core Data Protection

As chaves de Data Protection protegem os cookies técnicos de sessão do BFF no
servidor.

### 2.1 Política de rotação

- As chaves de Data Protection possuem ciclo de vida automático gerenciado pelo
  ASP.NET Core (expiração padrão de 90 dias com chave nova gerada
  antecipadamente).
- Em produção, as chaves devem ser criptografadas em repouso por certificado X.509
  configurado via `Security:DataProtectionCertificatePath`.

### 2.2 Rotação manual de emergência (em caso de comprometimento)

```powershell
# 1. Gerar novo diretório de chaves isolado
$novoKeyRing = "./deploy/e2e/keys_nova"
New-Item -ItemType Directory -Force $novoKeyRing | Out-Null

# 2. Atualizar a variável de montagem no compose
$env:ALTIVY_E2E_DATA_PROTECTION_KEYS_PATH = (Resolve-Path $novoKeyRing).Path

# 3. Recriar o container BFF (isso invalidará todas as sessões ativas existentes)
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-altivynotes.yaml up --no-build --force-recreate --detach bff
```

---

## 3. Rotação de Certificados TLS de Borda

A borda NGINX termina o TLS público e roteia o tráfego interno para o Keycloak,
BFF e Web.

### 3.1 Procedimento de substituição

```powershell
# 1. Gerar ou obter novos certificados válidos
# Em desenvolvimento com mkcert:
$AltivyCertsDirectory = Join-Path $env:APPDATA 'Microsoft\UserSecrets\Altivy.Notes\certs'
New-Item -ItemType Directory -Force $AltivyCertsDirectory | Out-Null
mkcert -cert-file (Join-Path $AltivyCertsDirectory 'tls.crt') `
  -key-file (Join-Path $AltivyCertsDirectory 'tls.key') `
  localhost 127.0.0.1 ::1
Copy-Item "$(mkcert -CAROOT)\rootCA.pem" (Join-Path $AltivyCertsDirectory 'rootCA.pem')

# 2. Recarregar o NGINX compartilhado sem interromper conexões
docker exec altivy-nginx-proxy nginx -s reload

# 3. Validar handshake TLS pela porta de borda
curl.exe --noproxy "*" --insecure --head https://localhost:7443/
```

---

## 4. Revogação de Acesso e Sessões de Usuários

### 4.1 Revogação Imediata de Permissões de Vault

Quando um usuário tem sua permissão revogada (ex.: remoção de grupo no Keycloak):

1. Remova o usuário do grupo correspondente via Admin Console ou API REST.
2. Na próxima requisição do usuário a `/api/v1/vaults/{vaultId}` ou ao SignalR
   `/hubs/vaults`, a API consulta o Keycloak e **rejeita imediatamente com HTTP 403**.
3. Nenhuma permissão em cache local subsiste na API.

### 4.2 Encerramento Forçado de Sessões no Keycloak (Logout Global)

Para forçar o logout de um usuário específico em todas as instâncias:

1. Acesse **Users > [Usuário] > Sessions**.
2. Clique em **Logout all sessions**.
3. O token de refresh é revogado no Keycloak. Na próxima tentativa de renovação
   automática pelo BFF, a chamada ao endpoint `/token` falha e a sessão técnica
   do BFF é encerrada imediatamente.

### 4.3 Script de Teste Automatizado de Revogação

Para validar o funcionamento da revogação em laboratório:

```powershell
# Executa a remoção temporária, valida negação 403 e restaura a associação
pwsh -File tools/scripts/test-keycloak-revocation.ps1 `
  -BaseUrl https://localhost:7443 `
  -Username lzocateli `
  -VaultId lab-0001
```

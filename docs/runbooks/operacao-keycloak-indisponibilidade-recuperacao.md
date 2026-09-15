# Runbook: Indisponibilidade do Keycloak

O Keycloak, PostgreSQL e gateway TLS são operados pela plataforma central em
`architecture-knowledge-base/platforms/identity`. Este runbook cobre o impacto
no AltivyNotes; não há uma stack local de Keycloak para reiniciar.

## Sinais

| Componente | Sinal | Resultado esperado de falha |
| --- | --- | --- |
| BFF | Discovery OIDC ou challenge indisponível | Login falha sem criar sessão |
| API | Decisão UMA indisponível | HTTP 503 `authorization-service-unavailable` |
| Browser | Não inicia login em `/bff/login` | Estado de erro, sem bypass de autorização |

## Diagnóstico

Na raiz do AltivyNotes, confirme o estado da aplicação e o discovery público:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-altivynotes.yaml ps
curl.exe --noproxy "*" --insecure --silent --include `
  https://localhost:7443/realms/altivy/.well-known/openid-configuration
```

O discovery deve retornar HTTP 200 e o issuer
`https://localhost:7443/realms/altivy`.

Em seguida, no diretório `architecture-knowledge-base/platforms/identity`,
execute o diagnóstico da plataforma:

```powershell
./scripts/test-platform.ps1
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" ps
```

## Mitigação e recuperação

A API e o BFF devem falhar fechados. Não habilite autorização local nem reduza
as verificações de token para contornar a indisponibilidade.

O responsável pela plataforma central deve executar os procedimentos de
recuperação e restore documentados em
`architecture-knowledge-base/platforms/identity/docs/operations.md`. Depois da
recuperação, reconcilie os clients do AltivyNotes com credenciais restritas ao
realm `altivy`, valide discovery e faça um login completo.

## Escalonamento

Escale para o responsável pela plataforma de identidade quando `test-platform`
falhar, houver risco ao bind mount PostgreSQL compartilhado, ou for necessário
restaurar backup. Registre os horários, estados de health e códigos HTTP sem
incluir tokens, credenciais ou conteúdo de notas.
# Runbook: Indisponibilidade do Keycloak

O Keycloak, PostgreSQL e gateway TLS são operados pela plataforma central em
`architecture-knowledge-base/platforms/identity`. Este runbook cobre o impacto
no AltivyNotes; não há uma stack `altivy-keycloak-lab` para reiniciar.

## Sinais

| Componente | Sinal | Resultado esperado de falha |
| --- | --- | --- |
| BFF | Discovery OIDC ou challenge indisponível | Login falha sem criar sessão |
| API | Decisão UMA indisponível | HTTP 503 `authorization-service-unavailable` |
| Browser | Não inicia login em `/bff/login` | Estado de erro, sem bypass de autorização |

## Diagnóstico

Na raiz do AltivyNotes, confirme o estado da aplicação e o discovery público:

```powershell
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/e2e/docker-compose-altivynotes.yaml ps
curl.exe --noproxy "*" --insecure --silent --include `
  https://localhost:7443/realms/altivy/.well-known/openid-configuration
```

O discovery deve retornar HTTP 200 e o issuer
`https://localhost:7443/realms/altivy`.

Em seguida, no diretório `architecture-knowledge-base/platforms/identity`,
execute o diagnóstico da plataforma:

```powershell
./scripts/test-platform.ps1
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Identity\.env" ps
```

## Mitigação e recuperação

A API e o BFF devem falhar fechados. Não habilite autorização local nem reduza
as verificações de token para contornar a indisponibilidade.

O responsável pela plataforma central deve executar os procedimentos de
recuperação e restore documentados em
`architecture-knowledge-base/platforms/identity/docs/operations.md`. Depois da
recuperação, reconcilie os clients do AltivyNotes com credenciais restritas ao
realm `altivy`, valide discovery e faça um login completo.

## Escalonamento

Escale para o responsável pela plataforma de identidade quando `test-platform`
falhar, houver risco ao bind mount PostgreSQL compartilhado, ou for necessário
restaurar backup. Registre os horários, estados de health e códigos HTTP sem
incluir tokens, credenciais ou conteúdo de notas.
# Runbook: Indisponibilidade, Backup e Recuperação do Keycloak

Este runbook orienta a operação diante de degradação ou indisponibilidade da
autoridade de identidade e autorização (Keycloak), além dos procedimentos de
backup e restauração do realm Altivy e do seu banco PostgreSQL dedicado.

## 1. Sinais e Sintomas de Indisponibilidade

| Componente | Sinal / Sintoma | Código HTTP / Log |
| --- | --- | --- |
| BFF (`altivy-bff`) | Falha no health check `/health/ready` | HTTP 503 com status `Unhealthy` |
| BFF (`altivy-bff`) | Falha no discovery OIDC ou no challenge | HTTP 500 no login com `IDX20803: Unable to obtain configuration` |
| API (`altivy-api`) | Falha ao consultar decisão UMA do Keycloak | HTTP 503 com `ProblemDetails` (`type: .../errors/authorization-service-unavailable`) |
| API (`altivy-api`) | Rejeição de tokens expirados sem emissão nova | HTTP 401 para requisições de API |
| Browser Angular | Sessão anônima não consegue navegar para `/bff/login` | Exibição de estado de erro no shell |

## 2. Diagnóstico Rápido

Execute a partir da raiz do repositório em um terminal PowerShell:

### 2.1 Verificar saúde e conectividade dos serviços

```powershell
# 1. Verificar status dos containers
docker compose -f deploy/keycloak/docker-compose-keycloak.yaml ps
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" -f deploy/e2e/docker-compose-altivynotes.yaml ps

# 2. Verificar logs recentes do Keycloak e do PostgreSQL
docker logs --tail 50 keycloak-altivy
docker logs --tail 50 postgres-keycloak-altivy

# 3. Testar discovery OIDC diretamente pela borda
curl.exe --noproxy "*" --insecure --silent --include https://localhost:7443/realms/altivy/.well-known/openid-configuration
```

**Resultado esperado:** HTTP 200 com JSON contendo `"issuer":"https://localhost:7443/realms/altivy"`.

### 2.2 Verificar logs da aplicação

```powershell
docker logs --tail 50 altivy-notes-bff-1
docker logs --tail 50 altivy-notes-api-1
```

## 3. Mitigação e Comportamento de Falha Fechada

A arquitetura do Altivy foi desenhada com o princípio de **falhar fechada**:
- A API rejeita qualquer tentativa de acesso ao domínio ou leitura de vaults se
  o Keycloak estiver inacessível (retornando HTTP 503 estruturado).
- Nenhuma autorização local sintética ou permissão paralela temporária deve ser
  ativada.
- O BFF bloqueia o início de novas sessões e falha a renovação de tokens
  expirados, invalidando a sessão server-side com segurança.

## 4. Recuperação do Serviço

### 4.1 Reinicialização limpa dos containers

```powershell
# Reiniciar o serviço do Keycloak mantendo o PostgreSQL ativo
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/keycloak/docker-compose-keycloak.yaml restart keycloak

# Aguardar a prontidão do serviço
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/keycloak/docker-compose-keycloak.yaml ps
```

### 4.2 Reconciliação dos clients e configurações

Se o Keycloak reiniciou mas perdeu a sincronização em memória dos clients:

```powershell
# Reexecutar o script idempotente de configuração
pwsh -File tools/scripts/configure-keycloak.ps1 `
  -BaseUrl https://localhost:7443 `
  -BffBaseUrl https://localhost:7443 `
  -SkipUser
```

## 5. Procedimentos de Backup e Restauração

### 5.1 Backup do PostgreSQL do Keycloak

Execute a extração consistente do banco de dados do Keycloak sem expor secrets no terminal:

```powershell
# Gerar dump lógico do database keycloak
$backupTimestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupPath = "./artifacts/backups/keycloak_db_$backupTimestamp.sql"
New-Item -ItemType Directory -Force (Split-Path $backupPath) | Out-Null

docker exec postgres-keycloak-altivy pg_dump -U keycloak -d keycloak --format=custom `
  | Set-Content -Path $backupPath -AsByteStream

Write-Output "Backup gerado em: $backupPath"
```

### 5.2 Restauração do PostgreSQL do Keycloak

```powershell
# 1. Parar temporariamente o Keycloak para evitar conexões ativas
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/keycloak/docker-compose-keycloak.yaml stop keycloak

# 2. Restaurar dump customizado
$backupFile = Get-ChildItem ./artifacts/backups/keycloak_db_*.sql | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content -Path $backupFile.FullName -AsByteStream `
  | docker exec -i postgres-keycloak-altivy pg_restore -U keycloak -d keycloak --clean --if-exists

# 3. Reiniciar o Keycloak e validar readiness
docker compose --env-file "$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env" `
  -f deploy/keycloak/docker-compose-keycloak.yaml start keycloak
```

## 6. Escalonamento

Se o serviço não restabelecer após a reinicialização e restauração do banco:
1. Contatar o administrador de infraestrutura/identidade responsável pelo cluster Keycloak.
2. Verificar expiração de certificados TLS ou falhas na rede externa.
3. Registrar o incidente mantendo os logs de auditoria e métricas exportados.

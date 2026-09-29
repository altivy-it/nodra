# Deploy de produção

Esta composição executa apenas web, BFF, API, worker e migrations. PostgreSQL,
NGINX e Keycloak são serviços compartilhados e não são iniciados por este
Compose.

- [Runbook de deploy da VPS](../../docs/runbooks/deploy-producao-vps.md):
  pré-requisitos, configuração externa, workflows, rollout e recuperação.
- [Plano de deploy](../../docs/plan/deploy-producao-vps.md): etapas e evidências.
- [ADR 2026-0007](../../docs/adr/2026-0007-deploy-altivy-vps-compartilhada.md):
  decisão de topologia.

`compose.env.example` contém somente paths e nomes fictícios de redes/containers.
Copie seus valores revisados para `/userapps/configs/altivynotes/compose.env` no
host. Secrets OIDC e connection strings permanecem em arquivos externos
separados descritos no runbook.

Build local containerizado da web de produção:

```powershell
docker build --build-arg WEB_CONFIGURATION=production `
  --build-arg WEB_BASE_HREF=/ `
  --file frontend/web/Dockerfile --tag altivy-web:local .
```

O workflow de release publica imagens identificadas por digest; não use a tag
local como artefato de produção.
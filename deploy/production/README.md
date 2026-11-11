# Deploy de produção

## Nomenclatura e compatibilidade

O produto e **Altivy Nodra** e o script operacional passa a se chamar
`nodra-ops.sh`. Imagens de produto usam o prefixo `nodra-`; variaveis
`ALTIVY_*`, namespace Compose `altivynotes`, caminhos externos, banco,
identidade e secrets existentes permanecem preservados. Nao mova nem recrie
dados ou recursos de identidade para acompanhar uma troca de marca.

O rollback usa o manifesto de imagens anterior aprovado e os mesmos bind
mounts, conforme o runbook existente. Nao requer reverter schema. A troca de
nome no GitHub nao publica imagens nem executa rollout.

> **Legado supersedido:** esta composição depende de NGINX/PostgreSQL/Keycloak
> compartilhados e de uma VPS. A instalação self-hosted ativa está em
> [`deploy/self-hosted`](../self-hosted/README.md).

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
  --file frontend/web/Dockerfile --tag nodra-web:0.1.0 .
```

O workflow de release publica imagens identificadas por digest; não use a tag
local como artefato de produção.

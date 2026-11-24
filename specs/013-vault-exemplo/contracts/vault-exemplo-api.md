# Contrato: Vault de exemplo

Delta em `contracts/openapi/nodra-api.yaml`.

## `GET /api/v1/vaults`

`VaultSummary` ganha o campo obrigatório na resposta `readOnly: boolean`
(aditivo; clientes antigos o ignoram).

```json
{ "items": [ { "id": "nodra-exemplo", "name": "Vault de exemplo", "readOnly": true } ], "nextCursor": null }
```

## Recusa de escrita

Toda operação que exige `vault:write` ou `vault:delete` em `nodra-exemplo` —
`POST|PUT|PATCH|DELETE /api/v1/vaults/{vaultId}/notes/{path}`,
`PUT|PATCH|DELETE /api/v1/vaults/{vaultId}/folders/{path}`,
`PATCH /api/v1/vaults/{vaultId}/trash/{operationId}` e
`DELETE /api/v1/vaults/{vaultId}/trash` — responde:

```http
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json

{ "status": 403, "title": "Vault de exemplo é somente leitura.", "code": "vault-read-only" }
```

A recusa ocorre antes da consulta ao Keycloak e de qualquer acesso ao filesystem.
Leituras (`GET`) seguem a decisão do Keycloak para `vault:read`.

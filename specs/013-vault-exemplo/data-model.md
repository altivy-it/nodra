# Modelo de Dados: Vault de exemplo

## Vault de exemplo (`SampleVault`)

| Campo | Valor | Regra |
|-------|-------|-------|
| `Id` | `nodra-exemplo` | Constante; válido para `VaultId.TryParse`. |
| `Name` | `Vault de exemplo` | Constante de exibição. |
| `State` | `Ready` (2) | Registrado por migration em `altivy.vaults`. |
| `ReadOnly` | `true` | Derivado de `SampleVault.IsReadOnly(id)`; não persistido. |

Recurso Keycloak: `vault-nodra-exemplo`, scope único `vault:read`.

## Documento modelo

| Campo | Valor |
|-------|-------|
| Caminho relativo | `documento-modelo.md` |
| Fonte canônica | Recurso embutido `Nodra.Notes.Api.SampleVault.documento-modelo.md` |
| Cópia materializada | `{VaultStorage:Root}/nodra-exemplo/documento-modelo.md` |

Transição: `ausente | divergente` → (inicialização da API) → `idêntico ao recurso`.

## Edição local (frontend)

Estado em memória da nota selecionada (`notes.selected`). Descartado ao trocar
de nota/vault, recarregar a página ou acionar "Restaurar original". Nunca é
enviado ao servidor.

## `VaultSummaryResponse`

`{ id: string, name: string, readOnly: boolean }` — `readOnly` é aditivo.

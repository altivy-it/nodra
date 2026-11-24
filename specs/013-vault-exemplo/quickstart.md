# Validação Rápida: Vault de exemplo

## Pré-requisitos

- Laboratório Podman em execução (`pwsh tools/scripts/start-local-lab.ps1 -Build`).
- Acesso pela borda nginx: `https://localhost:7443/nodra`.

## 1. Provisionar a leitura

```powershell
./tools/scripts/provision-sample-vault-authorization.ps1 -BaseUrl https://localhost:7443 -DryRun
./tools/scripts/provision-sample-vault-authorization.ps1 -BaseUrl https://localhost:7443
```

Esperado: recurso, policy e permission criados (ou "já existe" na repetição).
O `start-local-lab.ps1` executa esse passo automaticamente.

## 2. Testes automatizados

```bash
# backend (container dotnet-sdk)
dotnet test backend/tests/Nodra.Notes.Api.UnitTests
# frontend (a partir de frontend/web, container angular-cli)
npm exec -- ng test --watch=false
```

## 3. Cenários manuais / Playwright

1. Entrar e abrir o seletor de vaults: "Vault de exemplo" aparece com indicação de somente leitura.
2. Abrir `documento-modelo.md`: editor e prévia exibem o conteúdo completo.
3. Digitar no editor: a prévia muda, o aviso "alterações não são salvas" aparece e nenhuma requisição `PUT` é enviada.
4. Acionar "Restaurar original": o conteúdo volta em menos de 2 s.
5. Explorador: sem ações de criar, mover, excluir ou lixeira.
6. `curl -X PUT .../vaults/nodra-exemplo/notes/documento-modelo.md` com token válido → `403` com `code: vault-read-only`.
7. Alterar o arquivo no volume e reiniciar `nodra-api-1`: o conteúdo original volta.

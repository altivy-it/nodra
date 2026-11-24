# Pesquisa: Vault de exemplo somente leitura

## 1. Registro do vault na projeção

- **Decisão**: Migration EF Core escrita à mão que executa `INSERT ... ON CONFLICT DO NOTHING` em `altivy.vaults` com `state = 2` (Ready); o `Down` remove a linha.
- **Justificativa**: Toda instalação com PostgreSQL recebe o vault na atualização, sem passar pelo fluxo de provisionamento (que cria policies de dono). A operação é idempotente e não altera o modelo, portanto não muda o snapshot.
- **Alternativas consideradas**: Fila de provisionamento (`vault_provisioning_operations`) — rejeitada porque exige subject dono e cria permissões por usuário; inserção por hosted service — rejeitada porque a API não deve escrever a projeção fora das migrations.

## 2. Conteúdo canônico e materialização

- **Decisão**: Mover `tests/fixtures/markdown/documento-modelo.md` para `backend/apps/api/SampleVault/documento-modelo.md` como `EmbeddedResource`. Um `IHostedService` (`SampleVaultMaterializer`) cria `{VaultStorage:Root}/nodra-exemplo/` e grava o arquivo quando ausente ou divergente, usando temporário único + `File.Move(overwrite: true)`.
- **Justificativa**: O conteúdo é versionado com a aplicação (FR-002), restaurado em toda inicialização (FR-003/SC-005) e a gravação atômica tolera instâncias concorrentes.
- **Alternativas consideradas**: Seed no script do laboratório — rejeitado porque não vale para outras instalações; servir o recurso direto da memória sem filesystem — rejeitado porque quebraria o caminho único `FileSystemNoteStore` e a indexação.

## 3. Autorização de leitura para todos

- **Decisão**: Script `tools/scripts/provision-sample-vault-authorization.ps1` cria o resource `vault-nodra-exemplo` com somente `vault:read`, uma role policy sobre `default-roles-<realm>` com `fetchRoles: true` e uma resource permission. Validado no Keycloak do laboratório: recurso, policy e permission criados e avaliação `PERMIT` para o usuário de laboratório.
- **Justificativa**: Mantém o Keycloak como autoridade (Princípio II). `default-roles-<realm>` é atribuída a todo usuário novo; `fetchRoles` dispensa a role no token.
- **Alternativas consideradas**: Policy JavaScript — desabilitada por padrão no Keycloak; liberar leitura na API sem consultar o Keycloak — viola o Princípio II.

## 4. Proteção no servidor

- **Decisão**: Em `AuthorizeAsync` (endpoints de notas, pastas, lixeira e purge), quando `SampleVault.IsReadOnly(vaultId)` e o scope não é `vault:read`, retornar `403` `application/problem+json` com `code = "vault-read-only"` antes de consultar o Keycloak.
- **Justificativa**: Negar é seguro sem consulta externa; a resposta é distinguível de "Acesso negado" para a UI e testes (SC-003).
- **Alternativas consideradas**: `405`/`409` — semântica incorreta para uma regra de autorização; filtro de endpoint global — mais invasivo que o ponto único já existente.

## 5. Edição somente em memória

- **Decisão**: A listagem de vaults expõe `readOnly`. No frontend, `save()` atualiza apenas o estado local quando o vault é somente leitura; criação, exclusão, movimentação e lixeira são ocultadas; um aviso persistente oferece "Restaurar original", que reabre a nota do servidor.
- **Justificativa**: Atende FR-007 a FR-009 sem armazenamento no navegador.
- **Alternativas consideradas**: Editor em modo somente leitura — contraria o pedido de permitir experimentar livremente; `localStorage` — fora do escopo (premissa da spec).

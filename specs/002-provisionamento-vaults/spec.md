# Especificação da Feature: Provisionamento Pessoal de Vaults

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, aguardando revisão humana

**Entrada**: Migração das etapas P-08 a P-16 de `docs/plan/alternancia-vaults.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Solicitar vault pessoal (Prioridade: P1)

Como pessoa autenticada, quero solicitar a criação de um vault pessoal sem acessar APIs administrativas do Keycloak ou fornecer um path físico.

**Valor desta prioridade**: Criação precisa ser segura, idempotente e independente do browser.

**Teste independente**: Enviar uma solicitação autenticada e verificar operação durável, chave de idempotência e resposta assíncrona.

**Cenários de aceite**:

1. **Dado** um usuário autorizado e dentro dos limites configurados, **Quando** solicita um vault, **Então** recebe `202 Accepted` e um `operationId` sem a API prometer que o vault está pronto.
2. **Dado** um retry com a mesma `Idempotency-Key`, **Quando** a solicitação é repetida, **Então** nenhuma operação ou recurso duplicado é criado.

### Jornada do Usuário 2 - Acompanhar um vault pronto (Prioridade: P1)

Como pessoa autenticada, quero ver o vault no catálogo somente após provisioning físico e autorização positiva.

**Valor desta prioridade**: Um estado parcial não pode expor vault vazio, inacessível ou de outro usuário.

**Teste independente**: Simular sucesso e falha de filesystem/Keycloak e verificar estado e autorização no catálogo.

**Cenários de aceite**:

1. **Dado** que o provisioning concluiu e a autorização é positiva, **Quando** o catálogo é consultado, **Então** o vault `ready` aparece para o proprietário.
2. **Dado** uma falha entre PostgreSQL, Keycloak e filesystem, **Quando** o worker tenta novamente, **Então** a operação converge sem duplicar recursos nem revelar acesso parcial.

### Jornada do Usuário 3 - Arquivar e recuperar com segurança (Prioridade: P2)

Como pessoa autorizada, quero arquivar um vault e recuperá-lo dentro da política definida, sem exclusão física imediata ou perda silenciosa de conteúdo.

**Valor desta prioridade**: Ciclo de vida precisa respeitar retenção, backup e auditoria.

**Teste independente**: Executar transições de estado e provar retenção, alerta e recuperação sem apagar diretório.

**Cenários de aceite**:

1. **Dado** um vault arquivado, **Quando** o catálogo é consultado, **Então** ele não aparece como vault ativo.
2. **Dado** resíduo ou ausência física, **Quando** a reconciliação detecta o estado, **Então** cria ocorrência idempotente, degrada a projeção e alerta sem excluir conteúdo automaticamente.

## Casos de Borda

- Quota ou limite por sujeito excedido; conflito de idempotência; operação concorrente.
- Keycloak indisponível, permissão insuficiente, filesystem sem acesso ou path com traversal.
- Reparse point/symlink na raiz, reprocessamento após lease expirado e alerta já enviado.
- Vault arquivado, falha parcial, diretório órfão e prazo de retenção vencido.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: O usuário autenticado DEVE solicitar apenas vault pessoal; compartilhamento e memberships ficam fora do escopo.
- **FR-002**: A solicitação DEVE ser idempotente e assíncrona, com operação persistida antes da confirmação.
- **FR-003**: O `vaultId` DEVE ser gerado no servidor; nome informado nunca pode virar path físico.
- **FR-004**: O filesystem DEVE limitar a criação a `/vaults/{vaultId}` sob raiz confiável e rejeitar traversal, paths absolutos, symlinks e reparse points.
- **FR-005**: O vault só aparece no catálogo após estado `ready` e decisão positiva do Keycloak para o proprietário.
- **FR-006**: O worker DEVE reconciliar operação, projeção e filesystem por retry idempotente, lease e estados explícitos.
- **FR-007**: O provisioning de autorização do Keycloak DEVE usar credencial de menor privilégio; enquanto não houver estratégia aprovada, permanece bloqueado e não concede `manage-clients`.
- **FR-008**: A API DEVE expor criação assíncrona, consulta de operação e arquivamento por contrato OpenAPI versionado.
- **FR-009**: O frontend DEVE mostrar solicitação, progresso, erro e retry sem armazenar policy, membership, token ou path físico.
- **FR-010**: Reconciliação de resíduos DEVE registrar ocorrência idempotente, alertar o operador e preservar conteúdo; exclusão física automática não é permitida.
- **FR-011**: Backup/restore, upgrade e rollback devem preservar Markdown, PostgreSQL e chaves do BFF dentro dos limites definidos pelos mecanismos externos.

### Entidades Principais (quando houver dados)

- **VaultProvisioningOperation**: operação durável, idempotency key, estado, lease, retry e erro sanitizado.
- **Vault**: projeção com estados `pending`, `provisioning`, `ready`, `degraded`, `archived` ou `failed`.
- **VaultResidueOccurrence**: ocorrência idempotente de resíduo/ausência, retenção e estado de alerta.

## Critérios de Sucesso (obrigatório)

### Resultados Mensuráveis

- **SC-001**: Retries da mesma chave produzem uma única operação e um único vault.
- **SC-002**: Nenhum vault aparece antes de filesystem pronto e autorização positiva.
- **SC-003**: Falhas de Keycloak/filesystem não deixam autorização parcialmente utilizável.
- **SC-004**: Testes de integração com PostgreSQL e filesystem reais comprovam lease, retry, reconciliação e preservação.
- **SC-005**: Logs, métricas e relatórios não contêm conteúdo integral, tokens, paths físicos ou secrets.

## Premissas

- Keycloak é autoridade de autorização; o browser nunca usa Admin REST API.
- Markdown é fonte canônica; PostgreSQL é projeção reconstruível.
- O worker executa provisioning e reconciliação; não será criado microsserviço.
- Quotas detalhadas permanecem configuração a definir conforme P-10 legado.

## Proveniência da Migração

Origem: P-08 a P-16 de `docs/plan/alternancia-vaults.md`. P-08, P-09 e P-10 constam como concluídas, P-11 está em andamento, P-12 bloqueada por menor privilégio no Keycloak e P-13 a P-16 não iniciadas. O escopo não recebe autorização de implementação por esta migração.

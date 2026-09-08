# ADR 2026-0005: Criação autogerenciada de vaults

**Status:** accepted  
**Data:** 2026-09-04  
**Responsáveis:** equipe Altivy  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano de alternância segura entre vaults](../plan/alternancia-vaults.md), [ADR de catálogo autorizado de vaults](2026-0004-catalogo-autorizado-de-vaults.md), [ADR de BFF e autorização externa](2026-0002-bff-keycloak-autorizacao-externa.md)

## Contexto

O Altivy já permite alternar entre vaults autorizados, mas a criação de novos
vaults ainda depende de operação administrativa. A evolução desejada é permitir
que uma pessoa autenticada crie um vault pessoal sem receber acesso direto ao
Admin REST API do Keycloak, ao filesystem ou ao PostgreSQL.

A criação atravessa três recursos sem transação distribuída: projeção
PostgreSQL, recurso e permission no Keycloak Authorization Services e diretório
físico do vault. O fluxo precisa ser idempotente, assíncrono, auditável e capaz
de convergir após falha parcial.

O vault deve aparecer no catálogo somente quando estiver pronto e autorizado.
O nome informado pelo usuário é metadado e nunca pode ser usado como path
físico. PostgreSQL continua sendo projeção e controle operacional; os arquivos
Markdown continuam sendo a fonte canônica do conteúdo.

## Forças de decisão

- manter Keycloak como única autoridade de identidade e autorização;
- não persistir memberships, policies ou decisões como autoridade local;
- evitar sucesso falso quando filesystem, PostgreSQL ou Keycloak falharem;
- impedir duplicação em retries e duplo clique;
- proteger a raiz física contra traversal, symlinks e paths escolhidos pelo usuário;
- preservar operação self-hosted, backup, quota e recuperação;
- manter o primeiro recorte pequeno e reversível;
- permitir evolução posterior para compartilhamento sem comprometer o modelo pessoal.

## Opções consideradas

### Opção A: criação pessoal assíncrona com provisioning durável

A API cria uma operação idempotente, registra o vault em estado de provisioning,
reconcilia o recurso e a permission no Keycloak e agenda um job para criar a raiz
física. O vault só entra no catálogo quando estiver `ready`.

Vantagens: trata falhas parciais, não bloqueia a requisição HTTP em operações
externas, permite retry e mantém o browser sem privilégios administrativos.

Desvantagens: exige lifecycle, jobs, observabilidade, compensação e uma migration.

### Opção B: criação síncrona na requisição HTTP

A API cria PostgreSQL, Keycloak e diretório antes de responder `201 Created`.

Vantagens: fluxo inicial simples para o cliente.

Desvantagens: timeout e inconsistência são difíceis de tratar; uma falha após
uma etapa pode deixar recursos órfãos; não se ajusta bem a filesystem lento ou
Keycloak indisponível.

### Opção C: criação administrativa fora da aplicação

O usuário solicita o vault, mas um operador ou script externo realiza a criação.

Vantagens: menor mudança no núcleo do produto e menor privilégio no runtime.

Desvantagens: não atende criação autogerenciada, aumenta trabalho operacional e
mantém o produto dependente de intervenção humana.

### Opção D: memberships e autorização local no PostgreSQL

A aplicação persiste proprietário, membros, roles e decisões locais.

Vantagens: consulta e provisioning aparentemente mais simples.

Desvantagens: duplica Keycloak, cria divergência de revogação e viola as regras
arquiteturais e de segurança do Altivy.

## Decisão

Adotar a Opção A para o primeiro recorte, condicionada à aprovação humana deste
ADR:

- permitir inicialmente apenas vault pessoal;
- usar `vaultId` gerado pelo servidor como identidade física e lógica;
- aceitar `POST /api/v1/vaults` com `Idempotency-Key`;
- responder `202 Accepted` com `operationId`, sem afirmar que o vault está pronto;
- persistir o lifecycle `pending`, `provisioning`, `ready`, `degraded`, `archived`
  ou `failed`;
- usar job durável com lease, retry, backoff e erro sanitizado;
- criar a pasta somente sob a raiz confiável, como `/vaults/{vaultId}`;
- reconciliar no Keycloak o recurso `vault-{vaultId}`, scopes e permission do
  proprietário por adapter de menor privilégio;
- não permitir acesso do browser ao Admin API do Keycloak;
- não criar memberships, grupos ou policies locais;
- expor o vault no catálogo somente quando `ready` e autorizado;
- arquivar antes de considerar exclusão física definitiva;
- deixar compartilhamento, convites e papéis para outro ADR e outra entrega.

A decisão deve ser revista se o ambiente exigir múltiplos roots físicos, criação
síncrona por requisito de UX, compartilhamento na primeira entrega ou um
provisionador externo que altere as garantias de consistência.

## Consequências

### Positivas

- o usuário ganha autonomia sem receber credenciais administrativas;
- o browser permanece limitado à API normal do Altivy;
- retries podem convergir sem duplicar vaults;
- vault incompleto não aparece como autorizado;
- falhas de Keycloak e filesystem podem ser observadas e recuperadas;
- compartilhamento pode ser adicionado depois sem misturar seu modelo ao MVP.

### Negativas e trade-offs

- a operação não termina necessariamente na mesma requisição;
- são necessários estados, jobs, migration e telas de progresso;
- PostgreSQL, Keycloak e filesystem podem ficar temporariamente divergentes;
- quotas, backup e retenção precisam de política operacional antes do release;
- o adapter do Keycloak passa a exigir credencial de serviço cuidadosamente limitada.

## Segurança e privacidade

A API autoriza a criação antes de revelar ou alterar qualquer estado protegido.
O browser não recebe token administrativo, path físico, policy ou membership.

O nome é validado como metadado, mas o diretório usa somente o ID gerado pelo
servidor. A criação física valida confinamento na raiz, rejeita traversal,
paths absolutos, symlinks e reparse points e executa com permissões mínimas.

Logs e auditoria registram IDs técnicos, sujeito quando necessário, transições,
duração e resultado, sem conteúdo de notas, secrets, tokens ou nomes de vaults
não autorizados.

Uma credencial de serviço do Keycloak deve possuir somente as permissões
necessárias ao client `altivy-api`. Conta do realm `master`, senha administrativa
e acesso amplo a usuários, grupos ou outros clients não são aceitáveis.

No Keycloak 26.7.0 do laboratório, a service account do `altivy-api` possui
somente `uma_protection`. Essa permissão permite registrar recursos pelo
Protection API do próprio resource server, mas não criar ou reconciliar policies
e permissions. O Admin REST API exige `realm-management/manage-clients`, que
administra todos os clients do realm e não atende ao menor privilégio definido
neste ADR. Enquanto não houver uma alternativa aprovada que limite essa gestão
ao resource server `altivy-api`, o provisionamento automático de permissions de
proprietário permanece desabilitado.

## Dados, compatibilidade e migração

A mudança exige migration expand-contract para status operacional, operação de
provisionamento e idempotência. O schema não terá tabelas de membership como
autoridade local.

O endpoint atual `GET /api/v1/vaults` permanece compatível. Vaults existentes
continuam em estado compatível com `ready` após uma migração de dados explícita.
Nenhum conteúdo Markdown é migrado ou substituído.

## Observabilidade e operação

Medir:

- duração por etapa;
- quantidade de retries;
- operações pendentes, prontas e falhas;
- falhas de Keycloak, PostgreSQL e filesystem;
- quota rejeitada e idempotência reutilizada;
- idade máxima de operações em provisioning.

Métricas não devem incluir conteúdo, tokens ou metadados de candidatos negados.
O worker precisa de retry limitado, dead letter ou estado `failed` acionável e
reconciliação periódica. Backup e restauração devem incluir projeção, jobs e
vault físico conforme o runbook operacional.

## Validação

- teste unitário das transições e limites;
- teste de contrato de `202`, operação e erros;
- teste de idempotência com a mesma chave;
- integração com PostgreSQL e filesystem real;
- adapter Keycloak testado por contrato controlado e cenários de indisponibilidade;
- teste de falha entre cada par de etapas;
- teste de traversal, symlink, quota e autorização horizontal;
- E2E de criação, reload, retry, falha e aparecimento somente em `ready`;
- evidência de que outro sujeito não visualiza o vault.

## Rollout e rollback

Entregar primeiro o domínio, migration, job e adapter atrás de uma configuração
desativada. Depois habilitar somente para o laboratório, com quotas pequenas e
fixtures sintéticos. Promover para instalações reais apenas após backup,
reconciliação e testes de falha.

O rollback desativa novas criações, permite concluir ou cancelar operações em
andamento e mantém vaults já `ready` acessíveis pelos endpoints existentes. Não
remover dados físicos automaticamente durante rollback. Migration destrutiva,
exclusão definitiva e compartilhamento exigem decisões próprias.

## Referências

- [Plano mestre](../plano-mestre.md)
- [Plano de alternância segura entre vaults](../plan/alternancia-vaults.md)
- [ADR de catálogo autorizado de vaults](2026-0004-catalogo-autorizado-de-vaults.md)
- [Implementação do backend](../backend.md)
- [Configuração do Keycloak](../configuracao-keycloak.md)

# Especificação da Feature: Plugin Microsoft Teams e External Push

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, bloqueada para execução

**Entrada**: Migração de `docs/plan/app-altivy-notes-teams-external-push.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Adicionar plugins confiáveis (Prioridade: P1)

Como operador, quero habilitar plugins incluídos na distribuição sem carregar código arbitrário em runtime.

**Valor desta prioridade**: Integrações devem ser extensíveis sem acoplar o núcleo ao SDK de fornecedores.

**Teste independente**: Registrar plugin sintético, validar manifesto/capability e desabilitá-lo.

**Cenários de aceite**:

1. **Dado** um plugin com manifesto válido e compatível, **Quando** o host inicia, **Então** registra somente as capabilities declaradas.
2. **Dado** manifesto duplicado, incompatível ou com capability desconhecida, **Quando** o host valida, **Então** rejeita o plugin sem degradar o núcleo.

### Jornada do Usuário 2 - Salvar mensagem do Teams no Altivy Notes (Prioridade: P1)

Como usuário do Teams, quero revisar e enviar explicitamente uma mensagem ou texto para um vault autorizado.

**Valor desta prioridade**: A importação explícita permite capturar conteúdo sem ler o Microsoft Graph.

**Teste independente**: Executar uma message extension em tenant de teste e verificar confirmação e conteúdo persistido.

**Cenários de aceite**:

1. **Dado** que a pessoa aciona “Salvar no Altivy Notes”, **Quando** confirma título, destino e conteúdo, **Então** a entrega é persistida antes da confirmação.
2. **Dado** uma identidade Teams não vinculada ou sem autorização Keycloak, **Quando** a entrega é submetida, **Então** nenhum vault é revelado ou alterado.

### Jornada do Usuário 3 - Receber entregas de produtores autorizados (Prioridade: P2)

Como operador, quero aceitar external-push idempotente de produtores confiáveis além do Teams sem expor um proxy genérico.

**Valor desta prioridade**: O contrato neutro permite integrações confiáveis sem acoplar o núcleo.

**Teste independente**: Enviar duas vezes a mesma entrega autenticada e validar que existe uma única operação e documento.

**Cenários de aceite**:

1. **Dado** um produtor autorizado, **Quando** envia conteúdo válido, **Então** o sistema persiste inbox/job durável e processa Markdown com segurança.
2. **Dado** payload excessivo, replay ou conflito de edição, **Quando** a entrega é processada, **Então** é rejeitada ou colocada em conflito sem sobrescrita silenciosa.

## Casos de Borda

- Tenant bloqueia apps personalizados ou callback HTTPS público.
- Replay, clique repetido, indisponibilidade do Teams, retry e concorrência.
- Identidade externa não vinculada, autorização revogada ou vault inexistente.
- Markdown malicioso, traversal, conteúdo excessivo e falha parcial do worker.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: O Plugin Host DEVE aceitar apenas plugins confiáveis da distribuição e validar manifesto, versão e capabilities.
- **FR-002**: Desabilitar plugin DEVE remover rotas e jobs sem degradar readiness do núcleo.
- **FR-003**: `external-push/v1` DEVE autenticar produtores autorizados, persistir antes de responder e ser idempotente.
- **FR-004**: O Teams DEVE permitir revisão explícita de título, destino lógico e conteúdo antes do envio.
- **FR-005**: Identidade Teams não concede acesso; Keycloak continua sendo autoridade e a API verifica autorização por vault.
- **FR-006**: O fluxo inicial NÃO DEVE exigir leitura do Microsoft Graph nem criar microsserviço.
- **FR-007**: Conteúdo é gravado como Markdown seguro; PostgreSQL mantém estado técnico reconstruível.
- **FR-008**: Logs, traces e auditoria não registram tokens, secrets ou conteúdo integral.
- **FR-009**: Outgoing Webhook é somente fallback manual opcional e requer autenticação/HMAC documentada.

### Entidades Principais (quando houver dados)

- **Plugin**: módulo versionado da distribuição com manifesto e capabilities.
- **Conexão**: configuração externa que referencia secrets fora do banco.
- **Entrega**: inbox idempotente com origem, destino lógico, estado e job durável.

## Critérios de Sucesso (obrigatório)

### Resultados Mensuráveis

- **SC-001**: Plugin sintético demonstra registro/desativação sem alterar Notes, Vaults ou Indexing.
- **SC-002**: Mensagem/texto de teste chega uma vez ao vault autorizado e retries não duplicam documentos.
- **SC-003**: Nenhuma API administrativa do Keycloak é acessível pelo browser ou identidade Teams.
- **SC-004**: Indisponibilidade da integração não impede operações locais do Altivy.

## Premissas

- A feature é candidata à Fase 5 e não bloqueia o MVP.
- P-01 está bloqueado até nova autorização humana; não iniciar tenant, consentimento ou implementação por esta migração.
- O app requer callback HTTPS alcançável pelo Teams ou relay aprovado.
- Microsoft Graph, sync automático de Loop e leitura de conteúdo estão fora desta feature.

## Proveniência da Migração

Origem: `docs/plan/app-altivy-notes-teams-external-push.md`, status geral `blocked`, P-01 bloqueada e P-02 a P-16 `not-started`. A migração não libera a Fase 5.

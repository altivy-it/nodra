# Especificação da Feature: Espelhamento de Notas Loop do Teams

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, bloqueada para execução

**Entrada**: Migração de `docs/plan/integracao-teams-notas-reuniao.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Vincular notas de reunião à origem Loop (Prioridade: P1)

Como usuário, quero vincular uma reunião e seu arquivo Loop ao Altivy para importar notas sem misturar reuniões recorrentes.

**Valor desta prioridade**: A integração depende de identificar e autorizar a fonte correta.

**Teste independente**: Executar prova de conceito com Microsoft Graph v1.0 e arquivo Loop sintético/autorizado.

**Cenários de aceite**:

1. **Dado** um arquivo Loop acessível por Graph v1.0, **Quando** a reunião é vinculada, **Então** a associação é estável e limitada ao tenant/drive autorizado.
2. **Dado** que a descoberta automática não é viável, **Quando** a pessoa fornece URL ou seleciona o arquivo, **Então** a vinculação assistida é explícita e validada.

### Jornada do Usuário 2 - Manter um documento Markdown sincronizado (Prioridade: P1)

Como usuário, quero receber alterações de agenda, notas e tarefas em um documento Markdown rastreável, sem sobrescrita silenciosa.

**Valor desta prioridade**: O espelho deve ser útil em instalação self-hosted e preservar edições concorrentes.

**Teste independente**: Alterar uma nota de teste e validar conversão, idempotência, indexação e conflito.

**Cenários de aceite**:

1. **Dado** alteração válida na origem, **Quando** polling/delta detecta a mudança, **Então** uma atualização idempotente é processada.
2. **Dado** uma edição local concorrente, **Quando** a sincronização tenta gravar, **Então** preserva ambas as versões e coloca o mapping em conflito.

### Jornada do Usuário 3 - Operar em instalação privada com segurança (Prioridade: P2)

Como operador, quero sincronização por delta polling sem depender de webhook público, podendo ativar webhook apenas quando aprovado.

**Valor desta prioridade**: O produto deve continuar local-first e reduzir exposição de rede.

**Teste independente**: Desabilitar endpoint público e comprovar polling, retry, revogação e recuperação.

**Cenários de aceite**:

1. **Dado** ambiente privado, **Quando** o worker executa delta polling, **Então** sincroniza mudanças sem entrada pública.
2. **Dado** webhook habilitado, **Quando** chega sinal inválido, **Então** é rejeitado antes de buscar ou gravar conteúdo.

## Casos de Borda

- `410 Gone`, cursor delta inválido, throttling e subscription expirada.
- Permissão revogada entre fetch e commit, payload HTML malicioso e callback falso.
- Reunião recorrente, arquivo removido e conflito com edição local.
- Indisponibilidade do Graph não pode impedir operações locais.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: A integração DEVE usar Microsoft Graph v1.0 e bloquear qualquer necessidade de `/beta` até decisão explícita.
- **FR-002**: Deve existir decisão/POC comprovando descoberta automática ou definir vínculo assistido.
- **FR-003**: O espelho é unilateral Teams/Loop → Altivy e produz Markdown gerenciado, sem edição bidirecional.
- **FR-004**: Delta polling é a baseline self-hosted; webhook é opcional e condicionado a decisão de exposição.
- **FR-005**: Operações são duráveis e idempotentes; conflitos preservam conteúdo local e importado.
- **FR-006**: Consentimento administrativo e scopes mínimos devem ser comprovados; nenhuma escrita externa não necessária é permitida.
- **FR-007**: Conteúdo externo é sanitizado; logs não registram conteúdo, tokens ou URLs pré-autenticadas.
- **FR-008**: Remoção da origem não apaga automaticamente documento Markdown.
- **FR-009**: Esta integração não exige serviços externos obrigatórios nem bloqueia o MVP local.

### Entidades Principais (quando houver dados)

- **Mapping**: associação entre ocorrência de reunião, driveItem e vault autorizado.
- **Cursor**: posição incremental de sincronização, recuperável e sem conteúdo sensível.
- **Execução de sync**: estado idempotente, tentativas, lease e resultado.

## Critérios de Sucesso (obrigatório)

- **SC-001**: A POC localiza ou vincula corretamente um arquivo `.loop` por Graph v1.0.
- **SC-002**: Atualizações produzem um único Markdown por ocorrência e preservam conflitos.
- **SC-003**: Revogação impede commit de conteúdo; `410`, throttling e falhas são observáveis e recuperáveis.
- **SC-004**: Operações locais permanecem disponíveis com integração desabilitada.

## Premissas

- A integração está bloqueada por decisão humana de 2026-10-05 e requer nova autorização antes de executar P-01.
- O Microsoft 365 é fonte da área importada; o Markdown do Altivy é a representação canônica persistida.
- A integração `external-push` é especificada separadamente em `specs/004-app-teams-external-push/`.

## Proveniência da Migração

Origem: `docs/plan/integracao-teams-notas-reuniao.md`, status geral legado `blocked`, com P-01 a P-16 `not-started`. A migração não remove o bloqueio humano.

# Gestão do projeto no GitHub

Este documento define como usar GitHub Issues e GitHub Projects para gerenciar o Nodra com um fluxo equivalente a Work Items do Azure DevOps ou Jira.

## Limites da configuração

Os formulários em `.github/ISSUE_TEMPLATE/` são versionados neste repositório. O Project, seus campos e suas visualizações são recursos da conta ou organização do GitHub e precisam ser criados na interface do GitHub ou pela API. Este documento é a configuração de referência para evitar divergência entre ambientes.

O Project organiza intake, responsáveis e estado de Issues associadas a tarefas Spec Kit. A fonte versionada de requisitos, desenho, dependências e tarefas é exclusivamente o Spec Kit; GitHub Issues/Projects não definem escopo nem autorizam trabalho por conta própria.

## Tipos de demanda

| Tipo | Formulário | Uso | Label inicial |
| --- | --- | --- | --- |
| Bug | `bug-report.yml` | intake de comportamento incorreto; execução segue Spec Kit Bug Fixing | `type:bug` |
| Feature | `feature-request.yml` | intake de necessidade; execução segue Spec Kit SDD | `type:feature` |
| Tarefa Spec Kit | `spec-kit-task.yml` | autorização humana de uma tarefa já existente em artefato Spec Kit | `type:spec-task` |
| Tarefa técnica | `technical-task.yml` | intake técnico que precisa ser especificado antes da execução | `type:task` |
| Decisão arquitetural | `architecture-spike.yml` | intake de decisão; ADR é complementar e não cria plano de implementação | `type:spike` |

Vulnerabilidades não devem ser registradas nesses formulários. Use o fluxo de segurança do repositório.

## Project recomendado

Crie um Project de organização chamado `Altivy Delivery` e associe o repositório `lzocateli/Nodra`.

Configure os campos abaixo. `Status` é o campo padrão do Projects; os demais são campos personalizados.

| Campo | Tipo | Valores sugeridos |
| --- | --- | --- |
| Status | Single select | Inbox, Triage, Ready, In progress, Blocked, In review, Validation, Done, Cancelled |
| Type | Single select | Bug, Feature, Task, Spec task, Architecture decision |
| Priority | Single select | P0, P1, P2, P3 |
| Area | Iteration ou texto | Architecture, Backend, Frontend, Database, Security, DevOps, Documentation, Tests |
| Risk | Single select | Low, Medium, High |
| Spec artifact | Text | caminho do diretório `specs/<feature>/` ou artefato oficial `.specify/bugs/<slug>/` |
| Task ID | Text | ID de tarefa existente em `tasks.md` ou ação do fluxo oficial Bug Fixing |
| Iteration | Iteration | sprint ou ciclo de trabalho |
| Target | Single select | MVP, Fase 3, Fase 5, Post-MVP, Unplanned |
| Human approval | Single select | Pending, Approved, Not required |
| Blocked by | Text | Issue, ADR, decisão ou dependência externa |

Os valores dos campos devem permanecer em inglês somente quando isso facilitar integração com automações; os títulos e instruções para pessoas permanecem em português.

## Visualizações

Crie as seguintes views no mesmo Project:

### Backlog

- layout: Table;
- filtro: `status != Done` e `status != Cancelled`;
- ordenar por: `Priority` e `updated`;
- colunas: Title, Type, Status, Priority, Area, Spec artifact, Task ID, Iteration, Assignee.

### Kanban

- layout: Board;
- agrupar por: `Status`;
- filtro: `status != Done` e `status != Cancelled`;
- colunas visíveis: Inbox, Triage, Ready, In progress, Blocked, In review, Validation.

### Sprint atual

- layout: Board ou Table;
- filtro: `Iteration = @current`;
- agrupar por: `Status`;
- mostrar: Priority, Type, Area, Assignee e Target.

### Roadmap

- layout: Roadmap;
- eixo temporal: `Iteration` ou data alvo;
- agrupar por: `Target`;
- filtro: `status != Cancelled`.

### Risco e bloqueios

- layout: Table;
- filtro: `Risk = High OR Status = Blocked`;
- ordenar por: Priority e updated;
- mostrar: Title, Type, Risk, Status, Blocked by, Assignee, Spec artifact, Task ID.

### Tarefas Spec Kit

- layout: Table;
- filtro: `Type = Spec task`;
- agrupar por: `Spec artifact`;
- ordenar por: `Task ID`;
- mostrar: Title, Spec artifact, Task ID, Status, Human approval, Dependencies, Assignee.

## Fluxo operacional

1. A pessoa registra a demanda pelo Issue Form apropriado.
2. A Issue entra no Project com `Status = Inbox`.
3. A triagem confirma Type, Priority, Area, Risk, responsável e dependências.
4. Demandas incompletas ficam em `Triage`; demandas prontas passam a `Ready`.
5. `Ready` no Project não autoriza execução: uma pessoa ou o Copilot só pode executar uma tarefa que exista em artefato Spec Kit e tenha autorização humana explícita.
6. O trabalho em execução fica em `In progress`; impedimentos ficam em `Blocked` com causa explícita.
7. Um Pull Request deve referenciar a Issue, por exemplo `Implements #123`.
8. Com o PR aberto, a demanda passa a `In review`; após os checks, vai para `Validation`.
9. A revisão humana confirma os critérios de aceite e move a demanda para `Done`.
10. O fechamento da Issue não substitui evidências nem atualização dos artefatos oficiais Spec Kit.

## Automação recomendada

Use os workflows nativos do GitHub Projects para:

- adicionar automaticamente novas Issues do repositório ao Project;
- aplicar `Status = Inbox` em novas Issues;
- mover para `In progress` quando uma Issue for atribuída;
- mover para `In review` quando um Pull Request for aberto;
- mover para `Done` quando a Issue for fechada;
- arquivar itens concluídos após o prazo de retenção definido pela equipe.

A automação nativa não interpreta com segurança todos os campos de texto de um Issue Form. Durante a triagem, copie `Priority`, `Area`, `Risk`, `Spec artifact` e `Task ID` para os campos do Project. Uma Action com GitHub App ou token de Project pode automatizar essa sincronização depois, mas deve ser adicionada somente com permissões mínimas e revisão de segurança.

## Regras de governança

- Uma Issue representa intake ou rastreamento; artefatos Spec Kit são a única fonte versionada de escopo, plano e tarefas de aplicação.
- `Done` no Project não autoriza merge, publicação ou deploy por si só.
- Issues sem tarefa Spec Kit associada não devem ser executadas; `docs/plan/` permanece congelado enquanto os oito conjuntos migrados aguardam revisão humana e arquivamento das fontes legadas.
- Alterações de arquitetura, contrato, migration ou segurança devem atualizar os documentos correspondentes no mesmo change set.
- Dependências são registradas nos artefatos Spec Kit e podem ser espelhadas por links para Issues, ADRs ou outras tarefas; Project não é a fonte dessas dependências.
- Não registre secrets, conteúdo real de vaults ou dados pessoais nas Issues.

## Checklist de criação do Project

- [ ] Criar `Altivy Delivery` no nível da organização.
- [ ] Associar `lzocateli/Nodra`.
- [ ] Criar os campos personalizados da tabela.
- [ ] Criar as seis views descritas neste documento.
- [ ] Configurar automação para novas Issues e PRs.
- [ ] Criar ou padronizar labels `type:*` e `status:triage`.
- [ ] Definir responsáveis pela triagem e pela revisão humana.
- [ ] Fazer uma Issue de teste por tipo e confirmar filtros e views.

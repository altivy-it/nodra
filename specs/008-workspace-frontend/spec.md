# Especificação da Feature: Workspace do AltivyNotes

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, aguardando revisão humana

**Entrada**: Migração de `docs/plan/workspace-frontend-inspirado-no-obsidian.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Navegar pelo workspace (Prioridade: P1)

Como profissional que organiza conhecimento em Markdown, quero alternar módulos, explorar pastas e manter o contexto sem perder meu trabalho.

**Valor desta prioridade**: A navegação é a base do uso contínuo do produto.

**Teste independente**: Navegar entre módulos/notas e confirmar que abas, seleção e conteúdo não são descartados.

**Cenários de aceite**:

1. **Dado** o workspace aberto, **Quando** a pessoa seleciona módulo ou pasta por mouse/teclado, **Então** foco, contexto e estado visual permanecem previsíveis.
2. **Dado** texto não salvo, **Quando** a pessoa alterna painel ou módulo, **Então** o conteúdo não é perdido silenciosamente.

### Jornada do Usuário 2 - Trabalhar com painéis e propriedades (Prioridade: P1)

Como usuário, quero organizar notas em um shell responsivo e inspecionar propriedades sem alterar frontmatter silenciosamente.

**Valor desta prioridade**: O workspace deve ser denso, eficiente e compatível com Markdown existente.

**Teste independente**: Redimensionar painéis, editar uma propriedade e validar preservação do frontmatter.

**Cenários de aceite**:

1. **Dado** um viewport suportado, **Quando** painéis são redimensionados/recolhidos, **Então** a área central continua utilizável e sem sobreposição.
2. **Dado** uma nota sem frontmatter Altivy, **Quando** propriedades são exibidas, **Então** a nota continua editável e não recebe alteração silenciosa.

### Jornada do Usuário 3 - Consultar notificações e ajuda (Prioridade: P2)

Como usuário, quero consultar eventos operacionais e ajuda contextual sem expor logs ou misturar documentação com meu vault.

**Valor desta prioridade**: Informação útil deve ser acionável e respeitar privacidade.

**Teste independente**: Abrir central/ajuda, reconectar SignalR e verificar que conteúdo sensível não é exibido/indexado.

**Cenários de aceite**:

1. **Dado** uma reconexão, **Quando** notificações são reconstruídas pela API e atualizadas via SignalR, **Então** não há duplicidade.
2. **Dado** conteúdo de ajuda, **Quando** é exibido, **Então** é somente leitura e não participa da busca/indexação do vault.

## Casos de Borda

- Layout em desktop/tablet/celular, teclado, foco e alto contraste.
- Storage ausente, bloqueado, corrompido ou versão incompatível.
- API/SignalR offline, eventos repetidos, plugin opcional degradado.
- Frontmatter inválido e edição concorrente de nota.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: O shell DEVE separar barra de atividades, painel primário, área central e painel contextual conforme viewport.
- **FR-002**: Painéis DEVEM ser redimensionáveis/recolhíveis com teclado e estado estável.
- **FR-003**: Navegação e troca de módulo NÃO DEVEM descartar conteúdo não salvo, seleção, scroll ou abas sem tratamento explícito.
- **FR-004**: Preferências não sensíveis DEVEM ser validadas, versionadas e isoladas por usuário e vault; falha de storage não pode impedir o shell.
- **FR-005**: Notificações DEVEM usar API como fonte reconstruível e SignalR como aceleração, com IDs estáveis e deduplicação.
- **FR-006**: Ajuda DEVE ser somente leitura, empacotada fora do vault e excluída de CRUD/busca/grafo/indexação.
- **FR-007**: Propriedades/frontmatter DEVEM preservar Markdown e não alterar notas externas silenciosamente.
- **FR-008**: Slots não podem carregar código arbitrário/remoto; integração opcional degradada não pode derrubar o shell.
- **FR-009**: Interface DEVE ser acessível por teclado e atender contraste AA em desktop, tablet e celular.

### Entidades Principais (quando houver dados)

- **WorkspacePreference**: configuração não sensível de painel, tema, densidade, filtro, ordem e módulo, por usuário/vault.
- **Notification**: evento com ID, origem, categoria, severidade, instante, estado de leitura e ação segura opcional.
- **WorkspaceTab**: contexto de documento e estado de navegação preservado por sessão.

## Critérios de Sucesso (obrigatório)

- **SC-001**: Fluxos principais passam testes teclado, responsividade e contraste AA.
- **SC-002**: Nenhum texto não salvo é perdido em transições cobertas.
- **SC-003**: Notificações não duplicam após reconexão e não expõem conteúdo sensível.
- **SC-004**: Preferências inválidas/indisponíveis resultam em defaults seguros sem bloquear a aplicação.

## Premissas

- PO UI é a biblioteca de UI genérica aprovada; CodeMirror e Cytoscape são exceções existentes.
- O frontend Angular 22 usa contratos HTTP e não conhece paths físicos nem schema PostgreSQL.
- Plug-ins são confiáveis e distribuídos com a aplicação; não há execução dinâmica.

## Proveniência da Migração

Origem: `docs/plan/workspace-frontend-inspirado-no-obsidian.md`, status legado `in-progress`. P-01 a P-04, P-09 a P-11 estão em andamento; P-05 a P-08 estão `not-started`. Nenhuma etapa foi marcada concluída por esta migração.

# Especificação da Funcionalidade: Migração dos auxiliares para skills

**Branch da Funcionalidade**: `009-migracao-prompts-skills`

**Criada em**: 2026-10-22

**Status**: aprovado pelo solicitante em 2026-10-22 às 17:32 (-03:00); implementação autorizada, validação pendente

**Origem**: IA assistida

**Revisor humano**: solicitante desta conversa; decisões de escopo em 2026-10-22 às 17:22 (-03:00), após esclarecimento

**Entrada**: Executar a migração proposta em `.github/plano-migracao-prompts-skills.md`, formalizando primeiro em Spec Kit e apresentando tarefas para aprovação antes da implementação.

**Aprovação referenciada**: nesta conversa, ao invocar `/speckit-implement`,
o solicitante declarou “aprovado, registre e siga com a implementação”
em 2026-10-22 às 17:32 (-03:00), após a apresentação dos artefatos e T001–T019.
O aceite autoriza as tarefas, não dispensa seus gates nem aprova testes futuros.

## Cenários de Usuário e Testes *(obrigatório)*

### História de Usuário 1 - Registrar uma decisão justificada (Prioridade: P1)

Como mantenedor, quero solicitar explicitamente auxílio para redigir um ADR de
uma tarefa oficial, mantendo a decisão humana e sem criar um plano paralelo.

**Motivo da prioridade**: preservar autoridade e rastreabilidade das decisões.

**Teste independente**: solicitar o auxílio com e sem justificativa duradoura
e conferir o vínculo ao trabalho oficial e o status do registro.

**Cenários de aceitação**:

1. **Dado** um plano/tarefa oficial que identifica uma decisão duradoura,
   **Quando** o auxílio é solicitado, **Então** o ADR segue o template,
   sequência anual, origem, revisor humano e status `proposed`.
2. **Dado** um pedido sem vínculo oficial ou apenas uma escolha local reversível,
   **Quando** o auxílio é solicitado, **Então** nenhum ADR é escrito e a
   pendência ou o local apropriado da decisão é informado.
3. **Dado** um ADR proposto, **Quando** o auxiliar termina, **Então** não o
   aceita nem implementa a decisão automaticamente.

### História de Usuário 2 - Gerar testes de trabalho autorizado (Prioridade: P1)

Como mantenedor, quero obter testes derivados de uma tarefa autorizada, sem
introduzir requisitos ou alterações de produto por iniciativa do auxiliar.

**Motivo da prioridade**: impedir expansão de escopo e manter evidência real.

**Teste independente**: fornecer artefato e ID de tarefa, comparar os testes
propostos aos critérios e repetir sem a referência obrigatória.

**Cenários de aceitação**:

1. **Dado** artefato oficial, ID e autorização, **Quando** solicito testes,
   **Então** o auxiliar cobre os critérios da tarefa, respeita as instruções
   existentes e informa comandos, resultados e limites.
2. **Dado** ausência de referência ou autorização, **Quando** solicito testes,
   **Então** o auxiliar informa o bloqueio sem editar o produto.

### História de Usuário 3 - Revisar sem escrita e com validação controlada (Prioridade: P1)

Como revisor, quero inspecionar uma mudança vinculada ao trabalho oficial e
autorizar separadamente validações estreitas quando forem necessárias.

**Motivo da prioridade**: preservar o contrato de inspeção e os limites das ferramentas.

**Teste independente**: revisar um diff sem autorização para executar testes,
depois autorizar um comando estreito permitido e observar a distinção.

**Cenários de aceitação**:

1. **Dado** diff e referência oficial, **Quando** solicito revisão,
   **Então** recebo achados por severidade, localização, cenário, evidências,
   dúvidas e lacunas de teste, sem edição de código.
2. **Dado** validação necessária mas não autorizada, **Quando** o auxiliar a
   identifica, **Então** ele pede autorização e informa possíveis artefatos.
3. **Dado** autorização explícita, **Quando** as ferramentas do agente permitem
   o comando estreito, **Então** ele pode executá-lo e relatar seus efeitos;
   se não permitem, registra o limite sem ampliar permissões.
4. **Dado** ausência do artefato oficial, **Quando** a revisão começa,
   **Então** registra bloqueio de governança, sem substituir requisitos por Issues.

### História de Usuário 4 - Localizar textos sem alterar contratos (Prioridade: P2)

Como mantenedor, quero auditar ou traduzir textos humanos do Spec Kit em
escopo explícito, preservando literais, conteúdo do usuário e componentes gerenciados.

**Motivo da prioridade**: manter compreensão em pt-BR sem comprometer as ferramentas.

**Teste independente**: solicitar inventário sem alvo, auditoria sem modo e
tradução explícita sobre uma fixture descartável, comparando os literais.

**Cenários de aceitação**:

1. **Dado** nenhum alvo, **Quando** solicito localização, **Então** recebo
   inventário sem escrita.
2. **Dado** alvo sem modo explícito, **Quando** solicito localização,
   **Então** somente ocorre auditoria.
3. **Dado** tradução explícita com exclusões, **Quando** o auxiliar atua,
   **Então** preserva contratos, IDs, aliases, placeholders, marcadores,
   comandos, hashes e arquivos excluídos e relata drift e limites.

### Casos de Borda

- Agente correspondente indisponível: informar o bloqueio e solicitar seleção
  explícita de um agente compatível; não presumir equivalência nem permissões.
- Workspace com AltivyNotes e Dokpod: escolher a raiz AltivyNotes e o auxiliar
  prefixado correto; não operar sobre outro projeto.
- Pedido ambíguo ou sem autorização: esclarecer antes de escrita/execução.
- Validação gera relatórios ou caches: não descrevê-la como puramente read-only.
- Gate interativo indisponível: manter o prompt original do fluxo afetado e
  registrar `NOT RUN`, sem remover por validação estática apenas.
- Alteração preexistente em `.vscode/mcp.json`: preservar sem ler seu conteúdo
  ou incluí-la na migração.

## Requisitos *(obrigatório)*

### Requisitos Funcionais

- **FR-001**: Os quatro auxiliares DEVEM possuir entrada explícita distinta e
  prefixada `altivy-notes-*`, sem ativação automática ou pontes Local.
- **FR-002**: Cada auxiliar DEVE preservar entradas, entregáveis e restrições
  do fluxo original, com um único procedimento canônico após a migração.
- **FR-003**: ADR DEVE exigir justificativa duradoura e vínculo oficial,
  preservar template, sequência anual e `proposed`, sem aceitar decisões.
- **FR-004**: Geração de testes DEVE exigir artefato oficial, ID, critérios e
  autorização, sem criar requisitos ou ampliar alterações de produção.
- **FR-005**: Revisão NÃO DEVE editar código; validações estreitas exigem
  autorização explícita, divulgação dos efeitos e ferramentas permitidas.
- **FR-006**: Localização DEVE inventariar sem alvo e somente auditar sem modo,
  preservando literais técnicos, exclusões, hashes, overrides e conteúdo do usuário.
- **FR-007**: Os papéis arquitetural, de qualidade e de revisão DEVEM ser
  selecionados explicitamente quando disponíveis, sem seleção implícita de
  agente/modelo/ferramentas em campos não suportados; indisponibilidade bloqueia
  a execução dependente até escolha explícita compatível.
- **FR-008**: Documentação e consumidores DEVEM apontar aos novos auxiliares
  somente quando existentes, sem alterar processo, autoridade ou status histórico.
- **FR-009**: Cada fonte original SÓ DEVE ser removida após comparação semântica,
  teste interativo de entradas/restrições e revisão do diff correspondente.
- **FR-010**: A migração DEVE preservar `.vscode/mcp.json`, componentes
  gerenciados, skills `speckit-*`, constituição, planos históricos, produto,
  vaults, secrets e hooks; não instalar/reconciliar extensões.
- **FR-011**: Comandos de testes/build eventualmente autorizados DEVEM manter
  execução containerizada e os contratos existentes de proxy, sem runtime no host.
- **FR-012**: A entrega DEVE informar por fluxo o resultado estático e
  interativo, diferenciando execução observada, confirmação humana e `NOT RUN`.

## Critérios de Sucesso *(obrigatório)*

### Resultados Mensuráveis

- **SC-001**: Os quatro fluxos são acessíveis explicitamente e cada entrada
  seleciona o auxiliar correto com AltivyNotes isolado e em workspace com Dokpod.
- **SC-002**: Todos os cenários de aceitação das quatro histórias são
  demonstrados com resultado e responsável identificados antes da remoção final.
- **SC-003**: Nenhum cenário negativo produz escrita, execução ou aceitação
  sem a autorização e as condições exigidas.
- **SC-004**: Ao concluir, há zero referências operacionais aos quatro prompts
  removidos e zero alteração nas superfícies protegidas.
- **SC-005**: Todas as referências locais alteradas resolvem; os quatro
  procedimentos preservam integralmente os contratos inventariados.

## Premissas

- O solicitante aprovou os quatro nomes prefixados e migração completa, sem
  pontes Local; primeiro autorizou formalização e, às 17:32 (-03:00),
  aprovou os artefatos e a implementação de T001–T019.
- O solicitante aprovou revisão com validações estreitas explicitamente
  autorizadas e ferramentas permitidas; seleção dos agentes é explícita.
- Esta manutenção não muda a aplicação nem exige ADR arquitetural novo.
- Testes interativos exigem cliente com raiz AltivyNotes selecionada; resultado
  estático não substitui essa evidência.
- O plano preliminar em `.github/` é origem histórica, não processo concorrente.

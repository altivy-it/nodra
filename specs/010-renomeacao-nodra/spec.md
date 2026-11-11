# Feature Specification: Renomeacao para Altivy Nodra

**Feature Branch**: `010-renomeacao-nodra`

**Created**: 2026-11-11

**Status**: Em implementacao; renomeacao aplicada, qualificacao pendente.

**Input**: Decisao humana de adotar Altivy como empresa, Nodra como produto e aplicativo, Altivy Nodra como marca completa, nodra como nome do repositorio e futura CLI, preservando todas as integracoes existentes na origin/main.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reconhecer a identidade Nodra (Priority: P1)

Como usuario, quero encontrar a mesma identidade de produto na aplicacao, na documentacao e nos canais de distribuicao, distinguindo Nodra da empresa Altivy.

**Why this priority**: Registra a decisao de marca e elimina nomes concorrentes para o mesmo produto.

**Independent Test**: Inspecionar as superficies publicas e comparar seus nomes com o registro de marca desta especificacao.

**Acceptance Scenarios**:

1. **Given** a aplicacao acessivel, **When** o usuario abre suas telas, **Then** o nome do aplicativo e Nodra e a marca completa, quando apresentada, e Altivy Nodra.
2. **Given** a documentacao vigente, **When** uma pessoa consulta a apresentacao do produto, **Then** encontra a empresa Altivy, o produto Nodra, a tagline e o posicionamento definidos abaixo.
3. **Given** a direcao de marca com IA, **When** o usuario utiliza o nucleo self-hosted sem servico de IA, **Then** notas, edicao, persistencia e busca continuam independentes de IA e nuvem.

### User Story 2 - Desenvolver e distribuir o projeto renomeado (Priority: P1)

Como mantenedor, quero desenvolver, testar e distribuir Nodra em uma branch propria, sem perder as alteracoes integradas de outras branches.

**Why this priority**: Uma renomeacao incompleta pode quebrar builds, publicacao, links e operacao.

**Independent Test**: Verificar a ancestralidade da branch, executar os gates aplicaveis e conferir referencias ao repositorio e ao produto.

**Acceptance Scenarios**:

1. **Given** a origin/main consolidada, **When** a branch da renomeacao e criada, **Then** todos os commits previamente integrados permanecem em sua ancestralidade.
2. **Given** os projetos e seus testes, **When** os nomes tecnicos sao migrados, **Then** referencias internas, comandos documentados e artefatos de distribuicao permanecem coerentes e verificaveis.
3. **Given** permissao administrativa no repositorio remoto, **When** a renomeacao aprovada e executada, **Then** o repositorio passa a se chamar nodra e origin aponta para o destino confirmado, sem recriar o historico.

### User Story 3 - Atualizar instalacoes sem perder dados (Priority: P1)

Como operador, quero adotar Nodra sem perder vaults, acesso, configuracao ou capacidade de recuperacao.

**Why this priority**: O nome atual participa de identificadores operacionais; substituicao indiscriminada pode desconectar dados e identidades existentes.

**Independent Test**: Executar os cenarios de instalacao limpa e atualizacao previstos no plano, verificando acesso aos mesmos vaults e recuperacao documentada.

**Acceptance Scenarios**:

1. **Given** uma instalacao existente, **When** o operador segue a migracao, **Then** seus vaults e arquivos permanecem intactos e acessiveis.
2. **Given** configuracao externa de identidade e secrets, **When** os nomes publicos sao alterados, **Then** nenhuma credencial e lida, copiada, exibida ou substituida pela automacao de renomeacao.
3. **Given** identificadores legados preservados por compatibilidade, **When** o operador consulta o guia, **Then** encontra a justificativa, o procedimento de transicao e a recuperacao aplicavel.

### Edge Cases

- O destino remoto nodra ja existe ou o operador nao possui permissao administrativa.
- A organizacao altivy e diferente da organizacao atual altivy-it; transferencia exige decisao explicita.
- Instalacoes existentes usam nomes antigos em caminhos, volumes, banco, identidade ou configuracao externa.
- Nomes da empresa Altivy nao devem ser substituidos por Nodra.
- Documentos historicos, migracoes ja aplicadas e identificadores de compatibilidade nao devem ser reescritos sem criterio de preservacao.
- Links e imagens podem usar redirecionamentos do GitHub, mas nao devem depender exclusivamente deles.
- A main possui falha preexistente de dependencias Angular; a renomeacao nao deve ocultar essa limitacao nem declarar gates executados sem evidencia.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A empresa DEVE permanecer Altivy; o produto e o aplicativo DEVEM se chamar Nodra; a marca completa DEVE ser Altivy Nodra.
- **FR-002**: O registro de marca DEVE preservar a tagline literal "Your connected knowledge workspace.".
- **FR-003**: O registro de marca DEVE preservar o posicionamento literal "Altivy Nodra is an AI-powered knowledge workspace that connects your notes, documents, ideas, and conversations into a searchable personal knowledge graph.".
- **FR-004**: A identidade conceitual DEVE registrar a sequencia Notes → Nodes → Connections → Knowledge → AI e a associacao de Nodra a node, notas interligadas, grafo de conhecimento e IA.
- **FR-005**: A documentacao DEVE distinguir posicionamento de marca de funcionalidades efetivamente entregues, sem criar dependencia obrigatoria de IA, embeddings, nuvem ou internet para o nucleo.
- **FR-006**: A renomeacao DEVE abranger apresentacao, documentacao vigente, projetos, referencias internas, testes, automacao e distribuicao, com inventario verificavel das excecoes de compatibilidade e do historico preservado.
- **FR-007**: A branch da renomeacao DEVE descender da origin/main consolidada e preservar todas as integracoes existentes; a feature nao DEVE ser mesclada automaticamente na main.
- **FR-008**: O repositorio remoto DEVE passar a se chamar nodra na organizacao atual, com destino confirmado altivy-it/nodra, sem transferencia para outra organizacao.
- **FR-009**: A futura CLI DEVE ser referenciada como nodra; implementar uma CLI nova esta fora do escopo.
- **FR-010**: A migracao DEVE preservar vaults, conteudo, backups e identidade existente, documentando transicoes e recuperacao para identificadores operacionais alterados.
- **FR-011**: A renomeacao NAO DEVE acessar, copiar, exibir ou sobrescrever secrets externos nem criar arquivos de secrets no repositorio.
- **FR-012**: Referencias ativas ao repositorio DEVEM apontar ao destino confirmado; falhas de permissao ou indisponibilidade DEVEM ser informadas sem declarar a renomeacao concluida.
- **FR-013**: Gates de qualidade DEVEM produzir evidencia em containers; jornadas HTTP DEVEM usar a borda nginx containerizada e interfaces alteradas DEVEM receber verificacao visual.
- **FR-014**: O plano DEVE identificar alteracoes incompatíveis e excecoes antes da implementacao, sem renomear silenciosamente contratos ou identificadores persistidos.

### Key Entities

- **Identidade de marca**: empresa Altivy; produto Nodra; marca Altivy Nodra; tagline e posicionamento literais; nome reservado da futura CLI nodra.
- **Repositorio**: historico existente preservado, nome final nodra e organizacao a confirmar.
- **Instalacao existente**: vaults, configuracao externa, identidade e dados que devem sobreviver a transicao.
- **Excecao de compatibilidade**: identificador antigo preservado, justificativa, impacto e procedimento de transicao quando aplicavel.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Todas as superficies publicas inventariadas apresentam Nodra ou Altivy Nodra, sem nomes antigos inexplicados na documentacao vigente.
- **SC-002**: Todos os commits de origin/main presentes na criacao da branch continuam ancestrais da entrega.
- **SC-003**: Nenhum arquivo de usuario e perdido ou sobrescrito nos cenarios de atualizacao e recuperacao validados.
- **SC-004**: O repositorio nodra confirmado preserva o historico e e acessivel pelo remoto configurado apos a renomeacao.
- **SC-005**: Todas as ocorrencias legadas restantes no inventario possuem justificativa historica ou de compatibilidade.
- **SC-006**: Cada gate obrigatorio possui resultado registrado; bloqueios preexistentes sao distinguidos de regressoes e nao sao apresentados como sucesso.

## Assumptions

- A decisao de marca foi fornecida pelo usuario nesta conversa em 2026-11-11.
- A base consolidada e a main no commit a9546a5; branches remotas ja integradas nao serao excluidas.
- Renomear nao autoriza transferencia de organizacao implicitamente, publicacao de imagens, deploy, alteracao de DNS ou criacao de novos servicos de IA.
- A renomeacao do diretorio local nao e precondicao para o nome do repositorio; qualquer mudanca deve preservar o workspace ativo e caminhos externos.
- O usuario aprovou esta especificacao e autorizou seguir ao planejamento em 2026-11-11, confirmando altivy-it/nodra pelo formulario desta conversa. A implementacao depende de revisao humana do plano, das tarefas e das decisoes de compatibilidade.
- A falha preexistente de instalacao Angular foi reproduzida antes desta feature e pode bloquear os gates frontend ate correcao em fluxo autorizado.
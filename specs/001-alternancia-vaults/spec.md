# Especificação da Feature: Alternância Segura entre Vaults

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, aguardando revisão humana

**Entrada**: Migração do escopo não concluído de `docs/plan/alternancia-vaults.md` para Spec Kit.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Listar e selecionar vaults autorizados (Prioridade: P1)

Como pessoa autenticada, quero ver e selecionar somente vaults para os quais tenho `vault:read`, sem revelar metadados de outros vaults.

**Valor desta prioridade**: O workspace precisa estabelecer o contexto autorizado antes de carregar notas.

**Teste independente**: Consultar o catálogo com usuários que tenham permissões distintas e confirmar que a resposta não revela vaults negados.

**Cenários de aceite**:

1. **Dado** que uma pessoa possui `vault:read` em dois vaults, **Quando** consulta o catálogo, **Então** recebe somente esses vaults, com paginação por cursor.
2. **Dado** que um candidato é negado ou sua decisão falha, **Quando** a API monta a página, **Então** nenhum metadado negado é exposto e uma falha necessária retorna `503` sem página parcial.

### Jornada do Usuário 2 - Restaurar o contexto autorizado (Prioridade: P1)

Como pessoa autenticada, quero que o workspace restaure o último vault autorizado e o módulo ativo após navegação, recarga e novo login.

**Valor desta prioridade**: O contexto do trabalho deve persistir sem restaurar acesso revogado.

**Teste independente**: Selecionar um vault, recarregar, autenticar novamente e depois revogar o acesso para confirmar o fallback seguro.

**Cenários de aceite**:

1. **Dado** que o último vault continua autorizado, **Quando** a sessão é retomada, **Então** o workspace abre `/workspace/:vaultId/:module` no vault e módulo salvos.
2. **Dado** que a autorização foi revogada, **Quando** o contexto é restaurado, **Então** o sistema seleciona outro vault autorizado ou apresenta estado sem acesso.

### Jornada do Usuário 3 - Isolar dados ao alternar (Prioridade: P1)

Como pessoa com acesso a mais de um vault, quero alternar sem misturar notas, documento selecionado, preferências ou eventos em tempo real.

**Valor desta prioridade**: Uma alternância não pode expor ou modificar conteúdo de outro vault.

**Teste independente**: Alternar entre vaults sintéticos e confirmar que cada resposta e preferência permanece no seu escopo.

**Cenários de aceite**:

1. **Dado** que há uma nota selecionada, **Quando** o vault muda, **Então** dados e realtime antigos não são exibidos no novo contexto e respostas obsoletas são descartadas.
2. **Dado** que o seletor está aberto, **Quando** a pessoa troca o vault pelo teclado, **Então** foco, anúncio acessível e módulo atual são preservados sem recarga completa.

## Casos de Borda

- Usuário sem vault autorizado, vault único, resposta vazia e página sem candidatos autorizados.
- Revogação durante a sessão, Keycloak indisponível e candidato negado entre páginas.
- Recarregamento com ID persistido inválido, resposta assíncrona obsoleta e reconexão SignalR.
- Preferências locais ausentes, inválidas ou isoladas por usuário e vault.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: A API DEVE listar somente vaults com decisão positiva de `vault:read` no Keycloak.
- **FR-002**: O catálogo DEVE usar paginação keyset por cursor opaco e versionado, com limite padrão de 20 e máximo de 100.
- **FR-003**: A avaliação UMA DEVE limitar a concorrência a quatro decisões simultâneas e falhar fechada, retornando `503` sem página parcial quando uma decisão necessária falhar.
- **FR-004**: A URL canônica DEVE incluir vault e módulo ativos em `/workspace/:vaultId/:module`.
- **FR-005**: O último vault DEVE ser persistido como ID técnico por usuário e restaurado somente se continuar autorizado; o fallback deve usar outro vault autorizado ou estado sem acesso.
- **FR-006**: Notas, documento selecionado, preferências e realtime DEVEM permanecer isolados por usuário e vault.
- **FR-007**: A troca de vault DEVE preservar o módulo atual, tratar estados de carregamento/erro/vazio e não recarregar a aplicação inteira.
- **FR-008**: O seletor DEVE ser utilizável por teclado, ter foco visível, anúncio acessível e layout responsivo.
- **FR-009**: Identificadores, nomes e existência de vaults negados NÃO DEVEM aparecer em payloads, interface ou logs.

### Entidades Principais (quando houver dados)

- **VaultRecord**: projeção reconstruível do vault; não contém membership nem decisão de autorização.
- **Contexto de workspace**: módulo e ID do último vault, separados por usuário; não armazena conteúdo ou credenciais.

## Critérios de Sucesso (obrigatório)

### Resultados Mensuráveis

- **SC-001**: Respostas do catálogo não contêm IDs ou nomes de vaults negados em todos os cenários de autorização exercitados.
- **SC-002**: Testes E2E demonstram seleção, recarga, revogação e fallback seguro com usuário de dois vaults autorizados.
- **SC-003**: Testes confirmam que preferências, notas e eventos de dois vaults não se misturam.
- **SC-004**: O fluxo principal funciona por teclado e em viewports desktop e mobile, sem recarga completa.

## Premissas

- Keycloak continua sendo a autoridade de identidade e autorização; a API é PEP e falha fechada.
- PostgreSQL contém somente a projeção de vaults; não armazena memberships.
- `PreferencesService` mantém a chave por `subject` e `vaultId`.
- Não há nova dependência nem migration prevista para o catálogo desta feature.

## Proveniência da Migração

Escopo migrado: etapas P-01 a P-07 de `docs/plan/alternancia-vaults.md`. P-01 a P-06 estão registradas como concluídas no documento legado; P-07 permanece em andamento, com E2E multi-vault, revogação real e verificação visual ainda pendentes. A migração não converte esse histórico em checkboxes concluídos. As etapas P-08 a P-16 foram separadas para `specs/002-provisionamento-vaults/`.

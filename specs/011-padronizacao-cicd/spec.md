# Feature Specification: Padronizacao CI/CD self-hosted

**Feature Branch**: `011-padronizacao-cicd`
**Created**: 2026-11-11
**Status**: Draft
**Input**: Alinhar workflows, distribuicao self-hosted e ambientes GitHub do Nodra ao padrao funcional do Dokpod; orientar o cadastro seguro de variaveis/secrets e remover branches remotas ja integradas.

**Revisao humana**: Escopo aprovado pelo usuario nesta conversa em 2026-11-11
para preparar plano e tarefas. Por instrucao posterior do usuario, as aprovacoes
cabem aos administradores da Altivy. Para os quatro ambientes protegidos, usar
exatamente os revisores do tipo User configurados no Dokpod, apos validar que
sao proprietarios atuais da organizacao e possuem acesso de leitura ao Nodra.
Namespace GHCR escolhido explicitamente: ghcr.io/altivy-it/nodra-*.

## Clarifications

### Session 2026-11-18

- Q: Voce autoriza que os quatro ambientes protegidos do Nodra usem exatamente os mesmos revisores individuais do Dokpod, em vez de uma equipe dos proprietarios da organizacao? → A: Usar os mesmos revisores de usuario configurados no Dokpod, apos validar que sao proprietarios da Altivy e tem acesso ao Nodra; nao usar papel coletivo ou substituto.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validar mudancas com o mesmo processo (Priority: P1)

Como mantenedor, quero que PRs, merge groups e alteracoes na branch padrao
tenham gates previsiveis e um unico resultado agregador, sem credenciais de
publicacao ou producao disponiveis ao codigo nao confiavel.

**Why this priority**: CI confiavel e pre-requisito para distribuir e operar o produto.
**Independent Test**: Um PR pode executar todos os gates obrigatorios sem secrets; uma falha impede o resultado agregado de sucesso.

**Acceptance Scenarios**:

1. **Given** um PR ou merge group, **When** o CI e executado, **Then** todos os gates obrigatorios alimentam Result e nenhuma publicacao ou implantacao ocorre.
2. **Given** um PR sem secrets, inclusive Dependabot, **When** ele executa, **Then** usa apenas dados sinteticos e containers de teste.
3. **Given** uma nova execucao de PR, **When** a anterior ainda esta ativa, **Then** a politica de concorrencia nao cria cancelamentos que invalidem artificialmente o check obrigatorio.

### User Story 2 - Distribuir releases verificaveis (Priority: P1)

Como mantenedor, quero preparar, revisar e distribuir uma versao com aprovacoes
humanas e rastreabilidade entre fonte, imagens e manifesto, como no padrao do Dokpod.

**Why this priority**: Distribuicao self-hosted exige artefatos imutaveis e recuperacao documentada.
**Independent Test**: Uma release nao pode divulgar artefatos sem os gates e aprovacoes previstos; uma repeticao nao sobrescreve artefatos publicados.

**Acceptance Scenarios**:

1. **Given** uma mudanca elegivel integrada, **When** a automacao prepara uma versao, **Then** cria ou atualiza um Release PR e preserva a revisao humana.
2. **Given** uma candidata, **When** e promovida, **Then** a validacao em quality e a origem exata ficam registradas antes da divulgacao final prevista pela politica.
3. **Given** artefatos ja publicados, **When** uma operacao e repetida, **Then** ela e idempotente, nao sobrescreve digests e nao recompila silenciosamente o que foi aprovado.

### User Story 3 - Usar os mesmos ambientes protegidos (Priority: P1)

Como operador, quero os mesmos ambientes do Dokpod no Nodra, com papeis claros,
restricoes de origem e aprovacoes, sem compartilhar credenciais entre produtos.

**Why this priority**: Nomes iguais sem protecoes equivalentes nao constituem padronizacao operacional.
**Independent Test**: Comparar inventario e politicas dos ambientes; tentar origem nao permitida ou aprovacao ausente deve falhar fechado.

**Acceptance Scenarios**:

1. **Given** os repositorios, **When** a configuracao e conciliada, **Then** Nodra possui ci, copilot, quality, release, distribution e production.
2. **Given** uma operacao protegida, **When** falta aprovacao humana ou sua origem nao e permitida, **Then** ela nao publica nem altera o ambiente.
3. **Given** configuracao ausente, **When** o operador consulta o guia, **Then** identifica nome, escopo, finalidade e modo seguro de cadastrar cada variavel/secret, sem divulgar valores no chat ou no repositorio.

### User Story 4 - Implantar com manifesto validado (Priority: P2)

Como operador, quero selecionar uma release publicada e um ambiente autorizado,
aplicar a mesma release de forma idempotente e verificar a versao em execucao.

**Why this priority**: Impede deploy arbitrario ou divergente dos artefatos aprovados.
**Independent Test**: Uma origem sem release/manifesto valido e recusada antes de acessar o host; a mesma release aplicada duas vezes preserva o estado esperado.

**Acceptance Scenarios**:

1. **Given** uma tag de release valida, **When** um deploy e explicitamente autorizado, **Then** imagens e digests vem do manifesto validado, nao de entradas livres.
2. **Given** o host configurado, **When** a aplicacao termina, **Then** o smoke verifica a versao e a saude pela borda nginx.
3. **Given** uma mudanca apenas de CI, **When** e integrada, **Then** nao executa deploy automaticamente sem politica e autorizacao explicitas.

### Edge Cases

- PRs de forks e Dependabot nao recebem secrets.
- Configuracao obrigatoria ausente, token de App insuficiente ou plano GitHub sem uma protecao exigida devem falhar fechado.
- Imagens e workflows de referencia podem ter licencas incompatíveis com o Nodra.
- O PR 31 permanece aberto; a feature 011 parte do head 862ef1d e depende da integracao ou reconciliacao explicita dessa base antes de merge em main.
- A main avancou para fb29ed7 com um bump isolado do router; nao sobrescrever silenciosamente as correcoes de peers validadas na feature 010.
- Uma branch remota pode receber novos commits enquanto a limpeza e executada; a exclusao exige confirmacao de ancestralidade e protecao pelo SHA.
- Baselines de upgrade e plataforma de identidade real continuam necessarios para homologacao operacional; padronizar workflow nao equivale a homologar OIDC.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O alinhamento DEVE usar o comportamento do Dokpod como referencia e preservar as regras de seguranca, arquitetura e licencas do Nodra.
- **FR-002**: O CI DEVE atender PRs, merge groups, main e execucao manual, agregando gates obrigatorios em Result.
- **FR-003**: O CI de PR DEVE operar sem secrets e NAO DEVE publicar imagens, distribuir releases ou executar deploy.
- **FR-004**: A concorrencia DEVE preservar o resultado exigido de PRs/merge groups e impedir corrida de publicacao ou deploy no mesmo destino.
- **FR-005**: Release, publicacao/distribuicao e deploy DEVEM ter responsabilidades separadas, com aprovacoes humanas nos pontos equivalentes ao padrao de referencia.
- **FR-006**: O Nodra DEVE possuir os ambientes ci, copilot, quality, release, distribution e production, com protecoes e restricoes de origem verificadas contra a referencia.
- **FR-007**: Os ambientes quality, release, distribution e production DEVEM exigir aprovacao humana; mudanca de revisor ou politica exige decisao explicita.
- **FR-008**: A distribuicao DEVE preservar fonte/SHA, versao, digests, SBOM, proveniencia e imutabilidade; nenhuma versao publicada pode ser sobrescrita.
- **FR-009**: O deploy DEVE aceitar somente uma release publicada e manifesto validado, com origem e ambiente permitidos, execucao idempotente e smoke por nginx.
- **FR-010**: O guia DEVE listar cada variavel e secret por nome, escopo, finalidade, necessidade e procedimento seguro, sem copiar valores do Dokpod ou pedir secrets pelo chat.
- **FR-011**: Componentes novos DEVEM ter licenca permissiva da versao exata previamente verificada. Codigo AGPL do Dokpod NAO DEVE ser copiado nem incorporado como dependencia.
- **FR-012**: O alinhamento DEVE preservar contratos de configuracao externa, identidade, vaults, banco e instalacoes existentes; nenhuma migracao operacional implicita e permitida.
- **FR-013**: Branches da origin so PODEM ser excluidas apos comprovacao de integracao em origin/main e protecao contra mudanca do SHA; main e branches de PRs pendentes DEVEM ser preservadas.
- **FR-014**: Publicacao, deploy, alteracao de secrets ou criacao de credenciais NAO DEVEM ocorrer como efeito colateral da implementacao ou validacao.
- **FR-015**: Futuras imagens do produto DEVEM usar ghcr.io/altivy-it/nodra-*; consumidores ativos DEVEM ser alinhados sem mover, sobrescrever ou publicar imagens automaticamente.
- **FR-016**: A proposta DEVE atribuir responsabilidade e revisao aos administradores da Altivy. Os quatro ambientes protegidos DEVEM usar exatamente os revisores GitHub do tipo User configurados no Dokpod; antes de usa-los, confirmar que cada revisor e proprietario atual da organizacao e tem acesso de leitura ao Nodra. Nao usar RepositoryRole/actor_id 5 como revisor de Environment, outro usuario como fallback ou identidade inventada. Contribuicoes desta manutencao e da automacao configurada DEVEM usar lunadev00 <luna@altivy.it>, sem reatribuir autoria de terceiros ou reescrever historico implicitamente.

### Key Entities

- **Ambiente**: nome, papel, revisores, restricoes de origem e configuracoes por escopo.
- **Release**: versao, tag, SHA de origem, aprovacao e estado de candidata/publicada.
- **Manifesto**: referencias de artefatos, digests e evidencias de proveniencia.
- **Configuracao GitHub**: nome, tipo variavel/secret, escopo, finalidade e presenca, sem valor sensivel.
- **Branch elegivel para limpeza**: nome, SHA confirmado, ancestralidade e ausencia de trabalho pendente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Todos os gates obrigatorios alimentam Result; nenhum PR sem secrets precisa de credencial de producao ou publicacao para validar.
- **SC-002**: Os seis ambientes esperados existem e todas as diferencas de politica foram corrigidas ou explicitamente bloqueadas por limitacao/permissao documentada.
- **SC-003**: Toda release e deploy qualificados sao rastreaveis a fonte, versao, aprovacoes e artefatos imutaveis.
- **SC-004**: O operador consegue identificar no guia todas as configuracoes faltantes sem enviar valores sensiveis ao assistente.
- **SC-005**: Nenhuma branch nao integrada, secret, dado persistido ou configuracao instalada e perdido ou movido pela padronizacao.
- **SC-006**: Cada gate local/remoto e cada limitacao possui evidencia registrada; nao declarar publicacao ou homologacao que nao ocorreu.

## Assumptions

- Referencia consultada: altivy-it/dokpod, commit 01fe5b0c526742edb420541cdd9a0b4e9fa2e74e, em 2026-11-11.
- A API GitHub identifica o Dokpod como AGPL-3.0; somente seus conceitos e politicas servem de referencia, salvo componentes com origem e licenca permissiva verificadas separadamente.
- O catalogo canonico Altivy.Groundwork 0.11.3 foi identificado em altivy-it/altivy-groundwork, tag v0.11.3, commit c848afb4fa0b43cb9227abd2c347ab4ecccd6296. O LICENSE dessa tag foi consultado e confirma MIT; a adocao planejada usa essa origem, nao os arquivos do Dokpod AGPL.
- Revisao humana da especificacao, plano e tarefas antecede alteracoes de workflows e protecoes operacionais.
- Alterar definicoes de ambientes e aprovacoes nao autoriza executar publicacao, deploy ou acessar infraestrutura externa.
- O papel administrativo observado nos rulesets do Dokpod e RepositoryRole com actor_id 5 e nao pode ser usado como required reviewer de Environment. Os quatro ambientes protegidos do Nodra devem usar os revisores User ja configurados nos ambientes equivalentes do Dokpod, apos validar que cada um e owner atual da organizacao e possui acesso de leitura ao Nodra. Se a validacao falhar, bloquear a configuracao sem substituto.
- A limpeza autorizada excluiu somente 009-migracao-prompts-skills-revisao e chore/adotar-spec-kit; permaneceram main e 010-renomeacao-nodra.
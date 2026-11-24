# Especificação da Funcionalidade: Vault de exemplo somente leitura

**Branch da Funcionalidade**: `013-vault-exemplo`

**Criada em**: 2026-11-23

**Status**: Rascunho

**Entrada**: Descrição fornecida: "Crie um vault de exemplo para o projeto, mova o documento markdown para lá, não deixe que o usuário exclua esse vault, na verdade esse vault é apenas de exemplo, não pode ser editado, o documento de exemplo deve permitir que o usuário faça o que quiser, mas não grave para evitar perder os dados originais."

> Redija esta especificação em português brasileiro (pt-BR). Preserve identificadores, nomes de API, comandos e termos técnicos necessários.

## Cenários de Usuário e Testes *(obrigatório)*

### História de Usuário 1 - Explorar o vault de exemplo (Prioridade: P1)

Todo usuário autenticado encontra, na lista de vaults, o "Vault de exemplo". Nele está o documento modelo, que demonstra todos os recursos Markdown suportados pelo Nodra: títulos, cores, tabelas, código, fórmulas, diagramas e HTML.

**Motivo da prioridade**: O documento modelo é a referência de aprendizado do produto. Ele precisa estar disponível para todos, sem depender de vaults pessoais.

**Teste independente**: Entre com um usuário sem vaults próprios, selecione "Vault de exemplo" e abra o documento modelo. A prévia deve renderizar completamente.

**Cenários de aceitação**:

1. **Dado** um usuário autenticado, **Quando** ele abre o seletor de vaults, **Então** o "Vault de exemplo" aparece com a indicação de que é somente leitura.
2. **Dado** o vault de exemplo selecionado, **Quando** o usuário abre `documento-modelo.md`, **Então** o conteúdo original é exibido no editor e na prévia.
3. **Dado** um vault de laboratório que antes continha o documento modelo, **Quando** o ambiente é iniciado, **Então** o documento modelo não é mais semeado nesse vault, apenas no vault de exemplo.

---

### História de Usuário 2 - Experimentar o documento sem gravar (Prioridade: P1)

O usuário pode editar o documento de exemplo à vontade e ver o resultado na prévia em tempo real. Nenhuma alteração é gravada; o original fica sempre preservado e pode ser restaurado a qualquer momento.

**Motivo da prioridade**: Experimentar sintaxe Markdown sem medo de estragar a referência é o principal valor do vault de exemplo.

**Teste independente**: Altere o texto do documento, confirme que a prévia muda e que nenhuma requisição de gravação é feita. Recarregue a nota e confirme que o conteúdo original voltou.

**Cenários de aceitação**:

1. **Dado** o documento de exemplo aberto, **Quando** o usuário digita no editor, **Então** a prévia reflete a edição e o sistema informa que as alterações não serão salvas.
2. **Dado** um documento de exemplo com edições locais, **Quando** o usuário aciona "Restaurar original", **Então** o conteúdo original é recarregado e as edições locais são descartadas.
3. **Dado** um documento de exemplo com edições locais, **Quando** o usuário troca de nota, de vault ou recarrega a página, **Então** as edições são descartadas e o arquivo do vault permanece idêntico ao original.

---

### História de Usuário 3 - Proteger o vault de exemplo (Prioridade: P2)

O vault de exemplo não pode ser excluído nem alterado por nenhum usuário, mesmo que alguém tente a operação diretamente pela API.

**Motivo da prioridade**: Garante que a referência permaneça íntegra para todos os usuários e instalações.

**Teste independente**: Envie à API pedidos de criação, atualização, movimentação, exclusão de nota ou pasta, restauração e esvaziamento de lixeira no vault de exemplo. Todos devem ser recusados com um erro explícito de vault somente leitura.

**Cenários de aceitação**:

1. **Dado** o vault de exemplo selecionado, **Quando** o usuário visualiza o explorador, **Então** as ações de criar, renomear, mover, excluir e a lixeira ficam indisponíveis.
2. **Dado** qualquer usuário, inclusive um com permissões amplas, **Quando** ele solicita uma operação de escrita ou exclusão no vault de exemplo, **Então** a operação é recusada e o conteúdo permanece inalterado.
3. **Dado** que o arquivo do documento modelo foi alterado ou removido fora do Nodra, **Quando** o serviço reinicia, **Então** o conteúdo original é restaurado.

### Casos de Borda

- O usuário edita o documento de exemplo e fecha a aba: nada é gravado e não há perda de dados de outros vaults.
- O serviço de autorização fica indisponível: a listagem de vaults falha fechada, como nos demais vaults.
- A instalação não tem o vault de exemplo autorizado no provedor de identidade: o vault não aparece, e a aplicação segue funcionando com os demais vaults.
- Um usuário tenta criar uma nota no vault de exemplo por atalho de teclado: a ação é bloqueada.
- Várias instâncias do serviço iniciam ao mesmo tempo: a materialização do documento é idempotente e não corrompe o arquivo.

## Requisitos *(obrigatório)*

### Requisitos Funcionais

- **FR-001**: O sistema DEVE disponibilizar um vault de exemplo, de identificador estável `nodra-exemplo` e nome "Vault de exemplo", em toda instalação.
- **FR-002**: O vault de exemplo DEVE conter o documento modelo Markdown como conteúdo canônico, versionado com a aplicação.
- **FR-003**: O sistema DEVE restaurar o conteúdo original do documento modelo na inicialização sempre que o arquivo estiver ausente ou divergente.
- **FR-004**: A leitura do vault de exemplo DEVE continuar sujeita à decisão de autorização do provedor de identidade. A instalação DEVE poder conceder leitura a todos os usuários do realm.
- **FR-005**: O sistema DEVE recusar no servidor, independentemente das permissões concedidas, toda operação de escrita, movimentação, exclusão, restauração ou esvaziamento de lixeira no vault de exemplo. A recusa usa um código de problema identificável (`vault-read-only`).
- **FR-006**: A listagem de vaults DEVE indicar quais vaults são somente leitura.
- **FR-007**: Em vault somente leitura, a interface DEVE ocultar ou desabilitar as ações de criar, renomear, mover, excluir, arrastar e a lixeira.
- **FR-008**: Em vault somente leitura, o editor DEVE permitir edição livre com prévia imediata, sem enviar nenhuma gravação ao servidor.
- **FR-009**: O editor DEVE exibir de forma persistente o aviso de que as alterações no vault de exemplo não são salvas, e oferecer a ação "Restaurar original".
- **FR-010**: O documento modelo DEVE deixar de ser semeado em vaults de usuário ou de laboratório.
- **FR-011**: Nenhum usuário DEVE poder excluir o vault de exemplo.

### Entidades Principais

- **Vault de exemplo**: Vault de sistema, somente leitura, com identificador estável e conteúdo fornecido pela própria aplicação.
- **Documento modelo**: Nota Markdown canônica que demonstra os recursos de renderização do Nodra.
- **Edição local**: Estado em memória do editor, descartado ao sair da nota e nunca persistido.

## Critérios de Sucesso *(obrigatório)*

### Resultados Mensuráveis

- **SC-001**: 100% dos usuários autenticados de uma instalação configurada veem o vault de exemplo na lista de vaults.
- **SC-002**: 0 gravações chegam ao armazenamento do vault de exemplo durante uma sessão de edição, verificado pelo conteúdo do arquivo antes e depois.
- **SC-003**: 100% das tentativas diretas de escrita ou exclusão no vault de exemplo são recusadas com o código `vault-read-only`.
- **SC-004**: O usuário restaura o conteúdo original com uma única ação, em menos de 2 segundos.
- **SC-005**: Depois de alterado fora da aplicação, o documento modelo volta ao original na próxima inicialização do serviço.

## Premissas

- O identificador `nodra-exemplo` não colide com vaults existentes; os vaults atuais usam outros prefixos.
- A concessão de leitura a todos os usuários usa uma policy baseada no papel padrão do realm, provisionada por script administrativo. A aplicação não cria políticas de autorização em runtime.
- Não existe funcionalidade de exclusão de vault no produto atual. A proteção cobre a API, para impedir que uma funcionalidade futura ou uma chamada direta remova ou altere o vault.
- Edições locais não são guardadas em armazenamento do navegador. Fechar ou trocar de nota descarta as edições, sem confirmação adicional.
- Links internos do documento modelo apontam para âncoras do próprio documento, já que o vault de exemplo contém só esse arquivo.

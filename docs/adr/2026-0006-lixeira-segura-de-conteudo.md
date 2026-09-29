# ADR 2026-0006: Lixeira segura de conteúdo do vault

**Status:** accepted  
**Data:** 2026-09-28  
**Responsáveis:** produto e equipe Notes/Vaults  
**Origem:** decisão humana registrada em 2026-09-28, consolidada com assistência de IA  
**Revisor humano:** Lincoln Zocateli; aprovação e decisões registradas em 2026-09-28  
**Relacionado:** P-04 em [workspace frontend](../plan/workspace-frontend-inspirado-no-obsidian.md), [plano mestre](../plano-mestre.md) e [política de retenção de resíduos](../politica-retencao-recuperacao-residuos-vault.md)

## Contexto

O explorador permite excluir somente pastas vazias. A P-04 requer recuperar pastas não vazias, enquanto o plano mestre define os arquivos Markdown como fonte canônica e inclui lixeira e recuperação no escopo do produto. Excluir recursivamente ou mover uma pasta para um diretório que continue visível ao catálogo pode perder conteúdo, reindexar itens removidos ou expor dados indevidamente.

A política de retenção de resíduos trata falhas operacionais de vault, não a retenção de exclusões solicitadas pelo usuário. Não é seguro inferir que os 30 dias daquela política também se aplicam à lixeira do produto.

## Forças de decisão

- preservar arquivos e diretórios sem sobrescrita;
- manter o filesystem como fonte canônica e a projeção reconstruível;
- confinar a operação ao vault e rejeitar links simbólicos/reparse points;
- permitir restauração segura quando o caminho original foi reutilizado;
- impedir que itens na lixeira apareçam na listagem, busca, links ou indexação ativa;
- manter autorização no Keycloak e auditar ações sem registrar conteúdo;
- definir retenção e custo de armazenamento de forma explícita.

## Opções consideradas

### Opção A: movimentação confinada em área reservada do vault

Mover atomicamente o item para uma área interna reservada no mesmo filesystem e registrar um identificador de operação, caminho original e instante da exclusão. A listagem e a reconciliação excluem essa área do catálogo ativo. A restauração recusa colisão com qualquer item existente e nunca sobrescreve. A movimentação de pastas valida previamente toda a subárvore e bloqueia reparse points.

**Vantagens:** preserva conteúdo, pode ser atômica no mesmo filesystem e não exige novo datastore.  
**Trade-offs:** exige contrato/API de lixeira, reconciliação idempotente, auditoria, tratamento de falha parcial e decisão de retenção.

### Opção B: exclusão recursiva com confirmação

Remover permanentemente a pasta e o conteúdo depois de confirmação na interface.

**Vantagens:** implementação inicial menor.  
**Trade-offs:** não atende recuperação, torna erro humano irreversível e diverge do escopo do produto.

### Opção C: copiar conteúdo para PostgreSQL ou object storage

Manter uma cópia independente fora da árvore canônica do vault.

**Vantagens:** pode permitir retenção central e acesso administrativo uniforme.  
**Trade-offs:** duplica conteúdo, cria outra fonte de dados e introduz datastore/operação sem evidência ou decisão arquitetural aprovada.

## Decisão

Adotar a Opção A. A área `.altivy-trash` é reservada pelo servidor, não
configurável e não pode ser criada, listada ou acessada como pasta comum do
vault. Cada exclusão cria um diretório de operação com ID opaco gerado pelo
servidor, payload movido e manifesto. A listagem ativa, busca, links,
watchers e indexação ignoram essa área. Toda entrada e saída valida novamente
confinamento e reparse points; destinos ocupados nunca são sobrescritos.

O manifesto por operação é a fonte durável dos metadados da lixeira. Um
inventário é reconstruído por varredura da área reservada, sem tabela paralela
no PostgreSQL. O manifesto contém somente os campos operacionais necessários:
ID da operação, tipo, caminho original relativo, `sub` do autor e instante UTC
da exclusão e o prazo de retenção capturado nessa operação, quando configurado.
O parser falha fechado para manifestos ausentes, duplicados, inválidos ou com
caminhos fora do formato permitido. O `sub` só é usado para filtrar a lixeira
do usuário e registrar auditoria; nunca concede acesso ao vault. A operação
não depende de título ou conteúdo do item.

autorização locais. Um valor de retenção válido é capturado no manifesto no
O prazo de retenção é um parâmetro por usuário. Como a configuração atual do
realm não oferece um atributo de perfil gerenciável pelo usuário e consumível
pela API, foi adotado CRUD mínimo para esse parâmetro não autorizativo,
identificado pelo `sub`, sem introduzir contas, memberships ou políticas de
autorização locais. O handler consulta o parâmetro do sujeito no momento da
exclusão e captura o valor no manifesto; mudanças posteriores aplicam-se a
novas exclusões. Sem valor configurado, a remoção automática fica desabilitada
e os itens permanecem retidos. O intervalo permitido é de 1 a 3650 dias; não
se herda implicitamente o prazo de resíduos operacionais.

O usuário pode esvaziar a própria lixeira. A ação requer autorização vigente
do Keycloak para o vault, confirmação explícita na interface, reautenticação
recente pelo fluxo OIDC do Keycloak e auditoria append-only. O BFF já expõe
`/bff/reauthenticate`, que inicia o desafio com `prompt=login` e `max_age=0`.
O BFF vincula o desafio ao `sub` atual, exige que o retorno OIDC contenha o
mesmo `sub` e `auth_time` válido, e persiste a evidência somente no ticket
server-side. O proxy descarta qualquer `X-Altivy-Reauth-At` recebido do browser
e só injeta um valor derivado do ticket quando ainda recente; a API valida
novamente o limite temporal. A retomada após o redirect usa apenas um marcador
efêmero de intenção por aba/vault e consulta `/bff/reauthenticate/status` antes
de repetir a ação já confirmada. A aplicação nunca recebe, armazena ou
transmite a senha. Se a evidência não puder ser validada, a operação falha
fechada. A auditoria registra sujeito, vault, operação, instante, resultado e
instante de reautenticação, sem conteúdo ou senha. A intenção auditável é
persistida antes da exclusão; o resultado é registrado após a operação,
inclusive em falha parcial. Se a persistência do resultado falhar depois do
efeito no filesystem, o endpoint falha sem inventar um outcome; a intenção
permanece como evidência reconciliável.

Restaurar sempre solicita o caminho de destino ao usuário, com o caminho
original como sugestão editável. Se o original estiver ocupado ou não puder ser
restaurado, a ação apresenta uma mensagem também disponível ao passar o mouse
(e por descrição acessível) e permite informar outro destino. A API valida o
novo caminho e recusa qualquer colisão; nunca escolhe ou sobrescreve um destino
silenciosamente.

## Consequências

### Positivas

- exclusão não equivale a perda imediata de dados;
- restauração tem semântica explícita e não sobrescreve conteúdo;
- a projeção continua derivada do filesystem ativo.

### Negativas e trade-offs

- a lixeira aumenta o uso de disco até a política de retenção ser aplicada;
- renomear/mover e excluir/restaurar precisam lidar com falhas entre filesystem e projeção;
- versões anteriores do serviço podem não reconhecer a área reservada, exigindo compatibilidade e rollout coordenados.

## Segurança e privacidade

- exigir autorização antes de revelar existência, nome ou metadados de itens na lixeira;
- validar paths na entrada e no momento do uso, confirmar confinamento e bloquear reparse points;
- nunca aceitar o identificador da lixeira como path físico fornecido pelo cliente;
- validar o manifesto como entrada não confiável e não seguir conteúdo por reparse point;
- exigir reautenticação OIDC recente para esvaziar a lixeira, sem manipular senha na aplicação;
- filtrar itens pela identidade do autor após autorização do vault, sem tratar `sub` como permissão;
- persistir a intenção auditável antes de remover conteúdo e registrar o resultado depois, inclusive se parte da exclusão falhar;
- não servir conteúdo excluído por listagem, busca, arquivo estático ou link;
- não registrar conteúdo Markdown, nomes ou paths sensíveis em logs;
- auditar sujeito técnico, vault, operação, resultado e instante conforme as regras vigentes.

## Dados, compatibilidade e migração

A API deve introduzir operações versionadas para listar, excluir e restaurar itens, com `application/problem+json`, paginação por cursor, idempotência e autorização documentadas em OpenAPI. O catálogo ativo omite a área reservada. Jobs de indexação removem a projeção antiga após exclusão e a reconstroem após restauração.

A persistência do prazo tenta primeiro o User Profile do Keycloak, com claim confiável disponível à API e UX de autoatendimento configurada. Na configuração atual, o realm não declara um atributo de perfil de retenção nem existe integração da API para gerenciá-lo; por isso o fallback CRUD próprio foi adotado para este parâmetro não autorizativo. Ele é isolado por `sub`, aplica limites no backend e não aceita do browser decisões de expiração. A tabela `user_trash_retention_settings` possui uma linha por sujeito e migration explícita; purge automático permanece desabilitado até step-up e auditoria.

A implantação é expand-contract e mantém o comportamento atual de `DELETE` de pasta vazia até todos os consumidores suportarem a nova capacidade. Eventos de mover, restaurar e esvaziar já são gravados em `trash_audit_events`; o esvaziamento é exposto somente pela rota protegida, com confirmação na interface e reautenticação recente.

## Observabilidade e operação

Medir sucesso, conflito, falha de filesystem, atraso da reconciliação, idade e volume total da lixeira com cardinalidade controlada. Definir alertas para falhas repetidas e documentar backup/restore antes de habilitar qualquer remoção física.

## Validação

- integração com filesystem real para nota, pasta vazia e árvore não vazia;
- recusa de traversal, colisão, reparse point, path reservado e restauração sobre destino existente;
- falha e reinício entre movimentação do filesystem e atualização da projeção;
- idempotência, autorização negada e ausência de vazamento pela listagem/busca;
- jornada Playwright de excluir, listar, restaurar e resolver colisão;
- revisão de segurança e benchmark da operação em subárvore grande.

## Rollout e rollback

Esta ADR está aceita. Não habilitar remoção automática até implementar e validar o parâmetro de retenção, seus limites e o comportamento quando não configurado. No rollout, criar a área reservada sem mover conteúdo existente e habilitar a API somente após confirmar exclusão da área na listagem e reconciliação. Rollback desabilita as rotas novas, preserva integralmente os itens já movidos e não os remove nem os reintegra automaticamente ao catálogo.

## Referências

- [Plano da P-04](../plan/workspace-frontend-inspirado-no-obsidian.md#p-04-evoluir-o-explorador-de-pastas-e-arquivos)
- [Plano mestre](../plano-mestre.md)
- [Política de retenção e recuperação de resíduos](../politica-retencao-recuperacao-residuos-vault.md)

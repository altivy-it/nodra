# Especificação da Funcionalidade: Laboratório local com Podman

**Branch da Funcionalidade**: `012-laboratorio-local-podman`

**Criada em**: 2026-11-23

**Status**: Rascunho; aguardando revisão humana do desenho e das tarefas

**Entrada**: "OK corrija o que for necessario para que a stack funcione, crie ou ajuste o dockerfile (podmanfile) para que eu possa subir a stack completa na minha maquina local para poder testar funcionalidades e a stack antes de terminar o processo de ci/cd".

## Cenários de Usuário e Testes *(obrigatório)*

### História de Usuário 1 - Preparar as imagens locais (Prioridade: P1)

Como desenvolvedor, quero construir os componentes do Nodra na minha máquina,
sem depender de uma release publicada ou da conclusão do CI/CD.

**Motivo da prioridade**: O teste funcional depende de imagens executáveis.

**Teste independente**: Construir os cinco componentes da aplicação a partir
do checkout e verificar que o resultado pode ser usado no laboratório local.

**Cenários de aceitação**:

1. **Dado** um checkout e um engine local disponíveis, **Quando** preparo as
   imagens, **Então** os cinco componentes são construídos sem secrets no
   contexto de build.
2. **Dado** uma falha de build, **Quando** a preparação termina, **Então** ela
   informa o componente que falhou e não declara a stack pronta.

### História de Usuário 2 - Subir e testar a aplicação completa (Prioridade: P1)

Como desenvolvedor, quero acessar o Nodra local, autenticar e criar, editar e
reabrir notas, usando os serviços reais necessários ao produto.

**Motivo da prioridade**: Containers iniciados não comprovam uma aplicação útil.

**Teste independente**: Autenticar e executar o ciclo de uma nota pela borda
HTTPS, com autorização real e persistência após recriação dos consumidores.

**Cenários de aceitação**:

1. **Dado** identidade, banco e certificados preparados pelo operador,
   **Quando** inicio a aplicação, **Então** recebo uma URL local única e só
   há indicação de prontidão após os serviços necessários ficarem saudáveis.
2. **Dado** um usuário autorizado, **Quando** salvo uma nota e recrio os
   consumidores, **Então** o conteúdo permanece disponível.
3. **Dado** um pré-requisito ausente, **Quando** executo o preflight,
   **Então** ele falha antes de migrations ou alterações dos serviços.

### Casos de Borda

- Provider de composição incompatível, ausência de redes ou gateway.
- Certificado inválido ou issuer inacessível pelos containers.
- Diretórios externos com permissões incompatíveis com execução sem root.
- Conflito com containers, portas, imagens ou dados de outro projeto.
- Credenciais, realm, roles ou grants de teste ainda não preparados.
- Falha de build, migrations, readiness ou autenticação.

## Requisitos *(obrigatório)*

### Requisitos Funcionais

- **FR-001**: O laboratório DEVE permitir build local sem publicar imagens,
  iniciar workflows de release ou alterar políticas do GitHub.
- **FR-002**: O fluxo DEVE preservar a infraestrutura de identidade separada,
  conforme a ADR aceita; não duplicar gateway, PostgreSQL ou Keycloak no produto.
- **FR-003**: A operação DEVE suportar o engine e provider locais explicitamente
  documentados, sem pressupor que comandos de composição diferentes são iguais.
- **FR-004**: O preflight DEVE recusar ausência de configuração, redes,
  certificados e serviços externos antes de qualquer mudança persistente.
- **FR-005**: Secrets DEVEM permanecer externos; ajuda, erros e validações não
  podem exibir seus valores nem copiar arquivos de credenciais.
- **FR-006**: Somente a borda nginx DEVE ser usada pelo navegador e pelos testes
  HTTP; não publicar API ou BFF diretamente.
- **FR-007**: O fluxo DEVE preservar vault, banco e chaves de sessão; não remover
  volumes, reinstalar identidade ou parar serviços de outros projetos.
- **FR-008**: A prontidão DEVE exigir saúde dos consumidores e um smoke pela
  borda; a qualificação funcional DEVE incluir login e persistência real.
- **FR-009**: O guia DEVE listar os pré-requisitos, preparação, subida, consulta
  de estado e parada não destrutiva, distinguindo build de stack homologada.

## Critérios de Sucesso *(obrigatório)*

### Resultados Mensuráveis

- **SC-001**: Os cinco componentes são construídos localmente com sucesso.
- **SC-002**: Todos os cenários de pré-requisitos ausentes testados são recusados
  sem iniciar serviços ou migrations.
- **SC-003**: Login, criação, edição e reabertura de uma nota passam pela borda
  local, e o conteúdo permanece após uma recriação dos consumidores.
- **SC-004**: Nenhum serviço, dado ou configuração de outro projeto é alterado
  durante preparação, teste e parada do laboratório.

## Premissas

- A máquina alvo não foi confirmada; Linux com Podman rootless é a hipótese
  inicial, não uma alegação de suporte já validado para Windows.
- A ADR [2026-0008](../../docs/adr/2026-0008-deploy-self-hosted-compose.md)
  permanece aceita; a stack completa é composta pela plataforma
  `altivy-identity` e pelo Nodra, com ciclos de vida separados.
- O operador prepara configuração externa, certificados, realm, roles e grants.
  Esta feature não cria contas ou administra a plataforma compartilhada.
- A [Spec 005](../005-deploy-self-hosted-compose/spec.md) continua em rascunho;
  esta solicitação não aprova nem retoma suas tarefas legadas.
- O pedido autoriza a preparação desta proposta. Não há evidência de revisão
  humana do plano ou tarefas novos; a implementação depende dessa revisão.

# Especificação da Feature: Tema Personalizado de Login do Keycloak

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, aguardando homologação

**Entrada**: Migração de `docs/plan/tema-login-keycloak.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Usar identidade visual própria no login (Prioridade: P1)

Como usuário, quero uma experiência visual coerente com a marca Altivy nos fluxos de autenticação habilitados pelo Keycloak.

**Valor desta prioridade**: A interface de identidade deve ser clara e acessível sem duplicar autenticação no produto.

**Teste independente**: Validar login, erro, OTP, recuperação e atualização de credencial na versão fixada do Keycloak em laboratório.

**Cenários de aceite**:

1. **Dado** qualquer fluxo habilitado no realm, **Quando** a tela é exibida, **Então** usa somente assets locais aprovados, sem CDN ou recurso externo.
2. **Dado** um erro ou fluxo de recuperação, **Quando** a tela é renderizada, **Então** foco, contraste e navegação por teclado permanecem acessíveis.

### Jornada do Usuário 2 - Promover ou reverter tema com segurança (Prioridade: P2)

Como operador, quero promover o tema por versão/checksum e voltar ao tema padrão se a validação falhar.

**Valor desta prioridade**: A tematização não pode comprometer login, callbacks OIDC nem acesso a contas.

**Teste independente**: Promover em ambiente de homologação e executar rollback em ambiente representativo.

**Cenários de aceite**:

1. **Dado** um artefato aprovado, **Quando** promovido, **Então** versão, checksums e licença são verificados antes da montagem.
2. **Dado** falha de asset ou login, **Quando** ocorre rollback, **Então** o tema padrão volta sem alterar usuário, credenciais ou políticas.

## Casos de Borda

- Mudança de versão/template do Keycloak.
- Asset ausente, checksum divergente, fonte sem licença ou recurso externo.
- OTP, MFA, alteração/recuperação de credencial e tema claro/escuro.
- Falha de homologação, login, callback ou fluxo OIDC.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: O tema DEVE alterar somente apresentação, mantendo autenticação, autorização, callbacks, cookies e políticas no Keycloak.
- **FR-002**: Login, erro, OTP, recuperação e atualização de credencial DEVEM funcionar conforme os fluxos habilitados no realm.
- **FR-003**: Fontes, CSS, imagens e scripts DEVEM ser locais, licenciados, versionados e verificados por checksum.
- **FR-004**: O tema DEVE atender contraste AA, foco visível, legibilidade e responsividade.
- **FR-005**: O artefato DEVE ser removível e permitir retorno ao tema padrão sem perda de dados.
- **FR-006**: A promoção real permanece bloqueada até haver ambiente de homologação separado e revisão de segurança formal.

### Entidades Principais (quando houver dados)

- **Artefato de tema**: pacote identificado por versão, checksum, versão compatível do Keycloak e proveniência dos assets.

## Critérios de Sucesso (obrigatório)

- **SC-001**: Todos os fluxos habilitados são validados em laboratório e homologação.
- **SC-002**: Nenhum recurso de runtime depende de CDN, URL externa ou asset sem licença/checksum.
- **SC-003**: Rollback ao tema padrão é demonstrado sem alteração de identidade ou políticas.

## Premissas

- A fonte canônica da marca é o repositório `Altivy`; não serão criadas cópias canônicas de assets.
- Keycloak permanece dono da autenticação; Altivy não armazena tokens nem implementa login.
- Ambiente de homologação é dependência para P-04 e permanece não provisionado no legado.

## Proveniência da Migração

Origem: `docs/plan/tema-login-keycloak.md`, estado geral legado `approved`; P-01 a P-03 constam como concluídas no laboratório e P-04 está bloqueada pela ausência de homologação. A migração não declara P-04 concluída.

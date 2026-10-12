# Especificação da Feature: Deploy Self-Hosted por Docker Compose

**Branch da feature**: A definir antes de implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, aguardando revisão humana

**Entrada**: Migração de `docs/plan/deploy-self-hosted-compose.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Instalar a stack no host escolhido (Prioridade: P1)

Como operador, quero subir web, BFF, API e worker em containers, usando gateway e PostgreSQL compartilhados configurados externamente.

**Valor desta prioridade**: O produto é self-hosted e não deve exigir runtimes no host.

**Teste independente**: Validar configuração Compose, TLS e health pelo gateway.

**Cenários de aceite**:

1. **Dado** que secrets, issuer, certificados e volumes foram configurados, **Quando** a stack sobe, **Então** os serviços ficam saudáveis sem publicar portas próprias.
2. **Dado** que o host ainda não está configurado, **Quando** o preflight é executado, **Então** falha sem iniciar ou alterar serviços.

### Jornada do Usuário 2 - Autenticar e persistir conteúdo (Prioridade: P1)

Como operador e usuário, quero autenticar pelo Keycloak e gravar notas sem perder dados na recriação dos containers.

**Valor desta prioridade**: A instalação só é útil se a identidade externa e a persistência sobreviverem a reinícios.

**Teste independente**: Executar login e CRUD de nota via gateway, recriar consumidores e ler conteúdo persistido.

**Cenários de aceite**:

1. **Dado** um usuário autorizado, **Quando** entra pelo gateway, **Então** BFF e API usam OIDC/Keycloak e a API aplica autorização.
2. **Dado** que containers são recriados, **Quando** a aplicação retorna, **Então** vault, banco e chaves de sessão persistem.

### Jornada do Usuário 3 - Recuperar e atualizar instalação (Prioridade: P2)

Como operador, quero validar backup/restore e promover uma release por digest sem downgrade destrutivo.

**Valor desta prioridade**: Atualização self-hosted deve ser reproduzível e recuperável.

**Teste independente**: Restaurar snapshot isolado e validar compatibilidade/rollback documentados.

**Cenários de aceite**:

1. **Dado** backup de banco e vault, **Quando** restaurado em destino efêmero, **Então** arquivos e projeções verificadas são recuperáveis.
2. **Dado** uma release por digest, **Quando** ocorre rollback, **Então** imagens podem retornar sem executar downgrade destrutivo de schema.

## Casos de Borda

- Chaves TLS/PFX inválidas, callbacks OIDC ausentes e issuer inconsistente.
- Portas expostas fora de loopback, volumes com owner incompatível e secrets ausentes.
- PostgreSQL em recovery, worker transient failures e migration incompatível.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: A instalação DEVE usar containers e não exigir runtime de aplicação no host.
- **FR-002**: Somente o gateway HTTPS compartilhado DEVE publicar portas; PostgreSQL e serviços permanecem privados.
- **FR-003**: PostgreSQL, vault e chaves BFF DEVEM persistir após recriação de containers.
- **FR-004**: Keycloak decide identidade/autorização e a API aplica permissões; nenhum token é exposto ao browser.
- **FR-005**: Migrations usam role dedicada, separada de runtime.
- **FR-006**: Secrets não podem ser versionados nem lidos/exibidos durante validações.
- **FR-007**: Backup/restore e upgrade por digest devem ter evidências e runbook; downgrade de schema destrutivo não é rollback.

## Critérios de Sucesso

- **SC-001**: `docker compose config` e builds de containers passam sem secrets no repositório.
- **SC-002**: Login, autorização e CRUD via gateway passam e sobrevivem à recriação de consumidores.
- **SC-003**: Backup/restore preserva arquivos Markdown e a integridade das projeções testadas.

## Premissas

- ADR 2026-0008 registra a decisão de usar Compose self-hosted.
- Keycloak, gateway e PostgreSQL são serviços compartilhados gerenciados externamente; esta stack não os administra.
- P-02 e P-03 do plano legado estão em andamento; P-01 e P-04 têm registros de conclusão histórica.

## Proveniência da Migração

Origem: `docs/plan/deploy-self-hosted-compose.md`, estado geral legado `in-progress`. O status legado não concede autorização para novas ações operacionais.

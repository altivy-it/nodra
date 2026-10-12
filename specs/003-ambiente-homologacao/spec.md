# Especificação da Feature: Ambiente Real de Homologação

**Branch da feature**: A definir antes da implementação

**Criada em**: 2026-10-06

**Estado**: Rascunho de migração, aguardando revisão humana

**Entrada**: Migração de `docs/plan/ambiente-homologacao.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, identificadores, termos de contrato e marcadores literais.

## Cenários do Usuário e Testes (obrigatório)

### Jornada do Usuário 1 - Definir a topologia de homologação (Prioridade: P1)

Como responsável pelo produto, quero escolher uma opção de hospedagem isolada para homologação antes de provisionar infraestrutura.

**Valor desta prioridade**: A decisão envolve custo, isolamento e operação e não pode ser presumida pela automação.

**Teste independente**: Verificar decisão humana registrada para uma opção e suas pré-condições.

**Cenários de aceite**:

1. **Dado** que nenhuma opção foi aprovada, **Quando** uma etapa de provisionamento é iniciada, **Então** o fluxo permanece bloqueado.
2. **Dado** que uma opção foi aprovada, **Quando** o ambiente é provisionado, **Então** ele possui DNS, TLS, realm, banco e secrets próprios.

### Jornada do Usuário 2 - Validar uma versão em ambiente representativo (Prioridade: P1)

Como mantenedor, quero validar login e artefatos em um ambiente separado do laboratório e de produção.

**Valor desta prioridade**: Uma validação real permite promover artefatos com risco controlado.

**Teste independente**: Subir a stack de homologação e exercitar o fluxo OIDC pelo endpoint HTTPS do gateway.

**Cenários de aceite**:

1. **Dado** um tema ou release candidato, **Quando** é promovido por checksum, **Então** login, erro, recuperação e rollback são validados no ambiente.
2. **Dado** que homologação está indisponível, **Quando** uma validação falha, **Então** nenhum artefato é promovido para produção.

## Casos de Borda

- Provedor, DNS, orçamento ou certificado indisponível.
- Realm, callback, secrets ou banco compartilhados acidentalmente.
- Falha do host, TLS ou rede durante a promoção.

## Requisitos (obrigatório)

### Requisitos Funcionais

- **FR-001**: A opção de hospedagem (A, B, C ou composição) DEVE receber decisão humana registrada antes do provisionamento.
- **FR-002**: Homologação DEVE ser isolada do laboratório local/E2E e de produção, sem usuários, secrets, callbacks ou banco compartilhados.
- **FR-003**: O ambiente DEVE possuir DNS e TLS próprios e um realm Keycloak dedicado.
- **FR-004**: A stack DEVE ser reproduzível por Compose e manter serviços de dados privados.
- **FR-005**: A promoção DEVE usar artefatos identificados por digest/checksum e permitir rollback.
- **FR-006**: A validação DEVE cobrir login, erro, recuperação e o tema de Keycloak quando aplicável.

### Entidades Principais (quando houver dados)

- **Ambiente de homologação**: host, origem, TLS, realm, banco, configuração e secrets próprios.
- **Artefato promovido**: imagem/tema identificado por versão, digest ou checksum.

## Critérios de Sucesso (obrigatório)

### Resultados Mensuráveis

- **SC-001**: Nenhum recurso ou segredo de laboratório/produção é compartilhado com homologação.
- **SC-002**: Login, erro e recuperação passam via gateway HTTPS do ambiente.
- **SC-003**: A promoção pode ser interrompida e revertida sem alterar conteúdo de usuário.

## Premissas

- As opções A, B e C permanecem alternativas até nova decisão humana; o plano legado recomenda VPS dedicada, mas registra que ainda não havia decisão.
- Homologação não equivale a produção e não autoriza publicação.
- O tema de Keycloak é a primeira capacidade candidata a validar no ambiente.

## Proveniência da Migração

Origem: `docs/plan/ambiente-homologacao.md`, estado geral legado `approved`, com P-01 a P-05 ainda `not-started`. `approved` do plano antigo não equivale a aprovação de implementação pelo Spec Kit.

# Plano de Implementação: Ambiente Real de Homologação

**Branch**: A definir | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração de `docs/plan/ambiente-homologacao.md`.

**Idioma**: Português brasileiro (pt-BR); preserve caminhos e valores literais.

## Resumo

Definir humanamente a topologia e, após aprovação, provisionar uma homologação persistente isolada, com TLS, Keycloak e PostgreSQL próprios, Compose reproduzível e promoção/rollback de artefatos.

## Contexto Técnico

**Linguagem/Versão**: .NET 10 e Angular 22; stack de identidade existente.

**Dependências Principais**: Docker Compose, gateway nginx, Keycloak, PostgreSQL, DNS e TLS.

**Armazenamento**: Volumes persistentes exclusivos do ambiente.

**Testes**: Validação de Compose, smoke OIDC e testes de promoção/rollback.

**Plataforma Alvo**: Host self-hosted escolhido pelo operador.

**Tipo de Projeto**: Ambiente de implantação e validação.

**Objetivos de Desempenho**: Não definidos no plano legado.

**Restrições**: A escolha do host e do orçamento exige aprovação humana; não compartilhar credenciais ou dados entre ambientes.

**Escopo**: Host/DNS/TLS, stack de homologação, observabilidade do tema e promoção do tema de login.

## Verificação da Constituição

- Ambientes e secrets devem ser isolados.
- O deployment deve permanecer self-hosted e por containers.
- Só gateway HTTPS publica serviços.
- Nenhum provisionamento real ocorre antes da decisão humana da opção.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/003-ambiente-homologacao/{spec.md,plan.md,tasks.md}
```

### Código-fonte

```text
deploy/staging/
docs/observabilidade-alertas-slo.md
docs/configuracao-keycloak.md
```

**Decisão de Estrutura**: `deploy/staging/` é o destino proposto no plano legado; confirmar nome e limites no desenho antes de criar arquivos operacionais.

## Rastreabilidade da Migração

P-01 a P-05 estão `not-started` no plano legado. A opção VPS dedicada é recomendação, não decisão aprovada. A ordem permanece escolha humana → host/DNS/TLS → stack isolada → alerta de tema → promoção do tema. Este plano não autoriza provisionamento.

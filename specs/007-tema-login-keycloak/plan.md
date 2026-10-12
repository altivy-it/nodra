# Plano de Implementação: Tema Personalizado de Login do Keycloak

**Branch**: A definir | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração de `docs/plan/tema-login-keycloak.md`.

**Idioma**: Português brasileiro (pt-BR); preserve comandos, paths e nomes de asset.

## Resumo

Manter o tema local e licenciado do Keycloak que já passou por validação de laboratório; fechar observabilidade e validação em homologação real antes de qualquer promoção.

## Contexto Técnico

**Linguagem/Versão**: Keycloak `26.7.0` no laboratório legado; confirmar versão atual antes de implementação.

**Dependências Principais**: Templates oficiais Keycloak, assets canônicos de `Altivy`, manifests `SHA256SUMS`.

**Armazenamento**: Assets empacotados com imagem/artefato do tema; sem CDN.

**Testes**: Build do tema em container, verificações de checksum/licença, validação visual e OIDC em ambiente isolado.

**Plataforma Alvo**: Keycloak containerizado em laboratório e futura homologação.

**Tipo de Projeto**: Tema visual do provedor de identidade.

**Objetivos de Desempenho**: Sem dependência externa de rede; preservar tempo de carregamento dos fluxos de login.

**Restrições**: Não alterar protocolo OIDC, cookies, callbacks, credenciais ou autorização.

**Escopo**: Tema, assets locais, métrica/alerta e promoção/rollback em homologação.

## Verificação da Constituição

- Keycloak mantém identidade e autorização; o tema não cria autenticação local.
- Assets externos e não verificados são proibidos.
- Secrets e dados reais não podem entrar no artefato.
- Promoção fica bloqueada até homologação e revisão de segurança.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/007-tema-login-keycloak/{spec.md,plan.md,tasks.md}
```

### Código-fonte

```text
deploy/keycloak/
docs/configuracao-keycloak.md
docs/observabilidade-alertas-slo.md
```

**Decisão de Estrutura**: Manter fonte da marca no repositório `Altivy`; assets de integração não se tornam fonte canônica em AltivyNotes.

## Rastreabilidade da Migração

P-01 a P-03 constam como concluídas no laboratório. P-04 está bloqueada pela ausência de ambiente real de homologação, métrica específica de falha de tema e revisão formal de segurança. O legado continua congelado até revisão humana da migração.

# Plano: Ambiente real de homologação

> REFERÊNCIA LEGADA: escopo convertido para [Spec Kit](../../specs/003-ambiente-homologacao/spec.md), ainda em rascunho e aguardando revisão humana. Este arquivo não tem autoridade operacional; não executar nem atualizar etapas.

**Status:** approved
**Data de criação:** 2026-09-01
**Última atualização:** 2026-09-01
**Responsáveis:** não atribuídos
**Origem:** IA assistida
**Revisor humano:** Lincoln Zocateli
**Relacionado:** [Plano do tema de login do Keycloak](tema-login-keycloak.md) (bloqueio do P-04), [CI/CD e Segurança no GitHub](../ci-cd-seguranca.md), [Tutorial de configuração do Keycloak](../configuracao-keycloak.md)

## Objetivo

Provisionar um ambiente de homologação real, isolado do laboratório local e do
laboratório E2E, para permitir a promoção e a validação de artefatos (a
começar pelo tema de login do Keycloak) em condições representativas de
produção: DNS próprio, TLS real, realm e banco dedicados, sem dependência do
laptop de desenvolvimento.

## Contexto e premissas

- Hoje só existem `deploy/e2e` (efêmero, local) e `deploy/keycloak`
  (laboratório local); não há host, DNS ou TLS reais para o Altivy.
- O plano [tema-login-keycloak.md](tema-login-keycloak.md) tem a etapa P-04
  `blocked` explicitamente por falta desse ambiente.
- [configuracao-keycloak.md §12.3](../configuracao-keycloak.md) exige que
  desenvolvimento, homologação e produção não compartilhem usuários, secrets,
  callbacks ou banco; no mínimo, realms separados.
- [ci-cd-seguranca.md §9](../ci-cd-seguranca.md) já prevê a tabela de
  GitHub Environments `development` / `staging` / `production`, com a
  estratégia build once, promote many (mesmo digest promovido por tag).
- O Altivy é self-hosted por princípio para o produto (conteúdo, persistência,
  busca e edição não dependem de nuvem ou internet), mas isso não impede que a
  infraestrutura de hospedagem em si use uma VPS própria, como já ocorre no
  repositório `blog` (VPS Hostinger).
- Esta análise nasceu de uma conversa de planejamento e ainda não teve
  decisão humana sobre qual opção adotar.

## Não escopo

- provisionar produção; este plano cobre apenas homologação;
- decidir o provedor específico de VPS/hospedagem;
- implementar um workflow de deploy remoto; a instalação de aplicação é operada
  localmente pelo Compose self-hosted e ambientes isolados seguem plano próprio;
- alterar o tema de login ou qualquer artefato de aplicação.

## Dependências e decisões

- Decisão humana sobre qual opção (A, B ou C) adotar.
- Orçamento e disponibilidade operacional para manter um host adicional, se a
  opção A for escolhida.
- DNS e certificados TLS dedicados ao ambiente escolhido.

## Opções avaliadas

### Opção A — VPS dedicada de homologação (recomendada)

Host próprio, separado de qualquer outra carga (inclusive do blog). DNS
dedicado (ex.: `hml.altivy.it` para o app e um subdomínio próprio de
Keycloak, nunca reaproveitando `credential.zocate.li`). Mesma composição de
`deploy/e2e` + `deploy/keycloak`, mas com TLS real (certbot, como já usado no
blog), bind mounts persistentes próprios, realm Keycloak próprio e banco
próprio.

- **Prós:** isolamento total de rede, secrets e dados; falha em homologação
  nunca afeta produção nem o blog; mais fiel ao "ambiente real" exigido pelo
  P-04 do tema.
- **Contras:** custo de uma VPS adicional e mais um host para manter
  atualizado (patches de SO, backups, monitoração).

### Opção B — Stack isolada na mesma VPS já usada pelo blog

Reaproveita a VPS Hostinger existente, adicionando uma stack Compose separada
(rede, volumes e containers próprios) sob outro subdomínio.

- **Prós:** custo zero adicional de infraestrutura, provisionamento mais
  rápido.
- **Contras:** viola o espírito de menor privilégio e isolamento de rede
  quando dois produtos diferentes dividem host; um incidente de recurso
  (CPU/disco/rede) num serviço pode degradar o outro; contraria a orientação
  de não acoplar a operação do Altivy a outro repositório. Só faria sentido
  como solução temporária e explicitamente aceita como risco documentado.

### Opção C — Ambiente efêmero sob demanda (só para smoke test/E2E)

Sobe via pipeline (self-hosted runner ou runner temporário) só durante a
validação, depois desliga.

- **Prós:** custo mínimo, sem host permanente.
- **Contras:** não serve para o objetivo real do P-04 (validar sessão,
  callback OIDC, cookies, rollback, monitoração ao longo do tempo) nem para
  nada que dependa de estado persistente do Keycloak/PostgreSQL entre
  execuções. Serve como complemento, não substituto de staging.

### Recomendação

Opção A como ambiente de homologação real, com Opção C como camada adicional
de verificação pós-deploy dentro do pipeline (já previsto em
`ci-cd-seguranca.md §10.3`, "executar verificação pós-deploy").

## Etapas

### P-01: Decidir a opção de ambiente

**Status:** not-started
**Responsável:** Lincoln Zocateli
**Dependências:** nenhuma

**Objetivo:** obter decisão humana explícita sobre qual opção (A, B ou C, ou
combinação) será adotada.

Entregas:

- decisão registrada neste plano com justificativa;
- se a opção A for escolhida, provedor e custo aprovados.

Validação:

- entrada no histórico de status com nome do responsável pela decisão.

Evidências:

- pendente.

### P-02: Provisionar host, DNS e TLS

**Status:** not-started
**Responsável:** Lincoln Zocateli
**Dependências:** P-01

**Objetivo:** disponibilizar o host escolhido com DNS e TLS reais.

Entregas:

- host provisionado (se aplicável);
- subdomínios de homologação criados e resolvendo;
- certificados TLS válidos emitidos.

Validação:

- HTTPS acessível nos subdomínios de homologação sem erro de certificado.

Evidências:

- pendente.

### P-03: Criar a stack de homologação

**Status:** not-started
**Responsável:** Lincoln Zocateli
**Dependências:** P-02

**Objetivo:** criar `deploy/staging/` (ou nome equivalente) como variante de
`deploy/e2e` + `deploy/keycloak`, com realm Keycloak próprio, banco próprio e
sem copiar usuários ou secrets do laboratório.

Entregas:

- diretório `deploy/staging/` versionado, sem secrets reais;
- realm de homologação criado, distinto do laboratório e de produção;
- GitHub Environment `staging` criado com secrets próprios, conforme
  `ci-cd-seguranca.md §9`.

Validação:

- stack sobe via Compose no host de homologação;
- login, erro e recuperação funcionam no realm de homologação.

Evidências:

- pendente.

### P-04: Fechar lacuna de observabilidade do tema

**Status:** not-started
**Responsável:** Lincoln Zocateli
**Dependências:** nenhuma (pode ocorrer em paralelo)

**Objetivo:** criar métrica ou alerta específico para falha de carregamento do
tema de login, hoje ausente conforme registrado em
[tema-login-keycloak.md P-04](tema-login-keycloak.md).

Entregas:

- métrica ou alerta de erro 404/500 em `/resources/.../login/altivy/**`
  documentado em `observabilidade-alertas-slo.md`.

Validação:

- alerta dispara em teste controlado de asset ausente.

Evidências:

- pendente.

### P-05: Promover e validar o tema de login em homologação

**Status:** not-started
**Responsável:** Lincoln Zocateli
**Dependências:** P-03, P-04

**Objetivo:** executar de fato o P-04 do plano `tema-login-keycloak.md` no
ambiente novo, destravando aquele bloqueio.

Entregas:

- artefato do tema promovido por checksum conforme
  `configuracao-keycloak.md §6.4`;
- login, erro, recuperação e alternância de tema validados em homologação;
- rollback testado de verdade no ambiente novo.

Validação:

- critérios de aceite do P-04 de `tema-login-keycloak.md` atendidos.

Evidências:

- pendente.

## Critérios de aceite finais

- existe um ambiente de homologação real, com DNS e TLS próprios, distinto do
  laboratório local/E2E e de produção;
- o ambiente não compartilha usuários, secrets, callbacks ou banco com
  qualquer outro ambiente;
- o P-04 de `tema-login-keycloak.md` pode ser reavaliado e desbloqueado com
  base neste ambiente.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| optar pela Opção B por economia | acoplamento operacional entre Altivy e blog | tratar como decisão temporária e documentada, com plano de migração para Opção A |
| ambiente de homologação usar o mesmo realm/host do laboratório | dados ou secrets vazando entre ambientes | provisionar realm e banco dedicados, nunca reaproveitar o do laboratório |
| custo recorrente de VPS adicional não aprovado | atraso indefinido do provisionamento | decisão explícita de orçamento no P-01 antes de seguir |

## Rollout e rollback

Provisionar primeiro o host e o DNS (P-02), depois a stack (P-03), validando
cada etapa isoladamente antes de promover qualquer artefato de aplicação. Se o
ambiente apresentar problema estrutural (DNS, TLS, isolamento de rede), voltar
a usar apenas o laboratório local/E2E até corrigir, sem promover artefatos
para esse ambiente enquanto ele não estiver estável.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-09-01 | plano | - | proposed | análise de opções gerada em sessão de planejamento, aguardando decisão humana | IA assistida |

## Regras de atualização

- IA não define status acima de `proposed` sem aprovação humana explícita.
- A escolha da opção (A, B ou C) no P-01 exige decisão humana registrada antes
  de qualquer provisionamento real.
- Alterações neste plano não desbloqueiam automaticamente o P-04 de
  `tema-login-keycloak.md`; aquele plano deve ser atualizado separadamente
  quando este for concluído.

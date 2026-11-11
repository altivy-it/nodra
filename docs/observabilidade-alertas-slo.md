# Observabilidade, SLOs e Alertas: BFF e Keycloak

Este documento define os objetivos de nível de serviço (SLOs), indicadores-chave
de nível de serviço (SLIs), métricas OpenTelemetry e políticas de alertas para a
integração entre o Nodra BFF, Altivy API e Keycloak.

## 1. Indicadores e Objetivos de Nível de Serviço (SLIs e SLOs)

| Jornada / Operação | SLI (Métrica) | Objetivo (SLO) | Janela |
| --- | --- | --- | --- |
| **Login OIDC (Challenge + Callback)** | Proporção de callbacks concluídos com HTTP 302/200 vs erros 5xx | $\ge 99.5\%$ de sucesso | 30 dias rolantes |
| **Latência de Login** | Tempo de resposta do endpoint `/bff/login` até o challenge OIDC | $P_{95} \le 500\text{ ms}$ | 7 dias rolantes |
| **Decisão de Autorização UMA na API** | Proporção de decisões UMA com resposta válida ($\ne 503$) | $\ge 99.9\%$ de disponibilidade | 30 dias rolantes |
| **Latência do PDP (Keycloak)** | Tempo de resposta da consulta UMA remota (`KeycloakAuthorizationDecisionService`) | $P_{95} \le 150\text{ ms}$ | 7 dias rolantes |
| **Renovação de Tokens (Token Refresh)** | Proporção de renovações de access token concluídas antes da expiração | $\ge 99.0\%$ de sucesso | 30 dias rolantes |

## 2. Métricas OpenTelemetry Coletadas

### 2.1 Métricas do BFF (`altivy-bff`)

- `http.server.request.duration`: latência e status das requisições HTTP públicas (`/bff/session`, `/bff/login`, `/bff/logout`, proxy `/api/v1`).
- `bff.token_refresh.duration`: histograma de duração das renovações de token no backchannel do Keycloak.
- `bff.token_refresh.result`: contador de resultados das renovações (`success`, `rejected`, `timeout`, `failure`).

### 2.2 Métricas da API (`nodra-api`)

- `api.authorization.decision.duration`: histograma do tempo de avaliação de permissão pelo Keycloak.
- `api.authorization.decision.status`: contador com tags `result` (`granted`, `denied`), `scope` (`vault:read`, `vault:write`, etc.) e `recurso`.

> **Nota de Privacidade e Segurança:** As métricas coletadas mantêm cardinalidade
> fixa e não incluem `sub` de usuários, tokens, senhas, chaves ou conteúdo de
> vaults nos atributos/labels.

### 2.3 Métricas do worker (`altivy-worker`)

- `altivy.worker.vault_provisioning.processed`: contador de operações de
  provisioning processadas.
- `altivy.worker.vault_residue.detected`: contador de resíduos detectados, com
  tag `type` limitada a `missing-directory` ou `orphaned-directory`.
- `altivy.worker.vault_residue_alert.processed`: contador de alertas HTTPS
  processados pelo worker.
- `altivy.worker.failure`: contador de falhas operacionais, com tag `category`
  limitada a banco, filesystem, acesso ao filesystem, timeout ou configuração.

O worker exporta métricas, traces de `HttpClient` e métricas de runtime quando
`OpenTelemetry__OtlpEndpoint` é configurado. A ausência do endpoint mantém a
instrumentação local e não interrompe o processamento. IDs de vault, sujeitos,
paths, URLs de webhook e secrets não são tags de métrica.

## 3. Definição de Alertas e Severidades

### 3.1 Alertas Críticos (P1 - Atuação Imediata)

- **KeycloakDiscoveryUnavailable**:
  - *Condição:* O health check `/health/ready` do BFF falha consecutivamente por mais de 2 minutos devido à indisponibilidade do discovery do realm.
  - *Ação:* Acionar o [Runbook de Indisponibilidade do Keycloak](runbooks/operacao-keycloak-indisponibilidade-recuperacao.md).

- **AuthorizationServiceDegraded**:
  - *Condição:* A taxa de erros HTTP 503 no endpoint UMA da API ultrapassa $1\%$ em uma janela de 5 minutos.
  - *Ação:* Investigar conectividade entre API e Keycloak, ou saturação de CPU/conexões do PostgreSQL do Keycloak.

### 3.2 Alertas de Aviso (P2 - Investigação no Próximo Turno)

- **TokenRefreshFailureSpike**:
  - *Condição:* Mais de 5 falhas de renovação de refresh token em 10 minutos (indicativo de expiração forçada ou sessões revogadas em massa).
  - *Ação:* Avaliar se houve revogação intencional de sessões no Keycloak ou divergência de relógio/NTP entre os containers.

- **HighAuthorizationLatency**:
  - *Condição:* $P_{95}$ de decisão de autorização $> 300\text{ ms}$ por mais de 10 minutos contínuos.
  - *Ação:* Verificar tempo de resposta do banco PostgreSQL e índices do Keycloak.

# Integração com Grafana e Prometheus - k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted

---

## 1. Objetivo

Definir a estratégia de integração dos testes k6 com Prometheus e Grafana para observabilidade, análise histórica e correlação de métricas.

---

## 2. Visão geral

```text
k6
 ↓ métricas de execução
Prometheus / Remote Write
 ↓ consultas PromQL
Grafana
 ↓ dashboards e alertas
Times de Dev, QA, DevOps e Arquitetura
```

---

## 3. Métricas relevantes do k6

- `http_req_duration`
- `http_req_failed`
- `http_reqs`
- `vus`
- `vus_max`
- `iterations`
- `checks`
- Métricas customizadas por cenário, endpoint ou fluxo de negócio

---

## 4. Métricas da aplicação a correlacionar

Durante testes de carga, comparar métricas do k6 com:

- CPU da aplicação
- Memória
- Garbage Collection (.NET/Python runtime quando disponível)
- Latência do banco de dados
- Pool de conexões
- Erros HTTP 4xx/5xx
- Saturação de filas
- Latência de dependências externas
- Métricas de NGINX, API Gateway ou Load Balancer

---

## 5. Dashboards recomendados

### Dashboard executivo

- Resultado geral da execução
- Passou/falhou thresholds
- P95/P99
- Taxa de erro
- Throughput médio
- Comparação com baseline anterior

### Dashboard técnico

- Latência por endpoint
- Requests por segundo
- Erros por status code
- VUs ativos
- Duração por cenário
- Saturação de recursos
- Correlação com logs e traces

---

## 6. Labels obrigatórias

Toda execução deve identificar:

```text
application
team
environment
version
commit
scenario
pipeline_run_id
```

Essas labels permitem comparação histórica e filtros por aplicação, time, release e ambiente.

---

## 7. Alertas recomendados

- P95 acima do SLO.
- P99 acima do limite crítico.
- Taxa de erro acima de 1%.
- Queda brusca de throughput.
- Uso anormal de CPU ou memória durante o teste.
- Aumento de latência em dependências externas.

---

## 8. Recomendação operacional

A execução local pode usar apenas console summary. Execuções de pipeline devem exportar métricas para Prometheus ou armazenar artefatos JSON. Execuções formais de release devem ter dashboard Grafana vinculado como evidência.

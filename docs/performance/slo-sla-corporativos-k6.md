# Definição de SLO/SLA Corporativos para APIs - k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted

---

## 1. Objetivo

Definir critérios corporativos de SLO e SLA para validar performance de APIs usando k6 em ambientes de desenvolvimento, QA, staging e produção.

---

## 2. Definições

### SLA

Acordo formal de nível de serviço, normalmente associado a contratos, disponibilidade e impacto de negócio.

### SLO

Objetivo interno mensurável usado para avaliar se a aplicação atende ao desempenho esperado.

### SLI

Indicador usado para medir o comportamento real do serviço.

---

## 3. SLIs recomendados

- Latência P50
- Latência P95
- Latência P99
- Taxa de erro
- Throughput
- Disponibilidade
- Tempo de autenticação
- Tempo de resposta por endpoint crítico
- Saturação de recursos

---

## 4. SLO base recomendado

| Critério | Valor inicial recomendado |
|---|---:|
| Taxa de erro | < 1% |
| P95 APIs críticas | < 500 ms |
| P99 APIs críticas | < 1000 ms |
| Disponibilidade mensal | > 99% |
| Threshold violations | 0 |
| Regressão de latência por release | < 10% |

Os valores devem ser ajustados por criticidade, volume, horário de uso e dependências externas.

---

## 5. Classificação por criticidade

### APIs críticas

Exemplos:

- Autenticação
- Criação de pedidos
- Consulta operacional crítica
- Integrações financeiras
- Processos regulados

Critérios sugeridos:

```text
P95 < 500 ms
P99 < 1000 ms
Erro < 0.5%
```

### APIs importantes

```text
P95 < 800 ms
P99 < 1500 ms
Erro < 1%
```

### APIs administrativas ou baixa criticidade

```text
P95 < 1500 ms
P99 < 3000 ms
Erro < 2%
```

---

## 6. Thresholds k6 sugeridos

```javascript
export const options = {
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    checks: ['rate>0.99']
  }
};
```

---

## 7. Política de aprovação

| Ambiente | Exigência |
|---|---|
| DEV | Execução opcional, recomendada para endpoints críticos |
| PR | Smoke performance test |
| QA | Execução obrigatória para releases relevantes |
| Staging | Execução obrigatória para APIs críticas |
| Produção | Somente testes controlados, sintéticos ou aprovados |

---

## 8. Recomendação

Toda API crítica deve possuir SLO documentado e teste k6 associado. O pipeline deve falhar quando thresholds mínimos forem violados em ambientes controlados.

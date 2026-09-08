# Estratégia de Execução em Pipelines - k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted

---

## 1. Objetivo

Definir como os testes de performance com k6 serão executados em pipelines de CI/CD, para Azure DevOps, GitHub Actions e execução local.

---

## 2. Princípios

- Testes de performance devem ser versionados junto ao código.
- Testes devem possuir thresholds claros para aprovação ou reprovação.
- Ambientes produtivos não devem ser submetidos a carga sem aprovação formal.
- Execuções recorrentes devem gerar evidências armazenáveis.
- O mesmo script deve poder rodar localmente e em pipeline.

---

## 3. Tipos de execução

### 3.1 Localhost

Uso por desenvolvedores para validação rápida antes de Pull Request.

```bash
k6 run tests/performance/product-api.js
```

### 3.2 Pull Request

Execução leve, com baixa carga, para identificar regressões básicas.

Características:

- Duração curta.
- Poucos usuários virtuais.
- Foco em smoke performance.
- Thresholds mínimos.

### 3.3 Pipeline de Release

Execução mais robusta em ambiente QA ou staging.

Características:

- Carga próxima da esperada.
- Testes de fluxo completo.
- Geração de artefatos JSON, CSV e summary.
- Publicação de resultados no pipeline.

### 3.4 Execução agendada

Execução noturna ou semanal para tendência histórica.

Características:

- Testes mais longos.
- Integração com Prometheus/Grafana.
- Comparação entre releases.
- Detecção de degradação gradual.

---

## 4. Exemplo Azure DevOps

```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: ubuntu-latest

steps:
  - checkout: self

  - script: |
      docker run --rm -i grafana/k6 run - < tests/performance/product-api.js
    displayName: Run k6 performance test
```

---

## 5. Exemplo GitHub Actions

```yaml
name: performance-tests

on:
  pull_request:
  workflow_dispatch:

jobs:
  k6:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Run k6
        run: |
          docker run --rm -i grafana/k6 run - < tests/performance/product-api.js
```

---

## 6. Thresholds obrigatórios

Todo teste deve ter thresholds mínimos.

```javascript
export const options = {
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000']
  }
};
```

---

## 7. Critério de falha

O pipeline deve falhar quando:

- Taxa de erro exceder o limite definido.
- P95 ou P99 excederem o SLO estabelecido.
- A API retornar erros HTTP inesperados.
- O teste não conseguir autenticar.
- O ambiente de teste estiver indisponível.

---

## 8. Recomendação

Executar testes leves em Pull Requests e testes completos em releases ou execuções agendadas. Testes destrutivos, stress, spike e soak devem exigir aprovação manual e ambiente dedicado.

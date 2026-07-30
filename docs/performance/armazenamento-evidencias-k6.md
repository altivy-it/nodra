# Armazenamento de Evidências de Teste - k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted

---

## 1. Objetivo

Definir como os resultados dos testes de carga e performance serão armazenados para auditoria, troubleshooting, comparação histórica e governança técnica.

---

## 2. Artefatos mínimos

Cada execução relevante deve armazenar:

- Script k6 executado.
- Versão do commit Git.
- Ambiente alvo.
- Data e hora da execução.
- Responsável ou pipeline executor.
- Console summary.
- Resultado JSON.
- Resultado CSV quando aplicável.
- Status dos thresholds.
- Link para dashboard Grafana quando aplicável.
- Logs do pipeline.

---

## 3. Estrutura recomendada

```text
performance-evidence/
├── application-name/
│   ├── 2026/
│   │   ├── 07/
│   │   │   ├── 2026-07-30-release-1.12.0/
│   │   │   │   ├── summary.txt
│   │   │   │   ├── results.json
│   │   │   │   ├── results.csv
│   │   │   │   ├── thresholds.txt
│   │   │   │   ├── pipeline-log.txt
│   │   │   │   └── metadata.json
```

---

## 4. Metadados mínimos

```json
{
  "application": "product-api",
  "environment": "qa",
  "commit": "abc123",
  "branch": "main",
  "pipelineRunId": "12345",
  "tool": "k6",
  "k6Version": "x.y.z",
  "executionDate": "2026-07-30T12:00:00Z",
  "result": "passed",
  "dashboardUrl": "https://grafana.example.com/d/..."
}
```

---

## 5. Retenção

| Tipo de execução | Retenção recomendada |
|---|---:|
| Pull Request | 30 dias |
| Release QA | 1 ano |
| Release produção | 3 a 5 anos, conforme política corporativa |
| Auditoria/SOX | Conforme política de retenção aplicável |
| Testes exploratórios locais | Não obrigatório |

---

## 6. Local de armazenamento

Opções recomendadas:

- Azure DevOps Pipeline Artifacts
- GitHub Actions Artifacts
- Azure Blob Storage
- SharePoint corporativo para evidências formais
- Repositório dedicado de evidências quando aprovado pela governança

---

## 7. Requisitos de segurança

- Não armazenar tokens, senhas ou headers sensíveis nos artefatos.
- Mascarar dados pessoais ou transacionais.
- Evitar payloads reais de produção.
- Controlar acesso aos artefatos por grupo corporativo.
- Separar evidências de ambientes produtivos e não produtivos.

---

## 8. Recomendação

Para auditoria, armazenar ao menos `summary.txt`, `results.json`, `metadata.json` e logs do pipeline. Para análise técnica, manter também métricas históricas em Prometheus/Grafana.

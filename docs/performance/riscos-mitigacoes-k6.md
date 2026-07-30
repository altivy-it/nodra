# Riscos e Mitigações - Adoção do k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted

---

## 1. Objetivo

Identificar riscos técnicos, operacionais, de compliance e governança relacionados à adoção do k6 como ferramenta corporativa de testes de carga.

---

## 2. Riscos principais

| Risco | Impacto | Probabilidade | Mitigação |
|---|---:|---:|---|
| Uso incorreto da licença AGPLv3 | Alto | Baixa/Média | Usar binário oficial, evitar modificações e submeter customizações à revisão jurídica. |
| Testes gerarem carga em produção indevidamente | Alto | Média | Exigir aprovação manual e limitar execução por ambiente. |
| Scripts ficarem desatualizados | Médio | Alta | Manter testes no mesmo repositório da aplicação e revisar em PRs. |
| Thresholds mal definidos | Médio/Alto | Média | Definir SLO por criticidade e revisar baseline periodicamente. |
| Dados sensíveis em evidências | Alto | Média | Mascarar tokens, payloads e logs. Usar massa sintética. |
| Ambiente de teste não representar produção | Alto | Alta | Documentar diferenças e usar testes como tendência, não verdade absoluta. |
| Falha intermitente em pipeline | Médio | Média | Separar smoke performance de testes longos. Usar retry controlado somente para falhas de infraestrutura. |
| Falta de conhecimento em JavaScript/k6 | Médio | Média | Criar templates, exemplos e treinamento interno. |
| Sobrecarga no time DevOps | Médio | Média | Automatizar templates e delegar manutenção dos scripts aos times de aplicação. |
| Métricas sem histórico | Médio | Média | Integrar com Prometheus/Grafana e armazenar artefatos por release. |
| Comparação incorreta entre execuções | Médio | Média | Registrar metadados: commit, ambiente, versão, horário, carga e dados usados. |
| Custos de infraestrutura de teste | Médio | Média | Usar níveis de teste por ambiente e agendar testes pesados. |

---

## 3. Mitigações obrigatórias

- Implementar política de ambientes permitidos para teste.
- Definir limites de carga por tipo de pipeline.
- Criar templates oficiais de scripts k6.
- Exigir thresholds mínimos em todos os scripts.
- Armazenar evidências estruturadas.
- Validar licenciamento antes de customizações.
- Integrar resultados com dashboards para análise histórica.

---

## 4. Mitigações recomendadas

- Criar biblioteca interna de helpers k6.
- Criar pipeline template reutilizável.
- Criar dashboard padrão por aplicação.
- Definir processo de exceção temporária para releases urgentes.
- Realizar review trimestral dos SLOs.
- Usar massa de dados sintética ou anonimizada.

---

## 5. Plano de resposta a falhas

Quando um teste falhar:

1. Confirmar se a falha foi do ambiente, pipeline ou aplicação.
2. Verificar thresholds violados.
3. Comparar com baseline anterior.
4. Correlacionar com métricas de infraestrutura e aplicação.
5. Abrir issue vinculada ao commit/release.
6. Definir ação corretiva ou exceção aprovada.

---

## 6. Recomendação

A adoção do k6 é tecnicamente viável e recomendada, desde que acompanhada de governança mínima de licenciamento, ambientes, evidências e SLOs.

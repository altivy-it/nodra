# RACI - Testes de Performance com k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted

---

## 1. Objetivo

Definir responsabilidades entre Desenvolvimento, QA, DevOps e Arquitetura para criação, execução, análise e governança dos testes de performance.

---

## 2. Papéis

- **Dev:** time de desenvolvimento responsável pela aplicação.
- **QA:** time responsável por estratégia de testes e validação formal.
- **DevOps/SRE:** time responsável por pipelines, infraestrutura, observabilidade e execução automatizada.
- **Arquitetura:** time responsável por padrões, governança, critérios não funcionais e revisão técnica.

---

## 3. Matriz RACI

| Atividade | Dev | QA | DevOps/SRE | Arquitetura |
|---|---|---|---|---|
| Definir SLO/SLA da aplicação | C | C | C | A/R |
| Criar scripts k6 iniciais | R | C | C | C |
| Manter scripts junto com evolução da API | R | C | C | I |
| Definir massa de dados de teste | R | A/R | C | I |
| Configurar execução local | R | I | C | I |
| Configurar execução em Azure DevOps/GitHub Actions | C | I | A/R | C |
| Definir thresholds | R | C | C | A |
| Executar testes em Pull Request | R | I | A/R | I |
| Executar testes formais de release | C | A/R | R | C |
| Publicar artefatos de evidência | C | R | A/R | I |
| Criar dashboards Grafana | C | C | A/R | C |
| Analisar falhas de performance | R | C | R | C |
| Aprovar exceções temporárias | C | C | C | A/R |
| Revisar padrão anualmente | C | C | C | A/R |

Legenda:

- **R:** Responsible, executa a atividade.
- **A:** Accountable, dono final da decisão.
- **C:** Consulted, consultado durante a decisão.
- **I:** Informed, informado sobre resultado.

---

## 4. Modelo operacional

### Desenvolvimento

- Cria e mantém scripts de teste próximos ao código.
- Corrige regressões de performance.
- Define endpoints e fluxos críticos com apoio da arquitetura.

### QA

- Valida cenários de teste.
- Garante massa de dados adequada.
- Apoia a evidência formal de release.

### DevOps/SRE

- Automatiza execução em pipelines.
- Mantém integração com Grafana/Prometheus.
- Garante armazenamento de artefatos.

### Arquitetura

- Define padrões e thresholds mínimos.
- Aprova exceções.
- Revisa aderência ao ADR.

---

## 5. Recomendação

A responsabilidade diária deve ficar com os times de aplicação e plataforma. Arquitetura deve atuar como governança e evitar se tornar gargalo operacional.

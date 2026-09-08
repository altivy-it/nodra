# Engenharia assistida por GitHub Copilot

Este diretório define como pessoas e GitHub Copilot devem arquitetar, implementar, testar, revisar e preparar releases do Altivy.

Se você está começando a usar IA ou GitHub Copilot, leia primeiro o [tutorial prático](COPILOT_GUIDE.md).

## Estrutura

| Diretório/arquivo | Finalidade |
| --- | --- |
| `copilot-instructions.md` | regras globais carregadas em todo o workspace |
| `instructions/*.instructions.md` | regras carregadas por domínio e caminho de arquivo |
| `agents/*.agent.md` | especialistas com papéis e ferramentas limitados |
| `prompts/*.prompt.md` | tarefas focadas invocáveis por `/` |
| `skills/*/SKILL.md` | workflows multi-etapas invocáveis por `/` |
| `COPILOT_GUIDE.md` | tutorial de uso com fluxos e exemplos reais |
| `COMMIT_CONVENTIONS.md` | tipos, escopos e formato obrigatório de Conventional Commits |
| `SECRET-SCANNING.md` | política, instalação e tratamento de achados do Gitleaks |
| `CONTRIBUTING.md` | processo de desenvolvimento e contribuição |
| `GOVERNANCE.md` | papéis, decisões, revisão e releases |
| `SECURITY.md` | reporte e tratamento de vulnerabilidades |
| `PULL_REQUEST_TEMPLATE.md` | evidências mínimas de um pull request |
| `ISSUE_TEMPLATE/` | entrada estruturada de bugs e features |
| `ADR_TEMPLATE.md` | estrutura canônica para decisões arquiteturais |
| `PLAN_TEMPLATE.md` | estrutura canônica para planos e status por etapa |
| `PERFORMANCE_TESTING_CRITERIA.md` | critérios para testes de carga e desempenho |

## Como escolher

- Use **Altivy Delivery Lead** ou `/implement-feature` para entrega ponta a ponta.
- Use **Altivy Angular Engineer** para trabalho isolado em Angular.
- Use **Altivy .NET Engineer** para trabalho isolado em backend.
- Use **Altivy Solution Architect** ou `/create-adr` para decisões de arquitetura.
- Use `/plan-feature` para criar ou atualizar um plano em `docs/plan`.
- Use `/generate-tests` para aumentar evidência de um comportamento.
- Use `/review-change` ou `/pull-request-review` para revisão.
- Use `/release-readiness` antes de publicar uma versão.

## Hierarquia de autoridade

1. requisitos e decisões aceitas no [plano mestre](../docs/plano-mestre.md) e ADRs;
2. [instruções globais](copilot-instructions.md);
3. instruções específicas do caminho alterado;
4. skill, prompt ou agente selecionado;
5. convenções observadas no módulo e nos testes próximos.

Em caso de conflito, preserve integridade e segurança, interrompa alterações irreversíveis e registre a decisão necessária.

Decisões sobre colaboração e autoridade seguem a [governança](GOVERNANCE.md).
Regras detalhadas de CI e supply chain estão em
[CI/CD e segurança](../docs/ci-cd-seguranca.md).

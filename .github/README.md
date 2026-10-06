# Engenharia assistida por GitHub Copilot

Este diretório define como pessoas e GitHub Copilot devem arquitetar, implementar, testar, revisar e preparar releases do Altivy.

Se você está começando a usar IA ou GitHub Copilot, leia primeiro o [tutorial prático](COPILOT_GUIDE.md).

## Estrutura

| Diretório/arquivo | Finalidade |
| --- | --- |
| `copilot-instructions.md` | regras globais carregadas em todo o workspace |
| `instructions/*.instructions.md` | regras carregadas por domínio e caminho de arquivo |
| `agents/*.agent.md` | especialistas com papéis e ferramentas limitados |
| `prompts/*.prompt.md` | ferramentas complementares; não definem workflows de desenvolvimento |
| `skills/*/SKILL.md` | comandos oficiais do Spec Kit e gates complementares de revisão/release |
| `COPILOT_GUIDE.md` | tutorial de uso com fluxos e exemplos reais |
| `COMMIT_CONVENTIONS.md` | tipos, escopos e formato obrigatório de Conventional Commits |
| `SECRET-SCANNING.md` | política, instalação e tratamento de achados do Gitleaks |
| `CONTRIBUTING.md` | processo de desenvolvimento e contribuição |
| `GOVERNANCE.md` | papéis, decisões, revisão e releases |
| `SECURITY.md` | reporte e tratamento de vulnerabilidades |
| `PULL_REQUEST_TEMPLATE.md` | evidências mínimas de um pull request |
| `ISSUE_TEMPLATE/` | entrada estruturada de bugs e features |
| `ADR_TEMPLATE.md` | estrutura canônica para decisões arquiteturais |
| `PLAN_TEMPLATE.md` | legado obsoleto, mantido até a revisão/arquivamento dos planos convertidos |
| `../specs/` | requisitos, desenho e tarefas Spec Kit por feature |
| `../.specify/memory/constitution.md` | princípios duradouros do projeto e exigência documental pt-BR |
| `../.specify/templates/overrides/` | templates locais de saída do Spec Kit em pt-BR |
| `PERFORMANCE_TESTING_CRITERIA.md` | critérios para testes de carga e desempenho |

## Como escolher

- Use os comandos Spec Kit para todas as mudanças de aplicação: features pelo SDD e defeitos pela extensão oficial Bug Fixing. As skills instaladas expõem esses comandos; não criam um fluxo concorrente.
- Use os agentes Angular, .NET, Quality e Security somente para executar ou revisar partes de uma tarefa Spec Kit ativa.
- Use **Altivy Solution Architect** ou `/create-adr` para decisões de arquitetura.
- Use `/generate-tests` e `/review-change` somente em apoio a uma tarefa/artefato Spec Kit existente.
- Use `/pull-request-review` e `/release-readiness` como gates posteriores, sem alterar requisitos ou tarefas.

## Hierarquia de autoridade

As autoridades operam em âmbitos distintos:

- visão e escopo globais: [plano mestre](../docs/plano-mestre.md) e documentos de domínio;
- decisões arquiteturais: ADRs aceitos;
- princípios duradouros de engenharia: `.specify/memory/constitution.md`;
- regras operacionais e técnicas do repositório: [instruções globais](copilot-instructions.md) e instruções específicas do caminho; elas restringem, mas não substituem ou duplicam o processo Spec Kit;
- requisitos da feature: `specs/<feature>/spec.md`;
- desenho e execução da feature: `specs/<feature>/plan.md` e `tasks.md`;
- revisão e segurança: skills, prompts e agentes locais, subordinados ao escopo Spec Kit.

Uma feature não pode contrariar decisão arquitetural aceita nem regras invariáveis. Em caso de conflito entre fontes, preserve integridade e segurança, interrompa alterações irreversíveis e solicite decisão humana; não mantenha cópias concorrentes do requisito ou do plano.

Decisões sobre colaboração e autoridade seguem a [governança](GOVERNANCE.md).
Regras detalhadas de CI e supply chain estão em
[CI/CD e segurança](../docs/ci-cd-seguranca.md).

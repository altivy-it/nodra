# Engenharia assistida por GitHub Copilot

Este diretório define como pessoas e GitHub Copilot devem arquitetar, implementar, testar, revisar e preparar releases do Basalt Notes.

Se você está começando a usar IA ou GitHub Copilot, leia primeiro o [tutorial prático](COPILOT_GUIDE.md).

## Estrutura

| Diretório/arquivo | Finalidade |
| --- | --- |
| `copilot-instructions.md` | regras globais carregadas em todo o workspace |
| `instructions/*.instructions.md` | regras carregadas por domínio e caminho de arquivo |
| `agents/*.agent.md` | especialistas com papéis e ferramentas limitados |
| `prompts/*.prompt.md` | tarefas focadas invocáveis por `/` |
| `skills/*/SKILL.md` | workflows multi-etapas invocáveis por `/` |
| `ADR_TEMPLATE.md` | estrutura canônica para decisões arquiteturais |
| `PLAN_TEMPLATE.md` | estrutura canônica para planos e status por etapa |
| `COPILOT_GUIDE.md` | tutorial de uso com fluxos e exemplos reais |
| `COMMIT_CONVENTIONS.md` | tipos, escopos e formato obrigatório de Conventional Commits |
| [CI/CD e segurança](../docs/ci-cd-seguranca.md) | referência canônica de CI/CD, Sonar, segurança e supply chain |
| `CONTRIBUTING.md` | processo de desenvolvimento e contribuição |
| `SECURITY.md` | reporte e tratamento de vulnerabilidades |
| `PULL_REQUEST_TEMPLATE.md` | evidências mínimas de um pull request |
| `ISSUE_TEMPLATE/` | entrada estruturada de bugs e features |

## Como escolher

- Use **Basalt Delivery Lead** ou `/implement-feature` para entrega ponta a ponta.
- Use **Basalt Angular Engineer** para trabalho isolado em Angular.
- Use **Basalt .NET Engineer** para trabalho isolado em backend.
- Use **Basalt Solution Architect** ou `/create-adr` para decisões de arquitetura.
- Use `/plan-feature` para criar ou atualizar um plano em `docs/plan`.
- Use `/generate-tests` para aumentar evidência de um comportamento.
- Use `/review-change` ou `/pull-request-review` para revisão.
- Use `/release-readiness` antes de publicar uma versão.

## Hierarquia de autoridade

1. requisitos e decisões aceitas no [plano mestre](../README.md) e ADRs;
2. [instruções globais](copilot-instructions.md);
3. instruções específicas do caminho alterado;
4. skill, prompt ou agente selecionado;
5. convenções observadas no módulo e nos testes próximos.

Em caso de conflito, preserve integridade e segurança, interrompa alterações irreversíveis e registre a decisão necessária.

## ADRs e planos

- ADRs criados por humanos ou IA vivem em `docs/adr/NNNN-titulo.md` desde o status `proposed` e seguem [ADR_TEMPLATE.md](ADR_TEMPLATE.md).
- Planos criados por humanos ou IA vivem em `docs/plan/<tema>.md` e seguem [PLAN_TEMPLATE.md](PLAN_TEMPLATE.md).
- ADRs usam `proposed`, `accepted`, `superseded` ou `deprecated`.
- Planos possuem status geral e status em todas as etapas; transições registram autor, data e evidência.
- IA pode propor artefatos e atualizar progresso comprovado, mas aceite de ADR, aprovação de plano e dispensa de etapa são decisões humanas explícitas.

## Manutenção

- Uma regra global deve ser relevante para quase toda tarefa.
- Regras específicas pertencem a um único arquivo de instrução com `applyTo` restrito.
- Prompts executam uma tarefa; skills descrevem workflows.
- Agentes têm um papel e o menor conjunto de ferramentas necessário.
- Nomes, descrições e exemplos devem refletir a estrutura real do monorepo.
- Valide frontmatter, links e referências entre agentes após alterações.

## Escolha da customização

- Crie uma **instrução** para regra contínua descoberta por tarefa ou `applyTo`.
- Crie um **prompt** para uma tarefa única, curta e parametrizável.
- Crie uma **skill** para workflow multi-etapas com gates e recursos próprios.
- Crie um **agente** quando o papel exigir contexto isolado ou ferramentas restritas.
- Não crie nova customização se uma existente puder ser ampliada sem misturar responsabilidades.
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
| `ADR_TEMPLATE.md` | estrutura canônica para decisões arquiteturais |
| `PERFORMANCE_TESTING_CRITERIA.md` | baseline corporativo para testes de carga e performance |
| `PLAN_TEMPLATE.md` | estrutura canônica para planos e status por etapa |
| `COPILOT_GUIDE.md` | tutorial de uso com fluxos e exemplos reais |
| `COMMIT_CONVENTIONS.md` | tipos, escopos e formato obrigatório de Conventional Commits |
| [CI/CD e segurança](../docs/ci-cd-seguranca.md) | referência canônica de CI/CD, Sonar, segurança e supply chain |
| `CONTRIBUTING.md` | processo de desenvolvimento e contribuição |
| `SECURITY.md` | reporte e tratamento de vulnerabilidades |
| `PULL_REQUEST_TEMPLATE.md` | evidências mínimas de um pull request |
| `ISSUE_TEMPLATE/` | entrada estruturada de bugs e features |

Aplicações .NET em `apps/` usam `GlobalUsings.cs` por host para namespaces de
plataforma comuns. Namespaces de domínio, aplicação, infraestrutura e features
continuam explícitos; essa regra evita acoplamento global entre API, BFF e
futuros workers. Consulte `backend-dotnet.instructions.md` para detalhes.

## Categorias de testes

Toda classe de teste C# deve declarar `[Trait("Category", "...")]`. Use
`Unit` para testes isolados, `Integration` para testes que atravessam host,
filesystem, banco, container ou provider controlado, e `Architecture` para
regras estruturais. As categorias adicionais `Performance`, `Security` e
`Recovery` são reservadas aos respectivos tipos de evidência. A especificação
completa e os comandos de filtro estão em
[testing.instructions.md](instructions/testing.instructions.md).

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

1. requisitos e decisões aceitas no [plano mestre](../README.md) e ADRs;
2. [instruções globais](copilot-instructions.md);
3. instruções específicas do caminho alterado;
4. skill, prompt ou agente selecionado;
5. convenções observadas no módulo e nos testes próximos.

Em caso de conflito, preserve integridade e segurança, interrompa alterações irreversíveis e registre a decisão necessária.

## ADRs e planos

- ADRs criados por humanos ou IA vivem em `docs/adr/AAAA-NNNN-titulo.md` desde o status `proposed` e seguem [ADR_TEMPLATE.md](ADR_TEMPLATE.md).
- A sequência de ADR é anual e reinicia a cada ano civil: `AAAA-0001`, `AAAA-0002` e assim por diante.
- Critérios de carga e performance corporativos ficam em [PERFORMANCE_TESTING_CRITERIA.md](PERFORMANCE_TESTING_CRITERIA.md).
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

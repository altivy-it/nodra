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

## Spec Kit e extensões

O projeto mantém Spec Kit `1.0.13` com integração Copilot em modo skills e
scripts PowerShell. As extensões `assess`, `bug` e `git` estão em `1.0.1`,
instaladas pelo CLI a partir das fontes locais do Inventory360Api. A adaptação
usa os mesmos comandos e modelos da base, ajustando exemplos à stack e aos
diretórios do AltivyNotes. Preserva a constituição vigente e as specs existentes;
não importa decisões de arquitetura ou automações de produto do projeto de referência.

| Extensão | Comandos no chat | Artefatos e limites |
| --- | --- | --- |
| `assess` | `/speckit-assess-intake`, `/speckit-assess-research`, `/speckit-assess-define`, `/speckit-assess-shape`, `/speckit-assess-decide` | descoberta em `.specify/assessments/<slug>/`; `go` encaminha para especificação, não aprova implementação |
| `bug` | `/speckit-bug-assess`, `/speckit-bug-fix`, `/speckit-bug-test` | avaliação, correção autorizada e verificação em `.specify/bugs/<slug>/` |
| `git` | `/speckit-git-feature`, `/speckit-git-validate`, `/speckit-git-remote`, `/speckit-git-initialize`, `/speckit-git-commit` | operações Git de apoio, inclusive hooks configurados do Spec Kit; não autorizam execução de tarefas de produto |

### Hooks e autorização

- `.specify/extensions.yml` usa a mesma configuração do Inventory360Api: `auto_execute_hooks: true` e todos os 18 hooks Git com `enabled: true`.
- `.specify/extensions/git/git-config.yml` mantém `auto_commit.default: false` e as sobrescritas por evento desativadas.
- Os hooks obrigatórios inicializam o Git antes da constituição e criam a branch antes da especificação; os hooks de commit são opcionais. O script de commit respeita `git-config.yml`, cujo estado é o mesmo da base. Invocar o fluxo Spec Kit autoriza seus hooks configurados, não push, publicação ou deploy. As mensagens seguem [Conventional Commits](COMMIT_CONVENTIONS.md).
- Um veredito `go`, uma branch ou uma tarefa marcada não substitui revisão e autorização humanas dos artefatos Spec Kit.

### Localização pt-BR

Os comandos centrais e das extensões usam as mesmas definições localizadas
do Inventory360Api. O [prompt localize-speckit](prompts/localize-speckit.prompt.md)
segue o mesmo procedimento da base, adaptado ao nome e à documentação do AltivyNotes:

```text
/localize-speckit extensão=assess modo=auditar
/localize-speckit extensão=assess modo=traduzir
```

As regras de idioma, preservação de contratos e auditoria após atualização
são as da referência. Não há mecanismo adicional de tradução ou restrição de
execução exclusiva do AltivyNotes. Preserve a constituição, as specs e o
conteúdo específico deste projeto.

### Manutenção e validação

Na raiz do AltivyNotes, com Git e `uv` disponíveis:

```powershell
uv tool run --from git+https://github.com/github/spec-kit.git@v1.0.13 specify extension list
uv tool run --from git+https://github.com/github/spec-kit.git@v1.0.13 specify extension add --help
```

O primeiro comando lista extensões registradas sem executar suas skills;
o segundo consulta as opções da versão adotada. A referência está fixada em
`v1.0.13`; `uv` pode acessar a rede para preparar o ambiente temporário.
Instalações/atualizações exigem revisão do pacote e do diff; não copie registries
manualmente nem use `--force` sem conferir personalizações locais.

Depois de atualizar, confira versões, hashes dos manifests, skills materializadas,
aliases, YAML, links e a paridade dos hooks e da configuração Git com a base. Compare hashes
de constituição, templates e specs protegidos. Não execute fluxos de aplicação
apenas para testar o texto de uma skill.

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

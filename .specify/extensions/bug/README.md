# Extensão Spec Kit Bug Fixing

Fluxo de três etapas para avaliar, corrigir e validar defeitos. Cada defeito
ocupa seu próprio diretório em `.specify/bugs/<slug>/`, com um relatório
Markdown por etapa. Todos os relatórios devem ser escritos em pt-BR; preserve
nomes de comandos, slugs, estados e caminhos literais.

## Visão geral

1. **Avaliar**: ler a descrição do defeito, localizar caminhos de código e
  propor uma correção com base em evidências.
2. **Corrigir**: aplicar a remediação avaliada e registrar as alterações.
3. **Testar**: repetir a reprodução e executar os testes, registrando o resultado.

As etapas compartilham estes relatórios:

```text
.specify/bugs/<slug>/
├── assessment.md   # speckit.bug.assess
├── fix.md          # speckit.bug.fix
└── test.md         # speckit.bug.test
```

## Comandos

| Comando | Descrição | Saída |
| --- | --- | --- |
| `speckit.bug.assess` | Avalia um defeito descrito em texto ou URL. | `.specify/bugs/<slug>/assessment.md` |
| `speckit.bug.fix` | Aplica a correção proposta na avaliação. | `.specify/bugs/<slug>/fix.md` |
| `speckit.bug.test` | Valida a correção e registra as evidências. | `.specify/bugs/<slug>/test.md` |

## Convenções de slug

O *slug* é o nome do diretório do defeito e a referência compartilhada pelas
três etapas.

- **Fornecido pelo usuário**: normalize para minúsculas e kebab-case, sem
  acrescentar timestamp ou número quando não houver conflito.
- **Modo interativo**: se não houver slug, pergunte ao usuário e sugira um
  derivado do resumo do defeito.
- **Modo automatizado**: gere um slug curto e único. Se o diretório já existir,
  acrescente o menor sufixo que o torne único. Nunca sobrescreva um diretório.

## Instalação

Execute no CLI após inicializar o Spec Kit:

```bash
specify extension add bug
```

## Desativação

```bash
specify extension disable bug
specify extension enable bug
```

## Fluxo típico

```text
/speckit-bug-assess "Descreva o sintoma e a reprodução observada" slug=slug-do-defeito
/speckit-bug-fix slug=slug-do-defeito
/speckit-bug-test slug=slug-do-defeito
```

## Guardrails

- `speckit.bug.assess` e `speckit.bug.test` não alteram código-fonte. Leem o
  repositório e escrevem somente em `.specify/bugs/<slug>/`.
- `speckit.bug.fix` altera código apenas nos arquivos delimitados pela avaliação,
  salvo nova evidência registrada em `fix.md`.
- Nenhum comando sobrescreve relatórios existentes sem confirmação explícita.
- Não declare `verified` sem executar a reprodução ou sua equivalência
  automatizada; use os estados `partial` ou `not-run` quando apropriado.
- Preserve nos relatórios os títulos de seção canônicos em inglês quando
  necessários para comunicação entre etapas, acrescentando a tradução em pt-BR.

## Hooks

Esta extensão não registra hooks; as três etapas são invocadas explicitamente.

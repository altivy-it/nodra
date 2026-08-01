# Convenção de commits

Todo commit do Thothis deve seguir a especificação [Conventional Commits 1.0.0](https://www.conventionalcommits.org/pt-br/v1.0.0/). A convenção torna o histórico legível, permite gerar changelogs e comunica o impacto semântico de cada mudança.

## Formato

```text
<tipo>[escopo opcional][!]: <descrição>

[corpo opcional]

[rodapés opcionais]
```

Exemplo completo:

```text
feat(api)!: exige ETag na atualização de notas

Impede gravações concorrentes sem precondição e retorna HTTP 428 quando
If-Match não é informado.

BREAKING CHANGE: clientes devem enviar If-Match em toda atualização de nota.
Closes: #123
```

## Tipos permitidos

Esta tabela é a lista completa de tipos aceitos no repositório. Uma necessidade que não se encaixe nela deve usar o tipo mais próximo ou propor a alteração desta convenção no mesmo pull request.

| Tipo | Use quando | Exemplo | Efeito SemVer padrão |
| --- | --- | --- | --- |
| `feat` | adiciona ou amplia comportamento observável pelo usuário ou consumidor | `feat(web): adiciona notas fixadas` | minor |
| `fix` | corrige defeito, regressão, vulnerabilidade ou comportamento incorreto | `fix(api): rejeita path fora do vault` | patch |
| `docs` | altera somente documentação | `docs(backend): explica reconciliação do vault` | nenhum |
| `style` | altera somente formatação, espaços ou estilo sem mudar comportamento | `style(web): aplica formatação aos componentes` | nenhum |
| `refactor` | reorganiza código sem corrigir defeito nem adicionar comportamento | `refactor(worker): separa leitura e indexação` | nenhum |
| `perf` | melhora desempenho sem alterar o contrato funcional | `perf(db): reduz consultas da busca paginada` | patch |
| `test` | adiciona ou corrige testes sem alterar código de produção | `test(api): cobre conflito de ETag` | nenhum |
| `build` | altera dependências, ferramentas de build, empacotamento ou imagens base | `build(deps): atualiza Npgsql para 10.0.1` | nenhum |
| `ci` | altera pipelines, workflows, gates ou automação de CI/CD | `ci(actions): adiciona scan de vulnerabilidades` | nenhum |
| `chore` | executa manutenção que não cabe nos demais tipos e não altera produto | `chore(repo): remove configuração obsoleta` | nenhum |
| `revert` | reverte formalmente um commit anterior | `revert: reverte "feat(web): adiciona notas fixadas"` | depende do commit revertido |

`feat` e `fix` seguem a relação da especificação com SemVer. Qualquer tipo pode representar uma mudança major quando usar `!` e o rodapé `BREAKING CHANGE:`.

## Escopos permitidos

O escopo é opcional, mas recomendado quando identifica claramente a área afetada. Use somente um escopo por commit; se vários forem igualmente necessários, o commit provavelmente deve ser dividido ou usar `repo`.

| Escopo | Área |
| --- | --- |
| `web` | aplicação Angular em `apps/web` |
| `api` | host HTTP e SignalR em `apps/api` |
| `worker` | jobs e reconciliação em `apps/worker` |
| `frontend` | bibliotecas compartilhadas em `libs/frontend` |
| `backend` | bibliotecas compartilhadas em `libs/backend` |
| `contracts` | OpenAPI, schemas, DTOs públicos e clientes gerados |
| `db` | PostgreSQL, EF Core, migrations, índices e SQL |
| `deploy` | containers, Compose, proxy e operação |
| `deps` | atualização de dependências ou imagens base |
| `actions` | dependências e integrações do GitHub Actions |
| `security` | autenticação, autorização, secrets e correções de segurança |
| `observability` | logs, métricas, traces, health checks e alertas |
| `docs` | documentação transversal |
| `tooling` | scripts e ferramentas locais de engenharia |
| `repo` | configuração ou manutenção transversal do repositório |

Escopos mais específicos podem ser adicionados a esta tabela quando se tornarem uma fronteira estável do monorepo. Não invente um escopo apenas para repetir o tipo, como `docs(docs)` ou `test(tests)`.

## Descrição

- Escreva em português brasileiro, no imperativo e sem ponto final.
- Comece com letra minúscula após os dois-pontos.
- Descreva o resultado da mudança, não o ato de editar arquivos.
- Mantenha a primeira linha curta, preferencialmente com até 72 caracteres.
- Não use mensagens genéricas como `ajustes`, `alterações`, `update` ou `WIP`.

Correto:

```text
fix(worker): preserva nota quando a indexação falhar
```

Incorreto:

```text
Atualizações diversas
fix: corrigido bug.
```

## Corpo e rodapés

Use o corpo quando a motivação, o comportamento anterior ou uma decisão não forem evidentes. Separe-o do cabeçalho por uma linha em branco e prefira linhas com até 100 caracteres.

| Rodapé | Quando usar | Exemplo |
| --- | --- | --- |
| `BREAKING CHANGE:` | obrigatório para mudança incompatível, junto com `!` no cabeçalho | `BREAKING CHANGE: o endpoint agora exige If-Match.` |
| `Closes:` | fecha uma issue quando o commit chegar à branch padrão | `Closes: #123` |
| `Fixes:` | fecha uma issue associada à correção | `Fixes: #456` |
| `Refs:` | apenas relaciona issue, ADR ou pull request | `Refs: #789` |
| `Co-authored-by:` | credita coautoria real | `Co-authored-by: Nome <email@example.com>` |
| `Signed-off-by:` | registra sign-off quando exigido pelo processo | `Signed-off-by: Nome <email@example.com>` |

Rodapés adicionais são permitidos quando seguem o formato de token do Conventional Commits e têm significado reconhecido pelo processo do projeto.

## Mudanças incompatíveis

Uma mudança incompatível deve:

1. acrescentar `!` imediatamente antes dos dois-pontos;
2. incluir `BREAKING CHANGE:` com impacto e migração no rodapé;
3. possuir issue e ADR quando aplicável;
4. atualizar documentação, contrato e notas de release no mesmo change set.

```text
feat(contracts)!: substitui cursor textual por token opaco

BREAKING CHANGE: clientes devem tratar nextCursor como valor opaco e não analisá-lo.
Refs: #321
```

## Commits automáticos

Copilot, Dependabot e outras automações obedecem à mesma convenção. Antes de criar um commit, o agente deve revisar a mensagem final, não incluir secrets e não executar commit, push ou deploy sem solicitação explícita.

Exemplos:

```text
build(deps): atualiza Angular para 22.0.0
ci(actions): atualiza checkout para v7
chore(repo): sincroniza arquivos gerados
```

Quando uma ferramenta gerar uma mensagem fora desta convenção, ajuste-a antes do merge ou configure a ferramenta para produzir o formato correto.

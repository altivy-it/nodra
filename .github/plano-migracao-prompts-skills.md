# AltivyNotes — plano de migração de prompts para skills

| Campo | Valor |
| --- | --- |
| Estado | artefatos e tarefas aprovados; implementação iniciada, validação pendente |
| Origem | IA assistida |
| Revisor humano | solicitante desta conversa; decisões de escopo confirmadas em 2026-10-22 após a solicitação das 17:22 (-03:00) |
| Criado em / atualizado em | 2026-10-22 |
| Projeto | `/home/u_zocatel/projetos/AltivyNotes` |
| Escopo | quatro prompts locais; nenhuma alteração no produto |

Este documento é a origem histórica da
[feature Spec Kit 009](../specs/009-migracao-prompts-skills/spec.md).
O [desenho](../specs/009-migracao-prompts-skills/plan.md) e as
[tarefas](../specs/009-migracao-prompts-skills/tasks.md) são os artefatos
operacionais aprovados pelo solicitante nesta conversa em 2026-10-22 às
17:32 (-03:00), ao invocar `/speckit-implement` com “aprovado, registre e siga
com a implementação”. Essa autorização não dispensa os gates de validação.
Não retomar `docs/plan`, congelado.

## Decisões humanas e formalização

Em 2026-10-22, após a solicitação das 17:22 (-03:00), o solicitante escolheu:

- formalizar em Spec Kit agora e apresentar tarefas para aprovação antes da implementação;
- aprovar os quatro nomes `altivy-notes-*` e migrar sem pontes Local;
- permitir validações estreitas na revisão somente com autorização explícita
  e ferramentas permitidas, selecionando os agentes correspondentes
  explicitamente quando disponíveis.

As decisões estão registradas na
[pesquisa da feature](../specs/009-migracao-prompts-skills/research.md).
Não houve criação das quatro skills, remoção de prompts ou execução de testes
interativos. A alteração preexistente em `.vscode/mcp.json` foi preservada.

## Compatibilidade e fontes

O [guia oficial de migração](https://code.visualstudio.com/docs/agent-customization/migrate-customizations#_convert-prompt-files-to-skills),
consultado em 2026-10-22, informa que o harness Copilot não carrega
`*.prompt.md`. Um prompt mínimo pode apontar para a skill no Local, sem
duplicar procedimentos, mas o encaminhamento precisa ser validado e não
oferece compatibilidade no Copilot. Recomenda-se migração completa.

A conversão não preserva seleção de `agent`, `model` ou `tools`. Esses
contratos precisam de tratamento explícito, não de cópia para campos de
skill presumidos equivalentes. Consultar o
[formato suportado de skills](https://code.visualstudio.com/docs/agent-customization/agent-skills).

## Inventário e destino proposto

Os caminhos são relativos ao projeto; os destinos são propostos, não criados.

| Prompt atual | Skill proposta | Ação |
| --- | --- | --- |
| `.github/prompts/create-adr.prompt.md` | `.github/skills/altivy-notes-create-adr/SKILL.md` | migrar auxílio à redação de ADR |
| `.github/prompts/generate-tests.prompt.md` | `.github/skills/altivy-notes-generate-tests/SKILL.md` | migrar apoio a testes de tarefa oficial |
| `.github/prompts/review-change.prompt.md` | `.github/skills/altivy-notes-review-change/SKILL.md` | migrar revisão com vínculo a spec/tarefa |
| `.github/prompts/localize-speckit.prompt.md` | `.github/skills/altivy-notes-localize-speckit/SKILL.md` | migrar auditoria/tradução segura do Spec Kit |

Resultado: **quatro skills locais novas**, todas inicialmente explícitas.
Os prefixos evitam colisões com Dokpod, que possui prompts com os mesmos nomes.
As 25 skills `speckit-*` existentes não são equivalentes destes quatro
auxiliares e não serão convertidas, renomeadas ou regeneradas.

O prompt de localização não consta no manifesto Copilot consultado: é um
procedimento local que opera componentes gerenciados, não um arquivo gerado
que deva ser alterado na origem de uma extensão.

## Trabalho e revisão por fluxo

### A1 — ADR

- Preservar contexto, decisão e restrições; carregar o plano/tarefa pertinente
  e `docs/plano-mestre.md` sem duplicar essas fontes.
- Preservar papel do `Altivy Solution Architect`; revisar como selecioná-lo
  no harness destino.
- Referenciar `.github/ADR_TEMPLATE.md` e instruções arquiteturais.
- Manter `docs/adr/AAAA-NNNN-titulo.md`, sequência anual e status `proposed`.
- Decisões locais reversíveis continuam em `plan.md`/`research.md`.
- Não transformar o auxiliar em uma alternativa ao `/speckit-plan`.

### A2 — Testes

- Preservar caminho do artefato Spec Kit e ID da tarefa como entradas.
- Preservar papel do `Altivy Quality Engineer`, escopo e critérios da tarefa.
- Não gerar trabalho adicional por sugestão automática da skill.
- Usar instruções e comandos existentes do projeto; registrar teste focado,
  resultado e limites, respeitando execução containerizada.

### A3 — Revisão

- Preservar diff, branch, PR ou escopo e a rastreabilidade à spec/tarefa.
- Preservar papel `Altivy Code Reviewer`; confirmar ferramentas disponíveis.
- O prompt admite validações estreitas: separar inspeção sem escrita de
  execução de testes, que pode gerar artefatos. Não chamar ambas de read-only
  sem ressalvas.
- Definir no contrato de execução quando uma validação é permitida; nunca
  ampliar ferramentas de um agente restrito só para manter um comando legado.
- Garantir que achados, evidências e lacunas de teste continuam cobertos.

### A4 — Localização

- Preservar `extensão`, `arquivos`, `modo` e exclusões como orientações em
  linguagem natural.
- Sem modo explícito, somente auditar; sem alvo, apresentar inventário.
- Preservar contratos, IDs, placeholders, marcadores, aliases e sequência.
- Não tratar override como tradução automática de skill materializada.
- Manter exclusões, hashes, conteúdo do usuário e avisos de drift.
- Não alterar `.specify/extensions/bug/README.md`; usar a tradução existente
  quando aplicável. Não instalar/reconciliar extensões para testar a migração.

## Arquivos afetados

| Arquivo ou conjunto | Tratamento futuro |
| --- | --- |
| quatro prompts da tabela | retirar depois dos testes ou manter apenas ponte Local aprovada |
| quatro novos destinos da tabela | criar procedimento canônico sem cópia de instruções/templates |
| `.github/README.md` | atualizar inventário e referências |
| `.github/COPILOT_GUIDE.md` | atualizar comandos, agentes e exemplos |
| `.github/copilot-instructions.md` | atualizar link para localização e demais referências pertinentes |
| `.github/instructions/documentation.instructions.md` | revisar referência literal a `/create-adr` diante do novo nome |
| `.github/GOVERNANCE.md` | conferir coerência; não mudar processo, autoridade ou status histórico |
| `.github/agents/` | confirmar agentes correspondentes aos três nomes de exibição e seus contratos; não editar indiscriminadamente |

Recalcular links relativos a partir da nova profundidade de cada `SKILL.md`.
Manter o plano mestre e o template de ADR em seus módulos proprietários.
Buscar referências remanescentes em documentação e configurações antes de
remover os prompts; não alterar arquivos sem relação concreta.

**Preservar:** alteração preexistente em `.vscode/mcp.json`, componentes
gerenciados `.specify`, skills `speckit-*`, constituição, planos históricos,
código, testes da aplicação, vaults e secrets. Não migrar MCP neste escopo.

## Sequência e critérios de aceite

1. Revisão humana de nomes, fronteiras de revisão e política de compatibilidade.
2. Formalização em Spec Kit e autorização explícita das tarefas.
3. Registro da referência anterior; implementação independente de A1 a A4.
4. Atualização de referências e comparação semântica, fluxo por fluxo.
5. Testes de descoberta/argumentos/restrições no Copilot, com raiz AltivyNotes
   selecionada e também com Dokpod presente na workspace.
6. Remoção dos prompts somente após evidência e revisão do diff.

Aceite:

- quatro fluxos cobertos e quatro skills com nome igual ao diretório;
- `user-invocable: true` e `disable-model-invocation: true` nas novas skills;
- não há seleção implícita de agente/modelo/tools por campos não suportados;
- ADR sem justificativa não é criado; ADR nunca é aceito automaticamente;
- testes sem referência autorizada não iniciam alteração de produto;
- revisão não edita código, e execução de validação respeita o contrato
  explicitamente decidido;
- localização sem modo não traduz; literais técnicos permanecem intactos;
- nenhum componente gerenciado ou alteração preexistente foi sobrescrito;
- zero referências ativas a prompts excluídos e nenhum procedimento duplicado.

## Decisões originalmente pendentes, recuperação e limites

- Aprovar os quatro nomes prefixados.
- Confirmar se o suporte ao Local justifica alguma ponte; padrão proposto:
  migração completa.
- Decidir contrato de execução de validações pela revisão e seleção dos agentes.
- Designar revisor e autorizar tarefas no fluxo oficial.

Os três primeiros pontos foram decididos na formalização acima. O solicitante
é o revisor humano e aprovou os artefatos e T001–T019 às 17:32 (-03:00);
os testes e a retirada das fontes continuam sujeitos aos gates da feature.

Se falhar qualquer gate, preservar a fonte original e corrigir o recorte antes
da retirada. Recuperação por diff revisado, sem reset ou regeneração ampla.
A entrega original adicionou somente este plano. A formalização posterior
produziu os artefatos Spec Kit vinculados acima, sem executar a migração.
Descoberta e execução reais das skills propostas: **NOT RUN**.

# Contratos dos quatro auxiliares

Todos os nomes abaixo são skills explícitas: `user-invocable: true`,
`disable-model-invocation: true`, `name` igual ao diretório. Entradas são
orientações em linguagem natural, não flags de uma CLI nem interpolação
automática. Não há seleção implícita de agente/modelo/tools pelo frontmatter.

| Nome | Entradas | Agente a selecionar explicitamente | Saída e limite |
| --- | --- | --- | --- |
| `altivy-notes-create-adr` | decisão, contexto, restrições e plano/tarefa pertinente | `Altivy Solution Architect` | ADR justificado em `docs/adr/AAAA-NNNN-titulo.md`, conforme template, `proposed`, origem e revisor; sem implementar, aceitar ou criar plano paralelo |
| `altivy-notes-generate-tests` | artefato oficial, ID, critérios, autorização e escopo | `Altivy Quality Engineer` | testes e matriz de risco derivados da tarefa, comandos/resultados/limites; sem inventar critérios ou alterar produção fora da tarefa |
| `altivy-notes-review-change` | diff/branch/PR/escopo e referência oficial | `Altivy Code Reviewer` | achados priorizados com localização/cenário/evidência, dúvidas, lacunas e risco residual; sem editar código |
| `altivy-notes-localize-speckit` | extensão, arquivos, modo e exclusões | não impõe agente novo | inventário sem alvo; auditoria sem modo; tradução explícita preservando contratos e exclusões |

## Pré-condições e falha fechada

- Auxiliar de ADR/testes/revisão exige vínculo oficial; testes/escrita exigem
  autorização aplicável. Falta de referência informa bloqueio antes de agir.
- ADR exige justificativa duradoura; escolha local reversível continua no
  plano/pesquisa oficial. Carregar plano mestre e instruções sem duplicá-los.
- Se o agente correspondente não estiver disponível, informar a indisponibilidade
  e solicitar escolha explícita de alternativa compatível. Não adotar um papel
  genérico silenciosamente ou ampliar suas ferramentas.
- Revisão pode executar validação estreita somente após autorização explícita
  para o comando/escopo, respeitando ferramentas e execução containerizada.
  Relatórios/caches gerados devem ser informados; execução não é puramente read-only.
- Localização preserva YAML técnico, comandos, aliases, IDs, placeholders,
  marcadores, sequência, hashes, overrides, conteúdo do usuário e exclusões.
  Não instala/reconcilia componentes nem altera `.specify/extensions/bug/README.md`.
- Nenhuma skill concede autorização de commit, push, deploy ou trabalho adicional.

## Compatibilidade e retirada

Os prompts originais permanecem até comparação e testes de cada fluxo,
incluindo invocação com AltivyNotes e Dokpod presentes. Depois do aceite
verificável do fluxo, atualizar consumidores e remover sua origem sem pontes.
Documentos históricos podem citar nomes antigos se explicitamente históricos;
referências operacionais aos prompts excluídos devem chegar a zero.

---
name: altivy-notes-review-change
description: "Use quando solicitado explicitamente para revisar diff, branch ou PR do AltivyNotes contra spec e tarefa oficiais, sem editar código; validações estreitas exigem autorização explícita e ferramentas permitidas."
argument-hint: "Diff, branch, PR ou escopo; spec e tarefa oficiais; autorização específica de validação, se houver"
user-invocable: true
disable-model-invocation: true
---

# Auxílio à revisão de mudanças do AltivyNotes

## Escopo e seleção do agente

Na raiz AltivyNotes, selecione explicitamente **Altivy Code Reviewer**,
conforme seu [contrato](../../agents/code-reviewer.agent.md), e invoque
`/altivy-notes-review-change`. O frontmatter desta skill não seleciona agente,
modelo ou ferramentas. Se o agente não estiver disponível, informe o bloqueio
e solicite escolha explícita de alternativa compatível antes de prosseguir;
não amplie permissões nem substitua o papel silenciosamente.

## Entradas e pré-condições

Receba diff completo, branch, PR ou escopo e o artefato/tarefa oficiais
associados. Leia-os junto ao [plano mestre](../../../docs/plano-mestre.md)
e às instruções aplicáveis. Se não houver vínculo oficial Spec Kit, registre
bloqueio de processo; não substitua requisitos por texto de Issue.

## Contrato de execução

- Inspeção não edita arquivos: não corrija código, testes, contratos ou
  documentação durante a revisão.
- Só execute validação estreita para confirmar risco com autorização explícita
  para o comando e seu escopo, e somente se as ferramentas do agente permitirem.
  O pedido genérico de revisão não concede essa autorização.
- Antes da execução, informe comando, finalidade e possíveis efeitos
  (relatórios, caches ou outros artefatos). Se faltar autorização, solicite-a;
  se faltar ferramenta, registre o limite sem contorná-lo ou ampliá-la.
- Respeite a [execução containerizada](../../copilot-instructions.md) e os
  comandos existentes. Não instale dependências ou inicie runtime no host
  para confirmar um achado.
- Validações executadas podem gerar artefatos; não descreva essa execução
  como puramente read-only. Relate seus efeitos e resultados separadamente
  da inspeção, sem realizar correções.

## Procedimento

1. Leia o diff completo e os call sites relevantes para entender comportamento
   anterior/novo e a tarefa que autoriza a mudança.
2. Priorize perda/exposição de conteúdo, concorrência, autorização, contratos,
   migrations, compatibilidade, desempenho e operação. Siga as restrições do
   agente, inclusive as de frontend quando o recorte envolver UI.
3. Confirme cada achado com localização, comportamento esperado, falha,
   impacto e cenário reproduzível; não trate preferência estilística como bug.
4. Verifique testes ausentes e se os existentes realmente provam o
   comportamento; examine erro, rollback e observabilidade.
5. Se validação estreita for necessária, aplique primeiro o contrato de
   execução acima; se não puder executar, explicite a lacuna.

## Entrega e limites

Apresente achados primeiro, por severidade conforme o agente, com evidências,
localização, cenário e correção recomendada. Depois informe dúvidas, lacunas
de teste e resumo curto. Sem achados, declare isso e registre o risco residual.
Distinga resultados observados, inferências e validações não executadas.

Este auxiliar não autoriza correções, commit, push, publicação ou deploy.

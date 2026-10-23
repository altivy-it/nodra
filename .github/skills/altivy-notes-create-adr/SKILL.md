---
name: altivy-notes-create-adr
description: "Use quando solicitado explicitamente para analisar uma decisão arquitetural do AltivyNotes e redigir um ADR justificado dentro de uma tarefa Spec Kit ou de /speckit-plan, sem implementar ou aceitar a decisão."
argument-hint: "Plano ou tarefa Spec Kit; decisão arquitetural, contexto, restrições e revisor humano"
user-invocable: true
disable-model-invocation: true
---

# Auxílio à redação de ADR do AltivyNotes

## Escopo e seleção do agente

Este auxiliar só apoia uma tarefa Spec Kit existente ou uma decisão identificada
por `/speckit-plan`. Não cria requisitos, specs, planos ou tarefas e não autoriza
implementação.

Na raiz AltivyNotes, selecione explicitamente **Altivy Solution Architect**,
conforme seu [contrato](../../agents/solution-architect.agent.md), e invoque
`/altivy-notes-create-adr` com o contexto. O frontmatter desta skill não seleciona
agente, modelo ou ferramentas. Se o agente não estiver disponível, informe o
bloqueio e solicite escolha explícita de alternativa compatível antes de
prosseguir; não adote um agente genérico silenciosamente nem amplie permissões.

## Entradas e pré-condições

- Plano/tarefa oficial pertinente e a decisão que exige registro.
- Contexto, restrições e justificativa do efeito duradouro ou transversal.
- Autorização de redação e revisor humano, quando informado.

Sem vínculo oficial ou autorização aplicável, pare e explicite a pendência.
Consulte o [plano mestre](../../../docs/plano-mestre.md), o plano/tarefa,
as [instruções arquiteturais](../../instructions/architecture.instructions.md),
as [regras de documentação](../../instructions/documentation.instructions.md)
e o [template de ADR](../../ADR_TEMPLATE.md), sem duplicar essas fontes.

## Procedimento

1. Confirme se a decisão exige ADR e localize decisões relacionadas. Crie-o
   somente se o efeito ultrapassar a feature, estabelecer uma regra global,
   alterar fronteira/contrato relevante ou exigir registro por segurança,
   operação, compatibilidade ou migração. Escolha local reversível permanece
   em `plan.md`/`research.md`; explique por que não criar um ADR nesse caso.
2. Compare no máximo três opções plausíveis por integridade, segurança,
   desempenho, operação, custo e reversibilidade, respeitando o contrato do agente.
3. Recomende a opção mais simples que atenda aos requisitos com critérios
   falsificáveis, premissas e consequências explícitos.
4. Se justificado e autorizado, crie em `docs/adr/AAAA-NNNN-titulo.md` conforme
   o template. Use o próximo número anual disponível, reiniciando a sequência
   por ano, status `proposed`, origem `IA assistida` e revisor humano `pendente`
   quando não informado. Não sobrescreva arquivo existente.
5. Nunca marque o ADR como `accepted`: isso exige decisão humana explícita.
   Não implemente a decisão nem edite código ou contratos neste auxiliar.

## Entrega e limites

Informe justificativa para criar ou não criar o ADR, alternativas, recomendação,
referências à tarefa e às decisões relacionadas, arquivo produzido quando
aplicável, riscos e validação capaz de refutar a decisão.

Não faça commit, push, publicação ou deploy sem solicitação específica.

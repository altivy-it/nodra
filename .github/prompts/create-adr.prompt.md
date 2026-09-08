---
name: "Criar ADR"
description: "Analisa uma decisão arquitetural do Altivy e cria um ADR completo quando justificado."
argument-hint: "Decisão arquitetural, contexto e restrições"
agent: "Altivy Solution Architect"
---

Analise a decisão informada à luz do [plano mestre](../../README.md), das instruções arquiteturais e do [template de ADR](../ADR_TEMPLATE.md).

1. Confirme se a decisão exige ADR e localize decisões relacionadas.
2. Compare opções plausíveis por integridade, segurança, desempenho, operação, custo e reversibilidade.
3. Recomende uma opção com critérios falsificáveis.
4. Crie o ADR em `docs/adr/AAAA-NNNN-titulo.md` seguindo o template; use o próximo número sequencial anual disponível (reinicia por ano), status `proposed`, origem `IA assistida` e revisor humano `pendente` quando não informado.
5. Nunca marque o ADR como `accepted`; aprovação é decisão humana explícita.
6. Não implemente a decisão neste prompt.
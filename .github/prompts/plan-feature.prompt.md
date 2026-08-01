---
name: "Planejar Feature"
description: "Converte uma necessidade de produto em plano vertical executável para frontend, backend, contratos, dados, testes e operação."
argument-hint: "Necessidade, usuário e resultado esperado"
agent: "Thothis Delivery Lead"
---

Transforme a necessidade em um plano de entrega e crie ou atualize `docs/plan/<tema>.md` conforme [PLAN_TEMPLATE.md](../PLAN_TEMPLATE.md).

- use status geral `draft` quando houver pergunta bloqueante e `proposed` quando o plano estiver pronto para revisão humana;
- registre origem `IA assistida` e revisor humano `pendente` quando não informado;
- identifique persona, problema, resultado e não escopo;
- escreva critérios de aceite observáveis, incluindo erro, conflito e autorização;
- identifique módulos, contratos, migrations e riscos afetados;
- declare se um ADR é necessário;
- ordene slices verticais pequenos com dependências explícitas;
- atribua status `not-started` a cada etapa nova e mantenha um histórico para toda transição;
- mapeie testes unitários, integração, contrato e E2E;
- inclua segurança, observabilidade, rollout e rollback;
- destaque perguntas realmente bloqueantes e premissas adotadas.

Não marque plano como `approved` nem etapa como `completed` ou `skipped` sem aprovação ou evidência humana explícita. Use uma checklist curta o suficiente para orientar implementação e review.
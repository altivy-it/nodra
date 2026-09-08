---
name: "Documentação e ADR"
description: "Use ao criar ou alterar README, documentação técnica, runbook, ADR, guia de contribuição, changelog ou documentação de API."
applyTo: "**/*.md, docs/**, contracts/**/*.yaml, contracts/**/*.json"
---

# Documentação

- Escreva em português brasileiro, com termos técnicos consistentes e exemplos executáveis.
- O plano mestre em `docs/plano-mestre.md` é a autoridade para visão, escopo e decisões globais; o README raiz é a entrada pública do projeto.
- Documentos de frontend, backend e evolução detalham implementação sem duplicar o plano mestre.
- ADRs vivem em `docs/adr` desde a criação, usam `AAAA-NNNN-titulo.md` (ano e sequência anual), e status `proposed`, `accepted`, `superseded` ou `deprecated`.
- Planos vivem em `docs/plan`, usam `<tema>.md` em kebab-case e seguem `.github/PLAN_TEMPLATE.md`.
- Todo plano declara status geral `draft`, `proposed`, `approved`, `in-progress`, `blocked`, `completed`, `cancelled` ou `superseded`.
- Cada etapa usa identificador `P-NN` ou `WP-NN` no heading e declara status próprio `not-started`, `in-progress`, `blocked`, `completed` ou `skipped`; planos sem etapa ou status por etapa são inválidos.
- Humanos e IA criam ADRs e planos nos mesmos locais e formatos. IA mantém `proposed` em ADR e no máximo `proposed` em plano até aprovação humana explícita.
- Status `accepted`, `approved`, `completed` e `skipped` exigem decisão ou evidência verificável registrada no documento, issue ou pull request; IA não presume aprovação nem conclusão.
- Runbooks vivem em `docs/runbooks` e incluem sinais, diagnóstico, mitigação, recuperação e escalonamento.
- Comandos informam diretório, pré-requisitos, resultado esperado e impacto.
- Exemplos usam tags imutáveis e valores fictícios; nunca inclua secret real.
- Atualize documentação no mesmo change set de uma alteração de contrato ou operação.
- Links locais devem resolver e headings devem ser únicos e estáveis.
- Registre limitações, riscos e validações não executadas; não declare garantia sem evidência.
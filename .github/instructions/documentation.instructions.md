---
name: "Documentação e ADR"
description: "Use ao criar ou alterar README, documentação técnica, runbook, ADR, guia de contribuição, changelog ou documentação de API."
applyTo: "**/*.md, docs/**, contracts/**/*.yaml, contracts/**/*.json"
---

# Documentação

- Escreva em português brasileiro, com termos técnicos consistentes e exemplos executáveis.
- O README raiz é a autoridade para visão, escopo e decisões globais.
- Documentos de frontend, backend e evolução detalham implementação sem duplicar o plano mestre.
- ADRs vivem em `docs/adr`, usam numeração sequencial e status `proposed`, `accepted`, `superseded` ou `deprecated`.
- Runbooks vivem em `docs/runbooks` e incluem sinais, diagnóstico, mitigação, recuperação e escalonamento.
- Comandos informam diretório, pré-requisitos, resultado esperado e impacto.
- Exemplos usam tags imutáveis e valores fictícios; nunca inclua secret real.
- Atualize documentação no mesmo change set de uma alteração de contrato ou operação.
- Links locais devem resolver e headings devem ser únicos e estáveis.
- Registre limitações, riscos e validações não executadas; não declare garantia sem evidência.
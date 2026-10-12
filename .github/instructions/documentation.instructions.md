---
name: "Documentação e ADR"
description: "Use ao criar ou alterar README, documentação técnica, runbook, ADR, guia de contribuição, changelog ou documentação de API."
applyTo: "**/*.md, docs/**, contracts/**/*.yaml, contracts/**/*.json"
---

# Documentação

- Escreva em português brasileiro, com termos técnicos consistentes e exemplos executáveis.
- Esta regra inclui `spec.md`, `plan.md`, `tasks.md`, checklists, artefatos auxiliares de desenho e relatórios da extensão Spec Kit Bug Fixing; preserve apenas tokens e rótulos literais necessários à compatibilidade das ferramentas.
- Para qualquer mudança da aplicação, Spec Kit é o único processo de requisitos, planejamento e execução; instruções, agentes e prompts locais são subordinados e não podem estabelecer workflows ou planos alternativos.
- O plano mestre em `docs/plano-mestre.md` é a autoridade para visão e escopo globais; documentos de domínio detalham contexto. ADRs aceitos registram decisões arquiteturais. Esses artefatos não substituem os requisitos específicos de uma feature.
- Para features, mantenha requisitos e cenários em `specs/<feature>/spec.md`, desenho técnico em `specs/<feature>/plan.md` e tarefas em `specs/<feature>/tasks.md`, conforme o fluxo Spec Kit. Não replique esses artefatos em `docs/plan`.
- Crie ADR somente para decisões arquiteturais duradouras ou transversais, como mudança de fronteira, contrato, segurança, fonte canônica, persistência ou operação. Decisões locais e reversíveis da feature ficam em `plan.md`/`research.md`; ADR é registro da decisão, não uma segunda spec ou plano.
- Para defeitos, use a extensão oficial Spec Kit Bug Fixing e mantenha o diagnóstico, correção e validação nos artefatos oficiais da extensão.
- A constituição em `.specify/memory/constitution.md` registra princípios duradouros de engenharia e não duplica instruções operacionais de `.github/`.
- O README raiz é a entrada pública do projeto.
- Documentos de frontend, backend e evolução detalham implementação sem duplicar o plano mestre.
- ADRs vivem em `docs/adr` desde a criação, usam `AAAA-NNNN-titulo.md` (ano e sequência anual), e status `proposed`, `accepted`, `superseded` ou `deprecated`.
- `docs/plan` e `.github/PLAN_TEMPLATE.md` são legado congelado: não criar, atualizar ou executar esses planos. Os planos legados abertos já têm artefatos Spec Kit em rascunho; após revisão humana, arquive/substitua os originais por referências sem autoridade operacional.
- ADRs são documentos de decisão auxiliares, não planos de implementação. Use `/speckit-plan` para identificar e analisar a decisão; quando justificado, `/create-adr` apenas redige o registro dentro da tarefa Spec Kit. IA mantém o status `proposed`; somente decisão humana explícita pode torná-los `accepted`.
- Checkboxes de `specs/<feature>/tasks.md` são progresso de execução, não status de aprovação; IA pode concluí-los somente após implementação e validação verificáveis.
- Runbooks vivem em `docs/runbooks` e incluem sinais, diagnóstico, mitigação, recuperação e escalonamento.
- Comandos informam diretório, pré-requisitos, resultado esperado e impacto.
- Exemplos usam tags imutáveis e valores fictícios; nunca inclua secret real.
- Atualize documentação no mesmo change set de uma alteração de contrato ou operação.
- Links locais devem resolver e headings devem ser únicos e estáveis.
- Registre limitações, riscos e validações não executadas; não declare garantia sem evidência.
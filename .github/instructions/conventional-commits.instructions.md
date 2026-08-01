---
name: "Conventional Commits"
description: "Use ao criar, sugerir, revisar, corrigir ou explicar commits e mensagens de commit no Thothis. Exige Conventional Commits e os tipos e escopos aprovados pelo projeto."
---

# Commits

- Todo commit segue [COMMIT_CONVENTIONS.md](../COMMIT_CONVENTIONS.md) e Conventional Commits 1.0.0.
- Use o formato `<tipo>[escopo opcional][!]: <descrição>` e somente tipos e escopos listados na referência.
- Escreva a descrição em português brasileiro, no imperativo, iniciando com minúscula e sem ponto final.
- Mantenha cada commit coeso, com propósito único e sem arquivos não relacionados.
- Mudança incompatível usa `!` e inclui o rodapé `BREAKING CHANGE:` com impacto e migração.
- Inclua corpo quando a motivação não for evidente e rodapés para issues, coautoria ou sign-off quando aplicáveis.
- Revise a mensagem final antes de executar o commit; rejeite mensagens vagas, como `ajustes`, `update`, `mudanças` ou `WIP`.
- Nunca faça commit, push, publicação ou deploy sem solicitação explícita do usuário.

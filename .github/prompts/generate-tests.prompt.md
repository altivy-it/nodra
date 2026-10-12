---
name: "Gerar Testes"
description: "Cria evidência de teste para uma tarefa já definida em artefato oficial Spec Kit."
argument-hint: "Caminho do artefato Spec Kit e ID da tarefa"
agent: "Altivy Quality Engineer"
---

Crie testes somente para uma tarefa autorizada em `specs/<feature>/tasks.md` ou em artefato oficial de bug fixing Spec Kit. Se esse artefato não existir, pare e solicite que Spec Kit defina o escopo. Não derive nem acrescente critérios de aceite independentes.

- derive riscos e critérios exclusivamente dos artefatos Spec Kit ativos;
- escolha o nível mais barato que prova cada risco;
- siga os padrões e fixtures existentes;
- cubra happy path, limites, erro, autorização, concorrência e cancelamento quando definidos ou necessários para validar a tarefa; reporte lacunas ao Spec Kit, sem alterar requisitos;
- use PostgreSQL/filesystem reais para integração e Playwright para jornadas;
- demonstre que o teste discrimina a regressão quando viável;
- execute o teste novo e a suíte relacionada;
- não altere snapshots ou produção sem justificar.

Reporte cobertura, comandos, resultados e lacunas.
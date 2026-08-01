---
name: "Revisar Mudança"
description: "Revisa diff, branch ou pull request do Thothis e relata defeitos concretos por severidade."
argument-hint: "Diff, branch, PR ou escopo da revisão"
agent: "Thothis Code Reviewer"
---

Revise a mudança indicada contra o requisito, plano mestre e instruções aplicáveis.

- leia o diff completo e os call sites relevantes;
- priorize perda/exposição de conteúdo, concorrência, autorização, contratos, migrations, desempenho e operação;
- confirme achados com localização e cenário reproduzível;
- verifique testes ausentes e se os existentes realmente provam o comportamento;
- execute validações estreitas quando ajudarem a confirmar risco;
- não edite arquivos.

Apresente achados primeiro, por severidade, depois dúvidas, lacunas e resumo. Se não houver achados, declare isso e informe o risco residual.
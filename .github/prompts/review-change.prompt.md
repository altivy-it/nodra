---
name: "Revisar Mudança"
description: "Revisa diff, branch ou pull request do Altivy e relata defeitos concretos por severidade."
argument-hint: "Diff, branch, PR ou escopo da revisão"
agent: "Altivy Code Reviewer"
---

Revise a mudança contra a spec e a tarefa oficiais do Spec Kit, o [plano mestre](../../docs/plano-mestre.md) e as instruções aplicáveis. Se não houver artefato Spec Kit associado, registre bloqueio de processo e não substitua o requisito por texto da Issue.

- leia o diff completo e os call sites relevantes;
- priorize perda/exposição de conteúdo, concorrência, autorização, contratos, migrations, desempenho e operação;
- confirme achados com localização e cenário reproduzível;
- verifique testes ausentes e se os existentes realmente provam o comportamento;
- execute validações estreitas quando ajudarem a confirmar risco;
- não edite arquivos.

Apresente achados primeiro, por severidade, depois dúvidas, lacunas e resumo. Se não houver achados, declare isso e informe o risco residual.
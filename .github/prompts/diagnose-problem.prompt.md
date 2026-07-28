---
name: "Diagnosticar Problema"
description: "Investiga falha, regressão, teste quebrado ou comportamento inesperado do Basalt Notes com evidência antes de corrigir."
argument-hint: "Erro, comando, log, teste ou comportamento observado"
agent: "agent"
---

Diagnostique e corrija o problema informado.

1. Reproduza pelo comando ou teste mais estreito disponível.
2. Trace o fluxo até o código que decide o comportamento.
3. Declare uma hipótese falsificável e execute uma checagem barata.
4. Faça a menor correção na causa raiz.
5. Execute imediatamente a validação que falhava.
6. Adicione teste de regressão quando viável.
7. Rode verificações adjacentes proporcionais ao risco.
8. Não corrija falhas não relacionadas; registre-as separadamente.

Conclua com causa, correção, evidência e risco residual.
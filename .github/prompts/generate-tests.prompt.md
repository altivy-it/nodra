---
name: "Gerar Testes"
description: "Cria testes de regressão e comportamento para uma mudança do Basalt Notes no nível adequado."
argument-hint: "Comportamento, bug, diff ou arquivo a testar"
agent: "Basalt Quality Engineer"
---

Crie testes para o comportamento informado.

- derive riscos e critérios de aceite antes de editar;
- escolha o nível mais barato que prova cada risco;
- siga os padrões e fixtures existentes;
- inclua happy path, limites, erro, autorização, concorrência e cancelamento quando aplicável;
- use PostgreSQL/filesystem reais para integração e Playwright para jornadas;
- demonstre que o teste discrimina a regressão quando viável;
- execute o teste novo e a suíte relacionada;
- não altere snapshots ou produção sem justificar.

Reporte cobertura, comandos, resultados e lacunas.
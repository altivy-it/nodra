---
name: "Testes e Qualidade"
description: "Use ao escrever, alterar ou revisar testes unitários, integração, contrato, arquitetura, Playwright, desempenho, segurança ou recuperação."
applyTo: "tests/**, apps/**/*.spec.ts, apps/**/*.test.ts, libs/**/*.spec.ts, libs/**/*.test.ts, **/*Tests.cs"
---

# Estratégia de testes

- Teste o comportamento no menor nível que prove o requisito; não duplique a mesma asserção em todos os níveis.
- Testes são determinísticos, independentes, paralelizáveis quando seguro e sem internet.
- Use builders/fixtures com defaults válidos; deixe explícito apenas o dado relevante ao cenário.
- Não use delays fixos. Aguarde estado observável com timeout limitado.
- Não esconda falhas com retry de suíte; corrija flakiness na causa.

## Frontend

- Vitest cobre estado, adapters, componentes e sanitização.
- Playwright cobre jornadas, integrações no browser e regressões de layout críticas.
- Selecione elementos por role, label ou test ID estável; não dependa de classes CSS.
- Verifique teclado, foco, console, rede e viewports desktop/mobile nos fluxos relevantes.

## Backend

- xUnit cobre domínio e casos de uso.
- Testcontainers cobre PostgreSQL e integrações; filesystem usa diretório temporário real.
- WireMock ou servidor local controlado simula providers HTTP, não mocks de baixo nível.
- Teste concorrência, cancelamento, idempotência, retry, lease e falha entre arquivo e banco.

## Dados de teste

- `tests/fixtures` contém apenas dados sintéticos e sem secrets.
- Inclua Unicode, nomes longos, links quebrados, Markdown malicioso e grafos densos.
- Benchmarks têm baseline documentado e ambiente registrado.

## Evidência

- Um bug corrigido recebe teste que falha antes da correção quando viável.
- Registre comandos executados e limitações não verificadas.
- Não aprove mudança com testes ignorados, snapshots atualizados sem revisão ou cobertura irrelevante.
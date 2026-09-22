---
name: "Testes e Qualidade"
description: "Use ao escrever, alterar ou revisar testes unitários, integração, contrato, arquitetura, Playwright, desempenho, segurança ou recuperação."
applyTo: "backend/tests/**, frontend/tests/**, frontend/apps/**/*.spec.ts, frontend/apps/**/*.test.ts, frontend/libs/**/*.spec.ts, frontend/libs/**/*.test.ts, **/*Tests.cs"
---

# Estratégia de testes

- Teste o comportamento no menor nível que prove o requisito; não duplique a mesma asserção em todos os níveis.
- Testes são determinísticos, independentes, paralelizáveis quando seguro e sem internet.
- Use builders/fixtures com defaults válidos; deixe explícito apenas o dado relevante ao cenário.
- Não use delays fixos. Aguarde estado observável com timeout limitado.
- Não esconda falhas com retry de suíte; corrija flakiness na causa.

## Frontend

- Vitest cobre estado, adapters, componentes e sanitização.
- Testes de UI cobrem o comportamento Altivy sobre componentes PO UI (dados, estado, integração); não reteste comportamento interno já validado pela biblioteca (foco, ARIA, teclado do próprio componente), exceto no editor Markdown e no grafo de notas, que são implementações dedicadas.
- O loop de verificação visual (`frontend-visual-verification.instructions.md`) é evidência de desenvolvimento; não substitui Vitest nem Playwright como gate formal.
- Playwright cobre jornadas, integrações no browser e regressões de layout críticas.
- Selecione elementos por role, label ou test ID estável; não dependa de classes CSS.
- Verifique teclado, foco, console, rede e viewports desktop/mobile nos fluxos relevantes.

## Backend

- xUnit cobre domínio e casos de uso.
- Testcontainers cobre PostgreSQL e integrações; filesystem usa diretório temporário real.
- WireMock ou servidor local controlado simula providers HTTP, não mocks de baixo nível.
- Teste concorrência, cancelamento, idempotência, retry, lease e falha entre arquivo e banco.
- Toda classe de teste C# deve declarar `[Trait("Category", "<categoria>")]`.
- As categorias oficiais são `Unit`, `Integration`, `Architecture`, `Performance`, `Security` e `Recovery`.
- Use `Unit` para testes isolados sem processo, banco, filesystem ou rede real.
- Use `Integration` para testes que atravessam host HTTP, banco, filesystem, containers ou providers controlados.
- Use `Architecture` para regras de dependência, visibilidade, namespaces e composição estrutural.
- Use `Performance` para benchmarks ou thresholds de latência/carga; use `Security` para controles de segurança; use `Recovery` para backup, restore e falhas de recuperação.
- A categoria deve ser aplicada na classe para cobrir todos os métodos; métodos com comportamento excepcional podem adicionar traits complementares.
- Para testes unitários .NET, as bibliotecas pré-aprovadas são: `Microsoft.*`, `xunit.*`, `coverlet.collector`, `Moq`, `AutoFixture`, `AutoMoqCore`, `Bogus`, `CountryData.Bogus`, `Shouldly` e `TngTech.ArchUnitNET`.
- Bibliotecas de teste fora dessa lista exigem justificativa técnica e avaliação de licença antes da inclusão.

### Execução por categoria

Execute os testes pela trait `Category` para manter os gates explícitos:

```powershell
dotnet test Altivy.slnx --configuration Release --filter 'TestCategory=Unit|Category=Unit'
dotnet test Altivy.slnx --configuration Release --filter 'TestCategory=Integration|Category=Integration'
dotnet test Altivy.slnx --configuration Release --filter 'TestCategory=Architecture|Category=Architecture'
```

Para excluir integrações em um gate rápido:

```powershell
dotnet test Altivy.slnx --configuration Release --filter 'Category!=Integration'
```

O gate padrão do repositório usa `Category!=Integration`; testes PostgreSQL
devem ser executados pelo Compose descartável documentado no README da
infraestrutura, com `Category=Integration` e teardown obrigatório.

O nome `UnitTests` no projeto ou arquivo não substitui a trait. A categoria é a
fonte de verdade para seleção; cada nova classe deve ser categorizada antes de
ser adicionada à solução.

## Dados de teste

- `tests/fixtures` contém apenas dados sintéticos e sem secrets.
- Inclua Unicode, nomes longos, links quebrados, Markdown malicioso e grafos densos.
- Benchmarks têm baseline documentado e ambiente registrado.

## Carga e performance

- Siga os critérios corporativos definidos em `./.github/PERFORMANCE_TESTING_CRITERIA.md`.
- PRs devem usar ao menos smoke de carga com thresholds de latência e erro.
- Releases devem executar carga sustentada e publicar artefatos comparáveis entre execuções.
- Use p95 e p99 como métricas primárias de latência; média isolada não é evidência suficiente.
- Em regressões acima dos limites definidos, bloqueie promoção até decisão explícita de risco.

## Evidência

- Um bug corrigido recebe teste que falha antes da correção quando viável.
- Registre comandos executados e limitações não verificadas.
- Não aprove mudança com testes ignorados, snapshots atualizados sem revisão ou cobertura irrelevante.
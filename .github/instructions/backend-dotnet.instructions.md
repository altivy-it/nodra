---
name: "Backend .NET"
description: "Use ao criar ou alterar C#, .NET 10, ASP.NET Core, API, worker, domínio, aplicação, infraestrutura, SignalR ou serviços backend do Basalt Notes."
applyTo: "apps/api/**, apps/worker/**, libs/backend/**"
---

# .NET e ASP.NET Core

## Linguagem e projetos

- Use .NET 10 LTS, C# com nullable habilitado, implicit usings e analyzers tratados como erro no CI.
- Formate com `dotnet format`; siga `.editorconfig` e não suprima warnings sem justificativa.
- Use tipos imutáveis para comandos, resultados, eventos e value objects.
- Evite null como estado implícito; modele ausência e falha de forma explícita.
- Métodos assíncronos recebem `CancellationToken` e não bloqueiam com `.Result` ou `.Wait()`.
- Não use `async void`, exceto handlers exigidos por framework.

## Camadas

- Domínio contém invariantes e não referencia EF Core, ASP.NET Core ou filesystem.
- Aplicação contém casos de uso, autorização contextual e portas.
- Infraestrutura implementa adapters e não vaza entidades EF para contratos HTTP.
- API e worker são composition roots pequenos.
- Não crie repository genérico sobre EF Core. Use `DbContext` em adapters específicos ou repositórios orientados a agregados.
- Modele commands, queries e handlers como classes explícitas. Chamada direta é adequada para fluxos simples; aplique o padrão Mediator quando dispatch desacoplado ou comportamentos de pipeline removerem complexidade comprovada.
- Quando o padrão Mediator for necessário, use `Nuuvify.CommonPack.Mediator`, registre-o nos composition roots e centralize a versão. Não use a biblioteca MediatR por restrição de licenciamento.
- Behaviors de pipeline não ocultam autorização contextual, fronteiras transacionais, idempotência ou efeitos externos; esses aspectos permanecem explícitos e testáveis.
- Use somente pacotes com licença permissiva, gratuita e verificada conforme as instruções de arquitetura; licença comercial, restritiva, ambígua ou ausente bloqueia a inclusão.
- Para projetos .NET do Basalt Notes, os namespaces de pacotes pré-aprovados são: `Azure.*`, `Microsoft.*`, `SonarAnalyzer.CSharp.*` e `Nuuvify.*`.
- Pacotes fora dessa lista exigem justificativa técnica, validação de licença e avaliação de impacto antes da adoção.

## ASP.NET Core

- Use endpoint groups ou controllers de modo consistente; um módulo não mistura estilos.
- Todos os endpoints da API seguem RESTful e representam recursos ou sub-recursos; não implemente operações RPC, verbos em rotas ou sufixos de ação.
- Em .NET 10, quando uma consulta segura e idempotente precisar de corpo estruturado e `GET` não for adequado, exponha `QUERY` com `AcceptVerbs("QUERY")` em controllers ou `MapMethods(..., ["QUERY"], ...)` em Minimal APIs; nunca use `QUERY` para alterar estado.
- Toda controller, action e Minimal API possui documentação orientada ao usuário no OpenAPI. Controllers e actions públicas usam XML docs e metadados de resposta; Minimal APIs usam summary, description, tipos de resposta e demais metadados equivalentes.
- A documentação explica finalidade, autenticação e autorização, entradas, respostas, erros, headers, precondições e efeitos observáveis; não descreve detalhes internos nem apenas repete nomes do código.
- Retorne `ProblemDetails` padronizado e sem detalhes internos.
- Valide entrada na borda e invariantes no domínio.
- Propague correlation ID, identidade, autorização e cancelamento.
- Use rate limiting em autenticação, upload e operações caras.
- Health checks distinguem liveness, readiness e startup.
- SignalR transporta notificações; estado durável permanece no banco.

## Concorrência e resiliência

- Operações de nota usam ETag/versão e falham explicitamente em conflito.
- Jobs e handlers são idempotentes.
- Timeouts, retry e circuit breaker aplicam-se apenas a falhas transitórias e não multiplicam efeitos.
- Não mantenha locks durante I/O externo desnecessário.
- Não tente transação distribuída entre filesystem e PostgreSQL; registre estado reconciliável.

## Logging e observabilidade

- Use logs estruturados e scopes; não interpole conteúdo sensível.
- Nunca registre corpo de notas, credenciais, tokens ou connection strings.
- Instrumente duração, resultado e IDs técnicos, não texto do usuário.
- Exponha métricas de fila, indexação, conflito, busca e atraso de projeção.

## Testes

- xUnit para testes .NET; nomes descrevem cenário e resultado.
- Domínio usa testes unitários; infraestrutura usa PostgreSQL/filesystem reais via Testcontainers ou fixture temporária.
- Teste cancelamento, concorrência, idempotência e falha parcial.
- Execute restore, format check, build e testes do projeto afetado.

Consulte [implementação do backend](../../docs/backend.md).
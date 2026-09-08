---
name: "Backend .NET"
description: "Use ao criar ou alterar C#, .NET 10, ASP.NET Core, API, worker, domínio, aplicação, infraestrutura, SignalR ou serviços backend do Altivy."
applyTo: "backend/apps/**, backend/libs/**"
---

# .NET e ASP.NET Core

## Linguagem e projetos

- Use .NET 10 LTS, C# com nullable habilitado, implicit usings e analyzers tratados como erro no CI.
- Cada aplicação em `apps/` mantém um `GlobalUsings.cs` próprio para namespaces
	de plataforma compartilhados dentro daquele host; namespaces de domínio,
	aplicação, infraestrutura e features permanecem explícitos para preservar
	as fronteiras arquiteturais.
- Não crie um `GlobalUsings.cs` global para todo o monorepo: hosts, bibliotecas
	e testes possuem dependências e responsabilidades diferentes.
- Ao adicionar um namespace recorrente a um host, prefira atualizar o arquivo
	global daquele host; remova usings redundantes somente na mesma fatia e
	mantenha usings específicos próximos ao código quando melhorarem a leitura.
- Formate com `dotnet format`; siga `.editorconfig` e não suprima warnings sem justificativa.
- Use tipos imutáveis para comandos, resultados, eventos e value objects.
- Evite null como estado implícito; modele ausência e falha de forma explícita.
- Métodos assíncronos recebem `CancellationToken` e não bloqueiam com `.Result` ou `.Wait()`.
- Não use `async void`, exceto handlers exigidos por framework.

## Composition roots

- Mantenha `Program.cs` pequeno e dedicado à composição e à ordem de inicialização do host.
- Distribua a configuração do host por métodos de extensão do .NET, organizados por responsabilidade em pastas separadas:
	- `Configuration/`: métodos `Add...` para bind e validação da configuração da aplicação;
	- `DependencyInjection/`: métodos `Add...` para registros de serviços, opções e integrações no IoC;
	- `Middleware/`: métodos `Use...` para compor o pipeline HTTP;
	- `Endpoints/`: métodos `Map...` para registrar endpoints, grupos e contratos HTTP.
- Os métodos de extensão devem preservar a ordem necessária de registro e execução, retornar o builder apropriado para composição fluente e não mover regras de negócio para os hosts.

## Camadas

- Domínio contém invariantes e não referencia EF Core, ASP.NET Core ou filesystem.
- Aplicação contém casos de uso, autorização contextual e portas.
- Infraestrutura implementa adapters e não vaza entidades EF para contratos HTTP.
- API e worker são composition roots pequenos.
- Não crie repository genérico sobre EF Core. Use `DbContext` em adapters específicos ou repositórios orientados a agregados.
- O acesso a banco deve ser feito exclusivamente por EF Core; não use Dapper nem outro ORM adicional.
- SQL ou C# puro só podem ser usados quando a tradução por EF Core não for viável e houver justificativa técnica explícita no código, documento ou PR.
- Todo mapeamento de entidades para o banco deve usar Fluent API; nunca use annotations para configurar persistência ou relacionamento.
- Mantenha os mapeamentos de entidades em `Configs` dentro do projeto `Infra.Data.Postgres`.
- Modele commands, queries e handlers como classes explícitas. Chamada direta é adequada para fluxos simples; aplique o padrão Mediator quando dispatch desacoplado ou comportamentos de pipeline removerem complexidade comprovada.
- Quando o padrão Mediator for necessário, use `Nuuvify.CommonPack.Mediator`, registre-o nos composition roots e centralize a versão. Não use a biblioteca MediatR por restrição de licenciamento.
- Behaviors de pipeline não ocultam autorização contextual, fronteiras transacionais, idempotência ou efeitos externos; esses aspectos permanecem explícitos e testáveis.
- Use somente pacotes com licença permissiva, gratuita e verificada conforme as instruções de arquitetura; licença comercial, restritiva, ambígua ou ausente bloqueia a inclusão.
- Para projetos .NET do Altivy, os namespaces de pacotes pré-aprovados são: `Azure.*`, `Microsoft.*`, `SonarAnalyzer.CSharp.*` e `Nuuvify.*`.
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
- Quando o módulo usar persistência PostgreSQL, mantenha a configuração em `DataContextSetup.cs` e exponha as opções via `AddDbContextPoolSetup` ou `AddDbContextFactory` com `DbContextOptionsBuilder`.
- Quando houver `PostgresDbContext`, explicite nele as convenções de schema, naming, extensões e mapeamentos do banco para deixar claro ao desenvolvedor como a persistência está configurada.
- Crie uma factory de design-time para facilitar migrations a partir do projeto de dados PostgreSQL (`Infra.Data.Postgres` quando existir).

## Persistência e consultas

- Queries devem ser escritas com LINQ.
- Use SQL direto apenas com justificativa técnica explícita e quando não houver tradução viável por EF Core.
- Não use `LIKE` em consultas novas; prefira Full Text Search, trigramas ou outro índice apropriado ao caso. Quando a busca textual exigir novo recurso no banco, crie a migration correspondente para `tsvector`, índice GIN/GiST ou o objeto necessário ao contrato.
- Siga as melhores práticas de EF Core e de acesso a dados adotadas por uma arquitetura limpa e robusta.

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
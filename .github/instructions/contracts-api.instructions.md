---
name: "Contratos e API"
description: "Use ao criar ou alterar OpenAPI, endpoints, schemas JSON, DTOs públicos, paginação, erros, eventos SignalR ou clientes gerados."
applyTo: "contracts/**, apps/api/**/*.cs, apps/web/**/data-access/**"
---

# Contratos públicos

- OpenAPI em `contracts/openapi` é a descrição verificável da API `/api/v1`.
- Gere clientes e compare artefatos no CI; não edite código gerado.
- Todos os endpoints seguem RESTful: modele recursos e sub-recursos com substantivos, use a semântica do protocolo HTTP e não exponha RPC, verbos em rotas ou sufixos de ação como `:move` e `:rebuild`.
- Use `GET` para leitura sem corpo, `POST` para criação, `PUT` para substituição idempotente, `PATCH` para alteração parcial e `DELETE` para remoção; retorne status, `Location`, headers de cache e precondições coerentes com cada método.
- Quando uma consulta segura e idempotente exigir critérios estruturados no corpo e não puder ser representada adequadamente por `GET`, use o método HTTP `QUERY`; `QUERY` nunca altera estado e seu contrato deve documentar corpo, cache, idempotência e compatibilidade de proxy e cliente.
- Toda operação deve possuir documentação OpenAPI informativa para o consumidor, com `summary` e `description` específicos, autenticação e autorização exigidas, parâmetros, corpo, respostas de sucesso, erros, headers relevantes e exemplos sem dados sensíveis.
- Não aceite controller, action ou endpoint sem documentação pública correspondente nem descrições genéricas que apenas repitam o nome do método ou da rota.
- Erros usam `application/problem+json` com `type`, `title`, `status`, `detail`, `instance`, `traceId` e código estável quando necessário.
- Listas mutáveis usam paginação por cursor e limites máximos.
- Notas usam `ETag`, `If-Match` e respostas `409` ou `412` documentadas.
- Datas são ISO 8601 UTC; IDs são opacos para clientes.
- Paths são dados relativos normalizados, não identificadores inseridos diretamente na URL.
- Não exponha entity models, nomes de tabela, stack traces ou caminhos físicos.
- Mudança aditiva preserva consumidores; breaking change requer nova versão e estratégia de transição.
- Exemplos não contêm dados pessoais, secrets ou conteúdo real de vault.
- Testes de contrato validam estilo RESTful, documentação OpenAPI, serialização, status, headers e compatibilidade; operações `QUERY` também validam passagem pelo BFF/proxy e pelo cliente gerado.
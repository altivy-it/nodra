---
name: "Contratos e API"
description: "Use ao criar ou alterar OpenAPI, endpoints, schemas JSON, DTOs públicos, paginação, erros, eventos SignalR ou clientes gerados."
applyTo: "contracts/**, apps/api/**/*.cs, apps/web/**/data-access/**"
---

# Contratos públicos

- OpenAPI em `contracts/openapi` é a descrição verificável da API `/api/v1`.
- Gere clientes e compare artefatos no CI; não edite código gerado.
- Use recursos e verbos HTTP previsíveis; comandos não CRUD usam sufixo de ação documentado.
- Erros usam `application/problem+json` com `type`, `title`, `status`, `detail`, `instance`, `traceId` e código estável quando necessário.
- Listas mutáveis usam paginação por cursor e limites máximos.
- Notas usam `ETag`, `If-Match` e respostas `409` ou `412` documentadas.
- Datas são ISO 8601 UTC; IDs são opacos para clientes.
- Paths são dados relativos normalizados, não identificadores inseridos diretamente na URL.
- Não exponha entity models, nomes de tabela, stack traces ou caminhos físicos.
- Mudança aditiva preserva consumidores; breaking change requer nova versão e estratégia de transição.
- Exemplos não contêm dados pessoais, secrets ou conteúdo real de vault.
- Testes de contrato validam OpenAPI, serialização, status, headers e compatibilidade.
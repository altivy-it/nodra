---
name: pull-request-review
description: "Executa revisão rigorosa de branch, diff ou pull request do Thothis. Use para code review, security review, revisão arquitetural, contratos, migrations, testes e risco de release."
argument-hint: "PR, branch, diff e requisito relacionado"
---

# Pull Request Review

## 1. Preparar contexto

1. Leia o requisito, issue, ADR e documentação citados.
2. Determine base/head e leia o diff completo, incluindo arquivos gerados e migrations.
3. Liste áreas afetadas: frontend, backend, contratos, dados, segurança, deployment e docs.
4. Carregue as instruções correspondentes.

## 2. Entender comportamento

1. Trace os call sites e consumidores alterados.
2. Compare comportamento anterior e proposto.
3. Identifique invariantes de conteúdo, autorização e consistência.
4. Verifique compatibilidade de API, schema, Markdown, configuração e imagens.

## 3. Revisar por dimensão

### Correção

- critérios de aceite estão completos;
- erros, cancelamento, concorrência e bordas são tratados;
- não há estado impossível, corrida ou sobrescrita silenciosa.

### Arquitetura

- dependências respeitam os limites do monorepo;
- regra está no módulo proprietário;
- nova complexidade possui necessidade e ADR quando exigido.

### Segurança e privacidade

- autorização e confinamento de path são completos;
- conteúdo ativo, uploads e headers são validados;
- secrets e conteúdo não aparecem em logs, exemplos ou imagens.

### Dados e operação

- migration é compatível, observável e recuperável;
- índices e queries têm impacto conhecido;
- jobs são idempotentes e falhas parciais convergem;
- health, métricas, rollout e rollback foram tratados.

### Frontend

- contrato, estado e UX de erro estão alinhados;
- acessibilidade e responsividade foram preservadas;
- listas grandes e editor não sofreram regressão evidente.

### Testes

- cada risco relevante tem evidência no nível correto;
- testes falham pela razão certa e não dependem de timing/internet;
- PostgreSQL/filesystem e jornadas usam integração real quando necessário.

## 4. Confirmar achados

- Um achado deve ter localização, cenário válido, impacto e correção plausível.
- Execute validação estreita quando ela puder confirmar ou refutar o risco.
- Não relate estilo já coberto por formatter/linter.
- Não trate preferência arquitetural como bug sem violação concreta.

## 5. Formato

1. Achados ordenados por `Crítica`, `Alta`, `Média`, `Baixa`.
2. Para cada achado: localização, problema, cenário, impacto, correção e teste.
3. Dúvidas/premissas.
4. Lacunas de teste e risco residual.
5. Resumo curto da mudança.

Se não houver achados, declare explicitamente e indique o que não foi possível validar.
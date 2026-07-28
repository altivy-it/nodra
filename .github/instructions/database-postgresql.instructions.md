---
name: "PostgreSQL e pgvector"
description: "Use ao alterar EF Core, Npgsql, migrations, SQL, schema, índices, full-text search, pg_trgm, filas PostgreSQL ou pgvector."
applyTo: "libs/backend/**/*DbContext*.cs, libs/backend/**/Migrations/**, libs/backend/**/*.sql, tests/backend/**/*.sql, tests/backend/**/*Database*.cs"
---

# PostgreSQL, EF Core e pgvector

- Use PostgreSQL 18 e a imagem imutável definida no plano mestre.
- Schema da aplicação: `basalt_notes`; nomes SQL em `snake_case`; timestamps em `timestamptz` UTC.
- Migrações EF Core são explícitas, revisadas e nunca executadas pelos scripts de bootstrap da imagem.
- A role de runtime não cria databases, roles ou extensões.
- Alterações destrutivas seguem expand-contract: adicionar, migrar, observar, remover em release posterior.
- Toda migration possui caminho de rollout, impacto de lock, estratégia de rollback e teste com volume representativo.
- Não carregue coleções ilimitadas; projete apenas colunas necessárias e use paginação.
- Verifique planos críticos com `EXPLAIN (ANALYZE, BUFFERS)` em dados sintéticos.
- Full-text usa `tsvector` + GIN; fuzzy usa `pg_trgm`; busca híbrida combina rankings explicitamente.
- Comece pgvector com busca exata. Adicione HNSW apenas após medir recall, latência e memória.
- Registre modelo, dimensão, versão do chunker e hash de origem em embeddings.
- Consumidores da fila usam lease e `FOR UPDATE SKIP LOCKED`; jobs têm chave de idempotência.
- Testes de integração usam PostgreSQL real com `vector`, `pg_trgm`, `pgcrypto` e `citext` habilitados.
- Nunca use banco em memória como prova de comportamento PostgreSQL.
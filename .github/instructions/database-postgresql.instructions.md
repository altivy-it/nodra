---
name: "PostgreSQL e pgvector"
description: "Use ao alterar EF Core, Npgsql, migrations, SQL, schema, índices, full-text search, pg_trgm, filas PostgreSQL ou pgvector."
applyTo: "backend/libs/**/*DbContext*.cs, backend/libs/**/Migrations/**, backend/libs/**/*.sql, backend/tests/**/*.sql, backend/tests/**/*Database*.cs"
---

# PostgreSQL, EF Core e pgvector

- Use PostgreSQL 18 e a imagem imutável definida no plano mestre.
- Schema da aplicação: `altivy`; nomes SQL em `snake_case`; timestamps em `timestamptz` UTC.
- Migrações EF Core são explícitas, revisadas e nunca executadas pelos scripts de bootstrap da imagem.
- A role de runtime não cria databases, roles ou extensões.
- Alterações destrutivas seguem expand-contract: adicionar, migrar, observar, remover em release posterior.
- Toda migration possui caminho de rollout, impacto de lock, estratégia de rollback e teste com volume representativo.
- Não carregue coleções ilimitadas; projete apenas colunas necessárias e use paginação.
- Verifique planos críticos com `EXPLAIN (ANALYZE, BUFFERS)` em dados sintéticos.
- Full-text usa `tsvector` + GIN; fuzzy usa `pg_trgm`; busca híbrida combina rankings explicitamente.
- Não use `LIKE` em consultas novas; prefira Full Text Search, trigramas ou outro índice apropriado ao caso.
- Se a busca textual exigir novo recurso no banco, crie a migration correspondente para `tsvector`, índice GIN/GiST ou o objeto necessário ao contrato.
- Quando houver `PostgresDbContext`, explicite nele as convenções de schema, naming, extensões e mapeamentos do banco.
- Quando o módulo usar persistência PostgreSQL, mantenha a configuração em `DataContextSetup.cs` com `AddDbContextPoolSetup` ou `AddDbContextFactory` e `DbContextOptionsBuilder`.
- Crie uma factory de design-time para facilitar migrations a partir do projeto de dados PostgreSQL (`Infra.Data.Postgres` quando existir).
- Todo mapeamento de entidades para o banco deve usar Fluent API; nunca use annotations para configurar persistência ou relacionamento.
- Mantenha os mapeamentos de entidades em `Configs` dentro do projeto `Infra.Data.Postgres`.
- Comece pgvector com busca exata. Adicione HNSW apenas após medir recall, latência e memória.
- Registre modelo, dimensão, versão do chunker e hash de origem em embeddings.
- Consumidores da fila usam lease e `FOR UPDATE SKIP LOCKED`; jobs têm chave de idempotência.
- Tabelas com potencial de crescimento elevado ou retenção temporal usam Declarative Partitioning/Table Partitioning por faixa de data; para auditoria e jobs, prefira partições mensais, defina criação antecipada, rollover, partição de segurança, índices locais e retenção antes de produção.
- Testes de integração cobrem criação de partições, roteamento de linhas, consultas com pruning, rollover, partição de segurança e comportamento quando a partição esperada não existe.
- Testes de integração usam PostgreSQL real com `vector`, `pg_trgm`, `pgcrypto` e `citext` habilitados.
- Nunca use banco em memória como prova de comportamento PostgreSQL.
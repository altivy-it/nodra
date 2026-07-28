# Basalt Notes - Implementação do Backend

## 1. Objetivo

Definir a implementação da API, worker, persistência e integrações do Basalt Notes. As decisões globais permanecem no [Plano Mestre](../README.md).

## 2. Estrutura da solução .NET

```text
src/
├── BasaltNotes.Api/              # HTTP, OpenAPI, auth e SignalR
├── BasaltNotes.Worker/           # jobs, reconciliação e indexação
├── BasaltNotes.Application/      # casos de uso e portas
├── BasaltNotes.Domain/           # entidades, valores e regras
├── BasaltNotes.Infrastructure/   # PostgreSQL, filesystem e providers
└── BasaltNotes.Contracts/        # contratos públicos versionados
tests/
├── BasaltNotes.UnitTests/
├── BasaltNotes.IntegrationTests/
├── BasaltNotes.ArchitectureTests/
└── BasaltNotes.PerformanceTests/
```

Usar monólito modular e arquitetura em camadas pragmática. Regras de domínio não dependem de ASP.NET Core, EF Core ou filesystem. Não criar interfaces para classes sem fronteira ou substituição real.

## 3. Módulos

### 3.1 Identity

- usuário administrador inicial criado por fluxo de bootstrap;
- contas locais com hash de senha por algoritmo recomendado pelo ASP.NET Core Identity;
- federação por OpenID Connect;
- mapeamento de claims e grupos para funções internas;
- sessões revogáveis e política de lockout;
- funções iniciais `Owner`, `Editor` e `Reader` por vault.

### 3.2 Vaults

- raiz física configurada pelo operador, nunca pelo usuário comum;
- identificador estável independente do caminho físico;
- validação de acesso e capacidade de leitura/escrita no startup;
- políticas de anexos, limites, histórico e indexação;
- estado operacional: disponível, somente leitura, degradado ou indisponível.

### 3.3 Notes

- CRUD de notas e pastas;
- paths relativos normalizados com `/` no contrato;
- extensão `.md` obrigatória para notas;
- hash SHA-256 e versão monotônica na projeção;
- ETag forte baseada em versão/hash;
- rename e move tratados como operações de domínio;
- lixeira dentro de área controlada do vault;
- gravação temporária e replace atômico quando suportado.

### 3.4 Markdown

- parsing no backend com Markdig e extensões controladas;
- frontmatter YAML com parser seguro;
- extração de headings, tags, links, embeds e blocos;
- resolução documentada de wiki links e aliases;
- renderização HTML sanitizada;
- conteúdo desconhecido preservado, não descartado pelo parser.

### 3.5 Search e Knowledge Graph

- indexação textual com configuração adequada a português e conteúdo multilíngue;
- trigramas para títulos e caminhos;
- chunks determinísticos por heading e limites de tokens/caracteres;
- vetores associados a modelo, dimensão e versão do chunker;
- ranking híbrido por Reciprocal Rank Fusion;
- arestas explícitas para links resolvidos e não resolvidos;
- consultas de vizinhança com profundidade e limites obrigatórios.

## 4. Persistência PostgreSQL

### 4.1 Convenções

- schema `basalt_notes`;
- chaves primárias UUID v7 quando disponíveis na aplicação;
- timestamps em UTC com `timestamptz`;
- nomes em `snake_case` no banco;
- optimistic concurrency nas entidades mutáveis;
- exclusão física somente quando exigida por política;
- migrações EF Core revisadas e versionadas.

### 4.2 Índices iniciais

- unicidade de `(vault_id, normalized_path)` em notas ativas;
- B-tree para vault, datas, estado e foreign keys;
- GIN para `tsvector` e propriedades consultáveis quando apropriado;
- GIN/GiST de trigramas para título e caminho;
- índice por origem e destino em `note_links`;
- HNSW com distância cosseno apenas após volume e recall serem medidos;
- índice único de idempotência em `index_jobs`.

Começar com busca vetorial exata em bases pequenas. Índice aproximado não deve ser criado apenas por disponibilidade da extensão.

### 4.3 Migrações e bootstrap

- a imagem PostgreSQL habilita extensões no primeiro bootstrap;
- a aplicação valida `vector`, `pg_trgm`, `pgcrypto` e `citext` no readiness;
- migrações da aplicação não são colocadas em `/docker-entrypoint-initdb.d`;
- um processo explícito e exclusivo aplica migrações antes da nova API receber tráfego;
- a role de runtime não recebe privilégios para criar database ou role;
- mudanças destrutivas usam estratégia expand-contract entre releases.

## 5. Contrato HTTP

### 5.1 Padrões

- REST sob `/api/v1`;
- OpenAPI gerado e validado no CI;
- `application/problem+json` para erros;
- correlation ID em resposta e logs;
- paginação por cursor para listas mutáveis;
- `ETag`, `If-Match` e `If-None-Match` para notas;
- `Idempotency-Key` em uploads e operações adequadas;
- datas ISO 8601 em UTC;
- breaking changes somente em nova versão do contrato.

### 5.2 Recursos iniciais

```text
GET    /api/v1/session
GET    /api/v1/vaults
GET    /api/v1/vaults/{vaultId}/tree
POST   /api/v1/vaults/{vaultId}/notes
GET    /api/v1/vaults/{vaultId}/notes/{noteId}
PUT    /api/v1/vaults/{vaultId}/notes/{noteId}
POST   /api/v1/vaults/{vaultId}/notes/{noteId}:move
DELETE /api/v1/vaults/{vaultId}/notes/{noteId}
GET    /api/v1/vaults/{vaultId}/notes/{noteId}/backlinks
GET    /api/v1/vaults/{vaultId}/search
GET    /api/v1/vaults/{vaultId}/graph
POST   /api/v1/vaults/{vaultId}/attachments
GET    /api/v1/vaults/{vaultId}/index/status
POST   /api/v1/vaults/{vaultId}/index:rebuild
```

IDs são usados nos recursos; paths aparecem como dados e não devem ser inseridos diretamente em segmentos sem encoding e validação.

## 6. Filesystem

### 6.1 Proteções

- combinar somente raiz confiável e path relativo normalizado;
- comparar o caminho final com a raiz usando semântica apropriada ao sistema operacional;
- rejeitar `..`, paths absolutos, nomes reservados e caracteres inválidos;
- tratar symlinks/reparse points segundo política restritiva;
- impedir acesso às áreas internas da aplicação;
- limitar profundidade, comprimento e quantidade de entradas por operação.

### 6.2 Gravação

1. adquirir lock lógico curto para a nota;
2. comparar ETag recebida com estado atual;
3. validar encoding, tamanho e política;
4. gravar temporário no mesmo diretório;
5. garantir flush conforme política de durabilidade;
6. substituir o destino;
7. atualizar catálogo e criar job na mesma transação de banco;
8. publicar evento de domínio após commit.

Se a atualização do catálogo falhar após a escrita, a reconciliação deve detectar o novo hash. Se a escrita falhar, nenhuma projeção de sucesso deve ser persistida.

### 6.3 Alterações externas

- `FileSystemWatcher` gera sinais de baixa latência quando confiável;
- debounce agrupa eventos duplicados;
- varredura incremental compara path, tamanho, mtime e hash;
- varredura completa periódica detecta divergências acumuladas;
- rename externo é inferido por identidade/hash quando possível;
- conflitos com uma edição aberta geram notificação, não sobrescrita.

## 7. Jobs e worker

Usar tabela PostgreSQL como fila inicial, consumida com `FOR UPDATE SKIP LOCKED`.

Estados:

- `pending`;
- `processing` com lease;
- `completed`;
- `retry_scheduled`;
- `dead_letter`;
- `cancelled`.

Cada job possui chave de idempotência, tipo, payload versionado, contador de tentativas, próxima execução e erro sanitizado. O worker renova lease, aplica backoff com jitter e recupera jobs abandonados.

Tipos iniciais:

- `note.index`;
- `note.remove_projection`;
- `vault.reconcile`;
- `vault.rebuild`;
- `embedding.generate`;
- `attachment.inspect`;
- `revision.prune`.

## 8. Pipeline de indexação

1. ler arquivo e confirmar hash esperado;
2. detectar encoding e validar limites;
3. parsear frontmatter e Markdown;
4. atualizar nota, propriedades, headings, tags e links em transação;
5. gerar `tsvector` e chunks determinísticos;
6. resolver links contra catálogo atual;
7. criar jobs de embedding quando configurado;
8. publicar atualização via SignalR;
9. registrar métricas sem conteúdo sensível.

Reprocessar somente etapas afetadas quando versão do parser, chunker ou modelo mudar.

## 9. Embeddings

Criar uma interface de provider com:

- identificação imutável de modelo e versão;
- dimensão declarada;
- limite de batch e tamanho;
- timeout, retry e circuit breaker;
- suporte local e remoto;
- política explícita sobre envio de conteúdo para terceiros.

O padrão do produto é embeddings desabilitados. Ativação de provedor remoto exige consentimento administrativo e indicação clara de que conteúdo pode sair do ambiente.

## 10. Autenticação e autorização

- ASP.NET Core Identity para modo local;
- OpenID Connect para federação;
- cookies `Secure`, `HttpOnly` e `SameSite` apropriado para UI same-origin;
- antiforgery em operações mutáveis;
- políticas por ação e vault, não apenas por rota;
- validação de autorização antes de consultar path ou conteúdo;
- auditoria de login, administração, exportação e alterações de permissão;
- bootstrap inicial com segredo efêmero ou fluxo de primeiro acesso, nunca senha padrão.

## 11. Observabilidade e saúde

### 11.1 Métricas

- duração e falhas de leitura/gravação;
- conflitos por vault;
- profundidade e idade da fila;
- duração e falha de indexação;
- atraso entre arquivo e projeção;
- latência e quantidade de resultados de busca;
- disponibilidade do provider de embeddings;
- conexões SignalR.

### 11.2 Health checks

- liveness verifica apenas o processo;
- readiness verifica PostgreSQL, schema e acesso ao vault;
- startup permite migração e primeira reconciliação sem reinício prematuro;
- indisponibilidade de embeddings não torna a API indisponível.

## 12. Testes

### 12.1 Unitários

- normalização e segurança de paths;
- resolução de wiki links e aliases;
- parsing de frontmatter;
- política de autorização;
- ETags e conflitos;
- chunking e Reciprocal Rank Fusion;
- transições de jobs.

### 12.2 Integração

- PostgreSQL real com a imagem definida no plano mestre;
- extensões e migrações;
- leitura, escrita, replace e reconciliação em filesystem temporário;
- concorrência de consumers da fila;
- índices e planos das consultas críticas;
- autenticação e autorização em endpoints.

### 12.3 Resiliência e segurança

- falha entre escrita do arquivo e atualização da projeção;
- worker encerrado durante job;
- banco indisponível e recuperação;
- eventos duplicados e fora de ordem;
- traversal, symlink escape e nomes maliciosos;
- XSS no Markdown e frontmatter;
- upload poliglota e MIME inconsistente;
- usuário tentando acessar outro vault.

## 13. Sequência de implementação

1. solution, convenções, CI e arquitetura modular;
2. Compose de desenvolvimento, PostgreSQL e migrações;
3. vault, segurança de paths e catálogo;
4. CRUD de notas com ETag e gravação segura;
5. parser, propriedades, links e backlinks;
6. fila, worker e reconciliação;
7. pesquisa textual, fuzzy e filtros;
8. grafo e SignalR;
9. anexos, lixeira, revisão e auditoria;
10. OIDC, embeddings, hardening e desempenho.

## 14. Critérios de aceite do backend

- reconstrução do índice produz resultado funcional equivalente;
- operações concorrentes nunca perdem conteúdo silenciosamente;
- nenhum path aceito escapa da raiz do vault;
- jobs podem ser repetidos sem corromper projeções;
- indisponibilidade de embeddings degrada somente busca semântica;
- migrações são testadas contra versão anterior suportada;
- API publica contrato OpenAPI estável e erros padronizados;
- logs e traces não contêm conteúdo integral nem secrets;
- testes de integração usam PostgreSQL e filesystem reais.
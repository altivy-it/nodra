---
name: "Dockerfiles da Aplicação"
description: "Use ao criar, alterar ou revisar Dockerfiles e arquivos dockerignore do Basalt Notes. Cobre multi-stage, cache, usuário, imagem runtime, secrets, labels e health check."
applyTo: "**/Dockerfile*, **/.dockerignore"
---

# Dockerfiles

- Use build multi-stage e imagem runtime mínima, suportada e fixada por versão.
- Copie manifests/lockfiles antes do código para preservar cache.
- Restaure dependências com lockfile e falhe em divergência.
- Não instale SDK, compilador ou shell administrativo na imagem final sem necessidade operacional.
- Execute como usuário não root com UID/GID documentado.
- Use filesystem read-only e diretórios temporários explícitos quando viável.
- Nunca passe secret por `ARG`, `ENV`, camada ou contexto; use BuildKit secret ou secret de runtime.
- `.dockerignore` bloqueia `.git`, `.env`, secrets, vaults, dados, backups, coverage e artifacts locais.
- Declare labels OCI, porta e health check que prove saúde útil.
- Preserve encerramento gracioso; entrypoint usa `exec` e encaminha sinais.
- Não duplique a imagem PostgreSQL; use `lzocateli/postgresql:18.4-pgvector0.8.5-bookworm` no Compose.
- Valide análise BuildKit, build limpo, usuário, entrypoint, labels, porta, health e ausência de secrets.
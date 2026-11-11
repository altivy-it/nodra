---
name: "Containers e Deploy"
description: "Use ao criar ou alterar Dockerfile, Compose, proxy reverso, entrypoint, health check, bind mount, configuração de runtime ou arquivos de deploy do Nodra."
applyTo: "deploy/**, **/Dockerfile*, **/compose*.yml, **/compose*.yaml, **/.dockerignore"
---

# Containers e operação

- Use builds multi-stage e imagem runtime mínima, suportada e fixada por versão.
- Execute aplicação como usuário não root; filesystem da imagem é read-only quando viável.
- Não instale ferramentas de build na imagem final.
- Copie manifests antes do código para preservar cache.
- `.dockerignore` bloqueia `.git`, `.env`, secrets, dados, backups, cobertura e artefatos locais.
- Nunca use secret em `ARG`, `ENV` do Dockerfile ou camada; use BuildKit secret ou runtime secret.
- Declare labels OCI, porta e health check útil; não use health check que apenas verifica processo.
- Liveness não depende de serviço externo; readiness verifica dependências obrigatórias.
- Toda persistência usa bind mount documentado. O vault e PostgreSQL nunca ficam em camada gravável efêmera.
- Publique somente o web/proxy; API, worker, PostgreSQL e embeddings ficam em rede interna.
- Use a imagem `lzocateli/postgresql:18.4-pgvector0.8.6-bookworm`; não duplique sua construção.
- Compose define `depends_on` por health quando necessário, limites, restart policy e shutdown gracioso.
- Imagens recebem tags imutáveis; release inclui SBOM, provenance e scan.
- Valide build, execução, usuário, labels, portas, mounts, health, persistência e encerramento.
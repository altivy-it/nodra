---
name: "Docker Compose"
description: "Use ao criar ou alterar Docker Compose do Basalt Notes. Cobre serviços, redes, health dependencies, bind mounts, secrets, restart e perfis."
applyTo: "**/compose*.yml, **/compose*.yaml"
---

# Docker Compose

- Separe `web`, `api`, `worker` e `postgres`; embeddings são perfil opcional.
- Publique somente web/proxy por padrão; demais serviços ficam em rede interna.
- Use tags imutáveis e a imagem PostgreSQL definida no plano mestre.
- Toda persistência usa bind mount documentado; não use volume nomeado para vault ou PostgreSQL.
- Monte secrets como arquivos quando suportado; não versione valores em Compose ou `.env`.
- Use `depends_on` com condição de health somente para ordenar dependências reais; a aplicação também deve tolerar reconexão.
- Defina health checks, restart policy e `stop_grace_period` coerentes com cada processo.
- Aplique limites/configurações de recurso por perfil quando não prejudicar portabilidade.
- Não monte Docker socket nem execute containers privilegiados.
- Valide bootstrap limpo, readiness, restart, recriação com persistência e shutdown gracioso.
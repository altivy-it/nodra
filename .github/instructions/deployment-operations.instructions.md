---
name: "Operação e Deployment"
description: "Use ao alterar arquivos operacionais em deploy, proxy reverso, configuração runtime, bind mounts, backup, restore, rollout ou runbooks de implantação."
applyTo: "deploy/**"
---

# Operação e deployment

- Suporte os perfis local, equipe interna e ambiente isolado definidos no plano mestre.
- Configuração varia por ambiente; imagem permanece idêntica.
- Documente paths absolutos, owner/permissões e requisitos de SMB/NFS para bind mounts.
- TLS termina no proxy; valide forwarded headers e hosts confiáveis.
- Readiness bloqueia tráfego até PostgreSQL/schema/vault estarem prontos; embeddings opcionais não bloqueiam.
- Migração é etapa exclusiva e observável antes da promoção da nova API.
- Rollout inclui preflight, backup, migration, smoke test e verificação de métricas.
- Rollback não pode depender de downgrade destrutivo de schema; use compatibilidade expand-contract.
- Backups cobrem vault e PostgreSQL; execute restore em ambiente limpo.
- Tags de imagem são imutáveis; release inclui SBOM, provenance, scan e digest.
- Não faça publicação ou deploy sem solicitação explícita e credenciais já configuradas.
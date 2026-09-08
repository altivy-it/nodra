---
name: release-readiness
description: "Avalia prontidão de release do Altivy. Use antes de versionar, publicar imagens ou promover ambiente para validar qualidade, segurança, migrations, containers, backup, compatibilidade e operação."
argument-hint: "Versão candidata, base anterior e ambiente-alvo"
---

# Release Readiness

## Regra de execução

Esta skill avalia e prepara evidências. Não publica, não envia imagens e não faz deploy sem solicitação explícita.

## 1. Definir release

- versão SemVer e base comparada;
- features, correções, breaking changes e deprecações;
- plataformas e perfis de implantação suportados;
- versão mínima de schema e caminho de atualização;
- riscos aceitos formalmente.

## 2. Validar código

- restore/install reproduzível;
- format, lint e análise estática;
- builds de produção Angular e .NET;
- testes unitários, integração, contrato, arquitetura e Playwright;
- benchmarks críticos comparados ao baseline;
- nenhum teste ignorado novo ou flakiness não explicado.

## 3. Validar dados

- migration da versão anterior suportada em PostgreSQL real;
- estratégia expand-contract respeitada;
- tempo, locks e espaço de migration medidos quando relevante;
- rollback operacional ou procedimento forward-fix documentado;
- reconstrução da projeção a partir do vault testada;
- backup e restore executados em ambiente limpo.

## 4. Validar segurança e supply chain

- threat models e revisões da release concluídos;
- dependências e licenças revisadas;
- nenhum secret no Git, contexto, imagem ou logs;
- SBOM gerado;
- scan de vulnerabilidades sem crítica/alta não aceita;
- provenance/attestation configurada para publicação.

## 5. Validar containers

- Dockerfiles passam análise BuildKit e build limpo;
- imagens usam bases e tags imutáveis;
- usuário, entrypoint, labels, portas e health checks inspecionados;
- Compose inicia do zero e respeita dependências/readiness;
- bind mounts preservam vault e PostgreSQL após recriação;
- shutdown gracioso e restart foram testados;
- somente web/proxy está exposto por padrão.

## 6. Validar operação

- release notes e changelog descrevem impacto e migração;
- runbooks cobrem falhas novas;
- dashboards e alertas reconhecem o novo comportamento;
- configuração e secrets obrigatórios estão documentados;
- rollout, verificação pós-deploy e rollback têm responsáveis e comandos;
- RPO/RTO permanecem atendidos.

## 7. Decisão

Classifique cada gate como:

- `PASS`: evidência disponível e aprovada;
- `FAIL`: bloqueia release;
- `WAIVED`: risco aceito com responsável e prazo;
- `NOT RUN`: sem evidência, não equivale a sucesso.

Resultado final:

- **GO:** nenhum `FAIL`, nenhum `NOT RUN` obrigatório e waivers válidos;
- **NO-GO:** qualquer gate obrigatório falha ou não foi executado.

Informe matriz de gates, evidências/comandos, bloqueios, waivers, risco residual e recomendação final.
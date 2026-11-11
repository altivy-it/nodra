# Correcao de Bug: permissoes da imagem web

- **Slug**: web-container-permissions
- **Corrigido em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

As etapas de preparacao declaram USER root explicitamente; o runtime final
continua USER nginx. O mesmo build que falhava em /src/node_modules terminou
apos a correcao, sem alterar dependencias ou layout.

## Alteracoes

| Arquivo | Alteracao | Notas |
| --- | --- | --- |
| frontend/web/Dockerfile | USER root nas etapas de preparacao | USER nginx final preservado |

## Testes Adicionados ou Atualizados

Reutilizados build completo do Dockerfile, metadados OCI e smoke HTTP do nginx.

## Verificacao Local

podman build com o mesmo Dockerfile produziu nodra-web:qualification-010.
Inspecao confirmou user=nginx e entrypoint /docker-entrypoint.sh. A imagem
iniciou e respondeu HTTP pela porta 8080 do proprio nginx, publicada apenas
em loopback para o teste, com titulo Nodra nos assets compilados.

## Desvios da Avaliacao

Nenhum. A referencia docker.io/ apenas explicita o registry ja usado; versoes
de imagens e pacotes foram preservadas.

## Acompanhamento

Homologacao completa de OIDC e upgrade permanece na tarefa T031. Nenhuma imagem
foi publicada e nenhum ambiente compartilhado foi alterado.
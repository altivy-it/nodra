# Verificacao de Bug: permissoes da imagem web

- **Slug**: web-container-permissions
- **Testado em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Correcao**: [fix.md](fix.md)
- **Resultado**: verified

## Resumo

O EACCES deixou de ocorrer no mesmo build. A instalacao estrita e o build
Angular passaram; o nginx final permanece nao-root e responde HTTP.

## Verificacoes Executadas

| Verificacao | Acao | Resultado |
| --- | --- | --- |
| Reproducao apos correcao | podman build --format docker --pull=never --file frontend/web/Dockerfile . | pass |
| Resolucao de pacotes | npm ci no stage de build, sem flags permissivas | pass |
| Build Angular | configuration production, dist/nodra-web | pass |
| Usuario final | Inspecao OCI user=nginx | pass |
| Smoke runtime | HTTP 200 pela imagem nginx em loopback, titulo Nodra | pass |

## Riscos Residuais

Smoke de web estatico nao equivale a login OIDC, upgrade, recuperacao ou
qualificacao de release. Esses gates permanecem nas tarefas correspondentes.

## Recomendacao

Encerrar este defeito delimitado de permissoes; continuar T031 somente com os
pre-requisitos reais do laboratorio disponiveis.
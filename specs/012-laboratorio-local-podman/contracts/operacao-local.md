# Contrato proposto de operação local

## Inicializador

Adaptar [start-self-hosted.ps1](../../../tools/scripts/start-self-hosted.ps1)
com `-ContainerEngine docker|podman`, padrão `docker`. Implementado e coberto por
[test-local-container-engine.ps1](../../../tools/scripts/test-local-container-engine.ps1).

Preservar todos os parâmetros existentes. Verificar engine e provider antes
de resolver configuração. Podman exige provider Docker Compose v2 compatível
com `config --format json`, `config --no-interpolate` e `up --wait`.

## Saída e falhas

- Ajuda: código zero, sem engine, configuração ou rede.
- Provider, rede, certificado, configuração ou gateway ausente: erro sanitizado,
  sem migrations ou início de consumidores.
- Build aprovado: não implica subida aprovada.
- Subida aprovada: exige readiness e smoke pela borda nginx.
- Login ou persistência não validados: não declarar qualificação funcional.

## Preservação

Não executar `down -v`, apagar bind mounts, parar serviços de outros projetos,
recriar identidade ou imprimir configuração resolvida. API e BFF não recebem
portas publicadas no host.

Builds Podman usam `--format=docker` para preservar healthchecks de API, BFF
e web. Worker e migrator não definem healthcheck nos Dockerfiles atuais;
o migrator é transitório, e prontidão do worker requer evidência operacional.

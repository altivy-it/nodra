# Correção de Bug: vulnerabilidades nas imagens de produção

- **Slug**: ci-imagens-vulneraveis
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

As imagens de runtime usam ASP.NET 10.0.15; o bundle autossuficiente usa SDK
10.0.404 e incorpora o runtime corrigido. A web usa nginx 1.30.7 e a versão
corrigida de `zlib1g`. As novas bases foram fixadas por digest.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| [Dockerfile da API](../../../backend/apps/api/Dockerfile) | modificado | Atualiza a base de runtime sem alterar o health check. |
| [Dockerfile do BFF](../../../backend/apps/bff/Dockerfile) | modificado | Atualiza a base de runtime. |
| [Dockerfile do worker](../../../backend/apps/worker/Dockerfile) | modificado | Atualiza a base de runtime. |
| [migrations.Dockerfile](../../../deploy/production/migrations.Dockerfile) | modificado | Atualiza o SDK do bundle e a base de runtime. |
| [Dockerfile da web](../../../frontend/web/Dockerfile) | modificado | Atualiza nginx e fixa `zlib1g` corrigido. |
| [ci-cd-seguranca.md](../../../docs/ci-cd-seguranca.md) | documentação | Registra versões, manutenção e licenças nas fontes oficiais. |
| [quickstart.md](../../../specs/011-padronizacao-cicd/quickstart.md) | documentação | Registra a validação local e o gate remoto pendente. |

## Testes Adicionados ou Atualizados

- O gate existente de `validate-release-images.ps1` continua cobrindo as cinco
  imagens completas, sem alterar a seleção de severidades ou dispensar achados.
- As verificações locais também cobrem usuário, entrypoint, liveness por nginx,
  fallback da SPA e migrations reais com roles de menor privilégio.

## Verificação Local

- `podman build --format docker` para as cinco imagens: aprovado.
- Trivy 0.72.0 sobre os cinco archives: SBOM CycloneDX válido e nenhum achado
  `HIGH,CRITICAL` corrigível nem secret no gate aplicado.
- O trace do bundle confirmou `hostfxr` e runtime pack `10.0.15`.
- PostgreSQL 18.4 isolado: 12 migrations aplicadas e reaplicação idempotente com
  `altivy_migrator`; o worker conectou usando `altivy_runtime`.
- API, BFF e web responderam pela borda nginx; o fallback da SPA também passou.
- Os contratos de CI/CD passaram em container sem rede.

## Desvios da Avaliação

- O smoke inicial da web usou o proxy herdado do host para um endereço interno;
  foi reexecutado com `--noproxy '*'` e passou pela configuração nginx prevista.
- O primeiro smoke do worker esperava uma mensagem de log que não apareceu;
  a verificação foi substituída pela observação de conexões PostgreSQL reais.

## Acompanhamento

- Confirmar os cinco builds, scans e atestações no CI real; o Podman local não
  substitui Docker BuildKit nem valida proveniência carregada no Engine.
- O bundle emite aviso de biblioteca GSSAPI ausente durante a conexão de teste.
  A mesma biblioteca está ausente na base anterior; as migrations com `trust`
  não exigiram GSSAPI no laboratório. Autenticação GSS não foi validada.

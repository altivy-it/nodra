# Avaliação de Bug: BuildKit sem suporte a atestações no CI

- **Slug**: ci-buildkit-atestacoes
- **Criada em**: 2026-11-18
- **Origem**: normalização do [PR 32](https://github.com/altivy-it/nodra/pull/32)
- **Host interpretado**: github.com
- **Política de consulta**: allowlisted; consulta autenticada existente.
- **Veredito**: valid
- **Severidade**: high

## Relato

O job `Container Images` do PR 32 falha antes de produzir as evidências de
segurança. O usuário solicitou alinhar os PRs antes de continuar a spec.

## Sintoma

O build solicita proveniência e SBOM, mas o driver Docker do runner não
suporta atestações com o armazenamento de imagens disponível. O CI deve
construir e escanear as imagens sem desabilitar essas evidências.

## Reprodução

1. Consultar a
   [execução do PR 32](https://github.com/altivy-it/nodra/actions/runs/43464619051),
   job `Container Images`.
2. Executar `validate-release-images.ps1 -Image all -Tag ci` no runner com
   driver `docker` e image store clássico.
3. Observar `Attestation is not supported for the docker driver`; não são
   criados os relatórios que o passo seguinte tenta anexar.

O job de imagens do PR 31 falhou separadamente por resolução DNS de
`archive.ubuntu.com` e `security.ubuntu.com`. Esse erro não comprova o mesmo
defeito de atestação e precisa de nova execução para esclarecer a causa.

## Caminhos de Código Suspeitos

- [.github/workflows/ci.yml](../../../.github/workflows/ci.yml) — job
  `Container Images` usa o Docker padrão do runner.
- [validate-release-images.ps1](../../../tools/scripts/validate-release-images.ps1)
  — usa `buildx build --load --provenance=mode=max --sbom=true`, seguido de
  `docker save` e scan Trivy.
- [tasks.md](../../../specs/011-padronizacao-cicd/tasks.md) — T033 exige
  qualificação real de BuildKit, Trivy e atestações.

## Hipótese de Causa Raiz

Confiança **high**: o job usa o driver `docker` com o image store clássico.
Conforme a [documentação Docker](https://docs.docker.com/build/metadata/attestations/),
esse armazenamento não suporta índices com atestações. Apenas trocar para o
driver `docker-container` não resolve a carga com `--load` no store clássico.

## Correção Proposta

**Preferencial**: configurar, somente no runner efêmero do job, um Docker
Engine fixado com containerd image store antes do build. Uma alternativa
oficial é `docker/setup-docker-action` com `daemon-config`; a versão
[v5.7.0](https://github.com/docker/setup-docker-action/tree/v5.7.0) corresponde
ao commit `3f1ab1ab0e29231562e4db4ca600c1a49653bac6` e declara
[Apache-2.0](https://github.com/docker/setup-docker-action/blob/v5.7.0/LICENSE).
A action usa `latest` como padrão do Engine; a versão escolhida também precisa
ser fixada e verificada. Não alterar o daemon do host compartilhado.

Antes de aplicar, demonstrar que o build mantém proveniência e SBOM no índice
da imagem carregada e que o arquivo produzido por `docker save` continua
legível pelo Trivy. A documentação de armazenamento também alerta sobre
[arquivos incompletos com lazy pulling](https://docs.docker.com/engine/storage/containerd/#saving-and-pushing-images);
a configuração e o teste precisam evitar ou detectar esse risco.

**Alternativa**: exportar um arquivo OCI atestado com outro driver e validar
o scan diretamente, preservando também o comportamento de imagens locais
esperado pelos consumidores. Sua compatibilidade ainda não foi demonstrada.

**Arquivos Prováveis de Alteração**:

- `.github/workflows/ci.yml`
- `tools/scripts/test-cicd-contracts.ps1`
- `tools/scripts/validate-release-images.ps1`, se a qualificação exigir
  verificações adicionais do artefato.
- `specs/011-padronizacao-cicd/quickstart.md`

**Testes a Adicionar ou Atualizar**:

- Validar que o runner seleciona store compatível sem remover `--sbom` ou
  `--provenance`, e que configuração incompatível falha fechado.
- Em Docker/BuildKit real isolado, construir as cinco imagens, conferir
  proveniência e SBOM, e executar ambos os scans Trivy sobre os arquivos.
- Reexecutar o job remoto após atualização autorizada, sem push de imagem,
  release ou deploy.

## Riscos e Considerações

- O comando `docker` disponível neste host é uma emulação Podman, não um
  Docker Engine com BuildKit; não comprova a correção proposta.
- Reiniciar ou reconfigurar o daemon de um host compartilhado pode ocultar
  imagens existentes; a mudança deve limitar-se ao runner efêmero do CI.
- Não desativar atestações, dispensar o scanner ou publicar imagens para
  contornar a falha.
- O suporte ao runner, a integridade de `docker save` e a presença efetiva das
  duas atestações ainda precisam de evidência; T033 permanece pendente.

## Perguntas em Aberto

- Qual versão fixa do Docker Engine atende ao runner e aos testes de
  atestações e exportação, após verificação da versão exata?

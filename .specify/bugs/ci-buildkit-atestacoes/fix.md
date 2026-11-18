# Correção de Bug: BuildKit sem suporte a atestações no CI

- **Slug**: ci-buildkit-atestacoes
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

O job de imagens passa a iniciar um Docker Engine fixado em runner efêmero,
com image store containerd e download completo das camadas. O build mantém as
atestações e os scans; o CI também verifica seu conteúdo na imagem carregada.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| `.github/workflows/ci.yml` | modificado | Engine 29.9.3 isolado, store compatível e verificação das cinco imagens, sem publicar ou implantar. |
| `tools/scripts/validate-release-images.ps1` | modificado | Gerador SBOM Syft 1.12.1 fixado por digest; preservados `--load`, proveniência máxima e os dois scans Trivy. |
| `tools/scripts/assert-buildkit-attestations.ps1` | adicionado | Valida documentos SPDX, proveniência SLSA v1 máxima e digest comum sem registrar o conteúdo. |
| `tools/scripts/test-cicd-contracts.ps1` | modificado | Cobre configuração, ordem do job, cinco comandos de build e recusas de atestações inválidas. |
| `docs/ci-cd-seguranca.md` e `specs/011-padronizacao-cicd/quickstart.md` | modificados | Documentam dependências, licenças, execução, evidências e limites. |

## Testes Adicionados ou Atualizados

- `test-cicd-contracts.ps1` — exige setup fixado, store compatível, ausência de
  `lazy-pull` e verificação das cinco imagens após o build.
- Fixtures de `assert-buildkit-attestations.ps1` — aceitam atestações válidas e
  recusam SBOM ausente ou incompleto, subject ausente ou divergente,
  proveniência mínima, predicado incorreto e JSON inválido.
- Dry-run de `validate-release-images.ps1` — confere cinco builds com
  atestações, cinco exports e ambos os scans Trivy, além do scan remoto sem rebuild.

## Verificação Local

- `test-cicd-contracts.ps1` em container SDK, sem rede → aprovado.
- `actionlint` 1.7.12 em container → workflow aprovado.
- Parser PowerShell, `--help` e `Get-Help` em container → três scripts aprovados.
- [Prova isolada no GitHub Actions](https://github.com/altivy-it/nodra/actions/runs/44365783571)
  → Docker Engine 29.9.3, SBOM, proveniência máxima, verificador definitivo e
  ambos os scans Trivy aprovados para uma imagem sintética e seu archive.
- As duas primeiras provas identificaram ausência de `DOCKER_HOST` e `HOME`
  somente leitura; a prova final corrigiu ambos e passou.
- As cinco imagens reais dependem do próximo CI autorizado do PR 32; a prova
  sintética não comprova publicação, republish nem integridade de release.

## Desvios da Avaliação

- O shorthand `--sbom=true` foi substituído por `--sbom=generator=...` para
  fixar o gerador por versão e digest, mantendo a atestação SBOM.
- Foi acrescentado um verificador pequeno e testável para conferir o conteúdo
  das atestações sem montar o socket Docker no container de validação.

## Acompanhamento

- Executar `/speckit-bug-test slug=ci-buildkit-atestacoes` e confirmar o CI
  remoto das cinco imagens depois de atualizar o PR 32.
- Manter T033 pendente até haver evidência de origem, publicação e republish.

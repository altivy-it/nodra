# Verificação de Bug: BuildKit sem suporte a atestações no CI

- **Slug**: ci-buildkit-atestacoes
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

A configuração corrigida gerou SBOM e proveniência e permitiu os dois scans
Trivy em Docker real para uma imagem sintética. A reprodução original com as
cinco imagens do projeto ainda depende da atualização remota do PR 32.

## Verificações Executadas

| Verificação | Comando / ação | Resultado | Notas |
|---|---|---|---|
| Reprodução equivalente | [Execução 44365783571](https://github.com/altivy-it/nodra/actions/runs/44365783571) | pass | Engine 29.9.3 isolado, `--load`, SBOM, proveniência máxima, verificador definitivo e dois scans do archive, sem publicação. |
| Reprodução original | Job `Container Images` do PR 32 após o push | not-run | As cinco imagens reais ainda não foram testadas com a correção. |
| Contratos e recusas | `test-cicd-contracts.ps1` em container sem rede | pass | Configuração do Engine, cinco builds e scans; atestações incompletas, divergentes ou inválidas são recusadas. |
| Regressão documental | `test-validate-repository.ps1` e `validate-repository.ps1` em container | pass | 10 cenários e 242 arquivos Markdown. |
| Workflows | `actionlint` 1.7.12 em container | pass | Todos os workflows. |
| Sintaxe e ajuda | Parser PowerShell, `--help` e `Get-Help` em container | pass | Três scripts alterados. |
| Análise estática | `PSScriptAnalyzer` 1.24.0 em container | fail | Dois avisos já existentes: variável automática `$matches` no validador de imagens e ausência de BOM no teste UTF-8. Nenhum aviso no script novo. |

O `PSScriptAnalyzer` foi instalado apenas no container descartável após a
tentativa com o módulo ausente. A versão utilizada declara licença MIT. Os
avisos também foram conferidos no conteúdo anterior ao diff; o projeto exige
PowerShell 7, e o teste já continha caracteres Unicode sem BOM.

## Trechos de Saída

```text
Contratos estruturais CI/CD aprovados; nenhuma operacao remota executada.
SBOM e proveniencia mode=max presentes na imagem carregada.
Arquivo Docker legivel pelo Trivy nos dois formatos.
Testes do validador concluídos: 10 cenários aprovados.
```

## Riscos Residuais

- O Docker local é uma emulação Podman e não comprova o driver BuildKit.
- A prova usa imagem sintética; as cinco imagens reais e seus relatórios ainda
  precisam passar no CI do PR.
- Publicação, republish e atestação de release permanecem fora desta prova.

## Recomendação

Após o push autorizado, conferir o job completo das cinco imagens, as
atestações e `Result` no mesmo SHA. Não marcar T033 como concluída apenas com
a prova sintética.

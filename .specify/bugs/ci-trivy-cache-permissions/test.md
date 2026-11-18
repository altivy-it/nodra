# Verificação de Bug: limpeza do cache do Trivy no CI

- **Slug**: ci-trivy-cache-permissions
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

A falha de permissões foi reproduzida em container e a alternativa de volume
descartável passou nas verificações locais. O job real ainda precisa ser
reexecutado após a correção das vulnerabilidades identificadas nas imagens.

## Verificações Executadas

| Verificação | Comando / ação | Resultado | Notas |
|---|---|---|---|
| Reprodução | `Remove-Item` com UID não privilegiado em cache privado de root | pass | Reproduziu o erro anterior em container descartável. |
| Volume real | `podman volume create`, container com cache privado e `podman volume rm` | pass | O engine removeu o volume sem depender das permissões do host. |
| Contratos | `pwsh -NoProfile -File tools/scripts/test-cicd-contracts.ps1` | pass | Volume e arquivos temporários são limpos; erros de scan e de limpeza permanecem visíveis. |
| Regressão documental | `test-validate-repository.ps1` e `validate-repository.ps1` | pass | Dez cenários e 248 documentos antes deste relatório. |
| Sintaxe e ajuda | Parser PowerShell e `validate-release-images.ps1 --help` | pass | Executados em container sem rede. |
| Análise estática | `PSScriptAnalyzer` 1.24.0 em container | pass | Nenhum erro; dois avisos preexistentes e informações de parâmetros posicionais. |
| Job remoto | `Container Images` após atualizar o PR | not-run | A correção ainda não foi enviada. |

## Trechos de Saída

```text
Falha de remoção reproduzida com UID não privilegiado.
Cache com permissões privadas removido via volume do engine.
Contratos estruturais CI/CD aprovados; nenhuma operacao remota executada.
```

## Riscos Residuais

- O Docker real do GitHub ainda precisa confirmar a limpeza.
- Uma interrupção abrupta pode deixar um volume temporário no engine.
- A imagem da API contém achados críticos e altos; o gate permanece obrigatório.

## Recomendação

Reexecutar o CI completo após corrigir as imagens vulneráveis. Não considerar
esta verificação como aprovação de publicação ou release.

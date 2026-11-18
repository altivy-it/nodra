# Avaliação de Bug: limpeza do cache do Trivy no CI

- **Slug**: ci-trivy-cache-permissions
- **Criada em**: 2026-11-18
- **Origem**: [execução do CI do PR 32](https://github.com/altivy-it/nodra/actions/runs/44368752970)
- **Host interpretado**: github.com
- **Política de consulta**: allowlisted; consulta autenticada existente.
- **Veredito**: valid
- **Severidade**: high

## Relato

O job `Container Images` construiu a imagem da API e executou o Trivy, mas
terminou com `Access to the path .../cache/fanal is denied` ao remover o
diretório temporário. A exceção de limpeza ocultou a falha anterior do scan.

## Sintoma

O cache persistido em bind mount contém arquivos criados pelo usuário do
container, que o runner não privilegiado não consegue remover. A limpeza deve
ser confiável e não deve ocultar falhas de build ou achados do scanner.

## Reprodução

1. Consultar o job `Container Images` da execução informada.
2. Observar o scan Trivy e a falha em `Remove-Item` no `finally`.
3. Criar, em container descartável, um diretório de cache de propriedade de
   root e executar `Remove-Item` com UID não privilegiado: a mesma exceção é
   reproduzida.
4. Consultar somente as contagens do artifact `image-security`: o relatório
   da API contém dois achados `CRITICAL` e 18 `HIGH`; eles não podem ser
   ignorados e exigem correção independente.

## Caminhos de Código Suspeitos

- `tools/scripts/validate-release-images.ps1` — monta o cache temporário do
  host em `/root/.cache/trivy` e remove o diretório no `finally`.
- `tools/scripts/test-cicd-contracts.ps1` — exercita o scan em dry-run, mas não
  verifica a propriedade do cache nem a limpeza após falha do scanner.

## Hipótese de Causa Raiz

Confiança **high**: o Trivy cria diretórios privados como root no bind mount,
enquanto o processo PowerShell do runner não é root. `Remove-Item` falha ao
atravessá-los e sua exceção substitui a falha que provocou a limpeza.

## Correção Proposta

**Preferencial**: manter o cache em volume Docker exclusivo e temporário,
compartilhado entre os scans e removido ao final, sem montar esse cache em
diretório do host. Preservar a saída de falhas de build e do scanner e recusar
falhas de limpeza explicitamente. Não alterar o gate `HIGH,CRITICAL`, nem
conceder ao scanner acesso ao socket Docker.

**Arquivos Prováveis de Alteração**:

- `tools/scripts/validate-release-images.ps1`
- `tools/scripts/test-cicd-contracts.ps1`
- Documentação e relatórios diretamente relacionados.

**Testes a Adicionar ou Atualizar**:

- Verificar criação, reutilização e remoção do volume em sucesso e falha.
- Verificar que uma falha de scanner continua visível e retorna erro.
- Confirmar dry-run sem efeitos colaterais, as cinco imagens e ambos os scans.
- Reexecutar o job real no GitHub, sem publicar imagens.

## Riscos e Considerações

- Docker Desktop, Docker rootless e o runner Linux devem usar a mesma solução
  sem depender de equivalência de UIDs entre host e container.
- Encerramento abrupto pode impedir a remoção do volume temporário.
- O artifact indica vulnerabilidades reais na imagem da API. Corrigir a
  limpeza não torna o gate verde e não autoriza dispensar esses achados.

## Perguntas em Aberto

- Nenhuma para a correção da limpeza; os achados de imagem exigem avaliação
  própria.

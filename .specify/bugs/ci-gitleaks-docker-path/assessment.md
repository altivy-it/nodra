# Avaliação de Bug: seleção ambígua do Docker no teste Gitleaks

- **Slug**: ci-gitleaks-docker-path
- **Criada em**: 2026-11-18
- **Origem**: validação local da correção `ci-gitleaks-falso-positivo`
- **Veredito**: valid
- **Severidade**: medium

## Relato

Ao executar a orquestração do teste Gitleaks, o PowerShell encontrou dois
caminhos de `docker` no `PATH` e tentou executar ambos como um único comando.

## Sintoma

O teste falha antes de criar o container, embora exista um executável Docker
compatível. A seleção deve respeitar a ordem de resolução do `PATH` e
executar somente um programa.

## Reprodução

1. Usar um ambiente PowerShell cujo `PATH` exponha `/usr/bin/docker` e
   `/bin/docker`.
2. Executar `pwsh -NoProfile -File tools/scripts/test-gitleaks.ps1`.
3. Observar erro informando que `/usr/bin/docker /bin/docker` não é um
   comando reconhecido; o teste não chega ao runner em container.

## Caminhos de Código Suspeitos

- [test-gitleaks.ps1](../../../tools/scripts/test-gitleaks.ps1) — a seleção
  com `Get-Command` pode retornar vários resultados e `$docker.Source` é
  usado como executável.

## Hipótese de Causa Raiz

Confiança **high**: `Get-Command -CommandType Application` retornou mais de um
objeto. Ao acessar a propriedade `Source` do conjunto e usar o resultado com
o operador de chamada, o PowerShell concatena caminhos incompatíveis com um
único executável.

## Correção Proposta

**Preferencial**: selecionar somente o primeiro executável retornado, mantendo
o erro explícito quando não houver Docker e preservando os argumentos,
isolamento e limpeza da orquestração.

**Arquivos Prováveis de Alteração**:

- `tools/scripts/test-gitleaks.ps1`
- `tools/scripts/test-cicd-contracts.ps1`

**Testes a Adicionar ou Atualizar**:

- Simular dois executáveis Docker no `PATH` e exigir que a orquestração
  selecione o primeiro, complete os passos esperados e remova o temporário.
- Executar a orquestração com o runtime compatível disponível e as imagens
  locais, sem acesso à rede durante os testes.
- Preservar os testes de detecção de credenciais da correção original.

## Riscos e Considerações

- O teste sintético não substitui execução com Docker Engine real no GitHub.
- O runtime local é Podman compatível; não alterar daemon compartilhado nem
  remover recursos preexistentes.
- Selecionar apenas aplicações, sem executar aliases ou funções inesperadas.

## Perguntas em Aberto

- Nenhuma para a correção local.

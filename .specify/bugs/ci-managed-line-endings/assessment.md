# Avaliação de Bug: hashes de arquivos gerenciados no CI

- **Slug**: ci-managed-line-endings
- **Criada em**: 2026-11-18
- **Origem**: normalização do [PR 32](https://github.com/altivy-it/nodra/pull/32)
- **Host interpretado**: github.com
- **Política de consulta**: allowlisted; consulta autenticada existente.
- **Veredito**: valid
- **Severidade**: high

## Relato

Ao verificar os bloqueios do PR 32, o job `Hygiene / Managed files` falhou
na execução do CI. A verificação local anteriormente registrada havia
aprovado os mesmos arquivos gerenciados.

## Sintoma

O checkout do GitHub Actions não corresponde aos hashes registrados no
manifesto, impedindo a aprovação do CI. Os arquivos devem ser verificáveis
com o mesmo resultado independentemente do sistema de checkout.

## Reprodução

1. Consultar a execução
   [43464619051](https://github.com/altivy-it/nodra/actions/runs/43464619051)
   do PR 32, no commit `da5fba6`.
2. Observar o job `Hygiene / Managed files`: 14 scripts são reportados como
   modificados.
3. Comparar os hashes de `.groundwork/manifest.json` com os blobs versionados
   por Git e com os arquivos do checkout local. Para esses 14 scripts, os
   hashes do manifesto correspondem ao conteúdo com CRLF; os blobs usam LF.
4. Verificar os atributos Git: os caminhos afetados não possuem `text` ou
   `eol` definidos em `.gitattributes`.

## Caminhos de Código Suspeitos

- `.groundwork/manifest.json` — registra os hashes dos arquivos gerenciados.
- `tools/scripts/ci/Test-ManagedFiles.ps1` — compara o SHA-256 dos bytes do
  checkout diretamente com o manifesto.
- `.gitattributes` — não define os finais de linha dos scripts gerenciados.
- `.github/workflows/gw-pr-hygiene.yml` — executa a verificação no CI Linux.

## Hipótese de Causa Raiz

Confiança **high**: os arquivos gerados foram registrados no manifesto com
CRLF, enquanto o Git armazenou os blobs com LF. O checkout local preserva
CRLF e passa na comparação; o checkout Linux usa LF e produz 14 divergências.
Os outros 17 arquivos conferidos pela seleção padrão correspondem aos blobs
versionados. A divergência não comprova edição indevida dos scripts.

## Correção Proposta

**Preferencial**: definir, sem alterar os scripts gerenciados nem seus hashes,
uma política de checkout que preserve exatamente os bytes exigidos pelo
manifesto para os scripts afetados. Revisar a abrangência dos padrões de
`.gitattributes` e a compatibilidade das chamadas aos scripts em Linux e
Windows. A solução precisa manter a detecção de alterações reais.

**Alternativa**: reconciliar a geração e a validação de hashes no catálogo
canônico, com atualização oficial verificada e finais de linha determinísticos.
Não editar hashes manualmente nem enfraquecer a verificação.

**Arquivos Prováveis de Alteração**:

- `.gitattributes` e testes de checkout, se a política de atributos for
  suficiente.
- Catálogo Altivy.Groundwork, caso a normalização tenha de ser corrigida na
  origem; sua atualização deve seguir o fluxo oficial.

**Testes a Adicionar ou Atualizar**:

- Verificar os hashes em checkouts limpos com configurações de finais de linha
  representativas de Linux e Windows.
- Confirmar que uma modificação real em arquivo gerenciado continua sendo
  rejeitada.
- Reexecutar `Hygiene / Managed files` e o agregador `Result` no PR após a
  atualização autorizada da branch.

## Riscos e Considerações

- A política de atributos não deve alterar scripts não relacionados nem
  quebrar sua execução em Linux.
- Não modificar arquivos gerenciados, inventar hashes ou desativar o gate.
- O build das imagens e o scanner de histórico possuem falhas independentes;
  esta avaliação não afirma que o PR ficará verde após corrigir os hashes.
- Não houve publicação, deploy ou alteração de proteção remota.

## Perguntas em Aberto

- A política de checkout escolhida continua compatível com futuras versões
  oficiais do catálogo e com todos os consumidores dos scripts?

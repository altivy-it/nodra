# Avaliação de Bug: falso positivo do Gitleaks no histórico

- **Slug**: ci-gitleaks-falso-positivo
- **Criada em**: 2026-11-18
- **Origem**: normalização do [PR 32](https://github.com/altivy-it/nodra/pull/32)
- **Host interpretado**: github.com
- **Política de consulta**: allowlisted; consulta autenticada existente.
- **Veredito**: valid
- **Severidade**: high

## Relato

O job `Gitleaks / Full History` falha no PR 32 e impede a aprovação do CI.
O usuário solicitou normalizar os PRs e deixá-los alinhados para continuar a
próxima etapa.

## Sintoma

A varredura de histórico sinaliza como possível chave de API uma tag estática
de imagem local usada por uma tarefa do VS Code. O gate deve permitir essa
ocorrência comprovadamente pública sem deixar de detectar credenciais.

## Reprodução

1. Consultar a
   [execução do PR 32](https://github.com/altivy-it/nodra/actions/runs/43464619051).
2. Gerar um patch por commit a partir das refs remotas e tags, com o mesmo
   formato utilizado pelo workflow, e varrer o diretório com
   `docker.io/lzocateli/gitleaks:8.30.1`, em modo `dir` e com `--redact`.
3. Observar saída `1` e um achado `generic-api-key` no patch do commit
   `bbb9846bb748b9175e175f4c2cea8942459d8730`, referente a
   `.vscode/tasks.json`. O valor sinalizado corresponde à tag local usada em
   `podman image inspect`; não é uma credencial.

A reprodução local preparou 79 commits. Uma varredura inicial com `--all`
também encontrou duas cópias em refs de checkpoint locais do agente, que não
fazem parte do checkout remoto do CI. Nenhum valor sinalizado foi exibido.

## Caminhos de Código Suspeitos

- [.github/workflows/gw-gitleaks.yml](../../../.github/workflows/gw-gitleaks.yml)
  — gera arquivos de patch e aceita `.gitleaks.toml`; é gerenciado pelo
  catálogo e não deve ser editado localmente.
- [.vscode/tasks.json](../../../.vscode/tasks.json) — contém o comando que
  inspeciona imagens locais com a tag confundida com uma credencial.
- `.gitleaks.toml` — ainda não existe; o workflow usa a configuração padrão.

## Hipótese de Causa Raiz

Confiança **high**: a regra genérica associa a combinação do nome da imagem
com sua tag a uma chave. Como o modo `dir` recebe patches inteiros, o caminho
do achado é o arquivo do commit, não o caminho original da tarefa. Uma
exceção apenas por caminho de patch ou apenas pelo valor seria ampla demais.

## Correção Proposta

**Preferencial**: manter todas as regras padrão e adicionar uma exceção para
`generic-api-key` somente em arquivos de patch e somente quando a linha
completa for idêntica ao comando público conhecido. A expressão deve aceitar
o delimitador de linha que o Gitleaks 8.30.1 inclui em linhas intermediárias
e representar o separador da tag sem criar um novo achado na própria
configuração. A correspondência por conteúdo, em vez de um único hash de
commit, também deve cobrir o mesmo comando após squash ou rebase.

Um protótipo em container aprovou o histórico remoto, uma cópia da linha em
outro commit e o patch da própria configuração. Também bloqueou a tag em outro
contexto e uma linha alterada com credencial sintética adicional.

**Alternativa**: corrigir no catálogo canônico a varredura para preservar o
caminho original do arquivo e, então, restringir a exceção por caminho e
valor. Isso exige evolução e distribuição oficiais do componente gerenciado.

**Arquivos Prováveis de Alteração**:

- `.gitleaks.toml`
- `tools/scripts/test-gitleaks.ps1`

**Testes a Adicionar ou Atualizar**:

- Reproduzir a ocorrência histórica e uma cópia idêntica em outro commit.
- Exigir que a mesma tag em outro contexto e uma linha modificada com
  credencial sintética continuem bloqueadas.
- Verificar que regras diferentes e os padrões incorporados continuam ativos.
- Varrer o patch da própria configuração e, após atualização autorizada,
  reexecutar `Full History` no GitHub.

## Riscos e Considerações

- A exceção deve corresponder à linha completa; não aceitar prefixos, trechos
  arbitrários nem arquivos inteiros.
- A configuração não pode desativar regras, ignorar commits inteiros ou
  depender do hash resultante de squash ou rebase.
- Mudanças na tarefa podem exigir revisão da exceção; falhar fechado é
  preferível a aceitar uma linha modificada.
- O workflow gerenciado e a proteção de branch permanecem inalterados.

## Perguntas em Aberto

- Nenhuma para a correção local proposta; o resultado remoto exige atualização
  autorizada da branch e execução do CI.

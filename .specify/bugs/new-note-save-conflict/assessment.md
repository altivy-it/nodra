# Avaliação de Bug: salvamento automático reenvia versão desatualizada

- **Slug**: new-note-save-conflict
- **Criada em**: 2026-11-23T11:55:00-06:00
- **Origem**: texto colado e screenshot do usuário
- **Veredito**: valid
- **Severidade**: high

## Relato (literal ou resumido)

"Veja a mensagem no print, não estou conseguindo salvar novas notas". O
screenshot mostra a nota `nova-nota - minha nota 2.md` aberta no modo
Dividir, com a mensagem "Não foi possível salvar. A nota pode ter sido
alterada por outra pessoa." e o explorador sem a nova nota.

## Sintoma

Ao criar uma nota e continuar digitando enquanto o salvamento automático está
em andamento, a interface mostra erro de conflito, embora não exista outra
pessoa editando. O arquivo é criado no vault (`nova-nota - minha nota 2.md`
existe em `lab-0001`), mas o explorador não o lista até ser recarregado. Se o
salvamento rejeitado for o último da fila, o texto digitado durante o
salvamento não é gravado. O esperado é gravar todas as edições sem conflito
falso.

## Reprodução

1. Abrir `https://localhost:7443/notes`, vault `lab-0001`, e clicar em **Nova nota**.
2. Editar o caminho e esperar mais de 700 ms (o salvamento automático dispara um `POST`).
3. Enquanto o `POST` está em andamento, digitar no conteúdo.
4. Observar a rede: `POST 201` seguido de `POST 409` para o mesmo caminho.
   Reproduzido com Playwright pelo nginx, com atraso de 1,5 s no `POST`
   (`test_new_note_race.py`, harness da sessão).

O mesmo mecanismo afeta notas existentes: um `PUT` enfileirado durante outro
`PUT` reaproveita o `If-Match` antigo e recebe `412`. Isso foi inferido pelo
código e não foi reproduzido separadamente.

## Caminhos de Código Suspeitos

- `frontend/web/src/app/app.ts:save()`: enfileira em `pendingSave` um snapshot
  da nota com o `eTag` conhecido no momento da edição.
- `frontend/web/src/app/app.ts:flushSave()`: processa o snapshot sem
  atualizar o `eTag` que o salvamento anterior acabou de obter. Um `eTag`
  vazio gera novo `POST`, que recebe 409; um `eTag` antigo gera `PUT` com
  `If-Match` vencido, que recebe 412.
- `frontend/web/src/app/core/notes.service.ts:save()`: atualiza `selected`
  com o novo `eTag`, mas a fila em `App` não o recebe. A mensagem genérica de
  conflito é usada para qualquer falha.
- O explorador (`FolderExplorerService`) não recarrega após a criação. Apenas
  `notes` é atualizado, por isso a nova nota não aparece até um recarregamento.

## Hipótese de Causa Raiz

A fila de salvamento automático guarda cópias imutáveis da nota com a versão
(`eTag`) vigente no momento da edição. Quando o usuário edita durante um
salvamento, a cópia enfileirada fica com a versão anterior à resposta do
servidor e o próximo envio é rejeitado pelo controle otimista de concorrência
como conflito. Confiança: high, com reprodução de `POST 201` seguido de
`POST 409`.

## Correção Proposta

**Preferencial**: em `App.flushSave`, antes de enviar cada item da fila,
completar o `eTag` com a versão mais recente conhecida para o mesmo caminho
(`notes.selected()`), já que ela só avança por respostas do próprio servidor.
Após criar uma nota (`POST` bem-sucedido), recarregar o explorador para listar
o novo arquivo.

**Alternativas**:
- Fazer `NotesService.save` tratar 409/412 relendo a nota e reenviando. Essa
  opção foi descartada porque mascararia conflitos reais e poderia sobrescrever
  edições de outra sessão.

**Arquivos Prováveis de Alteração**:
- `frontend/web/src/app/app.ts`
- `frontend/web/src/app/app.spec.ts`

**Testes a Adicionar ou Atualizar**:
- Uma edição durante um `POST` pendente deve gerar `PUT` com o `If-Match`
  devolvido, e não um segundo `POST`.
- Uma edição durante um `PUT` pendente deve usar o novo `ETag` no `PUT` seguinte.
- A criação de uma nota recarrega o explorador.

## Riscos e Considerações

- Atualizar o `eTag` apenas a partir de `selected` com o mesmo caminho evita
  sobrescrever conflitos reais de outra sessão, que continuam gerando 412.
- Problema observado à parte: o campo de caminho de uma nota já salva aceita
  digitação após o sufixo `.md` normalizado, o que gera nomes como
  `x.md y.md`. Fica fora do escopo desta correção.

## Perguntas em Aberto

- Nenhuma.

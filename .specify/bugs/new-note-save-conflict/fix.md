# Correção de Bug: salvamento automático reenvia versão desatualizada

- **Slug**: new-note-save-conflict
- **Corrigido em**: 2026-11-23T11:50:00-06:00
- **Avaliação**: ./assessment.md
- **Estado**: applied

## Resumo

A fila de salvamento automático passou a repassar ao item seguinte o `ETag`
devolvido pelo salvamento anterior quando o item foi enfileirado com a versão
que esse salvamento substituiu. Após criar uma nota, o explorador é
recarregado.

## Alterações

| Arquivo | Alteração | Notas |
|------|--------|-------|
| `frontend/web/src/app/core/notes.service.ts` | modificado | `save()` retorna `{ path, eTag }` em sucesso e `null` em falha. |
| `frontend/web/src/app/app.ts` | modificado | `flushSave()` encadeia o `ETag` do salvamento anterior (mesmo caminho normalizado e mesma versão enviada) e chama `explorer.reload()` após a criação. |
| `frontend/web/src/app/app.spec.ts` | teste adicionado | Dois casos em `salvamento automático`. |

## Destaques do Diff

```ts
if (previous && note.eTag === previous.sentETag && sameNotePath(note.path, previous.path)) {
  note = { ...note, eTag: previous.eTag };
}
const saved = await this.notes.save(note);
previous = saved ? { path: saved.path, sentETag: note.eTag, eTag: saved.eTag } : null;
if (saved && !note.eTag) this.explorer.reload();
```

Para preservar a detecção de conflitos reais, o `ETag` só é substituído
quando veio da resposta do próprio salvamento anterior. A alternativa de
adotar qualquer `ETag` de `selected` foi descartada.

## Testes Adicionados ou Atualizados

- `app.spec.ts` › `envia PUT com o ETag criado quando a nota nova é editada durante o POST`:
  impede o segundo `POST`, que recebia 409, verifica o `If-Match: "v1"` e
  confirma o recarregamento do explorador.
- `app.spec.ts` › `mantém o conflito quando a versão enfileirada não veio do salvamento anterior`:
  confirma que um 412 real continua exibindo o erro.

## Verificação Local

- `ng test --watch=false` (container `angular-cli:22.2.4`) → 117/117.
- Verificação negativa: sem o repasse do `ETag`, o primeiro teste falha com
  "found none … POST" (um segundo `POST` no lugar do `PUT`).
- `ng build --configuration production` → OK.

## Desvios da Avaliação

- A avaliação sugeria completar o `ETag` a partir de `notes.selected()`. A
  implementação usa o resultado do salvamento anterior da própria fila, que é
  mais restrito e evita adotar uma versão obtida por releitura que mascararia
  um conflito.

## Acompanhamento

- O campo de caminho de uma nota já salva aceita texto após `.md`, o que gera
  nomes como `x.md y.md`. Fica para um bug separado.

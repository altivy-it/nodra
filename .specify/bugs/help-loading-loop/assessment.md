# Avaliação de Bug: ajuda presa em "Carregando ajuda..."

- **Slug**: help-loading-loop
- **Criada em**: 2026-11-23
- **Origem**: texto colado
- **Veredito**: valid
- **Severidade**: high

## Relato (literal ou resumido)

> Não estou conseguindo carregar o md, fica a mensagem Carregando Ajuda, mas
> não consigo sair dessa funcionalidade

## Sintoma

Ao abrir o módulo Ajuda em `https://localhost:7443/notes`, o painel fica em
"Carregando ajuda..." indefinidamente e não oferece forma de fechar. O esperado
é exibir o conteúdo de `help.md` (ou o erro com nova tentativa) e permitir
voltar ao módulo anterior.

## Reprodução

1. Entrar no laboratório pela borda `https://localhost:7443/notes`.
2. Selecionar o vault `lab-0001`.
3. Clicar em "Ajuda" na barra de atividades.
4. Observado com Playwright: 558 requisições para `https://localhost:7443/help.md`
   em 3 s, todas `404`, e o texto "Carregando ajuda..." permanente.

## Caminhos de Código Suspeitos

- `frontend/web/src/app/shell/help.service.ts:19` — usa a URL absoluta
  `/help.md`, que ignora o base path `/notes`; o `appBasePathInterceptor` só
  prefixa `/api/v1/`, `/bff/` e `/hubs/`. `/notes/help.md` responde `200`.
- `frontend/web/src/app/shell/workspace-shell.ts:70-73` — o `effect` chama
  `help.load()`, cuja parte síncrona lê `this.loading()`; o effect passa a
  depender de `loading` e reexecuta `load()` sempre que a falha termina.
- `frontend/web/src/app/shell/help-panel.ts` — o botão "Fechar ajuda" só
  existe no estado carregado; em carregamento e erro não há saída.

## Hipótese de Causa Raiz

A URL absoluta gera 404 sob o base path `/notes`. Cada falha devolve
`loading` para `false`, o que reexecuta o `effect` (dependência acidental de
`loading`) e dispara nova requisição: um loop infinito que mantém o painel em
"Carregando ajuda...". Como o painel não mostra o botão de fechar fora do
estado carregado, o usuário fica preso. Confiança: high (reproduzido).

## Correção Proposta

**Preferencial**: buscar `help.md` com URL relativa ao `<base href>` (via
`appPath`/`document.baseURI`), executar `help.load()` e
`trashSettings.load()` dentro de `untracked` no `effect` do shell, e exibir o
cabeçalho com "Fechar ajuda" em todos os estados do painel.

**Alternativas**:
- Incluir `/help.md` no interceptor de base path: mistura asset estático com
  rotas proxiadas e não resolve o loop.

**Arquivos Prováveis de Alteração**:
- `frontend/web/src/app/shell/help.service.ts`
- `frontend/web/src/app/shell/workspace-shell.ts`
- `frontend/web/src/app/shell/help-panel.ts`
- `frontend/web/src/app/shell/help.service.spec.ts`
- `frontend/web/src/app/shell/workspace-shell.spec.ts`

**Testes a Adicionar ou Atualizar**:
- `HelpService` resolve `help.md` sob o base path.
- O shell, com a ajuda falhando, faz uma única requisição e exibe "Fechar ajuda".

## Riscos e Considerações

- Sem risco de dados; afeta qualquer implantação com base path diferente de `/`.
- O loop gera carga contínua de requisições no proxy enquanto a ajuda está aberta.

## Perguntas em Aberto

- Nenhuma.

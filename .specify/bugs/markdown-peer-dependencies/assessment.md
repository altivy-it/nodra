# Avaliacao de Bug: peers Markdown incompatíveis

- **Slug**: markdown-peer-dependencies
- **Criada em**: 2026-11-11
- **Origem**: ERESOLVE revelado durante a correcao angular-peer-dependencies; usuario autorizou nesta conversa corrigir os peers existentes necessarios aos testes.
- **Veredito**: valid
- **Severidade**: high

## Relato

ngx-markdown@22.0.2 exige o peerOptional katex ^0.16.0, mas o manifest declara ^0.18.2 e o lockfile resolve 0.18.2. A instalacao estrita falha mesmo depois de alinhar o router Angular.

## Sintoma

A regeneracao do lockfile e a instalacao npm ci nao conseguem produzir uma arvore valida, bloqueando testes, build e verificacao visual.

## Reproducao

1. Usar a imagem oficial Angular do CI com frontend/web montado.
2. Alinhar router em 22.1.3 conforme avaliacao anterior.
3. Executar npm install --package-lock-only --ignore-scripts --no-audit --no-fund.
4. Observar o ERESOLVE entre katex@0.18.2 e o peer ^0.16.0 de ngx-markdown.

## Caminhos de Codigo Suspeitos

- frontend/web/package.json: faixa de KaTeX incompatível com o renderer Markdown instalado.
- frontend/web/package-lock.json: registra KaTeX 0.18.2 na raiz e versao 0.16.47 aninhada sob mermaid.

## Hipotese de Causa Raiz

Atualizacao isolada de KaTeX sem observar o peer do renderer. Confianca high: reproducao estrita e metadados oficiais npm confirmam o requisito. Os demais peers instalados consultados sao compativeis: marked ^17 ou ^18, mermaid >=10.6 <12 e marked-katex-extension ^5.

## Correcao Proposta

**Preferencial**: Fixar a dependencia direta KaTeX em 0.16.47, ja usada transitivamente pelo projeto, e regenerar o lockfile no container oficial sem flags permissivas. Manter a correcao Angular anterior e as demais atualizacoes integradas. Nao alterar codigo de renderizacao nesta correcao.

**Alternativas**: Atualizar ou substituir ngx-markdown ampliaria o escopo. Ignorar peers seria ocultar o defeito e nao e aceitavel.

**Arquivos Provaveis de Alteracao**:

- frontend/web/package.json
- frontend/web/package-lock.json

**Testes a Adicionar ou Atualizar**:

- Gate existente npm ci estrito seguido de testes e build de producao em container.
- Suites de renderizacao Markdown/matematica existentes, quando descobertas pelo runner.

## Riscos e Consideracoes

- KaTeX 0.16.47 tem licenca MIT, confirmada por npm view em 2026-11-11.
- Sem novos pacotes, mudanca de dados, identidade ou schema.
- Conferir o diff de lock e executar auditoria para nao trocar conflito de peers por dependencia vulneravel.
- Novo conflito fora desta fronteira deve ser registrado, sem forcar resolucao.

## Perguntas em Aberto

Nenhuma para a correcao delimitada autorizada pelo usuario.
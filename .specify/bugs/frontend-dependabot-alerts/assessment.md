# Avaliação de Bug: vulnerabilidades nas dependências do frontend

- **Slug**: frontend-dependabot-alerts
- **Criada em**: 2026-11-18
- **Origem**: alertas do Dependabot no GitHub
- **Veredito**: valid
- **Severidade**: critical

## Relato (literal ou resumido)

Há sete alertas abertos para `frontend/web/package-lock.json`:
cinco associados a Vite 8.1.5, um a KaTeX 0.16.47 e um a Piscina 5.2.0.
Os alertas abrangem execução de código, acesso fora da raiz permitida,
prototype pollution e execução remota de código em configurações específicas.

Referências: [alerta 28 — Piscina](https://github.com/altivy-it/nodra/security/dependabot/28),
[alertas 39–43 — Vite](https://github.com/altivy-it/nodra/security/dependabot),
e [alerta 31 — KaTeX](https://github.com/altivy-it/nodra/security/dependabot/31).
Os ranges e as versões corrigidas foram consultados nos metadados do Dependabot:
Piscina `>=5.0.0 <5.3.2` (corrigida em `5.3.2`), Vite `>=8.0.0 <=8.1.5`
(corrigida em `8.1.6`) e KaTeX `>=0.11.0 <0.18.2` (corrigida em `0.18.2`).

## Sintoma

O lockfile mantém versões afetadas de ferramentas de build e de uma
dependência de renderização em runtime. A correção precisa remover as versões
vulneráveis sem desabilitar auditoria nem quebrar os peers estritos do Angular,
do Markdown e do Mermaid.

## Reprodução

1. Consultar os sete alertas abertos para `frontend/web/package-lock.json`.
2. Conferir os pacotes resolvidos no lockfile: Vite 8.1.5, Piscina 5.2.0 e
   KaTeX 0.16.47, inclusive a cópia transitiva requerida por Mermaid.
3. Executar `npm ci` no container oficial do frontend e verificar a árvore
   instalada e a auditoria sem ignorar peers.

## Caminhos de Código Suspeitos

- `frontend/web/package-lock.json` — fixa as três versões afetadas e uma cópia
  transitiva de KaTeX sob Mermaid.
- `frontend/web/package.json` — declara KaTeX e versões compatíveis do builder,
  renderer Markdown e Mermaid.
- `frontend/web/src/app/shell/markdown-preview.spec.ts` — cobre renderização de
  fórmulas e transformação de blocos Mermaid.

## Hipótese de Causa Raiz

Confiança **high**: o builder Angular 22.1.0 fixa Piscina 5.2.0; a árvore
resolve Vite 8.1.5; e Mermaid 11.17.2 exige KaTeX `^0.16.47`, embora o
patch de segurança do KaTeX esteja em 0.18.2. A versão atual de ngx-markdown
também só aceita KaTeX `^0.16.0`, impedindo a elevação direta sem atualizar o
peer correspondente. O alerta crítico eleva a severidade geral; Vite e
Piscina são ferramentas de desenvolvimento, enquanto KaTeX também participa
da renderização em runtime.

## Correção Proposta

**Preferencial**: alinhar `@angular/build` a 22.2.4, compatível com o CLI já
declarado e com Piscina corrigida 5.3.2; atualizar Vite para 8.1.6; e atualizar
ngx-markdown para uma versão MIT que aceite KaTeX 0.18.2. Remover também a
cópia vulnerável exigida por Mermaid, mas somente após comprovar compatibilidade
da versão corrigida com os diagramas e fórmulas do projeto. Preservar a
instalação estrita com `npm ci`, sem `--force` ou `--legacy-peer-deps`.

**Alternativas**:
- Esperar Mermaid oferecer uma versão 11 compatível com KaTeX corrigido; isso
  mantém o peer atual, mas deixa o alerta runtime aberto.
- Aplicar override de KaTeX sob Mermaid apenas se os testes de renderização
  confirmarem compatibilidade; o override ultrapassa a faixa declarada por
  Mermaid e requer registro explícito desse risco.

**Arquivos Prováveis de Alteração**:

- `frontend/web/package.json`
- `frontend/web/package-lock.json`
- `frontend/web/src/app/shell/markdown-preview.spec.ts`, se for necessário
  acrescentar cobertura de renderização

**Testes a Adicionar ou Atualizar**:

- Executar `npm ci --no-audit --no-fund` sem permissões de peer relaxadas.
- Executar a suíte Vitest e o build de produção no container oficial.
- Confirmar que todas as ocorrências resolvidas de Vite, Piscina e KaTeX estão
  em versões corrigidas e que os sete alertas deixam de ser reportados.
- Cobrir renderização de KaTeX e transformação/execução controlada de Mermaid.

## Riscos e Considerações

- Forçar KaTeX 0.18.2 sob Mermaid 11.17.2 excede o range declarado por Mermaid;
  não adotar sem teste de compatibilidade.
- Atualizar `@angular/build` pode alterar dependências transitivas; revisar o
  diff do lockfile e manter a versão do CLI alinhada.
- Os alertas são observações do GitHub sobre a branch padrão; checks verdes de
  PR não significam que foram removidos da branch padrão antes do merge.
- Qualquer atualização precisa manter licenças permissivas verificadas para
  as versões exatas.

## Perguntas em Aberto

- Nenhuma para investigar a causa; a correção de KaTeX sob Mermaid depende de
  compatibilidade demonstrável.

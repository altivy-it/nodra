# Basalt Notes - Implementação do Frontend

## 1. Objetivo

Definir a arquitetura e o plano de implementação da aplicação Angular do Basalt Notes. As decisões de produto e arquitetura global permanecem no [Plano Mestre](../README.md).

## 2. Stack

- Angular 21 com componentes standalone;
- TypeScript em modo estrito;
- Angular Signals para estado local e derivado;
- RxJS para fluxos assíncronos e integrações;
- Angular Router com lazy loading por área;
- Angular CDK para acessibilidade, overlays, drag and drop e virtual scroll;
- CodeMirror 6 para edição Markdown;
- Cytoscape.js para grafos;
- cliente OpenAPI gerado para comunicação HTTP;
- SignalR client para eventos do servidor;
- Vitest para testes unitários e Playwright para E2E.

Evitar uma biblioteca visual completa como dependência estrutural. O design system deve usar Angular CDK, tokens próprios e componentes acessíveis, permitindo uma identidade visual distinta e controle sobre densidade e desempenho.

## 3. Arquitetura da aplicação

```text
src/app/
├── core/                 # bootstrap, auth, HTTP, config e telemetria
├── shell/                # layout, navegação, painéis e command palette
├── shared/               # componentes e utilitários sem regra de domínio
├── features/
│   ├── editor/
│   ├── explorer/
│   ├── search/
│   ├── graph/
│   ├── properties/
│   ├── backlinks/
│   ├── settings/
│   └── administration/
├── data-access/          # clientes gerados, DTOs e adapters
└── design-system/        # tokens, ícones e componentes base
```

Cada feature expõe apenas rotas, componentes públicos e uma fachada quando necessário. Componentes não acessam diretamente detalhes do cliente HTTP.

## 4. Estado e comunicação

### 4.1 Categorias de estado

- **estado do servidor:** notas, resultados, relações e configurações vindas da API;
- **estado de sessão:** usuário, vault atual, permissões e conexão SignalR;
- **estado de workspace:** abas, painel ativo, seleção e layout;
- **estado efêmero:** menus, dialogs, hover e texto de busca ainda não submetido.

Signals devem representar estado local e derivações. RxJS deve permanecer nas bordas assíncronas. Não criar um store global único; cada feature deve possuir estado de escopo conhecido.

### 4.2 Concorrência e salvamento

- carregar a nota com `ETag` e versão;
- manter estado `clean`, `dirty`, `saving`, `saved`, `conflict` ou `error`;
- autosave após período configurável de inatividade;
- enviar `If-Match` em atualizações;
- ao receber conflito, preservar as duas versões e abrir comparação;
- nunca substituir conteúdo local sem ação explícita do usuário;
- usar IndexedDB apenas como recuperação de rascunho, não como fonte de verdade.

## 5. Experiência principal

### 5.1 Layout desktop

- barra vertical de atividades com ícones e tooltips;
- painel lateral redimensionável para arquivos, busca, tags e favoritos;
- área central com abas e editor;
- painel contextual opcional para outline, propriedades e backlinks;
- barra de status discreta para salvamento, posição, links e indexação;
- command palette acessível por teclado.

Não usar cards decorativos para estruturar a área de trabalho. O produto deve parecer uma ferramenta densa, previsível e confortável para uso prolongado.

### 5.2 Editor

Primeira entrega:

- modo fonte Markdown;
- realce de sintaxe, pares, listas, tabelas e blocos de código;
- autocomplete para `[[wiki links]]`, tags e propriedades;
- preview seguro sincronizado;
- busca e substituição;
- outline clicável;
- atalhos de formatação;
- suporte a drag and drop e paste de anexos;
- restauração de posição e seleção por aba.

Evolução posterior:

- live preview preservando Markdown;
- embeds de notas e blocos;
- diagramas e fórmulas sob política explícita;
- folding e edição de propriedades estruturadas.

### 5.3 Árvore de arquivos

- virtualização para vaults grandes;
- expansão carregada sob demanda;
- criação, rename, move e exclusão com confirmação contextual;
- drag and drop com validação de destino;
- indicação de arquivo alterado, conflito e link não resolvido;
- menu de contexto e comandos equivalentes por teclado;
- atualização otimista somente quando a reversão for segura.

### 5.4 Pesquisa

- campo global com resultados incrementais e cancelamento de requisições;
- abas ou filtros para textual, semântica e comandos;
- filtros por caminho, tag, propriedade, tipo e data;
- snippets destacados retornados pela API;
- indicação do motivo do resultado: texto, título, link ou similaridade;
- paginação por cursor e lista virtualizada;
- busca semântica escondida ou desabilitada quando não configurada.

### 5.5 Grafo

- grafo local como padrão para navegação cotidiana;
- grafo global sob demanda;
- layouts executados em Web Worker quando aplicável;
- filtros por pasta, tag, tipo de link, data e profundidade;
- legenda, zoom, fit, foco e navegação por teclado;
- limite inicial de nós com expansão progressiva;
- painel de detalhes ao selecionar nó ou aresta.

## 6. Design system

### 6.1 Direção visual

O Basalt Notes deve transmitir precisão, permanência e foco. A interface terá superfícies neutras, tipografia editorial no conteúdo e tipografia funcional nos controles. Cores de destaque serão reservadas para ações, links, seleção e estados.

Tokens mínimos:

- cores de superfície, texto, borda, foco, seleção e estados;
- tipografia de interface, conteúdo e código;
- escala de espaçamento baseada em 4 px;
- raios de até 8 px;
- elevação restrita a overlays;
- densidade compacta, confortável e configurável;
- temas claro, escuro e alto contraste.

### 6.2 Componentes base

- icon button, button e split button;
- input, combobox, select, checkbox, toggle e slider;
- tabs, tree, list e data table;
- tooltip, menu, popover, dialog e toast;
- resizable panel e split view;
- empty, loading, offline, forbidden e error states;
- skeleton apenas quando o layout final for conhecido.

Usar Lucide Angular para ícones, com labels acessíveis e tooltips em ações não óbvias.

## 7. Segurança no browser

- não confiar em HTML vindo da API ou do Markdown;
- sanitizar a saída usando política única e testada;
- não usar `bypassSecurityTrustHtml` em features;
- bloquear `javascript:`, data URLs não permitidas e iframes arbitrários;
- adotar CSP sem `unsafe-eval` em produção;
- evitar tokens persistidos em `localStorage` quando autenticação por cookie for possível;
- não registrar conteúdo de notas em telemetria ou console;
- validar origem e tipo das mensagens SignalR.

## 8. Desempenho

- lazy loading por rota e feature;
- orçamento de bundle monitorado no CI;
- virtualização para árvore, resultados e listas extensas;
- Web Workers para parsing auxiliar e layouts pesados;
- cancelamento de buscas obsoletas;
- atualização incremental do grafo;
- carregamento de anexos e preview sob demanda;
- medição de Web Vitals e operações específicas do editor.

Metas iniciais em desktop corporativo intermediário:

- shell interativo em até 2,5 segundos após cache frio em rede local;
- troca entre notas abertas em até 100 ms;
- digitação sem tarefas acima de 50 ms no fluxo comum;
- árvore fluida com pelo menos 20 mil entradas virtualizadas.

## 9. Acessibilidade e responsividade

- WCAG 2.2 AA como meta;
- foco visível e ordem de tabulação previsível;
- atalhos remapeáveis e sem conflito obrigatório com navegador;
- labels, roles e anúncios adequados para screen readers;
- redução de movimento respeitando preferência do sistema;
- áreas redimensionáveis operáveis por teclado;
- tablet com painéis recolhíveis;
- celular voltado inicialmente a consulta e edição simples.

## 10. Testes

### 10.1 Unitários e componentes

- transições de estado de salvamento;
- resolução de comandos e atalhos;
- adapters entre DTOs e modelos de tela;
- componentes de árvore, tabs, filtros e propriedades;
- sanitização e renderização de casos maliciosos;
- acessibilidade automatizada dos componentes base.

### 10.2 Ponta a ponta

- login local e federado simulado;
- criar, editar, autosalvar e reabrir nota;
- conflito entre duas sessões;
- criar link e navegar por backlink;
- mover e renomear nota;
- upload e renderização segura de anexo;
- pesquisar e filtrar;
- navegar pelo grafo;
- recuperação de rascunho após interrupção.

Testar em Chromium, Firefox e WebKit, com viewports desktop e móvel representativos.

## 11. Sequência de implementação

1. shell, design tokens, roteamento e configuração em runtime;
2. autenticação, sessão e seleção de vault;
3. árvore de arquivos e contrato de notas;
4. CodeMirror, abas, preview e salvamento com ETag;
5. properties, outline, wiki links e backlinks;
6. pesquisa e command palette;
7. grafo local e global;
8. anexos, lixeira, histórico e administração;
9. acessibilidade, desempenho, telemetria e hardening.

## 12. Critérios de aceite do frontend

- nenhum fluxo essencial exige mouse;
- estados de carregamento, vazio, conflito, offline, erro e proibição são tratados;
- conteúdo nunca é perdido silenciosamente em conflito ou falha de rede;
- editor mantém desempenho nas dimensões suportadas;
- interface funciona nos navegadores definidos no plano mestre;
- testes E2E cobrem os principais caminhos de usuário;
- não há HTML não sanitizado nem conteúdo de notas em logs;
- bundle e acessibilidade respeitam os limites definidos no CI.
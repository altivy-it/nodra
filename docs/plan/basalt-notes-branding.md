# Plano de identidade visual e branding do Basalt Notes

**Status:** draft  
**Data de criação:** 2026-07-29  
**Última atualização:** 2026-07-29  
**Responsáveis:** equipe Basalt Notes  
**Origem:** proposta humana normalizada por IA assistida  
**Revisor humano:** pendente  
**Relacionado:** [Plano mestre](../../README.md), [Implementação do frontend](../frontend.md) e ADR de design system a avaliar

## Objetivo

Definir, validar e entregar uma identidade visual original e acessível para o Basalt Notes, composta por conceito de marca, logotipo, ícone, paleta, tipografia, tokens e arquivos de distribuição, sem criar dependência operacional de serviço proprietário ou internet.

O resultado deve comunicar precisão, permanência, conhecimento conectado e operação self-hosted, mantendo consistência entre aplicação, documentação, repositório, PWA e imagens de container.

## Contexto e premissas

- o nome Basalt Notes associa notas e gestão de conhecimento à solidez e durabilidade do basalto;
- a direção visual normativa do frontend exige precisão, permanência, foco, superfícies neutras, tipografia editorial no conteúdo e tipografia funcional nos controles;
- o conceito inicial preferido combina uma forma mineral de basalto com nós e conexões de um grafo de conhecimento;
- as alternativas iniciais são pedra hexagonal, letra `B` conectada e colunas basálticas;
- nenhuma alternativa, cor, fonte, slogan ou arquivo gerado está aprovada neste momento;
- toda marca deve funcionar nos temas claro, escuro e alto contraste, em monocromia e em tamanhos reduzidos;
- assets finais devem operar localmente e ser distribuídos com a aplicação, sem CDN, fonte remota ou chamada a serviço externo em runtime;
- IA generativa pode apoiar a exploração, mas não é requisito do produto nem substitui revisão humana, análise de originalidade ou preparação técnica dos assets;
- referências de mercado servem apenas para comparação de legibilidade e aplicação; o resultado não deve imitar identidade, ícone ou composição de terceiros;
- qualquer fonte, ferramenta ou asset externo exige licença da versão exata verificada e compatível com a política do repositório antes da adoção.

## Hipóteses visuais iniciais

#### Conceito de marca

**Basalto + grafo de conhecimento** é a hipótese principal. A forma mineral comunica solidez e durabilidade; nós e arestas comunicam links, backlinks e relações entre notas.

Valores a testar com usuários e responsáveis pelo produto:

- robustez;
- durabilidade;
- organização;
- confiabilidade;
- independência self-hosted;
- conexão entre informações;
- simplicidade.

Mensagens candidatas, ainda não aprovadas:

> Knowledge built on solid foundations.

> Your knowledge. Organized. Connected. Durable.

#### Alternativas de símbolo

1. **Pedra hexagonal:** forma mineral geométrica contendo uma rede de nós.
2. **Letra B conectada:** monograma construído por nós e linhas, associado a Basalt e backlinks.
3. **Colunas basálticas:** módulos hexagonais inspirados em formações naturais, adequados a ícones pequenos.

Evitar ícones genéricos de bloco de notas, reprodução de marcas concorrentes, detalhes frágeis em tamanho reduzido e dependência de cor para reconhecimento.

#### Paleta candidata

Os valores abaixo são pontos de partida, não tokens aprovados:

| Papel candidato | Cor |
| --- | --- |
| Basalt Black | `#1E1E1E` |
| Volcanic Gray | `#3D3D3D` |
| Stone Gray | `#707070` |
| Deep Teal | `#0F766E` |
| Azure Blue | `#2563EB` |
| Light Stone | `#E5E7EB` |
| Warm White | `#F8FAFC` |

A validação deve transformar cores aprovadas em tokens semânticos de superfície, texto, borda, foco, seleção e estado. A interface não deve ser dominada por uma única família cromática.

#### Tipografia candidata

Inter, Manrope e IBM Plex Sans são somente candidatas para a tipografia funcional. A tipografia editorial de conteúdo e a tipografia de código ainda precisam ser avaliadas.

Nenhuma fonte entra no produto antes de:

- verificar licença e arquivos da versão exata na fonte oficial;
- confirmar permissão para distribuição e uso self-hosted;
- avaliar legibilidade, caracteres portugueses, pesos necessários e impacto no bundle;
- definir fallback local adequado;
- hospedar os arquivos no próprio deployment, caso uma fonte externa seja aprovada.

## Não escopo

- renomear o produto ou alterar sua proposta de valor;
- criar site de marketing, campanha, material impresso ou estratégia comercial;
- registrar marca, domínio ou desenho industrial dentro deste plano;
- implementar a interface completa ou redefinir a arquitetura do design system;
- adotar serviço de IA, fonte remota ou ferramenta proprietária como dependência de runtime;
- considerar mockup, prompt ou imagem bruta gerada por IA como asset final;
- copiar linguagem visual, ícones, layouts ou elementos distintivos de outro produto.

## Dependências e decisões

- aprovação humana do brief, conceito, símbolo, slogan, paleta e tipografia;
- alinhamento com os tokens e requisitos descritos em [Implementação do frontend](../frontend.md);
- decisão sobre necessidade de ADR para tipografia, tooling ou dependência externa no design system;
- verificação documentada de licença, proveniência e termos aplicáveis a cada fonte, ferramenta e asset;
- revisão de originalidade e risco de confusão com marcas existentes antes da publicação;
- definição dos formatos, tamanhos e locais de integração no Angular, PWA, documentação, GitHub e containers;
- disponibilidade de testes de contraste, screenshots e build do frontend quando a aplicação existir;
- aprovação explícita antes de publicar ou substituir assets visíveis aos usuários.

## Etapas

### P-01: Consolidar brief e critérios

**Status:** in-progress  
**Responsável:** equipe de produto e design  
**Dependências:** nenhuma

**Objetivo:** transformar valores e restrições do produto em critérios observáveis para avaliar propostas.

Entregas:

- brief com público, personalidade, usos, restrições e atributos prioritários;
- lista de mensagens candidatas e decisão sobre necessidade de slogan;
- inventário de superfícies que receberão marca e ícone;
- critérios de legibilidade, acessibilidade, originalidade e operação offline;
- referências de mercado descritas por característica, sem instrução de imitação.

Validação:

- revisão humana confirma alinhamento com o plano mestre e o frontend;
- cada critério possui forma de avaliação e responsável;
- questões bloqueantes permanecem explícitas e sem decisão presumida.

Evidências:

- [docs/brand/brief.md](../brand/brief.md) — brief com público, personalidade, superfícies, critérios observáveis e questões bloqueantes. Aguarda revisão humana.

### P-02: Explorar conceitos visuais

**Status:** in-progress  
**Responsável:** design ou pessoa responsável pela identidade visual  
**Dependências:** P-01

**Objetivo:** produzir alternativas originais comparáveis sem tratar geração automática como decisão final.

Entregas:

- pelo menos três alternativas baseadas nas hipóteses de símbolo;
- variações com e sem wordmark, monocromáticas e para temas claro e escuro;
- testes preliminares em favicon, ícone PWA, cabeçalho e documentação;
- registro do processo, ferramentas e fontes de referência;
- matriz comparativa contra os critérios do brief.

Validação:

- alternativas permanecem reconhecíveis em `16x16`, `32x32` e `48x48` pixels;
- versões monocromáticas não dependem de cor para comunicar o símbolo;
- revisão humana seleciona uma direção ou solicita nova exploração;
- nenhum conceito selecionado apresenta semelhança material não resolvida com marca conhecida.

Evidências:

- [docs/brand/exploration/concept-a-basalt-graph.svg](../brand/exploration/concept-a-basalt-graph.svg) — hexágono flat-top + grafo de nós (hipótese principal).
- [docs/brand/exploration/concept-b-hexagonal-crystal.svg](../brand/exploration/concept-b-hexagonal-crystal.svg) — hexágono cristalino com facetas minerais.
- [docs/brand/exploration/concept-c-letter-b.svg](../brand/exploration/concept-c-letter-b.svg) — monograma B construído por nós e arestas.
- [docs/brand/exploration/comparison.md](../brand/exploration/comparison.md) — matriz comparativa com recomendação do Concept A. Aguarda seleção humana.

### P-03: Verificar licenças, proveniência e originalidade

**Status:** in-progress  
**Responsável:** responsáveis pelo repositório e revisão jurídica quando aplicável  
**Dependências:** P-02

**Objetivo:** impedir a adoção de asset, fonte ou ferramenta com origem, licença ou direito de distribuição incompatível.

Entregas:

- inventário de fontes, ferramentas, modelos e assets usados;
- evidência da licença e dos termos da versão exata de cada dependência;
- registro de proveniência dos arquivos e transformações realizadas;
- busca de similaridade e revisão de risco de confusão com terceiros;
- decisão humana documentada sobre uso, substituição ou bloqueio de cada item.

Validação:

- nenhum item com licença ausente, ambígua ou incompatível permanece na proposta;
- assets finais não contêm material de terceiros sem autorização verificável;
- a aplicação não depende de serviço de geração ou CDN em runtime;
- bloqueios e responsáveis estão registrados antes da implementação.

Evidências:

- [docs/brand/licenses.md](../brand/licenses.md) — inventário de fontes candidatas (Inter, Manrope, IBM Plex), ferramentas (SVGO, sharp) e assets produzidos nesta exploração. Decisão humana pendente por item.

### P-04: Definir paleta, tipografia e tokens

**Status:** in-progress  
**Responsável:** design e equipe frontend  
**Dependências:** P-01, P-03

**Objetivo:** converter a direção aprovada em tokens semânticos acessíveis e compatíveis com o design system.

Entregas:

- paletas para temas claro, escuro e alto contraste;
- tokens de superfície, texto, borda, foco, seleção, link e estados;
- famílias e fallbacks para interface, conteúdo e código;
- escala tipográfica e regras de aplicação do wordmark;
- documentação de contrastes e casos em que cor não pode ser o único sinal.

Validação:

- texto normal atinge contraste mínimo de `4.5:1`;
- texto grande e componentes visuais atingem no mínimo `3:1` quando aplicável;
- foco, seleção, erro, alerta e sucesso permanecem distinguíveis nos três temas;
- caracteres do português e conteúdo Markdown comum renderizam corretamente;
- nenhuma fonte é carregada por URL externa.

Evidências:

- [docs/brand/tokens/tokens.css](../brand/tokens/tokens.css) — CSS custom properties para os três temas (claro, escuro, alto contraste) com tokens semânticos e primitivos.
- [docs/brand/tokens/tokens.json](../brand/tokens/tokens.json) — tokens no formato DTCG com cores primitivas e semânticas por tema.
- [docs/brand/tokens/typography.md](../brand/tokens/typography.md) — papéis tipográficos, escala, fallbacks e critérios de acessibilidade. Aguarda aprovação de fonte.

### P-05: Produzir o sistema de marca

**Status:** in-progress  
**Responsável:** design  
**Dependências:** P-02, P-03, P-04

**Objetivo:** preparar arquivos finais consistentes, otimizados e reproduzíveis.

Entregas:

- logotipo horizontal e compacto;
- símbolo isolado e wordmark;
- versões para fundos claro, escuro e monocromático;
- SVG otimizado e PNGs nos tamanhos necessários;
- favicon, ícones PWA e avatar do repositório;
- área de proteção, tamanho mínimo e regras de uso indevido;
- arquivos-fonte e procedimento de exportação reproduzível.

Validação:

- SVGs não contêm scripts, referências externas, fontes embutidas desnecessárias ou metadados sensíveis;
- arquivos mantêm proporção, nitidez e legibilidade nos tamanhos definidos;
- PNGs e ícones correspondem ao SVG aprovado;
- revisão humana aprova o pacote final e registra a evidência.

Evidências:

- [docs/brand/assets/icon.svg](../brand/assets/icon.svg) — ícone isolado, tema claro.
- [docs/brand/assets/icon-dark.svg](../brand/assets/icon-dark.svg) — ícone, tema escuro.
- [docs/brand/assets/icon-mono.svg](../brand/assets/icon-mono.svg) — ícone monocromático.
- [docs/brand/assets/logo-horizontal.svg](../brand/assets/logo-horizontal.svg) — logo horizontal, tema claro.
- [docs/brand/assets/logo-horizontal-dark.svg](../brand/assets/logo-horizontal-dark.svg) — logo horizontal, tema escuro.
- [docs/brand/assets/logo-compact.svg](../brand/assets/logo-compact.svg) — logo empilhado.
- [docs/brand/assets/logo-mono.svg](../brand/assets/logo-mono.svg) — logo monochrome.
- [docs/brand/assets/favicon.svg](../brand/assets/favicon.svg) — variante simplificada para favicon. Todos aguardam aprovação humana; checksums e testes de contraste pendentes.

### P-06: Integrar e testar nos produtos

**Status:** not-started  
**Responsável:** equipe frontend e responsáveis pela documentação e containers  
**Dependências:** P-05

**Objetivo:** aplicar os assets aprovados sem regressão visual, funcional, de segurança ou desempenho.

Entregas:

- assets adicionados ao local proprietário do design system ou aplicação;
- favicon, manifest PWA, shell da aplicação e estados de carregamento atualizados;
- documentação e superfícies do repositório atualizadas de forma consistente;
- metadados de licença e proveniência mantidos junto aos assets;
- screenshots de referência em desktop e mobile para os três temas.

Validação:

- formatador, linter, build e testes do projeto frontend passam;
- Playwright confirma ausência de sobreposição, distorção e conteúdo ausente;
- inspeção de rede confirma que fontes e assets não exigem internet;
- CSP permanece sem relaxamento para carregar a identidade;
- orçamento de bundle e métricas de carregamento permanecem dentro dos limites do frontend.

Evidências:

- pendente.

### P-07: Publicar guia e governança da marca

**Status:** in-progress  
**Responsável:** equipe Basalt Notes  
**Dependências:** etapa de integração e testes nos produtos

**Objetivo:** tornar a identidade sustentável e evitar deriva ou uso inconsistente.

Entregas:

- guia curto de marca com usos corretos e incorretos;
- catálogo dos arquivos oficiais e seus checksums;
- responsáveis por aprovar alterações futuras;
- processo de atualização, depreciação e rollback de assets;
- registro das decisões, licenças e evidências finais.

Validação:

- links e arquivos do guia resolvem dentro do repositório;
- checksums correspondem aos assets publicados;
- uma pessoa sem contexto consegue identificar o asset correto para cada superfície;
- mudança futura exige revisão humana e repete validações de licença e acessibilidade.

Evidências:

- [docs/brand/guide.md](../brand/guide.md) — guia de marca com assets disponíveis, usos corretos, usos incorretos, área de proteção, contraste mínimo, catálogo e processo de aprovação. Checksums e responsáveis pendentes de preenchimento.

## Critérios de aceite finais

- conceito, símbolo, wordmark, paleta e tipografia possuem aprovação humana referenciada;
- o sistema comunica precisão, permanência e conhecimento conectado sem imitar terceiros;
- todos os assets possuem proveniência e direito de distribuição verificados;
- logotipo e ícone funcionam em temas claro, escuro, alto contraste e monocromático;
- favicon e ícones pequenos permanecem reconhecíveis nos tamanhos definidos;
- tokens de cor atendem aos critérios de contraste e não usam cor como único indicador;
- aplicação e documentação funcionam sem carregar fontes ou assets da internet;
- SVGs estão sanitizados e não contêm conteúdo executável ou referência externa;
- build, lint, testes e Playwright do frontend passam após a integração;
- guia, catálogo e procedimento de rollback correspondem aos arquivos publicados.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| semelhança com marca existente | confusão, disputa ou retrabalho | explorar alternativas, pesquisar similaridade e exigir revisão humana antes da publicação |
| asset gerado sem proveniência clara | bloqueio de distribuição | registrar ferramenta, processo e termos; redesenhar ou substituir quando houver dúvida |
| licença de fonte incompatível | impedimento de distribuição self-hosted | verificar versão exata antes da adoção e manter fallback sem dependência externa |
| símbolo perde legibilidade em tamanho pequeno | favicon e PWA irreconhecíveis | testar matriz de tamanhos desde a exploração e criar variante simplificada |
| contraste insuficiente | barreira de acessibilidade | medir combinações por token nos três temas e bloquear integração quando falhar |
| identidade domina a interface operacional | redução de densidade e foco | reservar marca a superfícies adequadas e preservar a direção utilitária do frontend |
| uso de CDN ou fonte remota | falha em ambiente isolado e risco de privacidade | hospedar assets aprovados localmente e testar sem internet |
| SVG não sanitizado | execução ou carregamento externo indesejado | otimizar, inspecionar e testar CSP antes da integração |
| mudança ampla sem rollback | regressão difícil de reverter | integrar em change set isolado, preservar assets anteriores e manter catálogo versionado |

## Rollout e rollback

- aprovar primeiro o brief e depois a direção visual; não produzir todo o pacote antes dessa decisão;
- integrar assets em ambiente de desenvolvimento e validar os três temas e tamanhos de viewport;
- promover para ambiente representativo somente após licença, acessibilidade, segurança e build passarem;
- substituir superfícies públicas em um change set rastreável e sem misturar refatorações não relacionadas;
- interromper a publicação diante de conflito de marca, licença incerta, contraste insuficiente, SVG inseguro ou dependência externa em runtime;
- no rollback, restaurar os assets e tokens anteriores, limpar caches versionados quando necessário e validar que manifest, favicon e documentação voltaram ao conjunto compatível;
- a reversão visual não deve exigir migração de dados nem alterar conteúdo de usuário.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-07-29 | plano | - | draft | conteúdo inicial incluído manualmente para orientar a identidade visual | responsável pelo projeto |
| 2026-07-29 | plano | draft | draft | normalização ao template sem aprovação das escolhas visuais | IA assistida |
| 2026-07-29 | todas as etapas | - | not-started | nenhuma etapa possui evidência de execução | IA assistida |
| 2026-07-29 | P-01 a P-05, P-07 | not-started | in-progress | entregáveis produzidos por IA assistida: brief, 3 SVGs de exploração, matriz comparativa, inventário de licenças, tokens CSS/JSON, tipografia, 8 SVGs de assets e guia de marca; todos aguardam revisão e aprovação humana | IA assistida |

## Regras de atualização

- o plano permanece `draft` até revisão humana do brief e das hipóteses iniciais;
- o status geral usa `draft`, `proposed`, `approved`, `in-progress`, `blocked`, `completed`, `cancelled` ou `superseded`;
- IA não eleva o status acima de `proposed` sem aprovação humana explícita e referenciada;
- etapas usam apenas `not-started`, `in-progress`, `blocked`, `completed` ou `skipped`;
- toda mudança de status adiciona uma entrada ao histórico;
- `completed` exige critérios finais atendidos e evidências registradas;
- `skipped` exige justificativa e aprovação humana;
- alterações de fonte, ferramenta, símbolo, paleta ou licença repetem as validações aplicáveis.

## Apêndice A: prompts exploratórios

Os prompts abaixo são insumos opcionais para exploração. O resultado não é
automaticamente original, licenciado, aprovado ou adequado como asset final.
Remova referências a marcas de terceiros, registre a ferramenta e submeta
qualquer saída às etapas de exploração, verificação de proveniência e produção
do sistema de marca.

#### Logotipo com símbolo e wordmark

```text
Create an original visual identity concept for a self-hosted knowledge management product named "Basalt Notes".

Combine an abstract basalt-inspired geometric form with a small knowledge graph made of connected nodes. Prioritize a distinctive silhouette, simple geometry, legibility at small sizes, and clear separation from existing software brands.

Explore a neutral mineral palette with a restrained teal accent. Include an original "Basalt Notes" wordmark using a generic sans-serif placeholder; do not reproduce a proprietary typeface or third-party logo.

Flat vector concept. White background. No shadows, gradients, mockups, mascots, stock symbols, or references to existing brand styles.
```

#### Ícone

```text
Design an original square icon concept for "Basalt Notes", a self-hosted Markdown knowledge platform.

Use an abstract basalt-inspired polygon containing a minimal network of connected nodes. The icon must remain recognizable at 16x16, 32x32, and 48x48 pixels and must also work in monochrome.

Use charcoal, neutral gray, and one restrained teal accent. No text, gradients, shadows, mockups, stock note symbols, or imitation of existing product icons.
```

#### Prancha de exploração

```text
Create an exploratory branding board for "Basalt Notes", a self-hosted Markdown knowledge management product.

Show three clearly different original directions based on basalt geometry and connected knowledge: a mineral polygon, an abstract connected letter B, and simplified basalt columns. For each direction, show symbol, temporary wordmark, monochrome version, light and dark backgrounds, favicon-size preview, candidate palette, and compact application-header usage.

Focus on precision, permanence, accessibility, and operational software. Do not imitate existing brands, generate marketing-page layouts, or present the concepts as legally cleared final assets.
```
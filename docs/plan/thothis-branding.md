# Plano de identidade visual e branding do Thothis

**Status:** blocked  
**Data de criação:** 2026-07-29  
**Última atualização:** 2026-08-01  
**Responsáveis:** equipe Thothis
**Origem:** proposta humana normalizada por IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano mestre](../../README.md), [Implementação do frontend](../frontend.md) e ADR de design system a avaliar

## Objetivo

Definir, validar e entregar uma identidade visual original e acessível para o Thothis, composta por conceito de marca, logotipo, ícone, paleta, tipografia, tokens e arquivos de distribuição, sem criar dependência operacional de serviço proprietário ou internet.

O resultado deve comunicar precisão, permanência, conhecimento conectado e operação self-hosted, mantendo consistência entre aplicação, documentação, repositório, PWA e imagens de container.

## Estado para retomada futura

A criação da marca está concluída: P-01 a P-05 foram aprovadas e entregam
brief, direção G, licenças, tokens, tipografia, fontes, SVGs, PNGs, checksums e
pipelines reproduzíveis. O logo também foi aplicado ao README.

O plano permanece `blocked`, e não `completed`, porque ainda faltam integração
e testes no produto. A retomada ocorre quando a aplicação Angular existir com,
no mínimo, `apps/web`, `angular.json`, `package.json` e manifest PWA. Nesse
momento:

1. mover P-06 de `blocked` para `in-progress`;
2. integrar assets, fontes, tokens, favicon, manifest e shell;
3. executar lint, build, testes e Playwright nos temas e viewports definidos;
4. concluir P-06 com as evidências geradas;
5. revisar o guia contra a integração real e concluir P-07;
6. marcar o plano `completed` somente após todos os critérios finais passarem.

Não é necessário reabrir P-01 a P-05, salvo mudança de nome, símbolo, paleta,
tipografia, licença ou risco de similaridade.

## Contexto e premissas

- o nome Thothis associa o produto a Thoth, à escrita, ao conhecimento, à memória e ao registro da informação;
- a plataforma estratégica candidata adota o arquétipo do Sábio e prioriza clareza, conhecimento, organização, confiabilidade, simplicidade, independência e inovação responsável;
- a direção visual normativa do frontend exige precisão, permanência, foco, superfícies neutras, tipografia editorial no conteúdo e tipografia funcional nos controles;
- os territórios visuais candidatos são escrita e registro, lua e tempo e uma síntese abstrata entre registro e conhecimento conectado;
- o conceito visual não deve copiar hieróglifos, representar divindades nem reproduzir símbolos religiosos ou arqueológicos;
- um monograma `T` pode ser explorado como tratamento tipográfico, mas não constitui direção obrigatória;
- a direção G, a paleta, a tipografia e o pacote de assets estão aprovados;
- toda marca deve funcionar nos temas claro, escuro e alto contraste, em monocromia e em tamanhos reduzidos;
- assets finais devem operar localmente e ser distribuídos com a aplicação, sem CDN, fonte remota ou chamada a serviço externo em runtime;
- IA generativa pode apoiar a exploração, mas não é requisito do produto nem substitui revisão humana, análise de originalidade ou preparação técnica dos assets;
- referências de mercado servem apenas para comparação de legibilidade e aplicação; o resultado não deve imitar identidade, ícone ou composição de terceiros;
- qualquer fonte, ferramenta ou asset externo exige licença da versão exata verificada e compatível com a política do repositório antes da adoção.

## Plataforma e hipóteses visuais iniciais

#### Plataforma estratégica candidata

O [brief de identidade visual](../brand/brief.md) é a fonte da formulação
completa. Para orientar as etapas deste plano, a marca parte destas hipóteses:

- **missão:** transformar informações dispersas em conhecimento acessível, conectado e útil;
- **visão:** ser uma plataforma de conhecimento confiável para pessoas e organizações que precisam preservar, relacionar e encontrar informação sob seu próprio controle;
- **arquétipo:** O Sábio, com linguagem clara, rigorosa e acessível, sem tom pretensioso, místico ou distante;
- **valores:** clareza, conhecimento, organização, confiabilidade, simplicidade, independência e inovação responsável.

Todas essas formulações permanecem candidatas e dependem de aprovação humana em P-01.

#### Conceito de marca

**Escrita, memória e tempo** formam o núcleo conceitual. Um gesto abstrato de
registro comunica escrita e preservação; ciclos e fases comunicam continuidade
e medição do tempo; relações discretas comunicam links, backlinks e contexto
entre notas sem se tornarem um grafo genérico.

Valores a testar com usuários e responsáveis pelo produto:

- clareza;
- conhecimento;
- organização;
- confiabilidade;
- independência self-hosted;
- simplicidade.
- inovação responsável;
- conexão entre informações;

Mensagens candidatas, ainda não aprovadas:

- `Knowledge Connected.`
- `Knowledge, written to endure.`
- `Write. Connect. Remember.`
- `From Information to Wisdom.`
- `Intelligence Built on Knowledge.`

Nenhuma mensagem está definida como tagline principal.

#### Alternativas de símbolo

1. **Escrita e registro:** gesto ou estrutura abstrata que comunique documentação, linguagem e preservação da informação.
2. **Lua e tempo:** ciclos, fases ou medição tratados geometricamente, sem ilustração literal ou iconografia religiosa.
3. **Síntese entre registro e conexão:** composição abstrata em que continuidade, ritmo ou relações entre formas comuniquem conhecimento conectado.

Um monograma `T` pode ser avaliado como tratamento tipográfico dentro das três
direções. Evitar íbis, constelações, redes neurais, livros, bibliotecas e grafos
literais, além de ícones genéricos de bloco de notas, reprodução de marcas
concorrentes, detalhes frágeis em tamanho reduzido e dependência de cor para
reconhecimento.

#### Paleta candidata

Os valores abaixo são pontos de partida, não tokens aprovados:

| Papel candidato | Cor |
| --- | --- |
| Archive Ink | `#1E1E1E` |
| Interface Gray | `#4A5568` |
| Muted Gray | `#718096` |
| Scribe Teal | `#0F766E` |
| Signal Blue | `#2563EB` |
| Light Border | `#E2E8F0` |
| Paper White | `#F8FAFC` |

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

**Status:** completed  
**Responsável:** equipe de produto e design  
**Dependências:** nenhuma

**Objetivo:** transformar valores e restrições do produto em critérios observáveis para avaliar propostas.

Entregas:

- brief com público, personalidade, usos, restrições e atributos prioritários;
- missão, visão, valores e arquétipo candidatos com decisão humana registrada;
- arquitetura de mensagens candidatas e decisão sobre necessidade de tagline;
- territórios visuais candidatos e limites para referências culturais;
- inventário de superfícies que receberão marca e ícone;
- critérios de legibilidade, acessibilidade, originalidade e operação offline;
- referências de mercado descritas por característica, sem instrução de imitação.

Validação:

- revisão humana confirma alinhamento com o plano mestre e o frontend;
- revisão humana confirma ou ajusta missão, visão, valores e arquétipo;
- teste de compreensão elimina mensagens que ampliem o escopo ou prometam capacidades inexistentes;
- cada critério possui forma de avaliação e responsável;
- questões bloqueantes permanecem explícitas e sem decisão presumida.

Evidências:

- [docs/brand/brief.md](../brand/brief.md) — brief aprovado por Lincoln Zocateli em 2026-07-31, com plataforma estratégica, direção G, tipografia e governança registradas.

### P-02: Explorar conceitos visuais

**Status:** completed
**Responsável:** design ou pessoa responsável pela identidade visual  
**Dependências:** P-01

**Objetivo:** produzir alternativas originais comparáveis sem tratar geração automática como decisão final.

Entregas:

- pelo menos três alternativas, uma por direção visual candidata;
- variações com e sem wordmark, monocromáticas e para temas claro e escuro;
- testes preliminares em favicon, ícone PWA, cabeçalho e documentação;
- registro do processo, ferramentas e fontes de referência;
- matriz comparativa contra os critérios do brief.

Validação:

- alternativas permanecem reconhecíveis em `16x16`, `32x32` e `48x48` pixels;
- versões monocromáticas não dependem de cor para comunicar o símbolo;
- referências a escrita, lua ou tempo permanecem abstratas e não reproduzem hieróglifos, divindades ou símbolos religiosos e arqueológicos;
- o resultado comunica o arquétipo do Sábio sem parecer pretensioso, místico ou distante;
- revisão humana seleciona uma direção ou solicita nova exploração;
- nenhum conceito selecionado apresenta semelhança material não resolvida com marca conhecida.

Evidências:

- [docs/brand/exploration.md](../brand/exploration.md) — três rodadas de símbolos candidatos, variações de tema e escala, matrizes preliminares e proveniência; direção G selecionada por Lincoln Zocateli em 2026-07-31 para verificação em P-03.

### P-03: Verificar licenças, proveniência e originalidade

**Status:** completed  
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

- [docs/brand/licenses.md](../brand/licenses.md) — inventário aprovado de fontes, ferramentas, assets, licenças e hashes.
- [docs/brand/similarity-review.md](../brand/similarity-review.md) — busca preliminar não encontrou marca ou logo idêntico; risco residual médio foi aceito explicitamente por Lincoln Zocateli.
- [docs/brand/exploration.md](../brand/exploration.md) — rodada 4 preservada como histórico de mitigação não adotado; G permanece selecionada.

### P-04: Definir paleta, tipografia e tokens

**Status:** completed  
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
- [docs/brand/tokens/typography.md](../brand/tokens/typography.md) — papéis tipográficos, escala, fallbacks, cobertura e orçamento aprovados e verificados.
- [docs/brand/fonts/SHA256SUMS](../brand/fonts/SHA256SUMS) — cinco WOFF2 self-hosted e licenças com hashes reproduzíveis.

### P-05: Produzir o sistema de marca

**Status:** completed
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

- [docs/brand/assets/dist/](../brand/assets/dist/) — cinco SVGs otimizados, sete PNGs e catálogo SHA-256 produzidos por script reproduzível.
- [tools/scripts/build-brand-assets.ps1](../../tools/scripts/build-brand-assets.ps1) — pipeline com imagem Node fixa, SVGO 3.3.2 e sharp 0.33.5.
- [tools/scripts/build-brand-fonts.ps1](../../tools/scripts/build-brand-fonts.ps1) — pipeline de fontes, orçamento, cobertura, hashes e wordmark em paths.
- pacote final aprovado por Lincoln Zocateli em 2026-08-01; risco residual registrado em P-03 foi aceito explicitamente.

### P-06: Integrar e testar nos produtos

**Status:** blocked  
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

- [README.md](../../README.md) — lockup claro integrado à superfície principal do repositório.
- bloqueio: em 2026-08-01 o repositório ainda não possui `apps/web`, `libs/frontend`, `angular.json`, `package.json` ou manifest PWA; favicon, shell, temas, build e Playwright dependem da criação da aplicação Angular em plano próprio.

### P-07: Publicar guia e governança da marca

**Status:** blocked
**Responsável:** equipe Thothis
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

- [docs/brand/guide.md](../brand/guide.md) — guia aprovado, catálogo, responsáveis, processo de atualização e rollback documentados; conclusão aguarda evidências de integração de P-06.
- condição de retomada: P-06 concluída com caminhos e superfícies reais da aplicação; então revisar o catálogo de uso e registrar a evidência final.

## Critérios de aceite finais

- conceito, símbolo, wordmark, paleta e tipografia possuem aprovação humana referenciada;
- missão, visão, valores, arquétipo e eventual tagline possuem decisão humana referenciada;
- o sistema comunica precisão, permanência e conhecimento conectado sem imitar terceiros;
- referências a escrita, lua e tempo permanecem abstratas e culturalmente responsáveis;
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
| referência cultural literal ou estereotipada | perda de originalidade e risco reputacional | trabalhar com abstrações formais, pesquisar contexto e bloquear hieróglifos, divindades e símbolos religiosos reconhecíveis |
| uso de CDN ou fonte remota | falha em ambiente isolado e risco de privacidade | hospedar assets aprovados localmente e testar sem internet |
| SVG não sanitizado | execução ou carregamento externo indesejado | otimizar, inspecionar e testar CSP antes da integração |
| mudança visual ampla | regressão difícil de corrigir | integrar em change set isolado e manter catálogo versionado dos assets aprovados |

## Rollout e rollback

- aprovar primeiro o brief e depois a direção visual; não produzir todo o pacote antes dessa decisão;
- integrar assets em ambiente de desenvolvimento e validar os três temas e tamanhos de viewport;
- promover para ambiente representativo somente após licença, acessibilidade, segurança e build passarem;
- substituir superfícies públicas em um change set rastreável e sem misturar refatorações não relacionadas;
- interromper a publicação diante de conflito de marca, licença incerta, contraste insuficiente, SVG inseguro ou dependência externa em runtime;
- em uma substituição, remover referências aos assets não aprovados, limpar caches versionados quando necessário e validar manifest, favicon e documentação;
- a reversão visual não deve exigir migração de dados nem alterar conteúdo de usuário.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-07-29 | plano | - | draft | conteúdo inicial incluído manualmente para orientar a identidade visual | responsável pelo projeto |
| 2026-07-29 | plano | draft | draft | normalização ao template sem aprovação das escolhas visuais | IA assistida |
| 2026-07-29 | todas as etapas | - | not-started | nenhuma etapa possui evidência de execução | IA assistida |
| 2026-07-31 | P-02, P-05 e P-07 | in-progress | not-started | conceitos e assets sem alinhamento com a identidade definida para Thothis foram removidos; nova exploração visual permanece pendente | IA assistida |
| 2026-07-31 | P-01 e P-02 | in-progress / not-started | in-progress / not-started | plataforma estratégica candidata e territórios de escrita/registro e lua/tempo consolidados por decisão humana, sem aprovação das propostas | IA assistida |
| 2026-07-31 | plano | completed | approved | aprovação humana explícita para executar o plano em fases; decisões e documentos associados permanecem sujeitos às aprovações próprias | Lincoln Zocateli |
| 2026-07-31 | P-02 | not-started | in-progress | três alternativas vetoriais originais produzidas para seleção humana, sem integração ou aprovação presumida | IA assistida |
| 2026-07-31 | P-02 | in-progress | in-progress | segunda rodada adiciona três novas silhuetas após solicitação humana, preservando a decisão como pendente | IA assistida |
| 2026-07-31 | P-02 | in-progress | in-progress | direção F indicada pelo revisor humano para refinamento; três variações adicionais produzidas sem aprovação final presumida | Lincoln Zocateli e IA assistida |
| 2026-07-31 | P-02 | in-progress | completed | direção G selecionada explicitamente pelo revisor humano; exploração comparativa concluída e encaminhada para verificação em P-03 | Lincoln Zocateli |
| 2026-07-31 | P-01 | in-progress | completed | brief, plataforma estratégica e decisões bloqueantes aprovados explicitamente pelo revisor humano | Lincoln Zocateli |
| 2026-07-31 | P-03 e P-04 | in-progress | in-progress | tipografia, tokens e itens selecionados aprovados; arquivos oficiais, hashes, similaridade e orçamento de bundle permanecem em verificação | Lincoln Zocateli e IA assistida |
| 2026-07-31 | P-05 | not-started | in-progress | pacote vetorial e raster inicial produzido com checksums e automação reproduzível; wordmark e verificações finais permanecem pendentes | IA assistida |
| 2026-08-01 | P-04 | in-progress | completed | fontes aprovadas produzidas localmente; cobertura portuguesa, licenças, hashes e orçamento inferior a 80 KB por família verificados | IA assistida |
| 2026-08-01 | P-05 | in-progress | in-progress | wordmark convertido em paths e lockups claro/escuro renderizados; aprovação final e pesquisa de similaridade permanecem pendentes | IA assistida |
| 2026-08-01 | P-05 | in-progress | blocked | pacote visual aprovado explicitamente por Lincoln Zocateli; pesquisa encontrou semelhança conceitual não resolvida com logos de estoque `T/Y + losango` | Lincoln Zocateli e IA assistida |
| 2026-08-01 | P-03 | in-progress | blocked | pesquisa nominal não encontrou marca de software exata, mas a busca visual encontrou motivo semelhante em marketplaces; requer refinamento ou aceitação explícita do risco residual | IA assistida |
| 2026-08-01 | plano | approved | blocked | P-03 bloqueia publicação e integração até decisão humana sobre o risco de similaridade | IA assistida |
| 2026-08-01 | P-03 | blocked | blocked | Lincoln Zocateli decidiu refinar o símbolo; alternativas J, K e L foram produzidas e aguardam seleção para nova pesquisa de similaridade | Lincoln Zocateli e IA assistida |
| 2026-08-01 | P-03 | blocked | completed | pesquisa não identificou marca ou logo idêntico; Lincoln Zocateli manteve G e aceitou explicitamente o risco residual de similaridade conceitual | Lincoln Zocateli |
| 2026-08-01 | P-05 | blocked | completed | pacote final G aprovado, validado e liberado após aceitação explícita do risco residual | Lincoln Zocateli |
| 2026-08-01 | P-06 | not-started | in-progress | P-03 e P-05 concluídas; integração e testes nos produtos foram liberados | IA assistida |
| 2026-08-01 | plano | blocked | in-progress | bloqueio de similaridade resolvido por decisão humana; execução avança para integração | Lincoln Zocateli e IA assistida |
| 2026-08-01 | P-06 | in-progress | blocked | logo integrado ao README; aplicação Angular e manifest PWA ainda não existem, impedindo integração e testes de produto | IA assistida |
| 2026-08-01 | P-07 | not-started | in-progress | guia e governança aprovados; conclusão depende das evidências de integração de P-06 | Lincoln Zocateli e IA assistida |
| 2026-08-01 | plano | in-progress | blocked | P-06 depende da criação da aplicação Angular, ausente no estado atual do repositório | IA assistida |
| 2026-08-01 | P-07 | in-progress | blocked | guia aprovado e pronto; conclusão aguarda a integração real de P-06 para validar caminhos, superfícies e rollback | IA assistida |
| 2026-08-01 | plano | blocked | blocked | criação da marca concluída em P-01 a P-05; estado e roteiro de retomada futura registrados para P-06 e P-07 | IA assistida |

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
Create an original visual identity concept for a self-hosted knowledge management product named "Thothis".

Explore an abstract synthesis of writing, preserved records, lunar cycles, and measured time. Connected knowledge may appear through continuity, rhythm, or relationships between forms, never as a generic network diagram. Prioritize a distinctive silhouette, simple geometry, legibility at small sizes, and clear separation from existing software brands.

Explore neutral interface colors with restrained teal and blue accents. Include an original "Thothis" wordmark using a generic sans-serif placeholder; do not reproduce a proprietary typeface or third-party logo.

Flat vector concept. White background. No shadows, gradients, mockups, mascots, copied hieroglyphs, literal deities, religious symbols, stock symbols, or references to existing brand styles.
```

#### Ícone

```text
Design an original square icon concept for "Thothis", a self-hosted Markdown knowledge platform.

Use an abstract mark that suggests writing, preserved records, cycles, and measured time without reproducing a known hieroglyph, deity, moon illustration, or religious symbol. The icon must remain recognizable at 16x16, 32x32, and 48x48 pixels and must also work in monochrome.

Use charcoal, neutral gray, and one restrained teal accent. No text, gradients, shadows, mockups, stock note symbols, or imitation of existing product icons.
```

#### Prancha de exploração

```text
Create an exploratory branding board for "Thothis", a self-hosted Markdown knowledge management product.

Show three clearly different original directions: an abstract writing and record gesture; a geometric study of lunar cycles and measured time; and a synthesis in which continuity or relationships between forms suggest connected knowledge. An original letter T may appear as a typographic treatment, but not as a required direction. For each direction, show symbol, temporary wordmark, monochrome version, light and dark backgrounds, favicon-size preview, candidate palette, and compact application-header usage.

Focus on wisdom, clarity, precision, permanence, accessibility, and operational software. Do not use ibis mascots, constellations, neural networks, literal books or libraries, copied hieroglyphs, deities, religious iconography, or generic graph symbols. Do not imitate existing brands, generate marketing-page layouts, or present the concepts as legally cleared final assets.
```
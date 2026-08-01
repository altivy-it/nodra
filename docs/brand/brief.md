# Brief de identidade visual — Thothis

**Versão:** 1.0
**Data:** 2026-07-31
**Status:** approved 
**Etapa do plano:** P-01  
**Relacionado:** [Plano de branding](../plan/thothis-branding.md)

---

## Produto

O **Thothis** é uma aplicação web self-hosted de gestão de conhecimento em Markdown.
O usuário escreve e organiza notas em arquivos `.md` no próprio sistema de arquivos.
Backlinks, grafo de conhecimento, busca e edição são providos pela interface web —
sem nuvem, sem conta obrigatória em serviço externo, sem dependência de internet em runtime.

O nome deriva de **Thoth**, divindade egípcia tradicionalmente associada à
escrita, à linguagem, ao conhecimento, à sabedoria, à ciência, à medição do
tempo e à preservação de registros. Esses atributos se conectam ao propósito do
produto: transformar informação dispersa em conhecimento acessível, relacionado
e durável.

A pronúncia oficial é **TÔ-TIS** (`/ˈto.tis/`). Em materiais internacionais,
`TOH-tis` pode ser usado apenas como orientação de pronúncia, sem alterar o nome
escrito. Os endereços previstos são **thothis.app** e **thothis.zocate.li**.

---

## Plataforma estratégica aprovada

As formulações desta seção foram aprovadas por Lincoln Zocateli em 2026-07-31.

### Missão

Transformar informações dispersas em conhecimento acessível, conectado e útil.

### Visão

Ser uma plataforma de conhecimento confiável para pessoas e organizações que
precisam preservar, relacionar e encontrar informação sob seu próprio controle.

### Valores

- clareza;
- conhecimento;
- organização;
- confiabilidade;
- simplicidade;
- independência;
- inovação responsável.

### Arquétipo

**O Sábio:** busca compreender, organizar e compartilhar conhecimento com
clareza, rigor e credibilidade. A linguagem deve ser analítica e acessível, sem
parecer pretensiosa, mística ou distante.

---

## Público

| Perfil | Motivação principal |
|---|---|
| Desenvolvedores e engenheiros | controle total dos dados, sem lock-in |
| Pesquisadores e acadêmicos | arquivamento durável, estrutura de links entre conceitos |
| Usuários técnicos em geral | privacidade, operação local, alternativa self-hosted ao Notion/Obsidian cloud |

---

## Personalidade da marca

| Atributo | Como se manifesta |
|---|---|
| **Sábia e confiável** | A linguagem comunica conhecimento preservado, precisão e confiança. |
| **Conectada e organizada** | Relações entre registros representam links, contexto e memória conectada. |
| **Tecnológica e moderna** | Formas simples e funcionais aproximam escrita, pesquisa e inteligência artificial. |
| **Precisa e funcional** | O design é minimalista, sem ornamentos desnecessários. |
| **Independente** | O visual não sugere dependência de nuvem ou serviços externos. |

---

## Atributos prioritários (ordem decrescente de importância)

1.  **Conhecimento e confiança** — o sistema deve comunicar escrita, memória e segurança dos dados.
2.  **Clareza e legibilidade** — o símbolo deve ser reconhecível em tamanhos pequenos (16-32 px).
3.  **Conhecimento conectado** — o símbolo deve evocar relações entre notas sem recorrer a ícone genérico de grafo.
4.  **Modernidade e apelo estético** — o design deve ser agradável e parecer atual.
5.  **Independência / self-hosted** — sem elementos que sugiram "nuvem" ou serviços SaaS.

---

## Território visual aprovado

**Síntese entre escrita, registro e conexão:** gestos e estruturas abstratas
comunicam documentação, preservação e relações entre informações. A direção
[G — Confluência aberta](assets/concept-g-open-confluence.svg) avança para
verificação técnica e produção.

O conhecimento conectado permanece um atributo funcional da composição: pode
aparecer em relações entre formas, ritmo ou continuidade, mas não como um
terceiro símbolo literal ou um grafo genérico.

Os territórios não autorizam reproduzir hieróglifos, representar Thoth, usar
iconografia religiosa, criar mascotes ou copiar símbolos arqueológicos. Íbis,
constelações, redes neurais, livros e bibliotecas literais não fazem parte desta
rodada de exploração.

---

## Superfícies que receberão a marca

| Superfície | Tamanho mínimo | Contexto |
|---|---|---|
| Favicon do navegador | 16 × 16 px | Aba do browser, bookmarks |
| Ícone da aba | 32 × 32 px | Browser |
| Ícone PWA pequeno | 192 × 192 px | Tela inicial do dispositivo |
| Ícone PWA grande | 512 × 512 px | Splash screen PWA |
| Header da aplicação | ~40 × 40 px (ícone) | Shell Angular, todas as páginas |
| Logo no login / onboarding | ~200 × 56 px | Tela de autenticação |
| README do repositório | ~120 × 120 px | GitHub, espelhos |
| Avatar do repositório | 400 × 400 px (quadrado) | GitHub organization |
| Imagem de container | ~64 × 64 px | Docker Hub listing |
| og:image / metadados sociais | 1200 × 630 px | Compartilhamento em redes |
| Documentação (docs/) | variável | Cabeçalhos de documentos Markdown |

---

## Arquitetura de mensagens

- **Conhecimento conectado:** Knowledge Connected.
- **Escrita e memória:** Knowledge, written to endure.
- **Fluxo de uso:** Write. Connect. Remember.
- **Transformação:** From Information to Wisdom.
- **Contexto para inteligência:** Intelligence Built on Knowledge.

Nenhuma tagline será incorporada ao sistema de marca nesta etapa. As mensagens
permanecem como referências editoriais, não como assinatura obrigatória do
logotipo.

---

## Critérios observáveis de avaliação

| Critério | Como avaliar | Responsável |
|---|---|---|
| Originalidade | Pesquisa de similaridade visual antes de qualquer adoção | revisão humana |
| Legibilidade em 16 px | Render sobre fundo branco e preto em grade 16 × 16 | designer + revisão humana |
| Independência de tema | Render nos três temas (claro, escuro, alto contraste) sem perda de identidade | designer |
| Monocromia | Render em preto e em branco mantém a silhueta reconhecível | designer |
| Acessibilidade de cor | Contraste ≥ 4.5:1 para texto, ≥ 3:1 para ícones e componentes ativos | ferramenta de contraste + revisão |
| Licença | Versão exata de cada fonte, ferramenta e asset documentada antes da adoção | responsável técnico |
| Operação offline | Nenhuma requisição de rede ao carregar assets de identidade | inspeção de rede no browser |
| SVG seguro | Nenhum script, referência externa, fonte embutida ou metadado sensível no SVG | inspeção manual + CSP |
| Aderência ao arquétipo | Comunica clareza e credibilidade sem tom pretensioso, místico ou literal | pesquisa com usuários + revisão humana |
| Referência cultural responsável | Evita hieróglifo copiado, divindade, símbolo religioso ou arqueológico reconhecível | pesquisa de referências + revisão humana |
| Mensagem de marca | Tagline é compreendida sem prometer SaaS, IA autônoma ou capacidade inexistente | teste de compreensão + revisão de produto |

---

## Referências de mercado

Listadas apenas para análise de legibilidade e aplicação. **Não imitar.**

| Produto | O que observar |
|---|---|
| Obsidian | Silhueta reconhecível em 16 px; porém depende de gradiente |
| Logseq | Nós e conexões visíveis; risco de confusão com ícones de grafo genéricos |
| Notion | Monograma limpo; escala perfeitamente; linguagem visual muito associada ao produto |
| Joplin | Elefante ilustrado — personalidade forte, mas frágil em escala |
| VSCode | Forma geométrica moderna; sem referência ao conteúdo da ferramenta |

**Vetor de diferenciação do Thothis:** combinar escrita, registro e conhecimento conectado em uma forma abstrata própria, sem reproduzir hieróglifos, divindades, animais, livros ou grafos genéricos literalmente.

---

## Restrições

- Nenhum asset carregado de CDN, fonte remota ou serviço externo em runtime.
- Funcionar nos três temas: **claro**, **escuro** e **alto contraste**.
- Funcionar em monocromia (impressão, contextos sem cor).
- Legível a partir de **16 × 16 px**.
- Licença **permissiva, verificada, gratuita para uso self-hosted** para fontes, ícones e ferramentas.
- Não imitar identidade visual, ícone ou composição de produto concorrente.
- SVGs sanitizados: sem scripts, sem referências externas, sem fontes embutidas desnecessárias.
- Assets de produção hospedados no próprio deployment.

---

## Decisões registradas

1. O sistema de marca não usa tagline obrigatória nesta etapa.
2. A direção G — Confluência aberta avança para produção.
3. Inter 4.1 é a tipografia funcional e base do wordmark.
4. IBM Plex Serif 6.4.0 atende conteúdo e IBM Plex Mono 6.4.0 atende código.
5. Os pesos aprovados são 400, 500, 600 e 700, sujeitos ao orçamento de bundle.
6. A identidade permanece estática nesta etapa.
7. Alterações futuras exigem aprovação de Lincoln Zocateli ou revisor humano formalmente designado.
8. Missão, visão, valores e arquétipo desta versão representam o produto.
9. O território aprovado sintetiza escrita, registro e conexão.

---

*Documento gerado por IA assistida como entrega da etapa P-01 e aprovado por
Lincoln Zocateli em 2026-07-31.*

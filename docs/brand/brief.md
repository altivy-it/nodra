# Brief de identidade visual — Basalt Notes

**Versão:** 0.1-draft  
**Data:** 2026-07-29  
**Status:** aguardando revisão humana  
**Etapa do plano:** P-01  
**Relacionado:** [Plano de branding](../plan/basalt-notes-branding.md)

---

## Produto

O **Basalt Notes** é uma aplicação web self-hosted de gestão de conhecimento em Markdown.
O usuário escreve e organiza notas em arquivos `.md` no próprio sistema de arquivos.
Backlinks, grafo de conhecimento, busca e edição são providos pela interface web —
sem nuvem, sem conta obrigatória em serviço externo, sem dependência de internet em runtime.

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
| **Sólida e duradoura** | A rocha central, abstrata e facetada, evoca permanência e confiança. |
| **Conectada e organizada** | As órbitas representam a estrutura de links e o conhecimento conectado. |
| **Tecnológica e moderna** | Linhas limpas e uma paleta de cores pastel transmitem um ar de modernidade. |
| **Precisa e funcional** | O design é minimalista, sem ornamentos desnecessários. |
| **Independente** | O visual não sugere dependência de nuvem ou serviços externos. |

---

## Atributos prioritários (ordem decrescente de importância)

1.  **Confiança e solidez** — o ícone deve comunicar permanência e segurança dos dados.
2.  **Clareza e legibilidade** — o símbolo deve ser reconhecível em tamanhos pequenos (16-32 px).
3.  **Conhecimento conectado** — a estrutura de órbitas deve evocar a conexão entre as notas.
4.  **Modernidade e apelo estético** — o design deve ser agradável e parecer atual.
5.  **Independência / self-hosted** — sem elementos que sugiram "nuvem" ou serviços SaaS.

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

## Mensagens candidatas

> **Principal:** Knowledge built on solid foundations.

> **Alternativa:** Your knowledge. Organized. Connected. Durable.

Nenhuma mensagem está aprovada. Avaliar em P-01 se slogan é necessário para as superfícies listadas.

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

---

## Referências de mercado

Listadas apenas para análise de legibilidade e aplicação. **Não imitar.**

| Produto | O que observar |
|---|---|
| Obsidian | Forma cristalina reconhecível em 16 px; porém depende de gradiente |
| Logseq | Nós e conexões visíveis; risco de confusão com ícones de grafo genéricos |
| Notion | Monograma limpo; escala perfeitamente; sem personalidade mineral |
| Joplin | Elefante ilustrado — personalidade forte, mas frágil em escala |
| VSCode | Forma geométrica moderna; sem referência ao conteúdo da ferramenta |

**Vetor de diferenciação do Basalt Notes:** combinar a forma mineral sólida (basalto) com os nós e arestas de um grafo de conhecimento, criando um símbolo que comunica as duas pilastras do produto ao mesmo tempo.

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

## Questões bloqueantes (sem decisão presumida)

1. Slogan é necessário nas superfícies mapeadas? Em quais delas aparece?
2. Qual conceito de símbolo avança para a fase de produção?
3. Qual tipografia funcional é aprovada para a aplicação e para o wordmark?
4. Quais pesos de fonte são necessários e qual o impacto no bundle?
5. A identidade inclui animação (ex.: loading spinner, transição do header) ou permanece estática?
6. Qual é o processo de aprovação de mudanças futuras e quem são os responsáveis?

---

*Documento gerado por IA assistida como entrega da etapa P-01.
Nenhuma escolha aqui está aprovada. Revisão humana obrigatória antes de avançar para P-02.*

# Inventário de licenças — P-03

**Status:** aguardando decisão humana por item  
**Etapa do plano:** P-03  
**Relacionado:** [Plano de branding](../../plan/basalt-notes-branding.md)

> Nenhum item abaixo está aprovado para uso até que a decisão humana seja registrada na coluna correspondente.
> Versão exata deve ser verificada no repositório oficial antes da adoção.

---

## Fontes tipográficas candidatas

### Tipografia funcional (interface e wordmark)

| Fonte | Versão avaliada | Licença | URL oficial | Distribuição self-hosted | Decisão |
|---|---|---|---|---|---|
| **Inter** | 4.1 | SIL Open Font License 1.1 | https://github.com/rsms/inter | ✓ permitida | pendente |
| **Manrope** | 1.4.3 | SIL Open Font License 1.1 | https://github.com/sharanda/manrope | ✓ permitida | pendente |
| **IBM Plex Sans** | 6.4.0 | SIL Open Font License 1.1 | https://github.com/IBM/plex | ✓ permitida | pendente |

### Tipografia editorial (conteúdo Markdown)

| Fonte | Versão avaliada | Licença | URL oficial | Distribuição self-hosted | Decisão |
|---|---|---|---|---|---|
| **IBM Plex Serif** | 6.4.0 | SIL Open Font License 1.1 | https://github.com/IBM/plex | ✓ permitida | pendente |
| **Merriweather** | 3.001 | SIL Open Font License 1.1 | https://github.com/SorkinType/Merriweather | ✓ permitida | pendente |
| **Lora** | 3.204 | SIL Open Font License 1.1 | https://github.com/cyrealtype/Lora-Cyrillic | ✓ permitida | pendente |

### Tipografia de código

| Fonte | Versão avaliada | Licença | URL oficial | Distribuição self-hosted | Decisão |
|---|---|---|---|---|---|
| **IBM Plex Mono** | 6.4.0 | SIL Open Font License 1.1 | https://github.com/IBM/plex | ✓ permitida | pendente |
| **JetBrains Mono** | 2.304 | SIL Open Font License 1.1 | https://github.com/JetBrains/JetBrainsMono | ✓ permitida | pendente |
| **Fira Code** | 6.2 | SIL Open Font License 1.1 | https://github.com/tonsky/FiraCode | ✓ permitida | pendente |

---

## Notas sobre a SIL Open Font License 1.1

A SIL OFL 1.1 é compatível com a política de dependências do Basalt Notes:

- Permite uso, cópia, incorporação em software e distribuição livre (inclusive self-hosted).
- Permite modificação, desde que o resultado use outro nome e mantenha a licença SIL OFL.
- **Proíbe** vender a fonte isolada como produto.
- Não é copyleft aplicado ao software que incorpora a fonte.

**Verificação obrigatória antes da adoção de qualquer fonte:**
1. Baixar os arquivos da versão exata do repositório oficial (não de espelhos).
2. Confirmar que o arquivo `OFL.txt` ou `LICENSE` está presente e refere SIL OFL 1.1.
3. Registrar o hash (SHA-256) do arquivo de fonte e do arquivo de licença.
4. Confirmar que os pesos necessários (400, 600, 700) estão disponíveis e testá-los.

---

## Ferramentas usadas na exploração e produção

| Ferramenta | Versão | Licença | Uso | URL | Decisão |
|---|---|---|---|---|---|
| **Inkscape** | 1.3.2 | GNU GPL 2.0 | Edição e exportação de SVG | https://inkscape.org | pendente (uso no design, não em runtime) |
| **SVGO** | 3.3.2 | MIT | Otimização de SVG antes de commit | https://github.com/svg/svgo | pendente |
| **sharp** | 0.33.5 | Apache-2.0 | Geração de PNGs a partir de SVG (build) | https://github.com/lovell/sharp | pendente |

> Ferramentas de design (Inkscape, Figma, etc.) são usadas no processo de produção, não em runtime.
> Somente ferramentas incorporadas ao build (SVGO, sharp) exigem verificação de licença aplicável ao repositório.

---

## Assets visuais produzidos nesta exploração

| Arquivo | Origem | Processo de criação | Referências externas | Decisão |
|---|---|---|---|---|
| `exploration/concept-a-basalt-graph.svg` | IA assistida (código SVG manual) | geometria hexagonal + grafo desenhado manualmente em SVG | nenhuma | pendente |
| `exploration/concept-b-hexagonal-crystal.svg` | IA assistida (código SVG manual) | polígonos aninhados desenhados manualmente em SVG | nenhuma | pendente |
| `exploration/concept-c-letter-b.svg` | IA assistida (código SVG manual) | nós e arestas desenhados manualmente em SVG | nenhuma | pendente |
| `assets/icon.svg` | IA assistida (código SVG manual) | refinamento do Concept A | nenhuma | pendente |
| `assets/favicon.svg` | IA assistida (código SVG manual) | variante simplificada do icon.svg | nenhuma | pendente |

**Todos os SVGs foram gerados como código vetorial puro**, sem uso de imagem raster embutida, fonte embutida, script ou referência a URL externa. Inspecionar com SVGO e revisar manualmente antes de publicar.

---

## Checklist de proveniência antes de publicar

- [ ] Nenhum arquivo de fonte foi copiado de CDN, Google Fonts ou serviço externo.
- [ ] Todos os arquivos de fonte possuem `OFL.txt` com versão 1.1 confirmada.
- [ ] Hashes SHA-256 dos arquivos de fonte registrados abaixo.
- [ ] SVGs inspecionados com SVGO: sem scripts, sem `<use href="...">` externos, sem `<image>`.
- [ ] Pesquisa de similaridade visual realizada e nenhuma confusão material identificada.
- [ ] Decisão humana documentada para cada item desta tabela.

---

## Hashes de proveniência (preencher após adoção)

```
# Formato:
# SHA256(arquivo) = hash
# Inter-Regular.woff2    = (preencher)
# Inter-SemiBold.woff2   = (preencher)
# Inter-Bold.woff2       = (preencher)
# OFL.txt (Inter 4.1)    = (preencher)
```

---

*Inventário produzido por IA assistida como entrega da etapa P-03.
Nenhum item está aprovado sem decisão humana explícita registrada neste arquivo.*

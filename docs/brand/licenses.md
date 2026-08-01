# Inventário de licenças — P-03

**Status:** approved  
**Etapa do plano:** P-03  
**Relacionado:** [Plano de branding](../plan/thothis-branding.md)

> As decisões humanas estão registradas por item. A publicação do sistema de
> marca foi liberada após a aceitação explícita do risco residual registrado na
> [revisão preliminar de similaridade](similarity-review.md).

---

## Fontes tipográficas candidatas

### Tipografia funcional (interface e wordmark)

| Fonte | Versão avaliada | Licença | URL oficial | Distribuição self-hosted | Decisão |
|---|---|---|---|---|---|
| **Inter** | 4.1 | SIL Open Font License 1.1 | https://github.com/rsms/inter | ✓ permitida | aprovada; arquivo e hash verificados |
| **Manrope** | 1.4.3 | SIL Open Font License 1.1 | https://github.com/sharanda/manrope | ✓ permitida | não adotada |
| **IBM Plex Sans** | 6.4.0 | SIL Open Font License 1.1 | https://github.com/IBM/plex | ✓ permitida | não adotada |

### Tipografia editorial (conteúdo Markdown)

| Fonte | Versão avaliada | Licença | URL oficial | Distribuição self-hosted | Decisão |
|---|---|---|---|---|---|
| **IBM Plex Serif** | 6.4.0 | SIL Open Font License 1.1 | https://github.com/IBM/plex | ✓ permitida | aprovada; arquivos e hashes verificados |
| **Merriweather** | 3.001 | SIL Open Font License 1.1 | https://github.com/SorkinType/Merriweather | ✓ permitida | não adotada |
| **Lora** | 3.204 | SIL Open Font License 1.1 | https://github.com/cyrealtype/Lora-Cyrillic | ✓ permitida | não adotada |

### Tipografia de código

| Fonte | Versão avaliada | Licença | URL oficial | Distribuição self-hosted | Decisão |
|---|---|---|---|---|---|
| **IBM Plex Mono** | 6.4.0 | SIL Open Font License 1.1 | https://github.com/IBM/plex | ✓ permitida | aprovada; arquivos e hashes verificados |
| **JetBrains Mono** | 2.304 | SIL Open Font License 1.1 | https://github.com/JetBrains/JetBrainsMono | ✓ permitida | não adotada |
| **Fira Code** | 6.2 | SIL Open Font License 1.1 | https://github.com/tonsky/FiraCode | ✓ permitida | não adotada |

---

## Notas sobre a SIL Open Font License 1.1

A SIL OFL 1.1 é compatível com a política de dependências do Thothis:

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
| **Inkscape** | 1.3.2 | GNU GPL 2.0 | Edição e exportação de SVG | https://inkscape.org | não adotada |
| **SVGO** | 3.3.2 | MIT | Otimização de SVG antes de commit | https://github.com/svg/svgo | aprovada; metadata npm verificado |
| **sharp** | 0.33.5 | Apache-2.0 | Geração de PNGs a partir de SVG (build) | https://github.com/lovell/sharp | aprovada; metadata npm verificado |
| **fonttools[woff]** | 4.59.0 | MIT | Subset WOFF2, cobertura e conversão do wordmark | https://github.com/fonttools/fonttools | aprovada; licença do wheel verificada |
| **@ibm/plex** | 6.4.0 | SIL Open Font License 1.1 | Distribuição oficial dos subsets Latin1 | https://www.npmjs.com/package/@ibm/plex | aprovada; metadata npm e licença verificadas |

> Ferramentas de design (Inkscape, Figma, etc.) são usadas no processo de produção, não em runtime.
> Somente ferramentas incorporadas ao build (SVGO, sharp) exigem verificação de licença aplicável ao repositório.

---

## Assets visuais

| Asset | Origem | Decisão |
| --- | --- | --- |
| [G — Confluência aberta](assets/concept-g-open-confluence.svg) | criação vetorial manual por IA assistida, sem material externo | selecionada por Lincoln Zocateli; versão final produzida |
| [Símbolo final](assets/thothis-symbol.svg) | redesenho vetorial da direção G, sem material externo | aprovado por Lincoln Zocateli; risco residual aceito |
| [Logo final](assets/thothis-logo.svg) | símbolo final e contornos gerados do Inter SemiBold 4.1 | aprovado por Lincoln Zocateli; risco residual aceito |

Os arquivos finais são gerados por scripts reproduzíveis. O wordmark foi
convertido em paths a partir do Inter SemiBold 4.1. A pesquisa preliminar não
encontrou marca ou logo idêntico; a semelhança conceitual com logos de estoque
`T/Y + losango` foi aceita explicitamente pelo revisor humano.

---

## Checklist de proveniência antes de publicar

- [x] Nenhum arquivo de fonte foi copiado de CDN, Google Fonts ou serviço externo.
- [x] Todos os arquivos de fonte possuem licença SIL OFL 1.1 confirmada.
- [x] Hashes SHA-256 dos arquivos de fonte e licença registrados.
- [x] SVGs inspecionados com SVGO: sem scripts, sem `<use href="...">` externos, sem `<image>`.
- [x] Risco residual encontrado na pesquisa preliminar foi aceito explicitamente.
- [x] Decisão humana documentada para cada item adotado nesta tabela.

O catálogo dos assets está em [assets/dist/SHA256SUMS](assets/dist/SHA256SUMS)
e o catálogo das fontes em [fonts/SHA256SUMS](fonts/SHA256SUMS).

---

## Transformações reproduzíveis

- Inter: release oficial `v4.1`; subset variável `U+0020-007E,U+00A0-00FF`,
	features `kern`, `liga` e `calt`, sem hinting.
- IBM Plex: subsets Latin1 oficiais do pacote `@ibm/plex@6.4.0`, sem
	modificação posterior, preservando o nome reservado `Plex`.
- Pesos distribuídos: Inter variável 400–700; Plex Serif 400/700; Plex Mono
	400/600.
- Pipeline: [build-brand-fonts.ps1](../../tools/scripts/build-brand-fonts.ps1).

---

*Inventário produzido por IA assistida como entrega da etapa P-03. Fontes,
ferramentas, assets e risco residual foram aprovados por Lincoln Zocateli.*

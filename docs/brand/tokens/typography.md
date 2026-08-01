# Tipografia — Thothis

**Status:** draft / aguardando aprovação humana  
**Etapa do plano:** P-04  
**Relacionado:** [tokens.json](tokens.json), [tokens.css](tokens.css), [licenses.md](../licenses.md)

---

## Papéis tipográficos

O Thothis usa três papéis distintos de tipografia, cada um com família própria:

| Papel | Família candidata | Fallback | Uso |
|---|---|---|---|
| **Funcional (sans)** | Inter | `system-ui, -apple-system, sans-serif` | Interface, controles, menus, wordmark |
| **Editorial (serif)** | IBM Plex Serif | `Georgia, serif` | Conteúdo Markdown do vault (parágrafos, títulos longos) |
| **Código (mono)** | IBM Plex Mono | `ui-monospace, monospace` | Blocos de código, paths, comandos |

---

## Decisões pendentes (nenhuma aprovada)

1. **Qual fonte serif para o conteúdo?**  
   - IBM Plex Serif: pares naturais com Plex Sans/Mono; moderna e editorial.  
   - Merriweather: projetada especificamente para leitura em tela; muito boa em tamanhos menores.  
   - Lora: mais expressiva; pode parecer mais "literária" para notas técnicas.

2. **Qual fonte mono para código?**  
   - IBM Plex Mono: familiar para desenvolvedores, ligaduras opcionais, par natural.  
   - JetBrains Mono: ligaduras de programação, boa diferenciação de `0Ol1I`.  
   - Fira Code: ligaduras ricas, muito popular na comunidade de desenvolvedores.

3. **Os pesos 400, 600 e 700 são suficientes?**  
   Avaliar se o peso 500 (medium) é necessário para estados intermediários (ex.: labels de formulário).

4. **Qual o impacto no bundle de cada conjunto?**  
   Medir com subsetting + WOFF2 antes de decidir. Meta: `< 80 KB` por família no subset latino.

---

## Escala tipográfica

```
xs:   12px  — metadados, badges, labels de input pequenos
sm:   14px  — secundário, captions, breadcrumbs
base: 16px  — corpo de texto, controles
lg:   20px  — subtítulos, destaques de seção
xl:   24px  — títulos de página (H2/H3 no shell)
2xl:  30px  — título principal da nota (H1 no editor)
3xl:  36px  — telas de onboarding, títulos de diálogo
```

---

## Regras de aplicação

### Wordmark

- Usar a família funcional (sans) aprovada.
- Texto: **Thothis**.
- Peso: **semibold (600)**.
- Tracking: `--tracking-normal` (`0`).
- Não usar fonte incorporada no SVG; converter para path após aprovação da fonte.

### Interface

- Corpo padrão: `--text-base` (16 px), `--font-sans`, `--font-normal`, `--leading-normal`.
- Rótulos de controle: `--text-sm` (14 px), `--font-medium`.
- Metadados e badges: `--text-xs` (12 px), `--font-normal` ou `--font-medium`.
- Títulos da interface: `--text-xl` ou `--text-2xl`, `--font-semibold`, `--leading-tight`.

### Conteúdo Markdown

- Parágrafos: `--text-base` (16 px), `--font-serif`, `--font-normal`, `--leading-relaxed`.
- Títulos (`#`): escala decrescente a partir de `--text-3xl`, `--font-serif`, `--font-bold`.
- `inline code`: `--text-sm`, `--font-mono`, `--font-normal`.
- Blocos de código: `--text-sm`, `--font-mono`, `--leading-relaxed`.

---

## Acessibilidade tipográfica

- O usuário pode ajustar o tamanho base via configuração; a escala usa `rem`, não `px`.
- Nenhum texto essencial abaixo de `12 px`.
- Não usar `font-weight < 400` para texto de conteúdo (risco de legibilidade).
- O contraste mínimo obrigatório (`--text-primary` sobre `--surface-app`) deve ser ≥ 7:1 nos três temas para atender WCAG 2.2 AAA como meta — mínimo aceitável é 4.5:1 (AA).
- Caracteres obrigatórios no subset: `A–Z a–z 0–9 ! @ # $ % & * ( ) – _ + = [ ] { } | ; : ' " , . < > / ? \\ ç ã â à á é ê í ó ô ú ü ñ Ç Ã Â À Á É Ê Í Ó Ô Ú Ü Ñ`.

---

## Fallback sem fonte aprovada

Até a aprovação e hospedagem local das fontes, a aplicação usa exclusivamente:

```css
--font-sans:  system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
--font-serif: Georgia, "Times New Roman", serif;
--font-mono:  ui-monospace, "Cascadia Code", "Courier New", monospace;
```

Nenhuma URL de fonte externa deve aparecer em CSS de produção.

---

*Documento produzido por IA assistida como entrega de P-04.
Nenhuma fonte, peso ou escala está aprovado sem decisão humana explícita.*

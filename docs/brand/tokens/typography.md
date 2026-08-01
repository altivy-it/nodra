# Tipografia — Thothis

**Status:** approved  
**Etapa do plano:** P-04  
**Relacionado:** [tokens.json](tokens.json), [tokens.css](tokens.css), [licenses.md](../licenses.md)

---

## Papéis tipográficos aprovados

O Thothis usa três papéis distintos de tipografia, cada um com família própria:

| Papel | Família aprovada | Fallback | Uso |
|---|---|---|---|
| **Funcional (sans)** | Inter | `system-ui, -apple-system, sans-serif` | Interface, controles, menus, wordmark |
| **Editorial (serif)** | IBM Plex Serif | `Georgia, serif` | Conteúdo Markdown do vault (parágrafos, títulos longos) |
| **Código (mono)** | IBM Plex Mono | `ui-monospace, monospace` | Blocos de código, paths, comandos |

---

## Decisões registradas

- **Funcional e wordmark:** Inter 4.1.
- **Conteúdo Markdown:** IBM Plex Serif 6.4.0.
- **Código:** IBM Plex Mono 6.4.0.
- **Pesos:** Inter variável 400–700; IBM Plex Serif 400/700; IBM Plex Mono 400/600.
- **Distribuição:** WOFF2 hospedado localmente, com subset latino e meta de `< 80 KB` por família.
- **Decisor:** Lincoln Zocateli, em 2026-07-31.

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

## Pacote self-hosted

| Família | Arquivos | Tamanho total | Resultado |
|---|---:|---:|---|
| Inter | 1 variável | 50.264 bytes | aprovado |
| IBM Plex Serif | 2 estáticos | 45.852 bytes | aprovado |
| IBM Plex Mono | 2 estáticos | 34.980 bytes | aprovado |

O Inter usa subset `U+0020-007E,U+00A0-00FF`, features `kern`, `liga` e
`calt`, sem hinting. Os arquivos IBM Plex são os subsets Latin1 oficiais, sem
modificação posterior. A cobertura dos caracteres portugueses obrigatórios é
verificada pelo pipeline e os hashes estão em
[fonts/SHA256SUMS](../fonts/SHA256SUMS).

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

## Fallback sem fonte disponível

Se uma fonte local não estiver disponível, a aplicação usa:

```css
--font-sans:  system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
--font-serif: Georgia, "Times New Roman", serif;
--font-mono:  ui-monospace, "Cascadia Code", "Courier New", monospace;
```

Nenhuma URL de fonte externa deve aparecer em CSS de produção.

---

*Documento produzido por IA assistida como entrega de P-04 e aprovado por
Lincoln Zocateli em 2026-07-31. Licenças, hashes, cobertura e orçamento foram
verificados em 2026-08-01.*

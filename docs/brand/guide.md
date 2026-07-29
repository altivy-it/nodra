# Guia de marca — Basalt Notes

**Versão:** 0.1-draft  
**Status:** aguardando aprovação humana  
**Etapa do plano:** P-07  
**Relacionado:** [Plano de branding](../../plan/basalt-notes-branding.md), [Brief](../brief.md), [Tokens](../tokens/)

> **Atenção:** este guia é um rascunho produzido por IA assistida.  
> Nenhuma escolha aqui está aprovada. Revisão e aprovação humana são obrigatórias antes de publicar qualquer asset nos produtos.

---

## 1. O símbolo

O ícone do Basalt Notes combina dois elementos:

1.  **A Rocha Abstrata:** Uma forma facetada e assimétrica no centro, com um contorno definido, representa a solidez, durabilidade e permanência do conhecimento. As diferentes faces (base, sombra, destaque) dão profundidade e um toque de modernidade.
2.  **As Órbitas Conectadas:** Duas elipses em "X" que cruzam o fundo simbolizam a estrutura de links, o grafo de conhecimento e a conexão entre as notas. Os pequenos nodos reforçam a ideia de pontos de informação interligados.

A paleta de cores pastel (azul, lavanda) sobre fundos neutros cria uma identidade visual que é ao mesmo tempo sólida e tecnológica, profissional e convidativa.

---

## 2. Assets disponíveis

| Arquivo | Uso | Fundo |
|---|---|---|
| [assets/icon.svg](assets/icon.svg) | Ícone isolado — interface, PWA, documentação | Claro / branco |
| [assets/icon-dark.svg](assets/icon-dark.svg) | Ícone isolado | Escuro |
| [assets/icon-mono.svg](assets/icon-mono.svg) | Ícone isolado | Qualquer (monocromia) |
| [assets/logo-horizontal.svg](assets/logo-horizontal.svg) | Logo com wordmark — header, README | Claro / branco |
| [assets/logo-horizontal-dark.svg](assets/logo-horizontal-dark.svg) | Logo com wordmark | Escuro |
| [assets/logo-compact.svg](assets/logo-compact.svg) | Logo empilhado — espaços quadrados | Claro / branco |
| [assets/logo-mono.svg](assets/logo-mono.svg) | Logo com wordmark | Qualquer (monocromia) |
| [assets/favicon.svg](assets/favicon.svg) | Favicon — variante simplificada para ≤ 48 px | Qualquer |

### Tamanhos de exportação recomendados (após aprovação)

| Superfície | Arquivo-fonte | Tamanho de saída |
|---|---|---|
| Favicon ICO | `favicon.svg` | 16, 32, 48 px |
| Ícone PWA pequeno | `icon.svg` | 192 × 192 px |
| Ícone PWA grande | `icon.svg` | 512 × 512 px |
| Avatar GitHub | `logo-compact.svg` | 400 × 400 px |
| og:image | `logo-horizontal.svg` + fundo | 1200 × 630 px |
| Header da aplicação | `icon.svg` ou `logo-horizontal.svg` | 40 px de altura |

---

## 3. Paleta de cores

A paleta principal usa tons pastel de azul e lavanda para criar uma atmosfera calma e tecnológica.

| Token (CSS) | Valor (HEX) | Papel no Logo (Tema Claro) |
|---|---|---|
| `---rock-highlight` | `#C3DAFE` | Destaque da rocha |
| `---rock-base` | `#A3BFFA` | Base da rocha |
| `---rock-shadow` | `#7F9CF5` | Sombra da rocha |
| `---orbit-path` | `#DCD7FE` | Órbitas e nodos |
| `---rock-stroke` | `#4A5568` | Contorno da rocha |
| `---border` | `#E2E8F0` | Borda do ícone |
| `---bg` | `#FFFFFF` | Fundo do ícone |

Para o tema escuro, os valores são ajustados para manter a identidade visual, usando tons mais escuros para a rocha e um lavanda mais sutil para as órbitas. Consulte os SVGs `icon-dark.svg` e `logo-horizontal-dark.svg` para os valores exatos.

---

## 4. Tipografia (draft)

| Papel | Família candidata | Fallback |
|---|---|---|
| Interface / Wordmark | Inter | `system-ui, sans-serif` |
| Conteúdo Markdown | IBM Plex Serif | `Georgia, serif` |
| Código | IBM Plex Mono | `ui-monospace, monospace` |

O wordmark "Basalt Notes" usa a fonte **Inter**, em peso "Medium" ou "SemiBold".

---

## 5. Área de proteção e tamanho mínimo

- **Área de proteção:** Manter uma margem equivalente a **10% da largura total** do asset em todos os lados.
- **Tamanho mínimo (ícone):** O ícone completo não deve ser exibido abaixo de **24 × 24 px**. Para 16 × 16 px, usar `favicon.svg`, que contém apenas a rocha central.
- **Tamanho mínimo (logo):** O logo horizontal não deve ser exibido abaixo de **120 px de largura**.

---

## 6. Usos incorretos

Não:
- Distorcer as proporções do logo.
- Rotacionar o ícone ou o wordmark.
- Adicionar efeitos como sombra, brilho ou gradientes.
- Alterar as cores fora da paleta definida.
- Usar o ícone completo em tamanhos onde as órbitas se tornam indistinguíveis (abaixo de 24px).
- Usar as versões de tema claro sobre fundos escuros e vice-versa.
- Carregar os assets de uma URL externa em runtime.

---

## 7. Contraste mínimo obrigatório

| Par | Ratio mínimo | Norma |
|---|---|---|
| `--text-primary` sobre `--surface-app` (tema claro) | ≥ 7:1 | WCAG 2.2 AAA (meta) |
| `--text-primary` sobre `--surface-app` (tema claro) | ≥ 4.5:1 | WCAG 2.2 AA (mínimo) |
| Texto grande (≥ 18 px regular ou ≥ 14 px bold) | ≥ 3:1 | WCAG 2.2 AA |
| Componentes ativos (borda, ícone funcional) | ≥ 3:1 | WCAG 2.2 AA |
| Foco visível | ≥ 3:1 | WCAG 2.2 AA |
| Cor não pode ser o **único** indicador de estado | — | WCAG 1.4.1 |

---

## 8. Catálogo de arquivos e checksums

> Preencher após aprovação e antes de publicar. Usar `sha256sum` ou equivalente.

```
# Checksums SHA-256 (preencher após aprovação)
# icon.svg                     = (pendente)
# icon-dark.svg                = (pendente)
# icon-mono.svg                = (pendente)
# logo-horizontal.svg          = (pendente)
# logo-horizontal-dark.svg     = (pendente)
# logo-compact.svg             = (pendente)
# logo-mono.svg                = (pendente)
# favicon.svg                  = (pendente)
```

---

## 9. Processo de atualização e aprovação

1. Qualquer alteração de símbolo, cor ou tipografia reabre as validações de licença, contraste e originalidade.
2. A proposta de alteração é submetida como PR com referência a este guia e ao plano de branding.
3. Aprovação humana explícita é registrada no PR antes do merge.
4. O catálogo de checksums é atualizado antes de promover assets para produção.
5. A versão anterior dos assets é preservada em `assets/archive/` com data até que todas as superfícies sejam migradas.

---

## 10. Responsáveis

| Responsabilidade | Titular (preencher após aprovação) |
|---|---|
| Aprovação de alterações no símbolo | — |
| Aprovação de alterações de cor e tokens | — |
| Aprovação de tipografia | — |
| Manutenção do catálogo e checksums | — |
| Revisão de originalidade e licença | — |

---

## 11. Rollback

Caso um asset publicado precise ser revertido:

1. Restaurar a versão anterior de `assets/archive/`.
2. Substituir nos pontos de integração (manifest PWA, Angular, documentação, Docker Hub).
3. Limpar caches versionados de service worker se necessário.
4. Verificar que `manifest.webmanifest`, favicon e documentação voltaram ao conjunto compatível.
5. O rollback não deve exigir migração de dados de usuário.

---

*Guia produzido por IA assistida como entrega da etapa P-07.
Nenhuma decisão aqui está aprovada. Revisão humana obrigatória antes de publicar.*

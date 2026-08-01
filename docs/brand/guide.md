# Guia de marca — Thothis

**Versão:** 0.3  
**Status:** approved 
**Etapa do plano:** P-07  
**Relacionado:** [Plano de branding](../plan/thothis-branding.md), [Brief](brief.md), [Tokens](tokens/)

> A direção G e o pacote técnico foram aprovados. O risco residual identificado
> na [revisão preliminar de similaridade](similarity-review.md) foi aceito pelo
> revisor humano; os assets estão liberados para integração.

---

## 1. O símbolo

O símbolo selecionado é **G — Confluência aberta**. Dois registros convergem em
uma haste central; o núcleo teal representa o encontro entre informações. A
forma funciona em monocromia e permanece reconhecível a partir de `16x16`.

A seleção não autoriza copiar hieróglifos, representar divindades, usar
iconografia religiosa ou arqueológica reconhecível nem recorrer a mascotes e
símbolos genéricos.

A pronúncia oficial da marca é **TÔ-TIS**. Em materiais internacionais, usar a
orientação `TOH-tis` em inglês e `TO-tis` em espanhol quando necessário.

---

## 2. Assets disponíveis

O catálogo em [assets/dist/SHA256SUMS](assets/dist/SHA256SUMS) registra os
artefatos produzidos. Os SVGs incluem símbolo claro, escuro e monocromático,
além de lockups claro e escuro. Os PNGs cobrem `16`, `32`, `48`, `64`, `192`,
`400` e `512` pixels.

Execute `./tools/scripts/build-brand-fonts.ps1` e depois
`./tools/scripts/build-brand-assets.ps1` a partir da raiz para recriar fontes,
wordmark, SVGs, PNGs e catálogos.

---

## 3. Paleta de cores

O símbolo usa Archive Ink `#1A202C` e Scribe Teal `#0F766E` no tema claro. No
tema escuro, usa branco `#FFFFFF` e teal claro `#5EEAD4`. A versão monocromática
usa uma única cor e não depende do núcleo colorido para manter a silhueta.

---

## 4. Tipografia

| Papel | Família aprovada | Fallback |
|---|---|---|
| Interface / Wordmark | Inter | `system-ui, sans-serif` |
| Conteúdo Markdown | IBM Plex Serif | `Georgia, serif` |
| Código | IBM Plex Mono | `ui-monospace, monospace` |

O wordmark usa **Inter SemiBold (600)** convertido em paths. As fontes locais e
seus hashes estão em [fonts/](fonts/); nenhuma fonte ou CDN externa é usada.

---

## 5. Área de proteção e tamanho mínimo

- **Área de proteção:** Manter uma margem equivalente a **10% da largura total** do asset em todos os lados.
- **Tamanho mínimo (ícone):** `16 px`; usar o símbolo isolado abaixo de `32 px`.
- **Tamanho mínimo (logo):** O logo horizontal não deve ser exibido abaixo de **120 px de largura**.

---

## 6. Usos incorretos

Não:
- Distorcer as proporções do logo.
- Rotacionar o ícone ou o wordmark.
- Adicionar efeitos como sombra, brilho ou gradientes.
- Alterar as cores fora da paleta definida.
- Publicar um símbolo antes de testar legibilidade, monocromia e contraste nos tamanhos-alvo.
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

O catálogo canônico é [assets/dist/SHA256SUMS](assets/dist/SHA256SUMS) e é
regenerado junto com os assets. Qualquer divergência bloqueia a publicação.

---

## 9. Processo de atualização e aprovação

1. Qualquer alteração de símbolo, cor ou tipografia reabre as validações de licença, contraste e originalidade.
2. A proposta de alteração é submetida como PR com referência a este guia e ao plano de branding.
3. Aprovação humana explícita é registrada no PR antes do merge.
4. O catálogo de checksums é atualizado antes de promover assets para produção.
5. Assets substituídos deixam de ser referenciados e distribuídos após a validação do novo conjunto.

---

## 10. Responsáveis

| Responsabilidade | Titular (preencher após aprovação) |
|---|---|
| Aprovação de alterações no símbolo | Lincoln Zocateli |
| Aprovação de alterações de cor e tokens | Lincoln Zocateli |
| Aprovação de tipografia | Lincoln Zocateli |
| Manutenção do catálogo e checksums | equipe Thothis |
| Revisão de originalidade e licença | Lincoln Zocateli ou revisor humano designado |

---

## 11. Substituição de assets

Uma substituição futura deve atualizar no mesmo change set o manifest PWA, a
aplicação, a documentação, os checksums e os metadados de licença. Assets sem
aprovação deixam de ser distribuídos; a mudança visual não pode exigir migração
de dados nem alterar conteúdo de usuário.

---

*Guia produzido por IA assistida e aprovado por Lincoln Zocateli em 2026-08-01.
A integração e validação nos produtos seguem em P-06.*

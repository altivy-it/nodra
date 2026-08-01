# Guia de marca — Thothis

**Versão:** 0.1-draft  
**Status:** aguardando aprovação humana  
**Etapa do plano:** P-07  
**Relacionado:** [Plano de branding](../plan/thothis-branding.md), [Brief](brief.md), [Tokens](tokens/)

> **Atenção:** este guia é um rascunho produzido por IA assistida.  
> Nenhuma escolha aqui está aprovada. Revisão e aprovação humana são obrigatórias antes de publicar qualquer asset nos produtos.

---

## 1. O símbolo

O Thothis ainda não possui símbolo, logotipo ou wordmark aprovados. O
[brief](brief.md) define dois territórios candidatos e uma possibilidade de
síntese para exploração em P-02:

1. **Escrita e registro:** gestos e estruturas abstratas ligados a documentação, linguagem e preservação da informação.
2. **Lua e tempo:** ciclos, fases e medição tratados como geometria abstrata, sem ilustração literal.
3. **Síntese entre registro e conexão:** continuidade, ritmo ou relações entre formas sugerem conhecimento conectado.

Essas direções não estão aprovadas. Nenhuma delas autoriza copiar hieróglifos,
representar divindades, usar iconografia religiosa ou arqueológica reconhecível
ou recorrer a mascotes e símbolos genéricos.

A pronúncia oficial da marca é **TÔ-TIS**. Em materiais internacionais, usar a
orientação `TOH-tis` em inglês e `TO-tis` em espanhol quando necessário.

---

## 2. Assets disponíveis

Nenhum asset visual está aprovado ou distribuído. A pasta `assets/` permanece
reservada para arquivos produzidos após seleção humana do conceito, verificação
de originalidade, licença, acessibilidade e segurança.

---

## 3. Paleta de cores

Os tokens atuais são candidatos funcionais da interface, não uma paleta de logo
aprovada. A paleta definitiva deve distinguir superfícies, texto, foco e estados
nos temas claro, escuro e alto contraste, sem depender de uma única família cromática.

---

## 4. Tipografia (draft)

| Papel | Família candidata | Fallback |
|---|---|---|
| Interface / Wordmark | Inter | `system-ui, sans-serif` |
| Conteúdo Markdown | IBM Plex Serif | `Georgia, serif` |
| Código | IBM Plex Mono | `ui-monospace, monospace` |

Se aprovada, a fonte **Inter** poderá ser testada no wordmark "Thothis" nos
pesos "Medium" e "SemiBold". Esta combinação ainda não é normativa.

---

## 5. Área de proteção e tamanho mínimo

- **Área de proteção:** Manter uma margem equivalente a **10% da largura total** do asset em todos os lados.
- **Tamanho mínimo (ícone):** será definido após testes reais em 16, 24, 32 e 48 px.
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

> Preencher após aprovação e antes de publicar. Usar `sha256sum` ou equivalente.

```
# Checksums SHA-256 (preencher após aprovação)
# Nenhum asset aprovado até o momento.
```

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
| Aprovação de alterações no símbolo | — |
| Aprovação de alterações de cor e tokens | — |
| Aprovação de tipografia | — |
| Manutenção do catálogo e checksums | — |
| Revisão de originalidade e licença | — |

---

## 11. Substituição de assets

Uma substituição futura deve atualizar no mesmo change set o manifest PWA, a
aplicação, a documentação, os checksums e os metadados de licença. Assets sem
aprovação deixam de ser distribuídos; a mudança visual não pode exigir migração
de dados nem alterar conteúdo de usuário.

---

*Guia produzido por IA assistida como entrega da etapa P-07.
Nenhuma decisão aqui está aprovada. Revisão humana obrigatória antes de publicar.*

---
name: "Verificação Visual Frontend"
description: "Use ao criar, alterar ou substituir uma tela ou componente Angular do Nodra; define o loop obrigatório de screenshot, análise e ajuste até a UI/UX estar correta."
applyTo: "frontend/web/**, frontend/libs/frontend/**"
---

# Verificação visual

- Toda tela ou componente Angular criado, alterado ou substituído passa por um loop de verificação visual antes de ser considerado pronto: capturar screenshot, analisar contra os critérios abaixo e ajustar até não haver defeito relevante.
- Sirva a aplicação em container, sempre atrás de um nginx também containerizado.
  Para testes simulados, use `deploy/e2e/nginx-mocked.conf` e abra somente a URL
  publicada pelo proxy; para fluxos reais, use a borda HTTPS do laboratório
  documentada em `deploy/e2e/README.md`. `ng serve` via alias pode iniciar o
  container Angular, mas sua porta nunca é a URL de navegador nem de Playwright.
  Não reaproveite um servidor se não for possível comprovar o proxy nginx na rota.
- Capture screenshot em pelo menos dois viewports por iteração: desktop (largura ≥ 1280px) e mobile (largura ≤ 480px), com `screenshot_page`. Capture também qualquer estado relevante da tela (loading, vazio, erro, forbidden, offline, conflito, foco) antes de concluir.
- Analise cada screenshot contra estes critérios antes de decidir que a tela está correta:
  - fora do editor Markdown e do grafo de notas, usa exclusivamente componentes PO UI, conforme `frontend-po-ui.instructions.md`; nenhum elemento parece reimplementado ou de outra biblioteca;
  - no editor Markdown e no grafo de notas, o comportamento e o layout seguem `frontend-design-system.instructions.md` para essas duas superfícies dedicadas;
  - layout sem sobreposição, corte de texto, quebra inesperada ou espaçamento inconsistente em nenhum dos viewports capturados;
  - hierarquia visual, contraste, temas (claro/escuro/alto contraste) e densidade coerentes com os tokens do Nodra;
  - estados de loading, vazio, erro, forbidden, offline e conflito são visualmente distintos e legíveis quando aplicáveis;
  - foco visível e ordem de navegação plausível, cruzando a captura com `read_page`;
  - console e rede sem erro novo introduzido pela mudança.
- Se algum critério falhar, corrija o código, capture novamente e reanalise. Não entregue uma tela com defeito visual conhecido e não corrigido.
- Limite o loop a no máximo 3 iterações de ajuste. Se o terceiro ciclo ainda apresentar defeito, pare, registre o que foi tentado, a hipótese restante e pergunte ao responsável antes de continuar ajustando às cegas.
- Guarde a captura final de cada viewport analisado e cite, no resumo de entrega, quais telas, estados e viewports foram verificados e o resultado da análise.
- Este loop é verificação de desenvolvimento; não substitui Vitest nem Playwright exigidos por `testing.instructions.md`.
- Nunca capture nem registre token, secret, conteúdo real de vault, credencial ou log sensível; use somente dados sintéticos do ambiente de desenvolvimento local.

Consulte `frontend-angular.instructions.md`, `frontend-po-ui.instructions.md`, `frontend-design-system.instructions.md` e `docs/frontend.md`.

# Plano de Implementação: Workspace do AltivyNotes

**Branch**: A definir | **Data**: 2026-10-06 | **Spec**: `spec.md`

**Entrada**: Migração de `docs/plan/workspace-frontend-inspirado-no-obsidian.md`.

**Idioma**: Português brasileiro (pt-BR); preserve nomes de tecnologias e caminhos.

## Resumo

Evoluir o shell Angular para workspace persistente e responsivo, usando limites existentes entre shell, features, data access e design system, sem copiar identidade do Obsidian.

## Contexto Técnico

**Linguagem/Versão**: Angular 22 e TypeScript estrito.

**Dependências Principais**: PO UI para UI genérica; CodeMirror 6 e Cytoscape para editor/grafo.

**Armazenamento**: Preferências não sensíveis no browser, isoladas por usuário/vault; conteúdo segue na API/filesystem.

**Testes**: Vitest/unit, Playwright via proxy nginx containerizado, validação visual e acessibilidade.

**Plataforma Alvo**: Browser desktop, tablet e celular servido pela aplicação containerizada.

**Tipo de Projeto**: Feature frontend Angular em aplicação existente.

**Objetivos de Desempenho**: Shell sem layout shift; métricas e limites conforme implementação/CI existente.

**Restrições**: Sem acesso direto a banco/filesystem; sem execução de plugins remotos; não descartar alterações locais.

**Escopo**: Shell, navegação, explorer, abas/editor, toolbar/account, notificações, painel contextual, acessibilidade e ajuda.

## Verificação da Constituição

- O frontend consome contratos HTTP e não conhece schema/paths físicos.
- Markdown e PostgreSQL mantêm fonte canônica/projeção conforme backend.
- Segurança/privacidade impedem logs, storage ou eventos com conteúdo/credenciais.
- PO UI é obrigatória para UI genérica; editor e grafo são exceções aprovadas.
- Telas alteradas recebem screenshots desktop/mobile e testes Playwright proporcionais.

## Estrutura do Projeto

### Documentação desta Feature

```text
specs/008-workspace-frontend/{spec.md,plan.md,tasks.md}
```

### Código-fonte

```text
frontend/web/src/app/
frontend/web/src/assets/
frontend/tests/e2e/
```

**Decisão de Estrutura**: Evoluir os módulos existentes; não reescrever serviços funcionais ou mover regras para o shell.

## Rastreabilidade da Migração

As 11 etapas P-01 a P-11 do plano legado são preservadas como tarefas pendentes da migração. O estado legado marca P-01–P-04 e P-09–P-11 `in-progress`, e P-05–P-08 `not-started`. Nenhuma tarefa passa a concluída sem evidência executada e revisão.

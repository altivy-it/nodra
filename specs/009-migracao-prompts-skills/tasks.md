# Tarefas: Migração dos auxiliares para skills

**Entrada**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md),
[data-model.md](data-model.md), [contratos](contracts/auxiliares.md) e
[quickstart.md](quickstart.md).

**Origem**: IA assistida.
**Revisor humano**: solicitante desta conversa.
**Autorização de implementação**: T001–T019 aprovadas em 2026-10-22 às
17:32 (-03:00), na invocação de `/speckit-implement`: “aprovado, registre e
siga com a implementação”. Os gates de cada tarefa não foram dispensados.
**Testes**: validações estáticas executadas e gates interativos confirmados
pelo solicitante; evidências e limites estão registrados no quickstart.

**Registro da primeira etapa (superada)**: após o preflight detectar indisponibilidade dos
agentes e da automação interativa, o solicitante escolheu nesta mesma execução:
“Autorizar criação das quatro skills e validação estática agora; manter prompts
e consumidores atuais até eu validar os cenários interativos no cliente”.
Depois, em 2026-10-22 às 21:30 (-03:00), confirmou todos os cenários do
quickstart nos quatro fluxos. T002 e os gates T006/T009/T012/T015 foram então
registrados com essa evidência humana; a integração e retirada foram concluídas.

## Fase 1: Preparação

**Objetivo**: preservar estado anterior e delimitar o trabalho autorizado.

- [X] T001 Registrar HEAD, estado Git e hashes das quatro fontes em `.github/prompts/` e dos três agentes em `.github/agents/` no registro de `specs/009-migracao-prompts-skills/quickstart.md`; registrar caminhos protegidos e estado de `.vscode/mcp.json` sem ler seu conteúdo ou secrets.

## Fase 2: Fundação

**Objetivo**: preparar validação segura e critérios comuns antes das histórias.

- [X] T002 Preparar cenários/fixtures descartáveis fora do produto conforme `specs/009-migracao-prompts-skills/quickstart.md`, registrar local e cleanup previsto, confirmar cliente, raiz e disponibilidade dos agentes sem instalar/reconciliar componentes. — Solicitante confirmou os cenários e fixtures descartáveis em workspace externa; o caminho exato não foi informado. Nenhuma fixture local foi criada nesta execução; cleanup do workspace cliente não foi verificado pela IA.
- [X] T003 Completar em `specs/009-migracao-prompts-skills/quickstart.md` a matriz prompt→destino→consumidores e os critérios estáticos de frontmatter, links, seleção explícita, contratos e hashes, preservando `NOT RUN` onde ainda não houver execução.

**Checkpoint previsto**: T001–T003 concluídas; estado anterior e protocolo de teste
rastreáveis. Na primeira etapa autorizada, T002 permanece parcial: preflight
concluído, fixtures e execução interativa adiadas.

## Fase 3: US1 — ADR (P1)

**Objetivo**: auxílio arquitetural justificado sem plano paralelo.
**Teste independente**: ADR válido em fixture e negativos sem vínculo/justificativa,
com `proposed`, sequência anual, origem e revisor.

- [X] T004 [US1] Registrar o resultado inicial dos cenários de ADR de `specs/009-migracao-prompts-skills/quickstart.md`, confirmando ausência do novo destino e preservando a fonte `.github/prompts/create-adr.prompt.md`.
- [X] T005 [US1] Criar `.github/skills/altivy-notes-create-adr/SKILL.md` a partir do contrato original, com os quatro campos obrigatórios da migração, seleção explícita de `Altivy Solution Architect`, links para plano mestre/template/instruções recalculados e bloqueios de vínculo/justificativa/aceitação.
- [X] T006 [US1] Comparar `.github/skills/altivy-notes-create-adr/SKILL.md` à fonte e ao agente, validar frontmatter/links e executar cenários positivos/negativos isolados e multi-root do quickstart; registrar evidência, responsável e limites em `specs/009-migracao-prompts-skills/quickstart.md`. — Comparação estática validada pela IA; cenários isolados, multi-root e negativos confirmados pelo solicitante em 2026-10-22.

## Fase 4: US2 — Testes (P1)

**Objetivo**: testes derivados exclusivamente de tarefa autorizada.
**Teste independente**: tarefa válida em fixture e negativos sem referência/autorização.

- [X] T007 [US2] Registrar o resultado inicial dos cenários de testes de `specs/009-migracao-prompts-skills/quickstart.md`, confirmando ausência do novo destino e preservando `.github/prompts/generate-tests.prompt.md`.
- [X] T008 [US2] Criar `.github/skills/altivy-notes-generate-tests/SKILL.md`, preservando artefato/ID/critérios/autorização, seleção explícita de `Altivy Quality Engineer`, fixtures e comandos existentes, execução containerizada e proibição de ampliar requisitos ou produção.
- [X] T009 [US2] Comparar `.github/skills/altivy-notes-generate-tests/SKILL.md` à fonte/agente, validar frontmatter/links e cenários positivos/negativos isolados e multi-root; registrar resultados e comandos em `specs/009-migracao-prompts-skills/quickstart.md`, sem executar testes do produto sem autorização específica. — Comparação estática validada pela IA; cenários isolados, multi-root e negativos confirmados pelo solicitante em 2026-10-22.

## Fase 5: US3 — Revisão (P1)

**Objetivo**: achados sem edição, validações estreitas controladas.
**Teste independente**: diff de fixture com e sem autorização de validação,
incluindo ferramentas insuficientes e vínculo ausente.

- [X] T010 [US3] Registrar resultado inicial dos cenários de revisão de `specs/009-migracao-prompts-skills/quickstart.md`, confirmando ausência do novo destino e preservando `.github/prompts/review-change.prompt.md`.
- [X] T011 [US3] Criar `.github/skills/altivy-notes-review-change/SKILL.md`, preservando diff/referência/achados, seleção explícita de `Altivy Code Reviewer`, não edição de código, autorização específica de comando/escopo, aviso de artefatos e ferramentas sem ampliação.
- [X] T012 [US3] Comparar `.github/skills/altivy-notes-review-change/SKILL.md` à fonte/agente e validar frontmatter/links, cenários isolados/multi-root, não execução sem autorização e falha fechada por ferramentas indisponíveis; registrar evidência em `specs/009-migracao-prompts-skills/quickstart.md`. — Comparação estática validada pela IA; cenários isolados, multi-root e negativos confirmados pelo solicitante em 2026-10-22.

## Fase 6: US4 — Localização (P2)

**Objetivo**: auditoria/tradução segura, sem mutação da instalação real.
**Teste independente**: inventário sem alvo, auditoria sem modo e tradução
explícita de fixture com literais/exclusões, hashes e drift.

- [X] T013 [US4] Registrar resultado inicial dos cenários de localização de `specs/009-migracao-prompts-skills/quickstart.md`, confirmando ausência do novo destino e preservando `.github/prompts/localize-speckit.prompt.md`.
- [X] T014 [US4] Criar `.github/skills/altivy-notes-localize-speckit/SKILL.md`, preservando extensão/arquivos/modo/exclusões, auditoria padrão, inventário sem alvo, literais/hashes/overrides/drift e exclusão de `.specify/extensions/bug/README.md`, sem instalação/reconciliação.
- [X] T015 [US4] Comparar `.github/skills/altivy-notes-localize-speckit/SKILL.md` à fonte, validar frontmatter/links e cenários isolados/multi-root somente com fixtures; registrar comparação de literais, hashes e exclusões em `specs/009-migracao-prompts-skills/quickstart.md`. — Comparação estática validada pela IA; cenários isolados, multi-root e negativos confirmados pelo solicitante em 2026-10-22.

## Fase 7: Integração e fechamento

**Objetivo**: retirar fontes somente com evidência e eliminar consumidores obsoletos.

- [X] T016 Após T006/T009/T012/T015 aprovadas, atualizar `.github/README.md`, `.github/COPILOT_GUIDE.md`, `.github/copilot-instructions.md` e `.github/instructions/documentation.instructions.md` para os novos nomes/links/contratos; conferir `.github/GOVERNANCE.md` sem mudar autoridade, processo ou histórico. — Consumidores atualizados; governança conferida sem alteração.
- [X] T017 Revisar diff de cada fluxo e consumidores, registrar aceite verificável em `specs/009-migracao-prompts-skills/quickstart.md` e remover somente então `.github/prompts/create-adr.prompt.md`, `.github/prompts/generate-tests.prompt.md`, `.github/prompts/review-change.prompt.md` e `.github/prompts/localize-speckit.prompt.md`; se algum gate não passar, preservar sua fonte e registrar bloqueio. — Gates interativos confirmados pelo solicitante; revisão estática concluída; quatro prompts removidos.
- [X] T018 Buscar consumidores remanescentes em documentação/configurações rastreadas, validar todos os links locais alterados e frontmatter dos quatro destinos, executar `git diff --check` e comparar superfícies protegidas ao estado T001; registrar resultados e limpar somente fixtures/artefatos temporários conhecidos em `specs/009-migracao-prompts-skills/quickstart.md`. — Sem referências operacionais aos aliases antigos; links/frontmatter/diff validados. Nenhuma fixture foi criada nesta execução; cleanup do workspace temporário do solicitante não foi verificado.
- [X] T019 Atualizar `.github/plano-migracao-prompts-skills.md` com referência à feature e resultado real, registrar pendências/responsáveis em `specs/009-migracao-prompts-skills/quickstart.md` e marcar apenas tarefas executadas/validadas em `specs/009-migracao-prompts-skills/tasks.md`; não fazer commit — Plano histórico atualizado; execução e evidências registradas. Nenhum commit realizado./push sem solicitação.

## Dependências e Ordem de Execução

- T001 → T002 → T003.
- US1: T003 → T004 → T005 → T006.
- US2: T003 → T007 → T008 → T009.
- US3: T003 → T010 → T011 → T012.
- US4: T003 → T013 → T014 → T015.
- T006 + T009 + T012 + T015 → T016 → T017 → T018 → T019.
- Histórias não dependem entre si; a integração final aguarda as quatro.
- Atualização de evidências no quickstart é compartilhada: serializar essas
  escritas para não sobrescrever resultados.
- Exceção autorizada para a primeira etapa: após o preflight de T002,
  T003 e os registros iniciais/criação podem avançar sem fixtures/testes
  interativos. T006/T009/T012/T015 permanecem parciais após a comparação
  estática, bloqueando a integração e a retirada.

## Oportunidades de Paralelismo

Os quatro destinos são independentes após T003. A criação T005/T008/T011/T014
pode ser dividida entre responsáveis distintos quando seus testes iniciais
estiverem completos; as dependências impedem marcá-las como tarefas prontas
sem ressalvas. Testes interativos e registro compartilhado ficam serializados.

## Estratégia de Entrega

MVP sugerido: US1 com sua fonte preservada até validação. Prosseguir por fluxo,
sem declarar migração completa antes dos quatro e do fechamento. Nenhuma
checkbox preenchida significa aprovação humana; a autorização das tarefas
deve ser registrada explicitamente antes de T001.

# Pesquisa e decisões: auxiliares para skills

**Origem**: IA assistida, baseada no inventário read-only e nas decisões humanas
desta conversa em 2026-10-22, após a solicitação das 17:22 (-03:00).
**Revisor humano**: solicitante; artefatos e tarefas aprovados explicitamente
em 2026-10-22 às 17:32 (-03:00), com “aprovado, registre e siga com a
implementação”, ao invocar `/speckit-implement` nesta conversa.

## R1 — Formalização antes da execução

- **Decisão**: produzir spec, desenho, tarefas e análise; apresentar as tarefas
  ao solicitante antes de criar as skills.
- **Justificativa**: decisão explícita do solicitante e processo do projeto.
- **Alternativas consideradas**: executar diretamente o plano preliminar,
  rejeitado porque não satisfaz seu gate de formalização.

## R2 — Quatro destinos explícitos

- **Decisão**: usar os quatro nomes `altivy-notes-*` da
  [proposta original](../../.github/plano-migracao-prompts-skills.md),
  invocação explícita, sem pontes Local ou descoberta automática.
- **Justificativa**: aprovação humana; evitar colisões na workspace com Dokpod
  e seleção involuntária de um auxiliar com efeitos de escrita.
- **Alternativas consideradas**: nomes genéricos, uma skill agregada ou pontes;
  não preservam a escolha e os contratos independentes aprovados.

## R3 — Agentes não são campos de skill

- **Decisão**: selecionar explicitamente os agentes existentes quando disponíveis,
  sem copiar `agent`, `model` ou `tools` para campos não suportados.
- **Justificativa**: o formato de skill não equivale ao dos prompts. O
  inventário identificou os agentes `Altivy Solution Architect`,
  `Altivy Quality Engineer` e `Altivy Code Reviewer`.
- **Alternativas consideradas**: fingir seleção por frontmatter ou ampliar
  ferramentas, rejeitadas. Ausência de agente exige informar bloqueio e
  obter escolha explícita de alternativa compatível.

## R4 — Revisão e validação têm efeitos diferentes

- **Decisão**: revisão não edita código; validações estreitas só com
  autorização explícita e ferramentas permitidas, avisando sobre artefatos.
- **Justificativa**: opção selecionada pelo solicitante; o agente revisor
  existente declara execução, mas isso não autoriza qualquer comando.
- **Alternativas consideradas**: inspeção apenas foi oferecida; execução
  irrestrita ou silenciosa não atende o limite humano.

## R5 — Localização e proteção de componentes

- **Decisão**: preservar auditoria como padrão, inventário sem alvo, literais,
  exclusões, hashes, overrides e os limites do prompt original.
- **Justificativa**: tradução não é instalação/reconciliação; não alterar
  `.specify/extensions/bug/README.md`, usar a tradução existente quando aplicável.
- **Alternativas consideradas**: regenerar skills `speckit-*` ou reconciliar
  extensões durante a migração, rejeitadas como fora do escopo.

## R6 — Gate interativo permanece obrigatório

- **Decisão**: testes estáticos e interativos separados; preservar a fonte
  original de cada fluxo até ambos passarem e seu diff ser revisado.
- **Justificativa**: presença textual não comprova seleção e comportamento
  do modelo; a dispensa feita em outro projeto não se aplica a AltivyNotes.
- **Alternativas consideradas**: remover após inspeção apenas, não autorizada.

## Fontes e limites

- Inventário dos quatro prompts e três agentes realizado em modo read-only;
  nenhuma alteração de produto ou teste executado.
- Formato de skills: referência da skill `agent-customization` carregada
  nesta conversa, incluindo os campos de invocação.
- [Guia oficial de skills](https://code.visualstudio.com/docs/agent-customization/agent-skills)
  e [migração](https://code.visualstudio.com/docs/agent-customization/migrate-customizations#_convert-prompt-files-to-skills):
  referências da proposta, consultadas originalmente em 2026-10-22; não
  reconsultadas nesta formalização.
- Nenhuma dúvida de desenho pendente; revisão/autorização das tarefas foi
  concedida posteriormente às 17:32 (-03:00). Testes reais continuam sendo
  gates futuros, não resultados já obtidos.

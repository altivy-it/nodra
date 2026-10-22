# Guia de validação da migração

**Estado**: implementação autorizada em 2026-10-22 às 17:32 (-03:00);
referência inicial registrada; testes interativos ainda `NOT RUN`.

**Autorização**: o solicitante aprovou os artefatos e T001–T019 nesta conversa,
invocando `/speckit-implement` com “aprovado, registre e siga com a implementação”.
Isso não dispensa testes ou autoriza retirada antecipada dos prompts.

## Referência inicial — T001

Registro observado pela IA em 2026-10-22 durante esta execução:

- Branch: `009-migracao-prompts-skills`.
- HEAD: `8e1f41df0cc60eb77d4f759f6fffded064b97f13`.
- Estado inicial: `.vscode/mcp.json` modificado; plano preliminar e feature
  009 não rastreados. Nenhuma outra alteração rastreada.
- MCP: somente estado ` M .vscode/mcp.json` consultado; conteúdo não lido,
  hasheado, copiado ou alterado.
- Protegidos: `.specify/` (exceto seleção da feature pelo script oficial),
  skills `speckit-*`, agentes, constituição, `backend/`, `frontend/`,
  `contracts/`, `deploy/`, `docs/plan/`, vaults e secrets. Nenhum acesso a
  vaults ou secrets faz parte da validação.
- Manifesto inicial dos 494 arquivos rastreados em `.specify/`,
  `.github/skills/`, `backend/`, `frontend/`, `contracts/`, `deploy/` e
  `docs/plan/`: ordenar caminhos, concatenar `caminho SHA256` com LF, sem LF
  final, e calcular SHA256 UTF-8; resultado
  `AF802E9AD02C29F8B4B21BAEBBDCC3B84504F147D13BC65AC844B0E53900A3A2`.
- Checklists: `requirements.md`, 16 itens, 16 marcados, zero desmarcados,
  `PASS`; checklist não foi modificada.

| Fonte/agente | SHA256 inicial |
| --- | --- |
| `.github/prompts/create-adr.prompt.md` | `8020C7B4FCF6A677625D380A29D7D7231B15A46EA474940BD3B276763C8FE6B5` |
| `.github/prompts/generate-tests.prompt.md` | `79AA8138A5C5828BFB22F1F161469DCE9142EC7021FFFCDE019F0D025E10FEA8` |
| `.github/prompts/review-change.prompt.md` | `5B1ABA205C791C1226FC379FC62A8F850F029E286F0C959AFC91A68BAC9EB6F3` |
| `.github/prompts/localize-speckit.prompt.md` | `9D44B451B92C66E27A1363E3D2732DCA0EB3C37FFE4526E22702F504E7BA2735` |
| `.github/agents/solution-architect.agent.md` | `9C1CBFF63A69E2CBA29A6E734068A93184AC2C7CF70FC2386EB3A885B1729BAF` |
| `.github/agents/quality-engineer.agent.md` | `71D6F4BCFDFD0F0FED3733295F6BA4DAC80AF4A50104561E88D057FEBB14EDAB` |
| `.github/agents/code-reviewer.agent.md` | `81B7BC5D83ECB5E35548485D7247A8A58D9C39FA96B558705D1EA13DA0AA608F` |

## Pré-requisitos

- Feature `specs/009-migracao-prompts-skills` selecionada explicitamente.
- Cliente Copilot com raiz AltivyNotes e suporte a skills/agentes.
- Para comandos de testes do produto, autorização específica e containers
  existentes; não instalar ferramentas ou iniciar runtime no host.
- Para cenários com escrita, usar fixture descartável em workspace temporária
  com o mesmo layout de customizações, sem secrets ou arquivos reais do usuário.
  Não alterar componentes gerenciados do repositório para testar.

## 1. Referência e validação estática

Na raiz AltivyNotes, registrar `git rev-parse HEAD`, estado e hashes das
fontes/arquivos protegidos em evidência da feature, sem ler MCP ou secrets.
Comparar cada novo procedimento ao prompt e ao agente correspondente.

Conferir os quatro contratos em [auxiliares.md](contracts/auxiliares.md),
frontmatter, nomes/diretórios e todos os links relativos alterados.
Executar `git diff --check` na raiz; resultado esperado: saída vazia e código 0.
Checagens de conteúdo não substituem as etapas interativas.

## 2. Invocação e cenários por fluxo

Executar cada cenário em sessão nova, primeiro com AltivyNotes isolado,
depois com Dokpod também na workspace e a raiz AltivyNotes selecionada.

| Fluxo | Solicitação de teste | Evidência esperada |
| --- | --- | --- |
| ADR | `/altivy-notes-create-adr` com plano/tarefa de fixture que justifique decisão duradoura; repetir com escolha local e sem vínculo | Agente correto explicitamente escolhido; ADR `proposed` no caso válido; nenhuma escrita nos negativos; nunca `accepted` |
| Testes | `/altivy-notes-generate-tests` com artefato/ID/autorização de fixture; repetir omitindo referência e autorização | Critérios preservados, comandos containerizados e resultados explícitos; nenhum trabalho iniciado nos negativos |
| Revisão | `/altivy-notes-review-change` com diff e referência de fixture; depois autorizar um comando estreito permitido | Sem edição de código; nenhuma execução antes de autorização; comando/efeitos/resultados registrados, sem ampliar ferramentas |
| Localização | `/altivy-notes-localize-speckit` sem alvo; com alvo sem modo; tradução explícita de fixture com exclusões e literais | Inventário; auditoria sem escrita; tradução restrita com todos os literais/exclusões intactos |

Também verificar:

- As quatro skills aparecem para invocação explícita e não são ativadas
  automaticamente por pedidos genéricos; conferir configuração e comportamento.
- Nenhuma invocação seleciona o auxiliar do Dokpod.
- Agente indisponível informa bloqueio e exige escolha explícita compatível.
- Pedidos ambíguos são esclarecidos, sem autorização inferida.
- Localização não instala extensões nem modifica a instalação real.

## 3. Gate de retirada e fechamento

Somente após sucesso estático e interativo de um fluxo e revisão do diff,
retirar seu prompt e atualizar consumidores para o destino existente.
Buscar nomes antigos em documentação/configurações rastreadas; distinguir
origem histórica de referência operacional.

Ao final, repetir links, diff e proteção de superfícies; nenhuma alteração
de produto, agentes, componentes gerenciados ou MCP deve integrar a entrega.
Build da aplicação não comprova comportamento de customizações e não é exigido
por esta migração documental.

## Registro por fluxo

Preencher durante a execução autorizada; incluir comando/pedido, data,
responsável e origem da evidência. Confirmação humana deve ser identificada
como tal, sem atribuir observação à IA.

| Fluxo | Comparação estática | Interativo isolado | Interativo multi-root | Negativos/restrições | Diff revisado e retirada |
| --- | --- | --- | --- | --- | --- |
| ADR | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| Testes | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| Revisão | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| Localização | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN |

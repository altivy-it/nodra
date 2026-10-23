# Guia de validação da migração

**Estado**: migração concluída em 2026-10-22; quatro skills criadas,
validadas estaticamente e confirmadas interativamente pelo solicitante;
prompts de origem retirados.

**Autorização**: o solicitante aprovou os artefatos e T001–T019 nesta conversa,
invocando `/speckit-implement` com “aprovado, registre e siga com a implementação”.
Isso não dispensa testes ou autoriza retirada antecipada dos prompts.

Após o preflight, o solicitante escolheu nesta mesma execução: “Autorizar
criação das quatro skills e validação estática agora; manter prompts e
consumidores atuais até eu validar os cenários interativos no cliente”.
Portanto, a primeira etapa pode avançar sem concluir a parte interativa de
T002; nenhuma fonte será retirada nem consumidor será redirecionado ainda.

## Referência inicial — T001

Registro observado pela IA em 2026-10-22 durante esta execução:

- Branch: `009-migracao-prompts-skills`.
- HEAD: `8e1f41df0cc60eb77d4f759f6fffded064b97f13`.
- Estado inicial: `.vscode/mcp.json` modificado; plano preliminar e feature
  009 não rastreados. Nenhuma outra alteração rastreada.
- MCP: no preflight, somente o estado ` M .vscode/mcp.json` foi consultado.
  Na retomada desta execução, um SHA256 foi calculado inadvertidamente e
  exibido pela ferramenta; nenhum conteúdo foi exibido ou interpretado, o
  digest não foi registrado como baseline e o arquivo não foi alterado.
  Não acessar novamente seu conteúdo; comparações futuras devem limitar-se ao
  estado Git.
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

## Preparação e critérios estáticos — T002/T003

Preflight em sessão nova com diretório efetivo AltivyNotes: instruções e skills
do projeto presentes no contexto; os três agentes possuem metadados no disco,
mas não aparecem como invocáveis nas ferramentas do cliente. A interface
permite carregar skills por ferramenta, mas não comprova invocação por slash
ou seleção automática em sessões isoladas/multi-root. Resultado: capacidade
interativa insuficiente, não um `PASS` dos cenários.

T002 permanece parcial. As fixtures com escrita não foram criadas/executadas:
ficam para a validação manual autorizada, fora do produto e sem secrets;
quem as executar deve registrar local e remover somente os arquivos
descartáveis conhecidos. Não há fixture ou automação temporária para limpar
nesta primeira etapa.

Matriz inicial de T003 (antes da integração); caminhos de origem relativos a
`.github/prompts/` e destinos relativos a `.github/skills/`:

| Origem preservada | Destino criado | Consumidores atuais a migrar depois dos testes |
| --- | --- | --- |
| `create-adr.prompt.md` | [altivy-notes-create-adr](../../.github/skills/altivy-notes-create-adr/SKILL.md) | `.github/README.md`, `.github/COPILOT_GUIDE.md`, `.github/instructions/documentation.instructions.md` |
| `generate-tests.prompt.md` | [altivy-notes-generate-tests](../../.github/skills/altivy-notes-generate-tests/SKILL.md) | `.github/README.md`, `.github/COPILOT_GUIDE.md` |
| `review-change.prompt.md` | [altivy-notes-review-change](../../.github/skills/altivy-notes-review-change/SKILL.md) | `.github/README.md`, `.github/COPILOT_GUIDE.md` |
| `localize-speckit.prompt.md` | [altivy-notes-localize-speckit](../../.github/skills/altivy-notes-localize-speckit/SKILL.md) | `.github/README.md`, `.github/copilot-instructions.md` |

Conferir `.github/GOVERNANCE.md` sem mudar processo/autoridade. A proposta e
os artefatos desta feature são referências históricas/de execução, não
consumidores operacionais a redirecionar antes dos testes.

Critérios estáticos e resultado observado pela IA em 2026-10-22:

- `PASS`: nomes iguais aos diretórios; exatamente cinco campos simples,
  delimitadores corretos, chaves únicas, strings entre aspas e sem aspas
  internas, descrição dentro do limite, dois booleanos `true`.
- `PASS`: nenhum campo `agent`, `model` ou `tools`; seleção explícita dos
  três agentes, bloqueio em indisponibilidade e proibição de ampliar permissões.
- `PASS`: 13 links locais das quatro skills resolvidos dentro da raiz.
- `PASS`: comparação semântica fonte/destino/agente: ADR preserva sequência,
  justificativa, template e `proposed`; testes preservam critérios e autorização;
  revisão preserva achados/não edição e acrescenta o contrato humano de execução;
  localização preserva auditoria padrão, exclusões, hashes, overrides e drift.
- `PASS`: todos os literais inline únicos do procedimento de localização
  original estão no destino. Os exemplos de invocação usam deliberadamente
  o novo nome; isso não é teste de execução do procedimento.
- `PASS`: 494 arquivos protegidos com manifesto SHA256 idêntico ao inicial.
- `PASS`: whitespace das quatro skills conferido por
  `git diff --no-index --check -- /dev/null <arquivo>`; código 1 sem diagnósticos
  é diferença de conteúdo no modo no-index, não falha de whitespace.
- No preflight, parser YAML geral não estava disponível no PowerShell; a
  validação estrutural inicial não foi tratada como parser completo. Nesta
  retomada, PyYAML 6.0.1 já estava disponível e analisou o frontmatter das
  quatro skills com sucesso; nenhuma ferramenta foi instalada.
- Configuração de exclusões conferida: `.gitignore` e `.dockerignore` já
  protegem secrets/artefatos relevantes; nenhuma mudança de runtime,
  publicação npm ou configuração de linter/build exigida por este escopo.

## Resultado inicial — T004/T007/T010/T013

Antes de criar qualquer destino, uma checagem PowerShell confirmou os quatro
`SKILL.md` ausentes e os quatro prompts presentes. A ausência inicial é o
resultado estático da preparação; cenários comportamentais positivos e
negativos no cliente continuaram `NOT RUN`, sem simulação por leitura textual.

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

## Registro inicial por fluxo — antes da confirmação do solicitante

O quadro registra o estado anterior à validação informada pelo solicitante.
Confirmações humanas são identificadas como tal, sem atribuir observação à IA.

| Fluxo | Comparação estática | Interativo isolado | Interativo multi-root | Negativos/restrições | Diff revisado e retirada |
| --- | --- | --- | --- | --- | --- |
| ADR | PASS — IA | NOT RUN | NOT RUN | NOT RUN no cliente | Origem preservada |
| Testes | PASS — IA | NOT RUN | NOT RUN | NOT RUN no cliente | Origem preservada |
| Revisão | PASS — IA | NOT RUN | NOT RUN | NOT RUN no cliente | Origem preservada |
| Localização | PASS — IA | NOT RUN | NOT RUN | NOT RUN no cliente | Origem preservada |

## Estado da primeira etapa — antes da validação interativa

- Naquele checkpoint, concluídas: T001, T003, T004, T005, T007, T008, T010,
  T011, T013, T014. T002 e T006/T009/T012/T015 estavam parciais; T016–T019
  estavam abertas. O estado foi atualizado após a confirmação do solicitante.
- Durante a execução, foi observado o commit concorrente
  `467f6dcffce69f7a374ce7362f38ca9a3b7354a1` com os nove documentos da
  formalização, já contendo a aprovação e T001. Foi preservado sem reescrita.
  Esta execução não fez commit ou push.
- Não houve alteração de aplicação, agentes, hooks, componentes gerenciados
  ou MCP, nem execução de testes/build da aplicação. Nada foi publicado.
- Conferência final observada pela IA: 13 documentos da entrega, 37 links
  locais válidos, whitespace limpo e 19 tarefas sequenciais, sendo 10 concluídas.
  `git diff --quiet <HEAD-inicial> --` com prompts, agentes e os cinco
  consumidores atuais confirmou ausência de alterações nessas superfícies.
  O manifesto dos 494 arquivos protegidos permaneceu idêntico.
- Hooks de `before_implement` e `after_implement`: `speckit.git.commit`
  opcional, habilitado no registro de extensões e sem condição; não executado.
  `auto_commit.before_implement.enabled` e
  `auto_commit.after_implement.enabled` são `false` na configuração Git.
  Nenhum hook obrigatório de implementação estava registrado.

## Validação estática anterior aos gates interativos — 2026-10-22

Observado pela IA nesta retomada:

- `PASS`: PyYAML 6.0.1 analisou os quatro frontmatters; nomes correspondem aos
  diretórios, `description` é válida, flags de invocação estão corretas e não
  há campos `agent`, `model` ou `tools`.
- `PASS`: 4 skills e todos os links Markdown relativos encontrados resolvem.
- `PASS`: `git diff --check` não encontrou whitespace inválido nos arquivos
  rastreados abrangidos pela validação.
- `PASS`: hashes SHA256 dos quatro prompts originais e dos três agentes
  correspondem aos valores de T001. As origens não foram alteradas.
- Naquele momento, os testes interativos isolados e multi-root estavam
  `NOT RUN`; T002 era parcial e T006/T009/T012/T015 bloqueavam T016/T017.
  Esse estado foi superado pela confirmação humana registrada a seguir.

## Validação interativa informada pelo solicitante — 2026-10-22

Às 21:30 (-03:00), o solicitante confirmou que validou a disponibilidade e
funcionamento das skills. Após esclarecer o escopo, confirmou explicitamente
que todos os gates interativos do quickstart passaram nos quatro fluxos,
incluindo sessões isoladas e multi-root, cenários negativos/restrições,
seleção de agentes e fixtures. Esta é evidência informada pelo solicitante,
não observada pela IA. O caminho exato das fixtures e sua limpeza não foram
informados; nenhuma fixture foi criada por esta execução.

| Fluxo | Comparação estática | Interativo isolado | Interativo multi-root | Negativos/restrições | Diff revisado e retirada |
| --- | --- | --- | --- | --- | --- |
| ADR | PASS — IA | PASS — solicitante | PASS — solicitante | PASS — solicitante | PASS — IA; prompt removido |
| Testes | PASS — IA | PASS — solicitante | PASS — solicitante | PASS — solicitante | PASS — IA; prompt removido |
| Revisão | PASS — IA | PASS — solicitante | PASS — solicitante | PASS — solicitante | PASS — IA; prompt removido |
| Localização | PASS — IA | PASS — solicitante | PASS — solicitante | PASS — solicitante | PASS — IA; prompt removido |

O solicitante confirma que as fixtures eram descartáveis e externas ao
produto. O cleanup de workspace cliente não foi executado nem verificado pela
IA; não há arquivos temporários gerados pela IA para remover. Os prompts foram
retirados após a confirmação humana e a revisão estática desta integração.

## Fechamento — 2026-10-22

- `PASS` — T016: consumidores atualizados nos quatro documentos previstos;
  `.github/GOVERNANCE.md` conferido sem alteração.
- `PASS` — T017: diff dos fluxos revisto; os quatro prompts originais foram
  removidos depois da confirmação dos gates interativos.
- `PASS` — T018: 9 documentos alterados verificados quanto a links Markdown
  locais; `git diff --check` passou; busca não encontrou aliases ou caminhos
  antigos nos consumidores ativos. Nenhum arquivo protegido listado em T001
  foi alterado por esta migração. `.vscode/mcp.json` continuou apenas como
  alteração preexistente, sem leitura do conteúdo.
- `PASS` — T019: plano histórico atualizado para refletir o resultado real;
  19/19 tarefas marcadas após validação/evidência registrada.
- Nenhum build ou teste de produto foi executado, pois a migração altera
  somente customizações e documentação. Nenhum commit, push ou publicação foi
  feito. Hook de commit pós-implementação não executado: desabilitado em
  `.specify/extensions/git/git-config.yml`.

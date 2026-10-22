# Plano de Implementação: Migração dos auxiliares para skills

**Branch**: `009-migracao-prompts-skills` | **Data**: 2026-10-22 | **Spec**: [spec.md](spec.md)

**Entrada**: especificação em `specs/009-migracao-prompts-skills/spec.md`.

**Origem**: IA assistida.
**Revisor humano**: solicitante desta conversa; aprovação explícita dos
artefatos e tarefas em 2026-10-22 às 17:32 (-03:00), ao invocar
`/speckit-implement`: “aprovado, registre e siga com a implementação”.
**Estado**: implementação autorizada; gates de validação permanecem obrigatórios.

## Resumo

Migrar quatro auxiliares locais para skills explícitas prefixadas, sem pontes
Local, preservando contratos e agentes. Criar cada destino, compará-lo com
sua origem, validar estaticamente e no cliente antes de remover a fonte.
Não alterar aplicação, agentes indiscriminadamente, hooks ou componentes
gerenciados. As decisões humanas estão em [research.md](research.md).

## Contexto Tecnico

**Linguagem/Versão**: Markdown e frontmatter YAML do formato `SKILL.md`.
**Dependências Principais**: cliente Copilot com suporte a skills e agentes
existentes; nenhuma dependência nova.
**Persistência**: arquivos versionados; sem banco, vault ou schema.
**Testes**: comparação semântica, validação de frontmatter/links, diff, hashes
e cenários interativos do [quickstart](quickstart.md).
**Plataforma Alvo**: VS Code/Copilot com raiz AltivyNotes selecionada,
isoladamente e em workspace com Dokpod.
**Tipo de Projeto**: manutenção das customizações do monorepo.
**Metas de Desempenho**: não aplicáveis; critérios são fidelidade e segurança.
**Restrições**: sem implementação antes da revisão/autorização; testes de
produto eventualmente autorizados continuam em containers; sem instalação
ou reconciliação de extensões.
**Escala/Escopo**: quatro prompts, quatro skills, consumidores diretos e este
conjunto Spec Kit; nenhum código ou teste da aplicação é parte da migração.

## Verificação da Constituição

| Gate | Antes da pesquisa | Após o desenho |
| --- | --- | --- |
| Integridade e portabilidade | Preservar arquivos do usuário e fontes até validação | Atendido pelo gate por fluxo e hashes |
| Identidade externa | Nenhuma mudança de identidade | Não aplicável ao produto; sem mudanças |
| Segurança e privacidade | Sem secrets/vaults/MCP; não ampliar ferramentas | Atendido por exclusões e falha fechada |
| Modularidade e compatibilidade | Sem dependência nova ou mudança pública | Contratos dos auxiliares e recuperação documentados |
| Qualidade e pt-BR | Evidência estática e interativa separadas | Cenários positivos/negativos e registro por fluxo definidos |
| Processo único e autorização | Spec Kit antes da migração | Tarefas autorizadas explicitamente às 17:32 (-03:00); nenhum auxiliar autoriza produto |

Não há violação identificada nem necessidade de ADR novo. A análise dos gates
é da IA; a aprovação humana dos artefatos está referenciada acima.

## Estrutura do Projeto

### Documentação desta Funcionalidade

```text
specs/009-migracao-prompts-skills/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/auxiliares.md
├── checklists/requirements.md
└── tasks.md
```

### Código-Fonte (raiz do repositório)

```text
.github/
├── prompts/{create-adr,generate-tests,review-change,localize-speckit}.prompt.md
├── skills/altivy-notes-create-adr/SKILL.md
├── skills/altivy-notes-generate-tests/SKILL.md
├── skills/altivy-notes-review-change/SKILL.md
├── skills/altivy-notes-localize-speckit/SKILL.md
├── README.md
├── COPILOT_GUIDE.md
├── copilot-instructions.md
├── instructions/documentation.instructions.md
├── GOVERNANCE.md
└── plano-migracao-prompts-skills.md
```

**Decisão de Estrutura**: cada skill possui um procedimento independente,
subordinado ao Spec Kit e às instruções existentes. O plano preliminar será
mantido como origem histórica com link para esta feature, sem autoridade de
execução. Agentes existentes serão referenciados, não reconfigurados.

## Desenho e validação

1. Registrar HEAD e hashes das fontes, agentes e superfícies protegidas,
   sem ler `.vscode/mcp.json` ou secrets; para MCP comparar somente estado
   Git, nunca seu conteúdo.
2. Usar `name` igual ao diretório, `description` acionável,
   `user-invocable: true`, `disable-model-invocation: true`. Não copiar campos
   `agent`, `model` ou `tools`.
3. Instruir seleção explícita de `Altivy Solution Architect`, `Altivy Quality
   Engineer` ou `Altivy Code Reviewer`, conforme o fluxo. Não prometer que
   frontmatter seleciona agentes; se indisponíveis, bloquear o trabalho
   dependente até escolha explícita de alternativa compatível.
4. Referenciar templates e instruções por links relativos recalculados,
   preservando o procedimento exclusivo do prompt sem replicar seus donos.
5. Na revisão, inspeção não escreve código. Comandos estreitos dependem de
   autorização específica e ferramentas declaradas, com aviso de artefatos.
6. Concluir a comparação semântica e testes interativos por fluxo antes da
   remoção. Uma sessão sem cliente disponível não aprova esse gate.
7. Atualizar consumidores depois dos destinos existirem. Remover origens
   somente dos fluxos aprovados; fontes temporárias ainda presentes são
   históricas/em migração, não uma segunda implementação mantida.
8. Conferir referências operacionais, links, diff e superfícies protegidas.
   Relatar pendências em vez de declarar conclusão sem evidência.

As interfaces são descritas em [contracts/auxiliares.md](contracts/auxiliares.md);
o modelo documental e estados estão em [data-model.md](data-model.md).
O registro de resultados será acrescentado ao [quickstart](quickstart.md)
durante execução autorizada, nunca preenchido antecipadamente como sucesso.

## Recuperação e limites

Preservar origens quando algum teste falhar. Se um fluxo já removido precisar
ser recuperado, usar somente o diff/arquivo da referência inicial após revisão,
sem reset amplo e sem sobrescrever trabalho preexistente. Não instalar
extensões para testar localização; usar fixtures descartáveis fora das
superfícies protegidas.

## Registro de Complexidade

Nenhuma exceção à constituição ou abstração nova necessária.

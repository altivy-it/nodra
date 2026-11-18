---
name: altivy-notes-localize-speckit
description: "Use quando solicitado explicitamente para auditar ou traduzir textos humanos do Spec Kit no Nodra após instalação ou atualização, preservando contratos, literais, exclusões e conteúdo do usuário. Sem modo explícito, somente audita."
argument-hint: "Extensão ou arquivos alvo; modo=auditar ou modo=traduzir; exclusões adicionais"
user-invocable: true
disable-model-invocation: true
---

# Localização segura do Spec Kit no Nodra

Use após instalar ou atualizar uma extensão, preset ou integração do Spec Kit,
como apoio ao trabalho oficial, nunca como processo concorrente. Siga a
[constituição](../../../.specify/memory/constitution.md) e as
[regras de documentação](../../instructions/documentation.instructions.md).

## Uso e autorização

Na raiz Nodra, invoque explicitamente:

```text
/altivy-notes-localize-speckit extensão=assess modo=auditar
/altivy-notes-localize-speckit extensão=assess modo=traduzir
/altivy-notes-localize-speckit arquivos=.github/skills/speckit-*/SKILL.md modo=traduzir
```

Essas entradas são orientações em linguagem natural, não opções do CLI nem
variáveis interpoladas pelo cliente. Sem modo explícito, somente audite.
Sem alvo explícito, inventarie os arquivos do Spec Kit no Nodra e
apresente candidatos; não traduza indiscriminadamente. Esclareça modos
inválidos ou contraditórios antes de qualquer escrita.

A tradução exige alvo, modo e autorização explícitos no escopo solicitado.
Esta skill não impõe um agente novo nem seleciona agente/modelo/tools pelo
frontmatter; não amplia as ferramentas do agente selecionado. Sem ferramenta
necessária, informe o bloqueio. Não instala nem reconcilia componentes para
validar a migração ou a tradução.

## Configuração nativa e limites

- Não há opção geral de idioma identificada nas referências oficiais do
  procedimento original para traduzir automaticamente qualquer extensão.
  Não invente `locale`, `language`, `--lang` ou um evento `after_install`.
- `installed` em `.specify/extensions.yml` registra extensões instaladas;
  editar a lista não instala componentes. Hooks antes/depois de comandos
  não equivalem a hooks de instalação.
- Overrides em `.specify/templates/overrides/` personalizam templates, mas
  não traduzem automaticamente skills materializadas em `.github/skills/`.
- Presets podem fornecer versões localizadas de comandos centrais e extensões.
  Nas integrações compatíveis, instalação/reconciliação materializa comandos
  ou skills; o agente não resolve a pilha a cada invocação. Isso distribui
  traduções previamente preparadas, não traduz extensões desconhecidas.
- Bundles podem declarar extensões, presets e workflows para instalação
  conjunta. Não acrescentam tradução automática; precisam incluir preset
  localizado que cubra os comandos desejados.
- A documentação online pode descrever capacidades posteriores à versão
  local. Consulte `.specify/init-options.json`, `.specify/integration.json`
  e, se o CLI já estiver disponível, `specify version` e o `--help` do
  subcomando antes de recomendar opções.
- Não instale extensões, presets, bundles, ferramentas ou dependências
  como parte da tradução. Distribuição por preset/bundle exige escopo e
  aprovação próprios.

## Procedimento

1. Leia a versão atual dos arquivos, inclusive alterações locais do
   desenvolvedor. Restrinja o trabalho à raiz Nodra e ao alvo solicitado.
2. Identifique a origem em `.specify/extensions/<extensão>/commands/`, as
   skills materializadas em `.github/skills/` e os textos pertinentes de
   `extension.yml` ou `workflow.yml`. Não suponha que fonte e skill sejam idênticas.
3. Informe candidatos e exclusões. Em auditoria, apresente evidências sem
   editar. Em tradução, prossiga somente no escopo explícito e peça
   autorização antes de ampliá-lo.
4. Registre referência anterior de conteúdo, tokens técnicos e hashes dos
   arquivos protegidos. Preserve trabalho local; não use reset, checkout,
   instalação forçada ou regeneração indiscriminada.
5. Traduza apenas texto humano para pt-BR, preservando força normativa,
   limites numéricos, sequência, condições de parada, aprovações e segurança.
6. Se fonte e skill forem equivalentes, confirme isso antes de replicar
   tradução. Preserve aliases específicos de cada representação; não copie
   uma sobre a outra cegamente.
7. Execute as verificações abaixo. Se algum contrato mudar, corrija a
   tradução antes de prosseguir. Não execute o fluxo de uma skill para
   testar seu texto: isso pode implementar código, instalar componentes
   ou produzir efeitos externos.
8. Informe resultados e limites. Remova automação temporária criada para
   a tarefa; não faça commit nem mantenha scripts sem solicitação explícita.

## O que traduzir

- Descrições, compatibilidade em linguagem natural, explicações, comentários
  documentais, perguntas e mensagens ao usuário.
- Exemplos de texto e descrições de tarefas, preservando IDs, marcadores e caminhos.
- Textos de relatórios gerados e referências internas às seções traduzidas.
  Mantenha reconhecimento dos títulos antigos consumidos por etapas posteriores.
- Em YAML, apenas campos textuais pertinentes como `description`, `prompt`
  e `message`. Nome de exibição só muda se confirmado como texto humano,
  não identidade ou chave de registro.

## O que preservar

- Nomes de arquivos, diretórios, comandos, aliases, opções do CLI, ferramentas
  e expressões executáveis.
- IDs de extensão, preset, workflow, integração e tarefas; chaves YAML/JSON;
  tipos, versões, dependências, prioridades, condições e ordem das etapas.
- Metadados técnicos de proveniência e descoberta, como `name`, `author`,
  `source`, `schema_version` e referências a arquivos.
- Marcadores e placeholders: `EXECUTE_COMMAND:`, `[NEEDS CLARIFICATION]`,
  `[NEEDS CLARIFICATION: ...]`, `__SPECKIT_COMMAND_*__`, `$ARGUMENTS`,
  `{command}`, `{extension}`, `{prompt}`, `{description}`, `{{ inputs.spec }}`
  e `{CORE_TEMPLATE}`.
- Variáveis/campos de saída como `BUG_SLUG`, `BUG_DIR`, `FEATURE_DIR`,
  `SPEC_FILE`, `TEMPLATE_CONTENT` e `AVAILABLE_DOCS`.
- Checkboxes e marcadores `- [ ]`, `[x]`, `[X]`, `[P]`, `[US1]`, `T001`,
  `CHK001`; estados literais `verified`, `partial`, `failed`, `applied`,
  `not-applied`, `converged` e `tasks_appended`.
- Regex, schemas, URLs, limites de rede, listas de hosts e regras contra
  instruções não confiáveis. Não amplie permissões, libere hosts bloqueados
  ou solicite secrets durante a tradução.
- Títulos/labels usados como contrato de leitura por outra etapa, salvo
  comprovação de que são apresentação e atualização consistente dos consumidores.

## Exclusões padrão

- Não altere `.specify/extensions/bug/README.md`; use a tradução existente
  em `.specify/extensions/bug/README.pt-BR.md` quando aplicável.
- Não altere templates originais com overrides pt-BR nem os overrides já
  traduzidos, salvo solicitação explícita.
- Não altere código, testes, specs de features ou constituição para acomodar tradução.
- Não reescreva hashes, registries ou manifests de instalação para ocultar
  modificações locais. Não remova avisos de arquivos modificados; registre-os
  para revisão.

## Validação obrigatória

- Compare comandos, IDs, aliases, placeholders, marcadores e literais técnicos,
  incluindo ocorrências. Para prosa traduzida em exemplos, explicite as
  substituições permitidas; não ignore todo o bloco de código.
- Confira comandos executáveis e exemplos JSON/YAML; não confunda prosa
  com contratos de máquina.
- Valide frontmatter/YAML com parser existente quando disponível, sem instalar
  dependências. Compare estrutura, permitindo só campos textuais autorizados.
  Sem parser, use diagnósticos do editor quando disponíveis e comparação
  estrutural, declarando o limite de cada verificação não disponível.
- Preserve indentação YAML, aspas, delimitadores, blocos Markdown, links e
  UTF-8; confira nomes e campos de descoberta.
- Revise equivalência semântica: somente leitura, append-only, bloqueios,
  consentimento, tratamento de erros, limites de perguntas e hooks iguais.
- Verifique por hashes que excluídos ficaram intactos. Não declare sucesso
  quando uma checagem falhar ou não puder executar.

## Resultado esperado e limites

Informe quantidade/escopo dos arquivos traduzidos, exclusões preservadas,
verificações e riscos residuais. A constituição orienta o Copilot, mas não
configura o CLI como tradutor. Este auxiliar é manual após instalações ou
atualizações, não um hook automático.

Atualizações/reconciliações podem preservar, avisar ou substituir arquivos
modificados conforme versão/operação; revise o diff e repita a auditoria,
sem presumir comportamento único. Este auxiliar não autoriza commit, push,
publicação ou deploy.

## Referências oficiais

- [Personalização](https://github.github.io/spec-kit/guides/customization.html)
- [Extensões e hooks](https://github.github.io/spec-kit/reference/extensions.html)
- [Presets e resolução](https://github.github.io/spec-kit/reference/presets.html)
- [Bundles](https://github.github.io/spec-kit/reference/bundles.html)
- [Comandos centrais](https://github.github.io/spec-kit/reference/core.html)

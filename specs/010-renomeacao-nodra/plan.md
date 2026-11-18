# Plano de Implementacao: Altivy Nodra

**Branch**: `010-renomeacao-nodra` | **Data**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificacao aprovada pelo usuario nesta conversa; destino confirmado `altivy-it/nodra`.

**Status**: Aprovado pelo usuario nesta conversa em 2026-11-11 para implementacao conforme plano e compatibilidade. O usuario tambem autorizou o fluxo Bug Fixing para o bloqueio Angular e a renomeacao remota ao final das verificacoes.

**Retomada autorizada**: Em 2026-11-11 o usuario aprovou explicitamente o design
apresentado e solicitou continuar o plano. A interface aprovada deve ser
preservada; a retomada trata formato, testes e qualificacao das tarefas abertas,
sem deploy, publicacao de imagens ou mudanca de infraestrutura compartilhada.

## Resumo

Adotar Nodra como nome do produto, preservando Altivy como empresa e Altivy Nodra como marca completa. Renomear o repositorio existente, a solucao, os projetos, o codigo e a documentacao ativa em uma branch derivada da main consolidada em a9546a5. Nao recriar repositorio nem perder commits.

A hipotese local e que os nomes publicos e de compilacao podem mudar sem alterar identificadores persistidos e externos. O teste discriminante combina build/testes dos projetos renomeados, contratos de compatibilidade e jornadas de instalacao/atualizacao pela borda nginx. Uma substituicao global sem classificacao esta descartada.

## Contexto Tecnico

**Linguagem/Versao**: C#/.NET 10, TypeScript/Angular 22, PowerShell 7, Bash e Python nos scripts ja existentes.

**Dependencias Principais**: ASP.NET Core, EF Core/Npgsql, PostgreSQL, Keycloak, nginx e bibliotecas frontend existentes; nenhuma nova dependencia.

**Persistencia**: Markdown canonico; PostgreSQL reconstruivel; preferencias do navegador e chaves criptograficas tecnicas existentes.

**Testes**: Suites backend existentes, Vitest/Angular, Playwright em `frontend/tests/e2e`, validacao de repositorio, contratos e sintaxe de containers/scripts.

**Plataforma Alvo**: Self-hosted em containers; HTTP somente pela borda nginx.

**Tipo de Projeto**: Monorepo modular com API, BFF, worker, frontend e contratos independentes.

**Metas de Desempenho**: Nao alterar comportamento, consultas, latencia ou recursos exigidos; nao ha nova funcionalidade de IA nesta entrega.

**Restricoes**: Nenhum acesso a secrets; nenhum deploy, publicacao de imagens, mudanca DNS ou transferencia de organizacao. Identidade e infraestrutura compartilhada do AKB nao pertencem a este repositorio.

**Escala/Escopo**: Transversal aos seis projetos de producao .NET, seis suites backend, frontend, CI, deploy, contratos, ferramentas e documentacao ativa; sem mudanca de schema ou de fonte de verdade.

## Verificacao da Constituicao

Gate anterior a pesquisa: atende aos principios I-V com as restricoes abaixo. Gate apos desenho: mesmas restricoes preservadas; execucao depende de revisao humana deste plano e das tarefas.

- Integridade: nao renomear schemas, bancos, roles, paths de vaults, particoes ou registros existentes.
- Identidade externa: manter realm/clientes/audience configurados; nao transferir nem recriar identidade.
- Seguranca: preservar o caminho externo de secrets e o discriminador Data Protection; nao imprimir configuracao resolvida com credenciais.
- Compatibilidade: aplicar a matriz em [contracts/compatibilidade.md](contracts/compatibilidade.md); namespaces nao sao tratados como contratos de dados.
- Qualidade: executar gates em containers e registrar separadamente o bloqueio Angular preexistente. Alteracao visual exige screenshot desktop/mobile.
- Governanca: specs antigas e `docs/plan/` sao registros historicos, nao alvos de substituicao indiscriminada. Mudancas na constituicao, se necessarias, seguem o comando oficial e revisao humana.

Nao ha nova decisao arquitetural que exija ADR: nao mudam fronteiras, seguranca, contratos funcionais, schema ou infraestrutura. Se a implementacao exigir alterar um desses itens, interromper a tarefa e submeter a decisao ao mantenedor.

## Estrutura do Projeto

### Documentacao desta Funcionalidade

```text
specs/010-renomeacao-nodra/
  spec.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/compatibilidade.md
  checklists/requirements.md
  tasks.md
```

### Codigo-Fonte

```text
Nodra.slnx                              # antes Altivy.slnx
backend/apps/{api,bff,worker}/           # Nodra.*.csproj e namespaces
backend/libs/{domain,application,infrastructure}/
backend/tests/                          # projetos e referencias Nodra.*
frontend/web/                           # titulo, rotas, pacote e alvo Angular
frontend/libs/                          # apenas referencias ativas ao produto
frontend/tests/                         # expectativas de marca e E2E
contracts/openapi/                      # titulo do contrato, sem mudar endpoints
deploy/                                # arquivos, imagens e referencias internas
tools/scripts/                          # comandos e validadores atualizados
.github/                               # CI, instrucoes e auxiliares locais
docs/                                  # documentacao vigente e registro de marca
```

**Decisao de Estrutura**: Manter pastas genericas e fronteiras existentes; renomear arquivos e diretorios especificos do produto quando isso nao for um identificador persistido. Atualizar consumidores no mesmo incremento. O diretorio local do workspace pode permanecer AltivyNotes sem impedir clone novo como nodra.

## Etapas de Entrega

1. Preparar inventario e testes de nomenclatura/compatibilidade; registrar a aprovacao humana das excecoes.
2. Atualizar marca no README, plano mestre e documentacao de marca; manter o posicionamento em ingles como literal aprovado, com contexto em pt-BR.
3. Migrar solucao, projetos, namespaces, referencias e entrypoints de containers; manter literais persistidos conforme contrato.
4. Migrar titulos, nomes visiveis, pacote/alvo frontend e assets pertinentes; nao alterar preferencias existentes sem migracao testada.
5. Atualizar CI, deploy, scripts, documentacao ativa e validadores, mantendo aliases operacionais legados onde necessario.
6. Executar gates, analisar ocorrencias restantes e registrar evidencias; nao marcar tarefas bloqueadas como concluidas.
7. Renomear o repositorio remoto para `altivy-it/nodra` preservando historico, issues e PRs; atualizar origin e verificar acesso. A operacao e global ao repositorio e nao fica isolada em branch; executar somente apos reconfirmacao do momento pelo mantenedor.

## Validacao e Bloqueios

- Repositorio: ancestralidade da base consolidada, formato, links, referencias de projetos e inventario de excecoes.
- Backend: formatador/analisadores, build e suites pertinentes em container; PostgreSQL real para contratos persistidos quando houver risco de alteracao.
- Frontend: instalacao reproduzivel, testes e build de producao em container; Playwright e screenshots via nginx para marca.
- Operacao: sintaxe e resolucao de Compose com valores sinteticos; nenhum arquivo externo de secrets e usado para validar.
- GitHub: permissao de administracao, ausencia de colisao no nome, preservacao de IDs/historico e autenticacao do origin apos renomear.
- Bloqueio conhecido: `npm ci` falha tanto na base quanto apos os merges com `@angular/router@22.2.0` versus `@angular/common@22.1.3`. Nao usar `--force` ou `--legacy-peer-deps` como evidencia de gate aprovado. Correcao exige o fluxo oficial de defeito e autorizacao propria.

## Registro de Complexidade

Sem violacoes propostas. Excecoes de nomenclatura sao contratos de compatibilidade, nao autorizacao para remover controles ou reescrever dados. O inventario final deve permitir distinguir empresa Altivy, legado preservado e ocorrencias indevidas.

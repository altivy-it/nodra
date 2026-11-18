# Altivy - Instruções globais

Toda documentação produzida ou atualizada para o projeto, inclusive artefatos
gerados pelo Spec Kit e relatórios de Bug Fixing, deve estar em português
brasileiro (pt-BR). Preserve em inglês comandos, identificadores, caminhos,
termos de contrato e marcadores literais exigidos por ferramentas.

## Contexto obrigatório

- O Nodra é uma aplicação web self-hosted de gestão de conhecimento em Markdown.
- Leia o [plano mestre](../docs/plano-mestre.md) antes de alterar arquitetura, contratos, persistência ou segurança.
- Consulte [frontend](../docs/frontend.md), [backend](../docs/backend.md) e [evolução do produto](../docs/evolucao-produto.md) conforme o escopo.
- Arquivos Markdown no vault são a fonte canônica; PostgreSQL é uma projeção reconstruível.
- A solução deve operar em containers, sem exigir runtimes instalados na máquina do usuário final.
- Runners de testes, ferramentas de build e runtimes da aplicação rodam em
	containers; comandos Docker no host apenas os orquestram. Sempre que um teste,
	preview ou validação exigir uma aplicação em execução, suba-a em container e
	acesse-a exclusivamente pela borda de um proxy reverso nginx também em
	container. Isso vale para Playwright com API simulada e para E2E com serviços
	reais: não use `ng serve`, BFF ou API diretamente como URL do navegador/teste.
	Testes unitários sem aplicação HTTP em execução continuam em containers e não
	exigem subir um nginx ocioso. Use a topologia de `deploy/e2e/` para ambientes
	reais e `deploy/e2e/nginx-mocked.conf` para testes simulados; os comandos de
	execução ficam em `frontend/tests/e2e/README.md`.

## Estrutura do monorepo

```text
backend/
├── apps/                   # hosts .NET: API, BFF e futuro worker
├── libs/                   # domínio, aplicação e infraestrutura .NET
└── tests/                  # testes backend
frontend/
├── apps/                   # aplicações Angular
├── libs/                   # bibliotecas frontend
└── tests/                  # testes frontend
contracts/                  # OpenAPI, schemas e exemplos independentes de linguagem
tests/fixtures/             # vaults e dados sintéticos sem informação sensível
deploy/                     # Dockerfiles, Compose, proxy e operação
docs/                       # arquitetura e evolução do produto
```

Não crie dependências circulares. `apps/*` compõem e hospedam; regras reutilizáveis pertencem a `libs/*`. `contracts/*` não depende de código de aplicação.

## Fluxo de trabalho

1. Comece pelo comportamento solicitado, teste relacionado e módulo proprietário.
2. Declare a hipótese local e a validação que pode refutá-la.
3. Faça a menor alteração coerente com o plano mestre.
4. Valide primeiro o recorte alterado; depois execute verificações amplas proporcionais ao risco.
5. Atualize contrato, migração, documentação e observabilidade quando o comportamento exigir.
6. Não corrija nem reformate código fora do escopo.

Para mudanças da aplicação, esse fluxo técnico é executado exclusivamente dentro dos processos Spec Kit definidos abaixo; ele não autoriza trabalho fora de uma tarefa Spec Kit.

## ADRs e planos

- Crie ADRs em `docs/adr/AAAA-NNNN-titulo.md` desde o status `proposed`, seguindo `.github/ADR_TEMPLATE.md`.
- Spec Kit é o único processo autorizado para desenvolver e manter a aplicação. Não execute features, correções, manutenção ou iniciativas de produto fora de um processo Spec Kit e de seus artefatos/tarefas.
- Para features e mudanças de produto, use `specs/<feature>/spec.md` para requisitos, `plan.md` para desenho e `tasks.md` para tarefas. Use `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, `/speckit-implement` e `/speckit-converge` conforme a etapa.
- Para correções de defeitos, instale e use a extensão oficial Spec Kit Bug Fixing (`/speckit-bug-assess`, `/speckit-bug-fix`, `/speckit-bug-test`). Não use fluxo de diagnóstico/correção independente.
- Use `.specify/memory/constitution.md` para princípios duradouros de engenharia; instruções operacionais do Copilot continuam em `.github/` e a constituição não duplica essas regras.
- O plano mestre e documentos de domínio definem visão e contexto do produto; ADRs aceitos registram decisões arquiteturais. Eles restringem as features, mas não substituem seus requisitos. Se houver conflito, interrompa e peça decisão humana.
- `docs/plan` está congelado para novos planos, atualizações e execução. Os sete planos legados abertos foram convertidos em oito conjuntos Spec Kit em rascunho e aguardam revisão humana; não retome essas iniciativas até a revisão dos artefatos. Depois, arquive ou substitua os originais por referências sem autoridade operacional.
- `.github/PLAN_TEMPLATE.md` está obsoleto e não deve ser usado para criar planos. Não crie fluxos locais de planejamento, implementação ou diagnóstico concorrentes; prompts, agentes e skills do repositório só podem apoiar uma tarefa Spec Kit existente.
- `/speckit-constitution` deve ser usado uma vez após a inicialização, registrando apenas princípios existentes ou explicitamente acordados.
- Registre origem e revisor humano em ADRs. IA não marca ADR como `accepted` sem decisão ou evidência humana explícita e referenciada.
- Checkboxes em `specs/<feature>/tasks.md` registram execução, não aprovação humana; só marque uma tarefa concluída após implementar e validar a evidência correspondente.

### Extensões e localização

- As extensões oficiais `assess`, `bug` e `git` apoiam o mesmo processo Spec Kit. `assess` registra descoberta em `.specify/assessments/<slug>/`; seu veredito `go` encaminha para especificação, nunca autoriza implementação ou aprova artefatos em nome de uma pessoa.
- Use as mesmas configurações Spec Kit do Inventory360Api: `auto_execute_hooks: true`, hooks Git com `enabled: true` e `git-config.yml` igual ao da referência. A invocação de um fluxo Spec Kit autoriza os hooks nele configurados; não autoriza push, publicação, deploy ou implementação de produto fora das tarefas aprovadas.
- Após instalar, atualizar ou reconciliar componentes, use
  [altivy-notes-localize-speckit](skills/altivy-notes-localize-speckit/SKILL.md)
  para auditar/traduzir textos humanos em pt-BR sem alterar comandos, aliases,
  IDs, placeholders ou contratos de leitura. Invoque explicitamente na raiz
  Nodra; sem modo explícito, somente audite. Não invente configuração de
  idioma ou hooks de instalação.
- Consulte [o guia de extensões e manutenção](README.md#spec-kit-e-extensões) para versões, comandos e validação. Preserve constituição, specs, overrides e personalizações locais ao atualizar.

## Commits

- Todo commit criado ou sugerido segue [Conventional Commits](COMMIT_CONVENTIONS.md) e usa os tipos e escopos aprovados pelo projeto.
- Escreva a descrição em português brasileiro, no imperativo, iniciando com minúscula e sem ponto final.
- Mudanças incompatíveis usam `!` no cabeçalho e o rodapé `BREAKING CHANGE:` com impacto e migração.
- Fora dos hooks autorizados pela invocação do fluxo Spec Kit, não faça commit sem solicitação explícita. Push, publicação e deploy sempre exigem solicitação explícita.

## Scripts e automação

- Para qualquer comando Angular do frontend, use o alias `ng` definido no
	profile do PowerShell, executando-o a partir de `frontend/web`. O alias inicia
	o container padronizado do projeto; não invoque `node`, `npm`, `npx` ou uma
	instalação local do Angular CLI diretamente no host.
- O profile também define o alias `npx`, que executa a ferramenta na mesma
	imagem containerizada de `node` e `npm`; use-o no PowerShell interativo quando
	for necessário executar um pacote diretamente. Em scripts, tarefas e CI que
	não carregam o profile, use explicitamente `npm exec -- <comando>` dentro do
	container apropriado; não invoque `npx` instalado no host.
- Antes de enviar `ng`, confirme que o diretório atual é `frontend/web`; se o
	terminal estiver na raiz do monorepo, envie primeiro
	`Set-Location $env:USERPROFILE\projetos\nodra\frontend\web`. Executar
	`ng` na raiz monta um diretório sem `angular.json` e falha com “outside a
	workspace”.
- Envie os comandos ao terminal como texto literal, sem caracteres de controle
	como `^U` ou `Ctrl+U` antes de `ng`. Se `^U` aparecer no prompt, descarte essa
	entrada e reenvie o comando limpo; ele não faz parte do comando Angular.
- Após `ng test` ou `ng build`, aguarde a conclusão e o resumo final do Angular
	ou Vitest antes de concluir que não houve saída. A geração do bundle pode
	terminar antes da execução dos testes e a captura do terminal pode chegar em
	partes.
- Exemplos: `Set-Location frontend/web; ng test --watch=false` e
	`Set-Location frontend/web; ng build --configuration production`.
- Ao criar ou refatorar scripts, siga `.github/instructions/script-authoring.instructions.md` e `.github/SCRIPTING.md`.
- Scripts PowerShell e Bash de automação do repositório pertencem a `tools/scripts/`.
- Ferramentas Python de automação pertencem a `tools/scripts/<nome>/`, são executadas com `uv` e compartilham o único `tools/pyproject.toml` do repositório.
- Scripts que fazem parte do build, deploy ou runtime de um módulo permanecem na pasta específica desse módulo.
- Todo script criado ou refatorado deve oferecer `--help` com finalidade, dependências, uso, exemplos e referência para documentação mais ampla quando ela existir.

## Regras invariáveis

- Preserve conteúdo do usuário; conflitos nunca podem causar sobrescrita silenciosa.
- Normalize e autorize paths antes de qualquer acesso ao filesystem.
- Nunca registre conteúdo integral de notas, tokens, senhas ou secrets.
- No desenvolvimento local em Windows, armazene todo secret exclusivamente em
	`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`. Isso inclui usuários
	e senhas administrativas do Keycloak, credenciais do PostgreSQL, client
	secrets, connection strings, credenciais SMTP, IdPs e qualquer outro valor
	sensível necessário ao projeto.
- Nunca crie arquivos `.env`, `.env.*`, `secrets.json`, cópias, templates com
	valores reais ou outros arquivos de secrets dentro da estrutura do
	repositório. Não use `dotnet user-secrets`; scripts, Compose, coleções HTTP,
	testes e documentação devem referenciar o arquivo externo ou variáveis de
	ambiente injetadas por processo seguro.
- Não leia, exiba, registre, copie ou sobrescreva o conteúdo do arquivo externo
	durante validações. Antes de mover ou gravar secrets, falhe fechado se o
	destino já existir.
- Conteúdo, persistência, busca e edição não podem depender de embeddings, nuvem ou internet; acesso autenticado depende somente do Keycloak configurado, que pode operar na rede local.
- Keycloak é a plataforma externa obrigatória de identidade e decisão de autorização. Não implemente cadastro, senha, recuperação de conta, diretório de usuários, memberships ou políticas de autorização no Nodra.
- O BFF atua somente como cliente OIDC e proteção de tokens; a API atua como PEP e falha fechada ao aplicar as permissões emitidas pelo Keycloak.
- Não adicione Redis, object storage, banco de grafos ou microsserviço sem ADR e evidência operacional.
- Tabelas com potencial de crescimento elevado ou retenção temporal devem usar Table Partitioning/Declarative Partitioning por faixa de data, preferencialmente partições mensais; a solução deve definir criação antecipada, rollover, partição de segurança para datas fora da janela, índices por partição, retenção e testes de roteamento antes da produção, independentemente do banco de dados adotado.
- Use somente dependências externas com licença permissiva, gratuita para todos os usos previstos e previamente verificada, como MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC ou PostgreSQL.
- Nunca sugira, instale ou adicione biblioteca paga, proprietária, source-available, com licença comercial obrigatória, copyleft ou outra restrição incompatível com distribuição e uso self-hosted. Licença ausente, ambígua ou não verificada bloqueia a dependência.
- O padrão arquitetural Mediator é permitido e deve ser aplicado quando houver necessidade comprovada de desacoplar dispatch, handlers e pipelines. Nesses casos, use `Nuuvify.CommonPack.Mediator` com versão centralizada e licença da versão exata verificada.
- Nunca use a biblioteca MediatR; a proibição é específica ao pacote e ao seu licenciamento, não ao padrão Mediator.
- Use tags de container e dependências fixadas de forma reproduzível; não publique nem faça deploy sem solicitação explícita.
- APIs públicas usam OpenAPI, `application/problem+json`, ETags e versionamento explícito.
- Mudanças de schema seguem expand-contract e devem ser testadas com PostgreSQL real.

## Qualidade mínima

- Código novo deve ter testes nos níveis adequados ao risco.
- Rode formatador, análise estática, build e testes do projeto alterado.
- Para fluxos do usuário, atualize ou adicione Playwright.
- Para PostgreSQL/filesystem, use testes de integração reais, não mocks como evidência principal.
- Reviews priorizam integridade de dados, segurança, regressões, concorrência e lacunas de teste.

## Instruções especializadas

As regras detalhadas em `.github/instructions/` são carregadas pelo caminho alterado ou pela descrição. Não replique essas regras em código ou documentação local.
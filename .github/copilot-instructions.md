# Thothis - Instruções globais

## Contexto obrigatório

- O Thothis é uma aplicação web self-hosted de gestão de conhecimento em Markdown.
- Leia o [plano mestre](../README.md) antes de alterar arquitetura, contratos, persistência ou segurança.
- Consulte [frontend](../docs/frontend.md), [backend](../docs/backend.md) e [evolução do produto](../docs/evolucao-produto.md) conforme o escopo.
- Arquivos Markdown no vault são a fonte canônica; PostgreSQL é uma projeção reconstruível.
- A solução deve operar em containers, sem exigir runtimes instalados na máquina do usuário final.

## Estrutura do monorepo

```text
apps/
├── web/                    # aplicação Angular
├── api/                    # host ASP.NET Core HTTP/SignalR
└── worker/                 # host .NET de jobs e reconciliação
libs/
├── frontend/               # bibliotecas Angular reutilizáveis
└── backend/                # projetos .NET de domínio, aplicação e infraestrutura
contracts/                  # OpenAPI, schemas e exemplos independentes de linguagem
tests/
├── frontend/               # E2E e testes compartilhados da UI
├── backend/                # integração, arquitetura e desempenho .NET
└── fixtures/               # vaults e dados sintéticos sem informação sensível
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

## ADRs e planos

- Crie ADRs em `docs/adr/AAAA-NNNN-titulo.md` desde o status `proposed`, seguindo `.github/ADR_TEMPLATE.md`.
- Crie planos em `docs/plan/<tema>.md`, seguindo `.github/PLAN_TEMPLATE.md`; não use `docs/planos` nem mantenha plano apenas no chat.
- Todo plano possui status geral e status em cada etapa. Atualize o histórico ao mudar qualquer status.
- Registre `Origem` como humano ou IA assistida e identifique o revisor humano.
- IA não marca ADR como `accepted`, plano como `approved` ou etapa como `completed` ou `skipped` sem decisão ou evidência humana explícita e referenciada.

## Commits

- Todo commit criado ou sugerido segue [Conventional Commits](COMMIT_CONVENTIONS.md) e usa os tipos e escopos aprovados pelo projeto.
- Escreva a descrição em português brasileiro, no imperativo, iniciando com minúscula e sem ponto final.
- Mudanças incompatíveis usam `!` no cabeçalho e o rodapé `BREAKING CHANGE:` com impacto e migração.
- Não faça commit, push, publicação ou deploy sem solicitação explícita.

## Scripts e automação

- Ao criar ou refatorar scripts, siga `.github/instructions/script-authoring.instructions.md` e `.github/SCRIPTING.md`.
- Scripts PowerShell e Bash de automação do repositório pertencem a `tools/scripts/`.
- Ferramentas Python de automação pertencem a `tools/scripts/<nome>/`, são executadas com `uv` e compartilham o único `tools/pyproject.toml` do repositório.
- Scripts que fazem parte do build, deploy ou runtime de um módulo permanecem na pasta específica desse módulo.
- Todo script criado ou refatorado deve oferecer `--help` com finalidade, dependências, uso, exemplos e referência para documentação mais ampla quando ela existir.

## Regras invariáveis

- Preserve conteúdo do usuário; conflitos nunca podem causar sobrescrita silenciosa.
- Normalize e autorize paths antes de qualquer acesso ao filesystem.
- Nunca registre conteúdo integral de notas, tokens, senhas ou secrets.
- Conteúdo, persistência, busca e edição não podem depender de embeddings, nuvem ou internet; acesso autenticado depende somente do Keycloak configurado, que pode operar na rede local.
- Keycloak é a plataforma externa obrigatória de identidade e decisão de autorização. Não implemente cadastro, senha, recuperação de conta, diretório de usuários, memberships ou políticas de autorização no Thothis.
- O BFF atua somente como cliente OIDC e proteção de tokens; a API atua como PEP e falha fechada ao aplicar as permissões emitidas pelo Keycloak.
- Não adicione Redis, object storage, banco de grafos ou microsserviço sem ADR e evidência operacional.
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
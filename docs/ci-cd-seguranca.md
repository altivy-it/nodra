# CI/CD e segurança no GitHub

**Status:** referência arquitetural inicial  
**Data de referência:** 28 de julho de 2026  
**Escopo:** recursos do repositório e da plataforma GitHub para integração contínua, entrega, segurança e cadeia de fornecimento do Altivy.

## 1. Objetivo

Este documento descreve como utilizar o GitHub para proteger mudanças, executar validações, publicar imagens de container e promover releases do Altivy. O Sonar faz parte dessa estratégia como gate de qualidade e segurança do código, mas não substitui os controles nativos do GitHub.

A solução deve combinar quatro camadas:

1. **governança do código:** pull requests, responsáveis, aprovações e rulesets;
2. **CI/CD:** build, testes, empacotamento, publicação e implantação;
3. **segurança:** código, dependências, secrets, workflows e containers;
4. **cadeia de fornecimento:** lockfiles, SBOM, proveniência, assinaturas e artefatos imutáveis.

```mermaid
flowchart LR
    PR[Pull request] --> CI[Build e testes]
    PR --> SONAR[Sonar Quality Gate]
    PR --> SEC[CodeQL e dependências]
    CI --> GATE[Checks obrigatórios]
    SONAR --> GATE
    SEC --> GATE
    GATE --> MAIN[Merge em main]
    MAIN --> IMAGE[Build de imagens]
    IMAGE --> SUPPLY[SBOM e proveniência]
    SUPPLY --> GHCR[GitHub Container Registry]
    GHCR --> ENV[GitHub Environment]
    ENV --> DEPLOY[Deploy aprovado]
```

## 2. Responsabilidades entre repositório e plataforma

### 2.1 Configuração versionada no repositório

O comportamento reproduzível deve ficar no Git:

| Arquivo ou diretório | Responsabilidade |
| --- | --- |
| `.github/workflows/ci.yml` | format, lint, build, testes e contratos |
| `.github/workflows/codeql.yml` | análise CodeQL de C# e TypeScript |
| `.github/workflows/container.yml` | build, smoke test, scan e inspeção das imagens |
| `.github/workflows/release.yml` | versão, SBOM, proveniência, publicação e GitHub Release |
| `.github/workflows/deploy.yml` | promoção do mesmo artefato entre ambientes |
| `.github/dependabot.yml` | atualizações de npm, NuGet, GitHub Actions e imagens base |
| `.github/CODEOWNERS` | responsáveis por módulos e arquivos críticos |
| `.github/SECURITY.md` | política de reporte e resposta a vulnerabilidades |
| lockfiles e manifests | versões reproduzíveis das dependências |
| configuração do Sonar | escopo, exclusões justificadas e relatórios de cobertura |

Os nomes dos workflows são uma proposta inicial. Eles devem ser criados quando os projetos executáveis existirem, mantendo responsabilidades pequenas e checks estáveis.

### 2.2 Estado de implementação

O repositório possui a fundação executável para a fase documental:

- `.github/workflows/ci.yml` executa a validação do repositório e publica os checks estáveis `CI / Repository Baseline` e `CI / Result`;
- `tools/scripts/validate-repository.ps1` verifica arquivos obrigatórios, destinos locais de links e imagens Markdown, referências explícitas, IDs explícitos de headings, status geral e por etapa dos planos e fixação de actions externas por SHA ou digest completo;
- `tools/scripts/test-validate-repository.ps1` cobre o cenário válido e rejeições de link ausente, referência sem definição, action mutável, status geral inválido e etapa sem status;
- os validadores permanecem em PowerShell 7 porque usam apenas APIs nativas, já executam em Windows e Linux e não justificam adicionar Python ou bootstrap com `uv`;
- `.github/CODEOWNERS` registra ownership global e explícito para arquivos críticos;
- `.github/dependabot.yml` atualiza GitHub Actions semanalmente;
- `.github/SECURITY.md` prepara a política de reporte responsável e informa que o canal ainda não está operacional;
- Um checklist administrativo do mantenedor, mantido fora deste repositório público, registra configurações que exigem acesso a **Settings** e evidência manual.

Ainda não existem projetos Angular ou .NET, contracts, lockfiles, Dockerfiles ou manifests de deploy. Os jobs de frontend, backend, integração, contracts, E2E, CodeQL compilado, container, release e deploy devem ser implementados junto com seus primeiros alvos executáveis. Criá-los antes disso produziria checks sem evidência ou permanentemente ignorados.

### 2.3 Configuração administrativa na plataforma GitHub

Alguns controles não vivem em arquivos do repositório e devem ser configurados em **Settings**:

- rulesets e proteção de branches e tags;
- checks obrigatórios;
- permissões padrão do `GITHUB_TOKEN`;
- GitHub Actions permitidas e política de fixação de versões;
- secrets e variables de repositório, ambiente ou organização;
- environments, revisores e branches autorizadas para implantação;
- Dependabot, code scanning, secret scanning e push protection;
- retenção de logs, artifacts e caches;
- acesso ao GitHub Container Registry;
- instalação e permissões do GitHub App do Sonar;
- limites de gastos e consumo do GitHub Actions.

Essas configurações devem ser inventariadas em checklist ou evidência administrativa, porque não são completamente reconstruídas apenas pelo clone do repositório.

## 3. GitHub Actions como motor de CI/CD

GitHub Actions executa workflows YAML em resposta a eventos como `pull_request`, `push`, tags, agenda e acionamento manual. Para o monorepo do Altivy, a divisão recomendada é:

| Job | Validação principal |
| --- | --- |
| `frontend` | format, lint, testes Vitest e build Angular |
| `backend` | format, análise estática, build e testes .NET |
| `integration` | PostgreSQL real, filesystem e Testcontainers |
| `contracts` | OpenAPI, schemas, clientes gerados e breaking changes |
| `e2e` | Playwright contra a aplicação containerizada |
| `sonar` | análise e Quality Gate |
| `codeql` | fluxos vulneráveis em C# e TypeScript |
| `dependency-review` | novas dependências e vulnerabilidades introduzidas pelo PR |
| `container` | BuildKit, smoke test, contrato e scan das imagens |
| `ci-result` | resultado agregado e estável para proteção da branch |

### 3.1 Recursos úteis do GitHub Actions

- **GitHub-hosted runners:** máquinas efêmeras mantidas pelo GitHub, preferidas para CI comum.
- **Self-hosted runners:** necessários somente para rede privada, hardware ou dependências especiais.
- **Service containers:** PostgreSQL e serviços auxiliares durante testes.
- **Matrix:** versões ou sistemas operacionais realmente suportados.
- **Cache:** pacotes npm, NuGet, Playwright e scanner Sonar, sempre derivados de lockfiles.
- **Artifacts:** cobertura, resultados de testes, traces do Playwright, logs sanitizados e SBOM.
- **Reusable workflows:** componentes compartilhados para build, scan, publicação e deploy.
- **Concurrency:** cancela execuções antigas do mesmo pull request e impede deploys simultâneos.
- **Path filters:** reduzem trabalho em monorepo, desde que não deixem checks obrigatórios pendentes.
- **Manual dispatch:** operações controladas, como promoção, rollback ou teste de recuperação.

Caches não são artefatos confiáveis ou assinados. Nunca devem conter tokens, certificados, credenciais ou arquivos que serão executados sem validação. Pull requests de forks podem ler caches acessíveis da branch base.

### 3.2 Segurança dos workflows

- declarar `permissions: { contents: read }` como base e ampliar por job;
- conceder `packages: write`, `security-events: write`, `attestations: write` ou `id-token: write` apenas onde necessário;
- fixar actions externas pelo SHA completo do commit;
- manter o Dependabot atualizando as referências das actions;
- evitar interpolar diretamente dados de issue, PR ou branch em scripts de shell;
- evitar `pull_request_target` executando código do pull request;
- não disponibilizar secrets em workflows de código não confiável;
- usar runners efêmeros e separar runners de build dos de deploy;
- definir timeout para jobs e limites de concorrência;
- não registrar conteúdo de notas, tokens ou connection strings nos logs.

Self-hosted runners não devem processar pull requests não confiáveis quando possuírem estado persistente, credenciais, acesso à rede interna ou ao daemon de containers do host.

## 4. Proteção da branch principal

Deve existir um ruleset para `main` com, no mínimo:

- pull request obrigatório;
- push direto e force push bloqueados;
- ao menos uma aprovação;
- aprovação anterior descartada após novos commits relevantes;
- resolução obrigatória de conversas;
- revisão de Code Owner quando houver equipe suficiente;
- checks de CI e segurança obrigatórios;
- histórico linear;
- exclusão da branch bloqueada;
- bypass restrito e auditável;
- merge queue quando a concorrência de contribuições justificar.

Checks candidatos a obrigatórios:

```text
CI / Frontend
CI / Backend
CI / Integration
CI / Contracts
CI / E2E
SonarQube Code Analysis
CodeQL
Dependency Review
Container Security
CI / Result
```

Os nomes efetivos devem permanecer estáveis. Um check marcado como obrigatório não pode desaparecer por causa de filtro de path. O job agregador `CI / Result` deve sempre executar e informar claramente quais validações foram executadas, ignoradas de forma válida ou falharam.

### 4.1 CODEOWNERS

O arquivo `.github/CODEOWNERS` pode atribuir responsáveis a áreas como:

```text
/.github/       @lzocateli
/frontend/web/      @lzocateli
/backend/apps/api/      @lzocateli
/backend/apps/worker/   @lzocateli
/libs/          @lzocateli
/contracts/     @lzocateli
/deploy/        @lzocateli
```

O próprio arquivo e os workflows devem possuir owner explícito. Em uma equipe de uma pessoa, CODEOWNERS ainda documenta responsabilidade, mas não cria independência de aprovação. A exigência de aprovação deve ser ajustada para não bloquear o trabalho individual sem oferecer ganho real.

## 5. Estratégia do Sonar

### 5.1 SonarQube Cloud ou SonarQube Server

| Opção | Vantagens | Custos e limitações |
| --- | --- | --- |
| SonarQube Cloud | SaaS, menor operação e integração direta com GitHub | código e resultados processados pelo serviço; plano conforme visibilidade e volume |
| SonarQube Server | controle de dados, rede e atualização | exige infraestrutura, TLS, backup, atualização, disponibilidade e licença para recursos avançados |

Para a fase inicial, SonarQube Cloud reduz o custo operacional. SonarQube Server é indicado quando houver requisito de manter análise e resultados dentro da infraestrutura controlada.

No SonarQube Server, recursos de branches, pull requests e monorepo dependem da edição. A documentação da SonarSource deve ser conferida antes da compra; o suporte formal de monorepo no Server está associado à edição Enterprise na documentação vigente desta referência.

### 5.2 Integração recomendada

- instalar o GitHub App oficial do Sonar com acesso apenas aos repositórios necessários;
- habilitar análise de pull request e PR decoration;
- tornar o Quality Gate um status check obrigatório;
- usar checkout com histórico completo, `fetch-depth: 0`;
- armazenar `SONAR_TOKEN` em GitHub Actions Secret;
- armazenar `SONAR_HOST_URL` como variable quando usar Server;
- usar token de projeto em vez de token global sempre que possível;
- importar cobertura Angular em LCOV;
- importar cobertura .NET em formato aceito pelo scanner, como OpenCover;
- definir corretamente fontes, testes, código gerado e exclusões justificadas;
- usar análise orientada a código novo, seguindo o princípio **Clean as You Code**.

Não se deve habilitar simultaneamente análise automática e análise pelo CI para o mesmo projeto quando isso gerar resultados duplicados.

### 5.3 Quality Gate inicial

Uma política inicial para código novo pode exigir:

- nenhuma vulnerabilidade nova;
- nenhum bug novo de severidade bloqueadora ou crítica;
- todos os Security Hotspots novos revisados;
- cobertura mínima de 80% no código novo;
- duplicação dentro do limite acordado no código novo;
- rating de confiabilidade, segurança e manutenibilidade compatível com release.

O limite de cobertura deve orientar comportamento, não incentivar testes sem valor. Código de integração com filesystem, PostgreSQL, autenticação e concorrência exige evidência adequada mesmo quando uma métrica global já foi atingida.

### 5.4 Sonar não substitui os demais controles

| Ferramenta | Responsabilidade predominante |
| --- | --- |
| Sonar | qualidade, bugs, duplicação, cobertura, vulnerabilidades e hotspots |
| CodeQL | análise de fluxo de dados e vulnerabilidades em C# e TypeScript |
| Dependabot | vulnerabilidades e atualização de componentes de terceiros |
| Dependency Review | impacto das dependências adicionadas ou alteradas no PR |
| Secret scanning | credenciais e tokens expostos |
| Trivy ou Grype | pacotes do sistema, filesystem e imagens de container |

Achados duplicados devem ser triados, mas a simples sobreposição não justifica desativar uma camada com cobertura diferente.

## 6. Segurança nativa do GitHub

### 6.1 Dependências

Ativar:

- Dependency Graph;
- Dependabot Alerts;
- Dependabot Security Updates;
- Dependabot Version Updates;
- atualizações agrupadas por ecossistema quando reduzirem ruído;
- Dependency Review em pull requests.

O `.github/dependabot.yml` deve cobrir npm, NuGet, GitHub Actions e Docker. Atualizações de segurança continuam passando pelos mesmos builds e testes das demais mudanças. Automerge só deve ser considerado para categorias de baixo risco e depois de checks completos.

Toda dependência nova ou atualizada deve ter a licença da versão exata verificada na fonte oficial e registrada no pull request. O gate de Dependency Review deve bloquear licenças desconhecidas ou incompatíveis, e o inventário de release deve preservar essa informação no SBOM.

Aceitar somente licenças permissivas e gratuitas para os usos previstos, como MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC e PostgreSQL. Bloquear dependências pagas, proprietárias, source-available, com licença comercial obrigatória, copyleft ou restrição incompatível com uso, modificação, distribuição e operação self-hosted. Licença ausente, ambígua, não detectada ou modificada entre versões exige bloqueio, não aprovação automática.

A biblioteca MediatR é proibida no projeto por sua política de licenciamento; essa restrição não se aplica ao padrão Mediator. Quando o padrão for necessário, a implementação aprovada é `Nuuvify.CommonPack.Mediator`, cuja versão exata ainda deve passar por verificação de licença, vulnerabilidades, manutenção, lockfile, Dependency Review e SBOM. Commands, queries, handlers e behaviors permanecem explícitos e testáveis.

### 6.2 Code scanning com CodeQL

CodeQL suporta as linguagens principais do Altivy:

- C# para API, worker e bibliotecas backend;
- JavaScript/TypeScript para Angular e bibliotecas frontend;
- workflows GitHub Actions para problemas na automação.

A configuração inicial pode usar default setup. A configuração avançada é indicada quando forem necessários query packs adicionais, controle de build compilado, agenda específica ou integração com outros resultados SARIF.

Resultados de scanners compatíveis, como Trivy, podem ser enviados em SARIF para a área de Code Scanning. Isso centraliza triagem e histórico, mas não elimina a necessidade de manter o relatório original como artifact quando útil.

### 6.3 Secret scanning e push protection

Secret scanning procura credenciais no histórico Git e em outras superfícies do GitHub. Push protection bloqueia credenciais reconhecidas antes que entrem no repositório.

Ao detectar um secret:

1. revogar ou rotacionar imediatamente;
2. investigar utilização indevida;
3. substituir o mecanismo por secret seguro ou OIDC;
4. remover do histórico somente quando necessário;
5. documentar o incidente sem repetir o valor sensível.

Apagar o texto do commit não torna uma credencial novamente segura. A rotação é obrigatória.

### 6.4 Security Advisories e reporte privado

Para um projeto público, habilitar **Private Vulnerability Reporting** permite receber relatos sem exposição antecipada. GitHub Security Advisories possibilita discutir, corrigir e preparar uma release de segurança de forma privada antes da divulgação coordenada.

Private Vulnerability Reporting é um recurso da plataforma GitHub, não um arquivo adicional do repositório. Quando habilitado, o pesquisador utiliza a aba **Security**, entra em **Advisories** e seleciona **Report a vulnerability** para enviar o relato em um canal privado.

Para habilitá-lo posteriormente, um administrador do repositório deve:

1. abrir **Settings** no repositório GitHub;
2. acessar **Security** ou **Code security and analysis**;
3. localizar **Private vulnerability reporting**;
4. selecionar **Enable**;
5. confirmar na aba **Security** que a opção de reporte privado está disponível.

Os nomes e a organização dos menus podem mudar conforme a evolução da interface do GitHub. Se o recurso não estiver disponível, verificar a visibilidade do repositório, o plano da conta e a documentação oficial antes de publicar um canal alternativo.

A política pública de reporte permanece no arquivo [`.github/SECURITY.md`](../.github/SECURITY.md). Esse documento orienta o pesquisador sobre versões suportadas, escopo e informações esperadas; o Private Vulnerability Reporting é o canal que recebe e gerencia o relato de forma privada.

**Ação pendente:** habilitar o Private Vulnerability Reporting nas configurações do repositório e registrar a evidência no checklist administrativo do mantenedor antes de divulgar a política de segurança como canal oficial.

## 7. Secrets, variables e identidade

Use o menor escopo possível:

| Escopo | Uso recomendado |
| --- | --- |
| Repository variable | URL, nome de projeto e configuração não sensível |
| Repository secret | token necessário em CI para todo o repositório, como `SONAR_TOKEN` |
| Environment variable | configuração não sensível específica de staging ou production |
| Environment secret | credencial de implantação liberada somente após as proteções do ambiente |
| Organization secret | valor compartilhado com política explícita de repositórios autorizados |

Secrets não são enviados normalmente para workflows originados de forks e não são repassados automaticamente a reusable workflows. O contrato de inputs e secrets deve ser explícito.

### 7.1 OIDC

Quando o provedor de infraestrutura oferecer suporte, GitHub Actions deve usar OpenID Connect para trocar a identidade do workflow por credenciais temporárias. Isso evita armazenar service principals, chaves ou tokens de longa duração.

Uma política OIDC deve restringir:

- organização e repositório;
- branch, tag ou environment permitido;
- workflow autorizado;
- audiência esperada;
- permissões concedidas no provedor.

O job de deploy recebe `id-token: write`; os jobs de build não recebem essa permissão.

## 8. Containers e cadeia de fornecimento

As imagens podem ser publicadas no GitHub Container Registry:

```text
ghcr.io/lzocateli/altivy-web:1.0.0
ghcr.io/lzocateli/altivy-api:1.0.0
ghcr.io/lzocateli/altivy-worker:1.0.0
```

Cada release deve:

- construir com BuildKit e contexto mínimo;
- utilizar imagens base fixadas e atualizadas de forma controlada;
- executar smoke test e health check;
- inspecionar usuário, entrypoint, portas, volumes e labels OCI;
- gerar SBOM em SPDX ou CycloneDX;
- executar scanner como Trivy ou Grype;
- publicar resultado SARIF quando aplicável;
- gerar artifact attestation e proveniência;
- publicar tag semântica imutável e registrar o digest SHA-256;
- criar GitHub Release com changelog e referências aos artefatos;
- promover exatamente o mesmo digest entre ambientes, sem rebuild.

`latest` pode existir como conveniência, mas nunca deve ser a única referência nem a referência de produção. O deploy deve usar versão imutável ou digest.

## 9. Environments e implantação

Criar environments conforme a topologia real:

| Environment | Proteção sugerida |
| --- | --- |
| `development` | deploy automático opcional após merge em `main` |
| `staging` | checks completos e smoke test após implantação |
| `production` | tag protegida, aprovação, sem autoaprovação e secrets próprios |

Para `production`:

- permitir somente tags de release ou workflow manual autorizado;
- exigir revisor quando houver uma segunda pessoa responsável;
- impedir autoaprovação quando o plano suportar e houver equipe;
- desabilitar bypass das proteções, salvo procedimento de emergência auditado;
- usar secrets exclusivos do ambiente;
- definir `concurrency` para impedir deploys simultâneos;
- registrar URL e histórico do deployment;
- executar verificação pós-deploy;
- manter rollback para o digest anterior.

Environments de repositórios privados e algumas deployment protection rules dependem do plano GitHub.

## 10. Pipeline recomendado por evento

### 10.1 Pull request

Executar:

1. validação de arquivos e lockfiles;
2. format e lint;
3. build Angular e .NET;
4. testes unitários;
5. testes de integração com PostgreSQL e filesystem reais;
6. validação de contratos OpenAPI;
7. Playwright para fluxos afetados;
8. análise Sonar e Quality Gate;
9. CodeQL;
10. Dependency Review;
11. build e scan de imagens quando arquivos de container forem afetados;
12. check agregador.

O pull request não publica release nem recebe credenciais de produção.

### 10.2 Push em `main`

Após repetir as validações necessárias:

- construir artefatos reproduzíveis;
- construir imagens candidatas uma única vez;
- gerar SBOM, scan e proveniência;
- publicar no GHCR com tag de commit;
- opcionalmente implantar em `development`;
- preservar relatórios e digests para promoção posterior.

### 10.3 Tag e release

Uma tag no padrão `vX.Y.Z` deve:

- apontar para commit aprovado de `main`;
- confirmar todos os gates;
- associar tags semânticas às imagens já construídas ou reconstruí-las de forma verificável conforme política definida;
- gerar GitHub Release e changelog;
- publicar SBOM e proveniência;
- promover por digest para staging e production;
- respeitar aprovação do environment.

A estratégia preferida é **build once, promote many**: o mesmo digest validado é promovido, sem recompilar para cada ambiente.

### 10.4 Execuções agendadas

Executar periodicamente:

- CodeQL completo;
- varredura das imagens publicadas;
- revisão de dependências e imagens base;
- testes de backup e restauração;
- smoke tests de ambientes;
- verificação de certificados e secrets com expiração;
- auditoria de actions desatualizadas e permissões excessivas.

## 11. Disponibilidade por plano

A disponibilidade exata deve ser confirmada no plano contratado e na visibilidade do repositório.

- GitHub Actions possui cotas de minutos, armazenamento, cache e artifacts.
- Repositórios públicos recebem gratuitamente vários recursos de segurança.
- Em repositórios privados, code scanning avançado e proteções de secrets podem exigir GitHub Code Security, GitHub Secret Protection, Team ou Enterprise.
- Environments em repositórios privados exigem GitHub Pro para conta pessoal ou GitHub Team para organização; algumas regras possuem restrições adicionais.
- Secrets de organização para repositórios privados não estão disponíveis no GitHub Free.
- Rulesets em múltiplos repositórios e controles organizacionais avançados dependem de Team ou Enterprise.
- SonarQube Cloud possui planos próprios.
- SonarQube Server possui edições com recursos distintos para branches, PRs e monorepos.

Licenciamento muda ao longo do tempo. Antes de implementar um controle como gate obrigatório, confirmar que ele está disponível para o proprietário e a visibilidade atuais do repositório.

## 12. Ordem de implantação

### Fase 1: governança e CI básico

- criar workflows de frontend, backend, testes e contratos;
- configurar ruleset de `main`;
- criar CODEOWNERS;
- limitar permissões do `GITHUB_TOKEN`;
- fixar actions por SHA;
- configurar cache e artifacts sem dados sensíveis.

### Fase 2: qualidade e segurança do código

- integrar Sonar e tornar o Quality Gate obrigatório;
- habilitar Dependency Graph, Dependabot e Dependency Review;
- habilitar CodeQL;
- habilitar secret scanning e push protection quando disponíveis;
- definir processo de triagem e prazo de correção.

### Fase 3: containers e releases

- criar build e smoke test das imagens;
- adicionar scanner de container e SARIF;
- publicar no GHCR;
- gerar SBOM e artifact attestations;
- criar release versionada e tags imutáveis.

### Fase 4: implantação controlada

- criar environments;
- configurar OIDC;
- separar permissões de build e deploy;
- adicionar aprovações de production;
- implementar pós-deploy, rollback e histórico de deployment;
- testar recuperação antes do primeiro release estável.

## 13. Critérios mínimos de aceite

O processo estará adequado para o primeiro release quando:

- `main` não aceitar mudanças sem pull request e checks obrigatórios;
- builds Angular e .NET forem reproduzíveis a partir dos lockfiles;
- testes unitários, integração, contrato e E2E produzirem evidências;
- o Quality Gate do Sonar bloquear código novo fora da política;
- CodeQL, dependências e secrets tiverem controles ativos ou limitação documentada;
- imagens forem testadas, escaneadas, identificadas por digest e acompanhadas por SBOM;
- credenciais de longa duração tiverem sido eliminadas do deploy quando OIDC estiver disponível;
- production exigir as proteções compatíveis com o tamanho da equipe e o plano GitHub;
- backup, restauração e rollback tiverem sido executados em ambiente controlado;
- exceções possuírem responsável, justificativa e prazo de revisão.

## 14. Referências oficiais

- [GitHub Actions](https://docs.github.com/en/actions)
- [Rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets)
- [CODEOWNERS](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
- [Environments](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments)
- [Segurança no GitHub Actions](https://docs.github.com/en/actions/reference/security/secure-use)
- [CodeQL](https://docs.github.com/en/code-security/code-scanning/introduction-to-code-scanning/about-code-scanning-with-codeql)
- [Dependabot Security Updates](https://docs.github.com/en/code-security/dependabot/dependabot-security-updates/about-dependabot-security-updates)
- [Secret scanning](https://docs.github.com/en/code-security/secret-scanning/introduction/about-secret-scanning)
- [Artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations)
- [SonarQube Cloud com GitHub](https://docs.sonarsource.com/sonarqube-cloud/getting-started/github/)
- [SonarQube Server com GitHub Actions](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/ci-integration/github-actions/)

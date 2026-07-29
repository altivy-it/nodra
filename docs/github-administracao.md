# Checklist administrativo do GitHub

**Status:** pendente de execução por administrador  
**Data de referência:** 28 de julho de 2026  
**Escopo:** configurações do GitHub que não são reconstruídas apenas pelos arquivos versionados.

## 1. Como usar

Execute este checklist no repositório GitHub depois que os respectivos workflows e projetos existirem. Para cada item concluído, registre responsável, data e uma referência auditável, como URL da configuração, captura sanitizada ou issue administrativa.

Não registre tokens, secrets, connection strings ou conteúdo de notas como evidência. Recursos dependentes de plano devem permanecer desmarcados até sua disponibilidade ser confirmada.

## 2. Governança

- [ ] Criar ruleset para `main` com pull request obrigatório, bloqueio de push direto, force push e exclusão.
- [ ] Exigir resolução de conversas e descartar aprovações após novos commits relevantes.
- [ ] Exigir revisão de Code Owner quando houver equipe suficiente para revisão independente.
- [ ] Exigir histórico linear e restringir bypass a responsáveis auditáveis.
- [ ] Adicionar `CI / Result` aos checks obrigatórios depois de sua primeira execução bem-sucedida.
- [ ] Proteger tags `v*` antes da primeira release.
- [ ] Avaliar merge queue quando houver concorrência de contribuições.

## 3. Actions e identidade

- [ ] Definir `GITHUB_TOKEN` como somente leitura por padrão.
- [ ] Permitir apenas actions necessárias e exigir referências por SHA quando a política da organização suportar.
- [ ] Configurar retenção de logs, artifacts e caches conforme a política operacional.
- [ ] Configurar limite de gastos e alertas de consumo do GitHub Actions.
- [ ] Confirmar que runners self-hosted, se adotados, não executam código não confiável com estado ou credenciais persistentes.
- [ ] Configurar OIDC por environment antes de qualquer deploy, restringindo repositório, ref, workflow e audiência.

## 4. Segurança do código

- [ ] Habilitar Dependency Graph.
- [ ] Habilitar Dependabot Alerts e Dependabot Security Updates.
- [ ] Confirmar execução do version update de GitHub Actions definido em `.github/dependabot.yml`.
- [ ] Habilitar Dependency Review quando o plano e os manifests permitirem.
- [ ] Habilitar CodeQL para C# e TypeScript depois da criação dos projetos executáveis.
- [ ] Habilitar secret scanning e push protection quando disponíveis.
- [ ] Habilitar Private Vulnerability Reporting e confirmar o botão **Report a vulnerability** na aba **Security**.
- [ ] Definir responsáveis, severidades e prazos para triagem de alertas.

## 5. Sonar

- [ ] Escolher SonarQube Cloud ou SonarQube Server e confirmar suporte a pull requests e monorepo.
- [ ] Instalar o GitHub App somente para o repositório necessário.
- [ ] Criar token de projeto e armazenar `SONAR_TOKEN` como repository secret.
- [ ] Configurar `SONAR_HOST_URL` como variable quando for usado SonarQube Server.
- [ ] Importar cobertura LCOV do Angular e OpenCover do .NET.
- [ ] Ativar Quality Gate orientado a código novo e torná-lo obrigatório após uma execução válida.
- [ ] Evitar análise automática e análise via CI simultâneas para o mesmo projeto.

## 6. Packages, releases e ambientes

- [ ] Confirmar acesso e política de retenção do GitHub Container Registry.
- [ ] Criar environments somente quando houver alvo real: `development`, `staging` e `production`.
- [ ] Restringir `production` a tags protegidas ou workflow manual autorizado.
- [ ] Configurar revisores de `production` quando houver uma segunda pessoa responsável.
- [ ] Usar secrets exclusivos por environment e impedir bypass fora do procedimento de emergência.
- [ ] Confirmar que release publica versão imutável, digest, SBOM e proveniência antes de promover o mesmo digest.
- [ ] Registrar URL, resultado pós-deploy e digest anterior para rollback em cada deployment.

## 7. Evidência administrativa

| Controle | Responsável | Data | Evidência ou issue | Próxima revisão |
| --- | --- | --- | --- | --- |
| Ruleset de `main` | pendente | pendente | pendente | pendente |
| Segurança do código | pendente | pendente | pendente | pendente |
| Private Vulnerability Reporting | pendente | pendente | pendente | pendente |
| Sonar Quality Gate | pendente | pendente | pendente | pendente |
| Environments e OIDC | pendente | pendente | pendente | pendente |
| GHCR e releases | pendente | pendente | pendente | pendente |

## 8. Bloqueios atuais

Na data de referência, o repositório contém o plano e a documentação, mas ainda não possui projetos Angular ou .NET, contracts, lockfiles, Dockerfiles ou manifests de deploy. Por isso, ainda não há alvos válidos para build, testes, CodeQL compilado, Dependency Review de npm/NuGet, containers, release ou deploy.

Esses gates devem ser adicionados junto com o primeiro projeto de cada ecossistema. Um check só deve se tornar obrigatório depois de existir em todos os pull requests relevantes e ter executado com sucesso; filtros de path não podem deixá-lo permanentemente pendente.
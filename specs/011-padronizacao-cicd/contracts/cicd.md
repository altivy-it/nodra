# Contratos de CI/CD

## Fontes e Permissoes

- Catalogo: Groundwork 0.11.3, MIT, commit c848afb4fa0b43cb9227abd2c347ab4ecccd6296.
- Referencia funcional: Dokpod 01fe5b0c526742edb420541cdd9a0b4e9fa2e74e; nenhum codigo AGPL sera copiado.
- CI: pull_request, merge_group, push main e workflow_dispatch, sem secrets de release/deploy e sem publicacao.
- Result agrega todos os jobs obrigatorios; nao ignorar skipped/cancelled/failure indevidos.
- Publicacao/deploy: permissoes minimas por job, workflow confiavel e ambientes protegidos; nao usar pull_request_target para executar codigo nao confiavel com secrets.

## Seeds do Produto

| Arquivo | Entradas/resultado |
| --- | --- |
| ci.yml | Gates Nodra e Result; cada gate participa de needs |
| publish.yml | workflow_call: tag, version, republish, preflight, publish_only; artefatos release-entries-* com kind/name/reference/digest |
| post-release.yml | workflow_call: tag/version/republish; nenhuma distribuicao extra automatica nao solicitada |
| deploy.yml | workflow_call: version/environment/manifest-artifact; aplicar e conferir somente manifesto validado |
| ci-cd.json | Solucao Nodra.slnx, imagens existentes, versionFiles backend/frontend, preview quality, deploy environments quality/production, sem auto-deploy |

Modos nao implementados devem falhar explicitamente. Imagens alvo:
ghcr.io/altivy-it/nodra-api, nodra-bff, nodra-worker, nodra-web e nodra-migrations.
Nao incluir pacote NuGet, agente Windows ou capacidade de licenciamento do Dokpod.

## Ambientes e Protecoes

| Nome | Revisores | Origens |
| --- | --- | --- |
| ci | nenhum | Conforme eventos do CI; sem secrets de producao |
| copilot | nenhum | Automacao reservada, sem secrets de deploy |
| quality | Administradores da Altivy | tags v* |
| release | Administradores da Altivy | main, tags v* |
| distribution | Administradores da Altivy | main, tags v* |
| production | Administradores da Altivy | tags v* |

prevent_self_review=false como na referencia. Revisores indisponiveis no plano
GitHub ou falta de permissao sao bloqueios, nao paridade presumida.
Regras legadas nao sao removidas sem revisao e autorizacao especifica do diff.

O papel administrativo usado pelo Dokpod nos rulesets e RepositoryRole com
actor_id 5. Required reviewers aceitam apenas IDs de usuarios/equipes, e
CODEOWNERS exige identificadores GitHub validos com permissao adequada.
Identificar a equipe administrativa real antes de aplica-los; se ausente,
bloquear configuracao em vez de usar conta pessoal ou remover a aprovacao.

## Autoria Institucional

Autor e committer das contribuicoes desta manutencao e da automacao configurada:
lunadev00 <luna@altivy.it>. Nao confundir autoria com permissao de aprovador.
Nao reescrever historico nem atribuir contribuicoes de terceiros a essa conta.
Assinatura GPG/SSH exige chave autorizada separadamente; email sozinho nao e
prova de assinatura criptografica.

## Deploy e Compatibilidade

DEPLOY_HOOK e um contrato do host com comandos apply/check que consomem o
manifesto validado e verificam a versao. A semente deve recusar configuracao
ausente. Nao instalar hook, mudar DNS, copiar secrets ou executar deploy por
efeito colateral. O desenho do adaptador para o Compose Nodra e revisado na
ADR; nao reutilizar o host/hook Dokpod implicitamente.

Paths externos, variaveis ALTIVY_* do runtime, AltivyPostgres, schema altivy,
realm/clientes Keycloak e key ring permanecem. secrets_dir_name do catalogo
deve ser Altivy.Notes para nao inventar um novo caminho externo de secrets.
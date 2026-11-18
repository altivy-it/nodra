# Deploy do Nodra

Semente do Altivy.Groundwork (componente `deploy`): este arquivo pertence ao projeto e
complementa o procedimento padrão de [deploy-e-rollback.md](deploy-e-rollback.md).
Contrato do Nodra aceito no ADR 0009. Configuração e homologação reais continuam
pendentes; nenhum deploy foi executado nesta implementação.

## Destinos

| Environment | Destino | Runner | Revisores |
| --- | --- | --- | --- |
| `quality` | Host Nodra a configurar pelo operador | GitHub-hosted com SSH | Administração da Altivy, equipe validada |
| `production` | Host Nodra a configurar pelo operador | GitHub-hosted com SSH | Administração da Altivy, equipe validada |

## Execução

O operador aciona gw-deploy.yml somente na tag de uma release publicada. O guard
canônico valida origem, manifesto, atestação e ambiente. deploy.yml recebe esse
artifact, valida o destino/hook e usa SSH com BatchMode e host key obrigatória.
O workflow envia manifesto e token efêmero por stdin para um diretório remoto
restrito, chama `DEPLOY_HOOK apply` e remove o diretório e a chave temporária.
Nenhuma imagem ou digest livre é aceito como entrada do workflow.

O hook é um contrato externo e **não está instalado nem homologado** por esta
feature. Deve implementar:

- `apply <manifesto> <arquivo-token> <actor>`: consumir somente o manifesto
	validado, conferir repositório/digests, gerar images.env em arquivo novo,
	executar preflight/backup/migrations e aplicar as mesmas imagens de forma
	idempotente com Compose, sem mover secrets ou dados.
- `check <manifesto> <versão>`: conferir digests, versão, usuário e saúde dos
	containers e usar apenas a borda nginx para verificações HTTP.

`tools/scripts/convert-release-manifest.ps1` produz apenas as cinco referências
OCI permitidas em `images.env`. Não verifica criptograficamente a atestação e
nunca substitui o guard de origem. O destino deve ser novo; não sobrescreve
configuração existente. Configure o hook real somente após revisão e testes
de upgrade/recuperação, mantendo publicação/deploy bloqueados até então.

Secrets por Environment: DEPLOY_SSH_KEY e DEPLOY_SSH_KNOWN_HOSTS. Variáveis:
DEPLOY_SSH_TARGET e DEPLOY_HOOK. Não enviar valores por chat nem armazená-los
no repositório. Ver [configuração GitHub](../ci-cd-seguranca.md).

## Migrações e compatibilidade

Esta feature não altera SQL, schema, vaults, realm ou key ring. O namespace novo
é ghcr.io/altivy-it/nodra-*; imagens já instaladas não são movidas ou substituídas
automaticamente. rollbackMinimumVersion permanece 0.0.0; uma mudança contract
futura exige revisão e atualização explícita desse limite.

## Smoke test e verificações após o deploy

O job smoke usa o mesmo Environment protegido e chama o hook check com o
manifesto validado e a versão esperada. Ele não comprova login/logout OIDC,
upgrade ou recuperação se o hook/laboratório não tiver sido qualificado.
T025/T031 da feature 010 e os gates remotos da feature 011 permanecem rastreados.

## Contatos

Administração da Altivy. Autoria das contribuições desta manutenção:
lunadev00 <luna@altivy.it>. Não usar uma conta pessoal como fallback de aprovador.

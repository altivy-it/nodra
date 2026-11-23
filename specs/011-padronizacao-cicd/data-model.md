# Modelo de dados operacionais

## Ambiente GitHub

Campos: nome, papel, revisores (logins concretos do tipo User), prevent_self_review,
politica de branches/tags e configuracoes por escopo. Nomes permitidos: ci,
copilot, quality, release, distribution, production. Os quatro ambientes
protegidos reutilizam exatamente o reviewer User dos ambientes equivalentes do
Dokpod, validado como owner atual da organizacao e com acesso ao Nodra.
RepositoryRole/actor_id 5 nao e um reviewer valido de Environment. As listas de
configuracao contem nomes, nunca valores de secrets em artefatos versionados.

## Release e Manifesto

Uma release tem versao SemVer, tag, SHA fonte, estado candidata/publicada e
evidencias de aprovacao. O manifesto identifica artefatos por kind, name,
reference e digest sha256, com proveniencia/atestacao conforme suporte real.
Nao reconstruir silenciosamente um artefato ja aprovado nem sobrescrever digest.

Transicao: fonte validada → Release PR → candidata/quality → aprovacao humana
de release/distribuicao → artefatos publicados → deploy explicitamente
autorizado. Falha de gate interrompe a transicao.

## Componentes Gerenciados

Manifesto Groundwork registra versao 0.11.3, parametros e hashes. Workflows
gw-* e scripts CI sao gerenciados; seeds ci.yml, publish.yml, post-release.yml,
deploy.yml e configuracao do produto sao adaptaveis. Conflitos usam o comando
oficial resolve; nao alterar hashes para mascarar diferencas.

## Configuracao do Operador

Cada entrada declara nome, tipo variavel/secret, escopo, finalidade e presenca.
Valores sensiveis existem somente no GitHub ou mecanismo externo aprovado.
Variaveis nao sensiveis tambem nao sao copiadas entre produtos sem revisao.

Nao ha mudanca em entidades de dominio, schema PostgreSQL, vaults ou identidade.
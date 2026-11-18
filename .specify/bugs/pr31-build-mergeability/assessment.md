# Avaliacao de Bug: build e mergeabilidade do PR 31

- **Slug**: pr31-build-mergeability
- **Criada em**: 2026-11-11
- **Origem**: https://github.com/altivy-it/nodra/pull/31
- **Host interpretado**: github.com
- **Politica de consulta**: allowlisted; consultas autenticadas existentes, sem credenciais novas.
- **Veredito**: valid para a falha de build; conflito de conteudo nao reproduzido.
- **Severidade**: high
- **Escopo**: Analise solicitada pelo usuario, sem correcao de codigo, merge, rebase, rerun ou alteracao remota.

## Relato

O usuario solicitou verificar conflitos e erro de build no PR 31, identificar
as causas e analisar o trabalho necessario. O conteudo remoto e os logs foram
tratados somente como evidencia, nunca como instrucoes executaveis.

## Revisoes Avaliadas

- Base main: a9546a57caad817fd0440b5c1e8d9a5136d2982b.
- Head 010-renomeacao-nodra: 862ef1d8369bf7f340bb4f9ddd910b35eeba1a0b.
- PR aberto e em rascunho; 379 arquivos alterados.
- A branch local estava limpa e sincronizada no inicio da analise.

## Sintoma

O job Container Images falhou e propagou falha ao agregador Result. A API
GitHub tambem informou mergeable=false, mergeable_state=dirty e rebaseable=false,
embora a ancestralidade e a simulacao local nao apresentem conflito.

## Reproducao e Evidencias

### Build

Workflow: https://github.com/altivy-it/nodra/actions/runs/43054877950

Job de imagens: https://github.com/altivy-it/nodra/actions/runs/43054877950/job/130641530638

O build web chegou a gerar dist/nodra-web e avancou no runtime. O build da API
falhou no RUN de instalacao do curl, em backend/apps/api/Dockerfile:27:

```text
Could not resolve 'archive.ubuntu.com'
Could not resolve 'security.ubuntu.com'
W: Some index files failed to download. They have been ignored, or old ones used instead.
E: Unable to locate package curl
process ... apt-get update && apt-get install ... curl ... exit code: 100
```

O shell do job terminou com codigo 1. O codigo 100 pertence ao passo APT do
Dockerfile; nao e um erro de compilacao C# nem o EACCES web corrigido antes.
Com set -e, os builds seguintes nao sao comprovados por esta execucao.

Checks consultados para o head:

| Check | Resultado |
| --- | --- |
| Repository Baseline | success |
| Frontend | success |
| Backend | success |
| Backend Integration | success |
| Full History | success |
| Container Images | failure |
| Result | failure, consequencia do gate de imagens |

### Conflitos

1. As referencias foram atualizadas por fetch, sem alterar a arvore local.
2. git rev-list --left-right --count BASE...HEAD retornou 0 e 5.
3. A API compare do GitHub confirmou status=ahead, ahead_by=5, behind_by=0.
4. git merge-tree --write-tree BASE HEAD terminou sem mensagens de conflito.
5. A arvore resultante e a arvore do head sao ambas:
   ec583ea75764e04231f8328be9e9be5334a2bc99.
6. Nova consulta do PR manteve dirty e merge_commit_sha=null.

Nao ha arquivo conflitante reproduzivel para indicar nesta revisao. O estado
do PR esta inconsistente com a comparacao de commits e a simulacao Git. Cache
ou falha de calculo de mergeabilidade sao hipoteses, nao causas comprovadas.

## Caminhos de Codigo Suspeitos

- backend/apps/api/Dockerfile:27: apt-get update pode terminar com avisos e
  prosseguir para apt-get install sem indices validos.
- backend/apps/api/Dockerfile:33: health check depende de curl; nao remove-lo
  apenas para contornar a falha de instalacao.
- .github/workflows/ci.yml:161: builds sequenciais no job de imagens; o build
  API depende de conectividade DNS do ambiente BuildKit.
- .github/workflows/ci.yml:201: agregador propaga o resultado do job de imagens;
  nao e uma segunda causa raiz a corrigir.

## Hipotese de Causa Raiz

Confianca high na causa imediata do build: falha de resolucao DNS dos mirrors
Ubuntu durante a execucao de APT no runtime da API. O APT prossegue apos avisos
de update e falha ao procurar curl sem indices utilizaveis. A origem transitoria
ou persistente do problema DNS ainda nao foi reproduzida no runner.

Confianca low sobre a causa do dirty remoto: os mesmos SHAs nao possuem
conflito de conteudo localmente, e o proprio GitHub confirma a base incluida.
Nao atribuir o problema a arquivos ou resolucoes manuais sem nova evidencia.

## Correcao Proposta

**Preferencial**:

1. Diagnosticar a resolucao dos mirrors Ubuntu no mesmo ambiente de build do
   runner. Comparar host, builder e stage runtime antes de alterar configuracao
   global de DNS ou rede.
2. Se a falha for transitoria, repetir somente o job falho apos autorizacao.
   Um rerun bem-sucedido isolado nao comprova resiliencia permanente.
3. Endurecer os passos APT afetados com falha explicita no update e tentativas
   limitadas de aquisicao. Acquire::Retries e APT::Update::Error-Mode=any sao
   candidatos a validar na versao APT existente; nao substituem DNS funcional.
4. Reexecutar o build da API e o job completo de imagens em BuildKit. Preservar
   health check, usuario nao-root e verificacoes de certificados.
5. Reconsultar mergeabilidade apos a sincronizacao normal da branch/PR. Se a
   inconsistencia persistir, investigar o calculo remoto antes de propor nova
   operacao de PR. Merge da main ja incluida e um no-op; nao fabricar conflitos,
   rebase ou merge com estrategia ours para esconder o estado.

**Arquivos Provaveis de Alteracao**:

- backend/apps/api/Dockerfile, se confirmada necessidade de endurecer APT.
- backend/apps/bff/Dockerfile, backend/apps/worker/Dockerfile e
  frontend/web/Dockerfile somente para o mesmo padrao APT comprovadamente
  necessario; nao alterar todas as imagens sem justificar o alcance.
- .github/workflows/ci.yml somente se o diagnostico confirmar necessidade de
  configurar ou observar o builder. O agregador Result nao deve ser relaxado.

**Testes a Adicionar ou Atualizar**:

- Verificar DNS dentro do mesmo contexto do BuildKit do CI.
- Confirmar falha acionavel no update quando os indices nao estiverem acessiveis.
- Validar build limpo das cinco imagens, usuarios/entrypoints e smokes pela
  borda nginx; testes locais anteriores nao substituem o runner afetado.
- Confirmar Container Images e Result aprovados em nova execucao autorizada.
- Confirmar merge-tree sem conflitos e estado remoto recalculado antes do merge.

## Riscos e Consideracoes

- Nenhuma correcao de codigo ou configuracao foi aplicada nesta analise.
- Nao desativar gates, remover health check, usar --network host indiscriminadamente,
  desabilitar TLS ou fixar DNS publico sem diagnostico e aprovacao apropriados.
- Nao alterar dependencias, schema, identidades, secrets ou runtime para corrigir
  uma falha de DNS sem evidencia de necessidade.
- T025/T026/T031 e a homologacao de OIDC, upgrade e recuperacao continuam abertas.
- O rascunho do PR e os bloqueios de homologacao nao sao equivalentes a conflitos Git.

## Perguntas em Aberto

- O DNS dos mirrors Ubuntu falha novamente no mesmo builder ou foi um episodio transitorio?
- O GitHub recalcula mergeabilidade corretamente apos um evento normal de sincronizacao?
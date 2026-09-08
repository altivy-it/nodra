# ADR 2026-0004: Catálogo autorizado de vaults

**Status:** accepted
**Data:** 2026-09-04  
**Responsáveis:** equipe Altivy  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Plano de alternância segura entre vaults](../plan/alternancia-vaults.md), [ADR de BFF e autorização externa](2026-0002-bff-keycloak-autorizacao-externa.md)

## Contexto

O workspace precisa listar somente os vaults para os quais a pessoa autenticada
possui `vault:read`. O PostgreSQL já contém a projeção dos vaults, mas não é e
não pode se tornar autoridade de memberships ou políticas. O Keycloak
Authorization Services permanece responsável por decidir o acesso, enquanto a
API atua como Policy Enforcement Point antes de revelar metadados.

A API atual autoriza operações sobre um vault conhecido. A alternância exige
enumerar candidatos sem revelar ID, nome ou existência daqueles que forem
negados. Também precisa lidar com revogação, indisponibilidade do Keycloak e
catálogos maiores sem introduzir uma nova fonte de autorização.

## Forças de decisão

- impedir enumeração e exposição de metadados de vaults negados;
- manter Keycloak como única autoridade de autorização;
- preservar PostgreSQL como projeção reconstruível, sem memberships;
- falhar fechado sem apresentar uma lista parcial como resultado confiável;
- limitar carga e latência das avaliações UMA;
- manter a mudança aditiva, reversível e sem migration;
- permitir evolução futura baseada em métricas reais.

## Opções consideradas

### Opção A: Catálogo PostgreSQL filtrado por UMA por vault

A API lê candidatos ordenados no PostgreSQL e avalia `vault:read` no Keycloak
para cada candidato antes de incluí-lo na resposta. As avaliações têm
concorrência limitada e a leitura continua até preencher a página autorizada
ou esgotar o catálogo.

Vantagens: reutiliza contratos e serviços existentes, mantém responsabilidades
separadas e não persiste decisões locais. Desvantagens: produz chamadas N+1 ao
Keycloak e exige tratamento cuidadoso de paginação e falha parcial.

### Opção B: Persistir memberships ou decisões no PostgreSQL

A consulta local relacionaria usuários e vaults sem avaliar o Keycloak a cada
listagem.

Vantagens: menor latência nominal. Desvantagens: duplica a autoridade de
autorização, cria risco de divergência e revogação tardia e contraria o ADR
2026-0002 e as regras invariáveis do produto.

### Opção C: Enumerar permissões em lote pela Protection API

A API consultaria recursos e permissões por uma integração administrativa ou
batch específica do Keycloak.

Vantagens: pode reduzir chamadas em catálogos grandes. Desvantagens: amplia
privilégios e acoplamento, exige novo modelo operacional e ainda não possui
evidência de necessidade ou comportamento de revogação no Altivy.

## Decisão

Adotar a opção A no MVP:

- o PostgreSQL fornece somente candidatos por paginação keyset ordenada por ID;
- a API avalia `VaultAuthorization.ResourceFor(id)` e `vault:read` antes de
  incluir qualquer ID ou nome na resposta;
- o limite padrão é 20 vaults autorizados e o máximo é 100;
- no máximo quatro decisões UMA são executadas simultaneamente;
- o cursor é opaco, versionado e representa a última posição de candidato
  consumida, sem usar offset;
- candidatos negados são omitidos sem indicação de existência ou motivo;
- se uma decisão necessária ficar indisponível, a requisição inteira retorna
  `503 application/problem+json`, sem página parcial;
- o MVP não armazena memberships, decisões ou cache de autorização;
- estratégia batch ou cache exige métricas, revisão deste ADR e nova decisão.

## Consequências

### Positivas

- autorização e revogação continuam sob responsabilidade do Keycloak;
- a infraestrutura de dados permanece neutra em relação a identidade;
- consumidores existentes não são quebrados pelo novo endpoint;
- o fluxo pode ser removido sem migration ou reversão de dados.

### Negativas e trade-offs

- a latência cresce com o número de candidatos e decisões UMA;
- uma página autorizada pode exigir múltiplas leituras internas;
- uma indisponibilidade durante a avaliação invalida todo o resultado;
- o cursor precisa ser tratado como dado não confiável e pode evoluir por versão.

## Segurança e privacidade

A API valida o bearer token antes de consultar ou retornar o catálogo. Somente
decisões positivas liberam ID e nome. Respostas, logs e métricas não incluem
metadados de candidatos negados, tokens, memberships, policies ou scopes além
do requisito público documentado.

Cursores são opacos para clientes, validados integralmente e não carregam nome,
membership ou decisão. Limites e concorrência reduzem abuso do endpoint, mas
rate limiting adicional só será adotado com evidência operacional e desenho
compatível com instalações self-hosted.

## Dados, compatibilidade e migração

`VaultRecord` e o schema atual são reutilizados sem migration. A ordenação por
ID fornece a chave estável para paginação. O endpoint `GET /api/v1/vaults` é
aditivo; endpoints individuais e de notas permanecem disponíveis.

O navegador poderá persistir apenas o último ID técnico por sujeito. Essa
preferência nunca substitui a revalidação contra o catálogo autorizado após
login ou recarga.

## Observabilidade e operação

Devem ser observadas duração da listagem, quantidade de candidatos avaliados,
quantidade de páginas internas e resultados permitido, negado e indisponível,
sem registrar IDs ou nomes negados. Alertas e investigação devem distinguir
falha do PostgreSQL, token inválido e indisponibilidade do Keycloak.

## Validação

- testes de integração comprovam paginação keyset estável no PostgreSQL;
- testes HTTP cobrem autorizados, negados, catálogo vazio e travessia de lotes;
- testes confirmam concorrência máxima de quatro decisões;
- qualquer indisponibilidade necessária retorna `503` sem itens parciais;
- cursor ou limite inválido retorna `400 application/problem+json`;
- payloads e logs não revelam ID ou nome de vault negado;
- revogação remove o vault no carregamento seguinte e aciona fallback seguro;
- métricas do laboratório permitem reavaliar a estratégia N+1.

## Rollout e rollback

O endpoint aditivo é entregue antes do seletor frontend. O frontend só passa a
consumi-lo após contrato, autorização e isolamento terem sido validados. O
rollout é interrompido diante de exposição de metadados, lista parcial em
indisponibilidade ou mistura de estado entre vaults.

O rollback desativa primeiro o consumo no frontend e depois remove o endpoint,
mantendo os endpoints existentes. Não há migration a reverter. Recursos
sintéticos do laboratório são reconciliados de forma idempotente.

## Referências

- [Plano mestre](../../README.md)
- [Plano de alternância segura entre vaults](../plan/alternancia-vaults.md)
- [ADR de BFF e autorização externa](2026-0002-bff-keycloak-autorizacao-externa.md)
- [Implementação do backend](../backend.md)
- [Configuração do Keycloak](../configuracao-keycloak.md)
# Pesquisa: renomeacao para Altivy Nodra

## 1. Empresa, produto e posicionamento

**Decisao**: Empresa Altivy; produto/aplicativo Nodra; marca completa Altivy Nodra; futura CLI nodra. Preservar os literais de tagline e posicionamento da spec.

**Justificativa**: Decisao humana explicita de 2026-11-11. A associacao a node reforca notas conectadas e grafo sem mudar a arquitetura.

**Alternativas consideradas**: Altivy Notes e Altivy AI Notes foram rejeitados pelo usuario. Novas capacidades de IA nao fazem parte da renomeacao.

## 2. Repositorio e historico

**Decisao**: Renomear `altivy-it/AltivyNotes` para `altivy-it/nodra`, sem transferencia de organizacao, clone novo ou exclusao de branches.

**Justificativa**: Usuario confirmou a organizacao atual. A main a9546a5 contem as integracoes anteriores, inclusive historicos antes incorporados por squash. A branch 010 descende dessa base.

**Alternativas consideradas**: `altivy/nodra` exigiria transferencia e foi descartado pela resposta humana. Recriar repositorio perderia contexto operacional e nao atende a preservacao solicitada.

## 3. Nomes de compilacao

**Decisao**: Usar `Nodra.slnx` e prefixo `Nodra` em projetos, namespaces e assemblies hoje prefixados por Altivy, preservando o restante da estrutura. Atualizar testes, referencias e Dockerfiles no mesmo incremento.

**Justificativa**: Os donos sao os csproj e seus consumidores; nao ha necessidade de mover responsabilidades ou trocar frameworks.

**Alternativas consideradas**: Manter todos os nomes internos antigos deixaria o projeto parcialmente renomeado. Alterar nomes de entidades e regras de dominio nao e necessario.

## 4. Persistencia e configuracao externa

**Decisao**: Preservar schema `altivy`, roles/banco existentes, realm/clientes/audience, Data Protection, cookies/headers publicos ja usados e localStorage existente. Preservar as chaves de configuracao `AltivyPostgres` e variaveis `ALTIVY_*` como interface legada durante esta entrega; qualquer alias novo precisa de teste de precedencia e nao pode alterar secrets.

**Justificativa**: Evidencias em `PostgresDbContext.cs`, migrations, appsettings, Compose e servicos de preferencias mostram que estes nomes participam de estado duravel ou integracoes. A troca cega causa perda de acesso mesmo sem apagar bytes.

**Alternativas consideradas**: Renomear schema, realm e clients geraria mudanca arquitetural/operacional incompatível sem necessidade para a marca. Criar fallback de autenticacao entre realms e inadequado: issuer/audience continuam explicitamente configurados, sem enfraquecer validacao.

## 5. Infraestrutura de terceiros e empresa

**Decisao**: Nao alterar AKB, imagens base de lzocateli, dominios/certificados da empresa, CODEOWNERS ou organizacao altivy-it. Nomes de imagens do produto e referencias internas podem mudar; publicar imagens exige solicitacao separada.

**Justificativa**: A marca corporativa continua Altivy, e esses recursos nao se tornam propriedade da feature por conterem esse texto.

**Alternativas consideradas**: Trocar dominios para nomes inventados ou mudar o namespace do registry sem decisao operacional criaria referencias inexistentes.

## 6. Documentos historicos e ferramentas

**Decisao**: Atualizar documentacao vigente, preservando specs anteriores, ADRs historicos e `docs/plan/` congelado como registros. Atualizar links ativos afetados por arquivos renomeados; fornecer referencias de compatibilidade quando um registro historico depender de caminho antigo.

**Justificativa**: Os registros descrevem decisoes e execucoes no tempo. A governanca proibe editar planos congelados como trabalho corrente.

**Alternativas consideradas**: Replace-all sobre todos os arquivos destruiria rastreabilidade e poderia alterar ferramentas oficiais do Spec Kit ou tokens exigidos.

## 7. Validacao

**Decisao**: Reutilizar suites e topologias existentes em containers; registrar o bloqueio Angular separadamente, sem mascarar a falha com flags permissivas.

**Justificativa**: A falha `ERESOLVE` foi reproduzida com manifests da origin/main anterior, antes da feature. Ela impede afirmar testes/build/screenshot aprovados ate remediacao autorizada.

**Alternativas consideradas**: Instalar runtimes locais ou acessar Angular diretamente contraria instrucoes e nao fornece evidencia aceitavel.

## 8. Resultado da implementacao e excecoes observadas

Solucao e projetos foram renomeados para Nodra, preservando camadas e contratos
persistidos. README e plano mestre registram a empresa Altivy, o produto Nodra,
a marca completa e os literais aprovados. Contrato OpenAPI foi renomeado para
`contracts/openapi/nodra-api.yaml` sem mudar endpoints ou schemas publicos.

As quatro skills auxiliares mantem aliases `altivy-notes-*` por compatibilidade
com comandos e registros anteriores; seu conteudo ativo referencia o produto
Nodra. Componentes oficiais do Spec Kit, constituicao ratificada, specs
anteriores, ADRs e planos congelados nao foram reescritos. A marca corporativa
Altivy, domínios, fornecedores de imagens base e infraestrutura AKB permanecem.

Dados e SQL publicados estao intactos. As mudancas C# nas migrations limitam-se
a namespaces; o loader resolve recursos pelo assembly atual e sufixo estavel.
O namespace Compose instalado foi preservado, assim como variaveis ALTIVY_*,
paths externos, schema, identidades e preferências do navegador.

Os conflitos de peers frontend e as dependencias criptograficas backend foram
corrigidos exclusivamente apos autorizacao humana, com avaliacoes, correcoes e
verificacoes em `.specify/bugs/`. Nao constituem atualizacao geral da stack.

A suite PostgreSQL passou 36/36 com UTC explicito em banco sintetico. O primeiro
banco, configurado pela imagem em America/Sao_Paulo, revelou a sensibilidade de
uma fronteira de particao ao timezone; nenhum SQL foi alterado para contornar
esse resultado. O formatador apontou regras de estilo em codigo existente,
registradas como pendencia em vez de limpeza alheia ao rename.

## 9. Atualizacao remota posterior a base

O fetch no repositorio ja renomeado encontrou
`origin/dependabot/npm_and_yarn/frontend/web/angular/router-22.2.1`.
Seu diff altera exclusivamente o router e seus peers declarados no lockfile.
O usuario escolheu integrar o historico e manter 22.1.3, explicitamente, em
vez de atualizar toda a familia Angular ou deixar o historico de fora.
A resolucao nao deve alterar arquivos ja testados. Esta decisao e especifica
ao bump isolado; nao autoriza descartar futuras alteracoes de outras branches.
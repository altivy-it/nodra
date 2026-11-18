# Contrato de nomenclatura e compatibilidade

## Nomes a migrar

| Superficie | Nome alvo | Condicao |
| --- | --- | --- |
| Aplicativo, titulo e rotas com titulo | Nodra | Sem anunciar funcionalidades novas |
| Marca completa | Altivy Nodra | Empresa continua Altivy |
| Solucao | Nodra.slnx | Todas as referencias acompanham |
| Projetos/namespaces/assemblies | Nodra.* | Estrutura de camadas e sufixos preservados |
| Pacote e alvo Angular | nodra / nodra-web | Scripts e buildTarget coerentes |
| Imagens e nomes de arquivos do produto | nodra-* | Sem publicar ou alterar bind mounts externos |
| GitHub | altivy-it/nodra | Preservar ID, historico, issues e PRs |
| CLI documentada | nodra | Nenhuma implementacao nova |

## Identificadores preservados nesta entrega

| Literal ou categoria | Dono/consumidor | Motivo |
| --- | --- | --- |
| schema altivy, roles e banco existentes | PostgreSQL, migrations e SQL | Dados e historico de migrations |
| realm altivy, clientes altivy-* e audience altivy-api | Keycloak/AKB e configuracao API/BFF | Identidade externa, sem recriar recursos |
| Altivy.Bff como nome da aplicacao Data Protection | BFF e key ring existente | Nao invalidar protecao criptografica |
| Cookies __Host-Altivy.* e X-Altivy-Antiforgery | BFF e frontend | Protocolo deve permanecer sincronizado |
| ConnectionStrings:AltivyPostgres | API, worker e Compose | Configuracao externa instalada |
| Variaveis ALTIVY_* | Compose, scripts, CI e operadores | Interface externa de configuracao |
| Namespace Compose altivy-notes/altivynotes e aliases de servicos instalados | Compose e infraestrutura externa | Nao iniciar um ambiente paralelo por troca de marca |
| Microsoft/UserSecrets/Altivy.Notes/.env | Configuracao externa Windows | Caminho unico imposto pelas regras de seguranca |
| altivy:last-vault:v1:* e altivy:preferences:v1:* | Navegador | Preferencias existentes sem descarte |
| altivy.trash-purge.pending-v1 | Navegador | Nao perder rastreio de operacao pendente |
| Dominios/certificados altivy e identidade AKB | Infraestrutura externa da empresa | Fora do escopo de rename do produto |
| Specs anteriores, ADRs e docs/plan | Historico | Preservar evidencias e planos congelados |
| Aliases altivy-notes-* das quatro skills auxiliares | Instrucoes locais e registros historicos | Compatibilidade de comandos, sem alterar componentes oficiais |
| Titulo historico da constituicao ratificada | Governanca | Sem alterar principios ou ratificacao no fluxo de renomeacao |
| Paths locais existentes | Ambiente de desenvolvimento/operacao | Nao mover workspace, vaults ou secrets implicitamente |

Preservar identificadores nao autoriza preservar nomes de produto visiveis incorretos. Nao adicionar fallback de issuer/audience nem aceitar tokens de dois realms para facilitar a renomeacao.

## Operacao GitHub

A renomeacao remota e global: todas as branches passam a pertencer ao mesmo repositorio com nome novo imediatamente. Ela nao pode aguardar um merge como se fosse uma alteracao de arquivo. Antes de executar, confirmar momento com o mantenedor, permissao administrativa e ausencia de repositorio nodra conflitante. Nao transferir organizacao, excluir origem, recriar repositorio ou executar push/deploy como efeito colateral.

## Recuperacao

Antes de merge, a main permanece com seus arquivos e a feature fica isolada. Nao e preciso restaurar dados porque este desenho nao os altera. Se o nome remoto precisar voltar, a reversao administrativa exige autorizacao especifica e verificacao de colisao; nao executar automaticamente. Para um clone que perdeu acesso pelo URL antigo, atualizar somente a URL de origin para o destino confirmado e validar autenticacao.
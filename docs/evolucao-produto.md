# Nodra - Evolução do Produto

## 1. Objetivo

Organizar a evolução do Nodra em resultados verificáveis, evitando que a busca por paridade total com ferramentas existentes impeça entregas utilizáveis. A visão e as restrições arquiteturais estão no [Plano Mestre](plano-mestre.md).

## 2. Método de planejamento

Cada fase deve possuir:

- problema e público-alvo explícitos;
- hipótese de valor;
- escopo e não escopo;
- critérios de entrada e saída;
- métricas de produto e operação;
- riscos e decisão de continuidade;
- demonstração executável em containers;
- documentação de instalação e recuperação compatível com a fase.

As datas serão definidas após estimativa da equipe. O plano usa fases e gates, não prazos artificiais.

## 3. Personas iniciais

### 3.1 Profissional individual

Precisa organizar conhecimento no computador corporativo sem instalar aplicativo. Valoriza velocidade, Markdown, atalhos, busca e portabilidade.

### 3.2 Equipe técnica

Compartilha runbooks, decisões e documentação em rede interna. Precisa de identidade corporativa, permissões, links, pesquisa e auditoria.

### 3.3 Operador de plataforma

Implanta, atualiza, monitora e recupera a solução. Precisa de imagens imutáveis, Compose claro, health checks, backups e diagnóstico sem acessar conteúdo sensível.

## 4. Fase 0 - Descoberta e fundação

### Resultado

Arquitetura validada e riscos técnicos mais relevantes testados antes da construção ampla.

### Entregas

- entrevistas e mapa de jornadas das três personas;
- matriz de capacidades inspiradas no Obsidian, sem promessa de compatibilidade;
- ADRs para wiki links, concorrência, BFF/Keycloak/autorização e histórico;
- prova de conceito do CodeMirror com arquivos grandes;
- prova de gravação e reconciliação em bind mount local, SMB e NFS disponível;
- benchmark inicial de PostgreSQL full-text e pgvector;
- threat model e classificação de dados;
- esqueleto de CI, versionamento e Compose.

### Gate de saída

- nenhuma limitação conhecida inviabiliza bind mount nos ambientes-alvo;
- conflitos e falhas de gravação têm estratégia demonstrada;
- stack aprovada e orçamento inicial de desempenho definido;
- backlog do MVP priorizado por valor e risco.

## 5. Fase 1 - MVP pessoal utilizável

### Hipótese

Um profissional consegue usar o Nodra diariamente para criar e encontrar conhecimento sem instalar aplicativo nativo.

### Entregas

- implantação local por Docker Compose;
- integração com realm Keycloak de teste e login pelo BFF;
- um vault por instalação;
- árvore de arquivos e CRUD de notas/pastas;
- editor Markdown, preview, tabs e autosave com ETag;
- busca textual por conteúdo, título e caminho;
- tags e frontmatter básico;
- wiki links, backlinks e links não resolvidos;
- anexos de imagem com política de tamanho;
- command palette e atalhos essenciais;
- lixeira e restauração simples;
- backup e reconstrução manual documentados.

### Não inclui

- cadastro, senha ou recuperação de conta dentro do Nodra;
- administração de usuários ou políticas na interface do Nodra;
- colaboração;
- embeddings;
- grafo global avançado;
- histórico completo;
- múltiplos vaults por instalação.

### Métricas

- taxa de sucesso acima de 95% nos fluxos essenciais em teste de usabilidade;
- zero perda silenciosa de conteúdo nos cenários testados;
- abertura e salvamento dentro das metas do plano mestre;
- ao menos cinco usuários-piloto usando por duas semanas;
- menos de um erro não recuperável por 100 sessões.

### Gate de saída

- restauração realizada em ambiente limpo;
- testes E2E essenciais estáveis;
- feedback confirma utilidade diária;
- nenhuma vulnerabilidade crítica conhecida.

## 6. Fase 2 - Conhecimento conectado

### Hipótese

Backlinks, propriedades, grafo e pesquisa avançada reduzem o tempo para reencontrar e relacionar informações.

### Entregas

- aliases e resolução avançada de links;
- embeds de nota, heading e bloco;
- propriedades tipadas e filtros combináveis;
- grafo local e global com filtros;
- pesquisa fuzzy e linguagem de filtros;
- notas recentes, favoritas e relacionadas por links;
- outline, rename com atualização assistida de links;
- reconciliação externa observável;
- histórico de revisões configurável;
- importação e exportação de vaults compatíveis.

### Métricas

- tempo mediano para localizar nota reduzido em comparação ao MVP;
- backlinks e grafo usados por parcela mensurável dos usuários ativos;
- índice converge após mudança externa dentro do SLO definido;
- recall de links resolvidos acima de 99% no conjunto de testes;
- interface mantém fluidez nos vaults de referência.

### Gate de saída

- consultas de grafo e busca atendem metas para 100 mil notas;
- rename/move não gera perda de conteúdo;
- regras de compatibilidade Markdown estão documentadas.

## 7. Fase 3 - Prontidão corporativa

### Hipótese

Equipes podem adotar a solução quando identidade, autorização, operação e governança se integram ao ambiente corporativo.

### Entregas

- múltiplos vaults;
- autorização fina por recurso e scope no Keycloak Authorization Services;
- papéis `Owner`, `Editor` e `Reader` administrados no Keycloak;
- reconciliação observável dos recursos de vault com o Keycloak;
- auditoria e políticas de retenção;
- proxy reverso e headers encaminhados configuráveis;
- dashboards OpenTelemetry e alertas recomendados;
- backup agendado, restore e runbooks;
- atualização suportada entre versões;
- configuração read-only para vaults publicados;
- documentação de hardening e capacity planning;
- testes em Docker Desktop e Linux server.

### Métricas

- provisionamento de ambiente novo dentro do tempo operacional acordado;
- restore atende RPO e RTO;
- autorização sem violações no conjunto de testes;
- disponibilidade e erro dentro dos SLOs do piloto;
- atualização concluída sem perda nos cenários suportados.

### Gate de saída

- revisão de segurança concluída;
- pelo menos um piloto de equipe operando em ambiente representativo;
- runbooks executados por pessoa que não participou da implementação;
- critérios de release `1.0.0` do plano mestre atendidos.

## 8. Fase 4 - Descoberta semântica opcional

### Hipótese

Pesquisa semântica local ou controlada encontra conhecimento relevante que não compartilha os mesmos termos da consulta.

### Entregas

- contrato de providers de embeddings;
- provider local em container opcional;
- provider remoto sob consentimento e política explícita;
- chunking versionado e reindexação controlada;
- pesquisa híbrida com explicação do sinal;
- sugestões de notas relacionadas;
- medição de qualidade, latência e custo;
- painel administrativo de estado e reindexação.

### Métricas

- melhoria mensurável de NDCG/recall em conjunto de avaliação realista;
- latência híbrida dentro da meta acordada;
- custo e uso de recursos visíveis ao operador;
- nenhuma chamada externa quando o recurso estiver desabilitado;
- usuários conseguem distinguir pesquisa textual de semântica.

### Gate de saída

- qualidade supera baseline lexical de forma relevante;
- política de privacidade aprovada;
- troca de modelo e rollback foram testados;
- recurso permanece opcional e isolado.

## 9. Fase 5 - Colaboração e extensibilidade

Esta fase exige nova descoberta. Não deve começar automaticamente após a fase anterior.

### Candidatos

- comentários e menções;
- presença de usuários;
- compartilhamento por link com expiração;
- edição simultânea por CRDT;
- templates e automações declarativas;
- API pública e webhooks;
- extensões sandboxed com permissões;
- integrações com Git, SharePoint ou sistemas corporativos.
- conectores opcionais para Google Calendar e Outlook Calendar via Microsoft
	Graph, com polling incremental como baseline privada e webhook como aceleração
	explicitamente habilitada;

### Pré-condições

- demanda validada por múltiplos clientes ou equipes;
- modelo de autorização consolidado;
- threat model específico;
- custo operacional e de suporte estimado;
- formato de arquivo continua independente da colaboração.
- funções locais continuam operando sem provider externo, e cada conector
	possui scopes mínimos, quota, ameaça, rollout e rollback avaliados.

## 10. Matriz de priorização

Cada iniciativa recebe notas de 1 a 5:

| Critério | Peso |
| --- | ---: |
| valor para o usuário | 30% |
| redução de risco | 20% |
| alcance | 15% |
| alinhamento estratégico | 15% |
| confiança nas evidências | 10% |
| esforço inverso | 10% |

Itens de segurança, integridade e recuperação podem ser mandatórios independentemente da pontuação.

## 11. Métricas permanentes

### Produto

- usuários ativos e retenção por perfil;
- notas criadas, editadas e reencontradas;
- sucesso e tempo das jornadas essenciais;
- uso de backlinks, propriedades, busca e grafo;
- conflitos e recuperações de rascunho;
- satisfação e motivos de abandono.

### Operação

- disponibilidade e latência por operação;
- erro por versão e ambiente;
- idade e profundidade da fila;
- atraso de indexação;
- duração e sucesso de backup/restore;
- consumo de CPU, memória e armazenamento;
- vulnerabilidades por severidade e tempo de correção.

Não coletar conteúdo de notas, consultas integrais ou dados pessoais desnecessários. Telemetria de produto deve ser opt-in quando aplicável e documentada.

## 12. Política de compatibilidade

- Markdown e anexos permanecem acessíveis sem o Nodra;
- extensões próprias de sintaxe devem degradar para texto legível;
- mudanças de resolução de links exigem migração ou modo compatível;
- importadores não alteram a origem sem confirmação;
- exportação deve produzir estrutura autocontida e documentada;
- API segue versionamento explícito;
- dados de uma versão estável não podem exigir downgrade destrutivo.

## 13. Estratégia de releases

- `0.x`: validação, mudanças de contrato ainda possíveis e migrações documentadas;
- `1.0`: estabilidade dos fluxos, contrato e operação conforme plano mestre;
- minor releases adicionam capacidades compatíveis;
- patch releases corrigem defeitos e segurança sem mudar contrato;
- canal preview separado para recursos experimentais;
- deprecações anunciadas com alternativa e prazo definido.

## 14. Processo de decisão por fase

Ao fim de cada fase, registrar uma decisão:

- **prosseguir:** evidências sustentam a próxima hipótese;
- **ajustar:** valor existe, mas escopo ou solução precisa mudar;
- **manter:** produto atual atende e o próximo investimento não se justifica;
- **interromper:** riscos, custo ou falta de valor invalidam a direção.

A decisão deve incluir métricas, feedback qualitativo, dívida técnica relevante, riscos de segurança e impacto operacional.

## 15. Definition of Done de uma capacidade

Uma capacidade só está concluída quando:

- critérios funcionais e de autorização estão atendidos;
- estados de erro, vazio, conflito e indisponibilidade foram tratados;
- testes proporcionais ao risco foram automatizados;
- telemetria operacional existe sem expor conteúdo;
- documentação do usuário e do operador foi atualizada;
- migração, atualização e rollback foram considerados;
- acessibilidade e desempenho foram verificados;
- threat model foi atualizado quando a superfície de ataque mudou;
- imagens e dependências passaram pelas verificações de supply chain.
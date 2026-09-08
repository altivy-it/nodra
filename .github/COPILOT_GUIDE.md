# Tutorial de GitHub Copilot no Altivy

Este guia explica como usar as customizações do GitHub Copilot presentes em `.github` para planejar, desenvolver, testar, revisar e preparar releases do Altivy.

O objetivo não é aprender a escrever frases sofisticadas. O objetivo é fornecer ao Copilot contexto, resultado esperado, limites e evidências suficientes para que ele trabalhe como parte do processo de engenharia.

## 1. Modelo mental

As customizações cumprem funções diferentes:

| Recurso | O que representa | Como é usado | Exemplo |
| --- | --- | --- | --- |
| Instrução global | regras permanentes do repositório | carregada automaticamente | Markdown é a fonte canônica |
| Instrução específica | regras de um domínio ou caminho | carregada quando a tarefa ou arquivo corresponde | regras Angular para `frontend/web` |
| Agente | profissional especializado com ferramentas e responsabilidades | selecionado no chat ou chamado por outro agente | `Altivy .NET Engineer` |
| Prompt | comando curto para uma tarefa focada | digitado com `/` no chat | `/generate-tests` |
| Skill | workflow completo com várias etapas e gates | digitado com `/` ou descoberto automaticamente | `/feature-delivery` |
| Documento | fonte humana de arquitetura, processo ou política | lido pelo Copilot quando referenciado | plano mestre, ADR, segurança |
| Template | estrutura mínima de um artefato | preenchido por pessoa ou Copilot | pull request e ADR |

Uma forma simples de lembrar:

- **instruções dizem como trabalhar**;
- **agentes dizem quem deve trabalhar**;
- **prompts dizem qual tarefa executar**;
- **skills dizem qual processo completo seguir**;
- **documentos dizem por que e dentro de quais decisões o produto existe**.

## 2. O que acontece automaticamente

Você não precisa anexar manualmente todas as regras em cada conversa.

O arquivo [copilot-instructions.md](copilot-instructions.md) fornece contexto global. As instruções em `instructions/` possuem `description` e `applyTo`, permitindo que o Copilot as descubra pela tarefa ou pelo arquivo alterado.

Exemplo: ao solicitar uma alteração em `frontend/web/src/app/features/editor`, o Copilot deve receber as regras globais, Angular, segurança e possivelmente design system. Ao alterar uma migration em `backend/libs`, deve receber as regras .NET, PostgreSQL, segurança e arquitetura.

Ainda assim, escreva explicitamente o resultado desejado. Instruções definem padrões; elas não substituem requisitos.

## 3. Preparação no VS Code

1. Abra a raiz `altivy` no workspace.
2. Confirme que a extensão GitHub Copilot Chat está habilitada e autenticada.
3. Abra o painel de Chat do Copilot.
4. Use o seletor de agentes no topo do chat para escolher um agente especializado.
5. Digite `/` no campo do chat para listar prompts e skills disponíveis.
6. Quando o Copilot solicitar permissão para editar arquivos ou executar comandos, leia a ação e aprove apenas se ela corresponde à tarefa.

Os nomes e a interface podem variar ligeiramente conforme a versão do VS Code e da extensão. Se um prompt ou agente novo não aparecer, recarregue a janela com `Developer: Reload Window` e abra um novo chat no workspace.

## 4. Como escrever um bom pedido

Um pedido útil contém cinco elementos:

1. **Contexto:** quem precisa e em qual situação.
2. **Resultado:** comportamento observável esperado.
3. **Escopo:** módulos ou superfícies incluídas.
4. **Limites:** o que não deve mudar.
5. **Evidência:** como saber que terminou corretamente.

Modelo:

```text
Contexto: [pessoa e problema].
Resultado: [comportamento observável].
Escopo: [projetos, módulos ou fluxos].
Limites: [restrições e não escopo].
Validação: [testes, comandos ou cenários].
```

Exemplo fraco:

```text
Faça backlinks.
```

Exemplo melhor:

```text
Contexto: ao abrir uma nota, o usuário precisa descobrir quais notas apontam para ela.
Resultado: mostrar backlinks agrupados pela nota de origem, com trecho e navegação por clique.
Escopo: contrato, backend, painel contextual Angular e testes.
Limites: não implementar grafo global nem embeddings.
Validação: criar duas notas ligadas por [[Wiki Link]], abrir a nota de destino e verificar o backlink; links não resolvidos não devem aparecer como backlink resolvido.
```

Você não precisa indicar classes, tabelas ou implementação se ainda não souber. Descreva o problema e deixe o agente localizar o módulo proprietário.

## 5. Instruções

### 5.1 Quando você usa instruções

Na maior parte do tempo, instruções são automáticas. Você não as invoca com `/`.

Use-as conscientemente para:

- conferir qual padrão será aplicado a um arquivo;
- explicar por que o Copilot rejeitou ou evitou uma solução;
- alterar uma convenção permanente do projeto;
- orientar uma nova área ainda não coberta.

Exemplo de efeito automático:

```text
Crie o endpoint de atualização de nota com controle de concorrência.
```

Ao trabalhar em `backend/apps/api` e `backend/libs`, o Copilot deve aplicar, entre outras:

- `.github/copilot-instructions.md`;
- `architecture.instructions.md`;
- `backend-dotnet.instructions.md`;
- `contracts-api.instructions.md`;
- `security.instructions.md`.

Isso deve levá-lo a usar ETag/`If-Match`, `ProblemDetails`, autorização antes do acesso e testes apropriados.

### 5.2 Quando alterar uma instrução

Altere uma instrução quando a regra deve valer para várias tarefas futuras.

Bom exemplo:

```text
Todas as migrations devem registrar impacto esperado de lock e tempo de execução.
```

Mau exemplo para uma instrução permanente:

```text
Na issue 42, o botão deve ficar à esquerda.
```

Esse segundo item pertence ao requisito da issue ou ao prompt da implementação.

### 5.3 Como conferir o carregamento

Se o Copilot ignorar uma regra:

1. confirme que o arquivo está dentro da raiz correta do workspace;
2. confira o `applyTo` da instrução;
3. confira se a `description` contém termos relacionados à tarefa;
4. mencione a instrução explicitamente no pedido;
5. abra um novo chat após modificar customizações.

Exemplo:

```text
Revise esta mudança considerando explicitamente as instruções de segurança e PostgreSQL em .github/instructions.
```

## 6. Agentes

Agentes são especialistas. Selecione o agente no topo do chat antes de enviar o pedido. Um agente também pode delegar partes da tarefa a outros agentes quando sua configuração permite.

### 6.1 Qual agente escolher

| Situação | Agente recomendado |
| --- | --- |
| feature envolvendo várias camadas | `Altivy Delivery Lead` |
| decisão arquitetural ou novo componente | `Altivy Solution Architect` |
| componente, estado ou UX Angular | `Altivy Angular Engineer` |
| API, domínio, worker ou PostgreSQL | `Altivy .NET Engineer` |
| criação e execução de testes | `Altivy Quality Engineer` |
| threat model ou revisão de segurança | `Altivy Security Reviewer` |
| revisão de diff ou pull request | `Altivy Code Reviewer` |

### 6.2 Delivery Lead

Use para uma entrega ponta a ponta. Ele pode coordenar os demais especialistas.

Exemplo:

```text
Implemente favoritos de notas.

Critérios de aceite:
- o usuário pode favoritar e desfavoritar uma nota;
- favoritos aparecem em uma seção própria da navegação;
- a ordenação é pela ação mais recente;
- a permissão é por usuário e vault;
- excluir uma nota remove sua referência dos favoritos;
- inclua contrato, backend, frontend e testes.

Fora do escopo: pastas favoritas e compartilhamento de favoritos.
```

O esperado é que o agente:

1. refine critérios e riscos;
2. identifique contrato e modelo de dados;
3. delegue backend e frontend;
4. solicite testes e reviews;
5. execute validações;
6. relate o que foi feito sem commit ou deploy automático.

### 6.3 Solution Architect

Use antes de implementar uma decisão estrutural.

Exemplo:

```text
Avalie se a fila de indexação deve permanecer no PostgreSQL ou migrar para RabbitMQ quando houver múltiplos workers. Considere volume de 100 mil notas, operação self-hosted, idempotência, recuperação e custo operacional. Não edite arquivos.
```

O agente deve comparar opções e recomendar uma decisão. Ele não deve adicionar RabbitMQ apenas porque é tecnicamente possível.

### 6.4 Angular Engineer

Use para tarefa isolada no frontend.

```text
No explorador de arquivos, implemente navegação completa por teclado: setas movem o foco, direita expande, esquerda recolhe e Enter abre a nota. Preserve virtualização e adicione testes de componente. Não altere o contrato da API.
```

### 6.5 .NET Engineer

Use para tarefa isolada no backend.

```text
Implemente a reconciliação incremental de notas alteradas externamente. Use hash para confirmar mudança, jobs idempotentes e PostgreSQL real nos testes. Não implemente embeddings e não confie apenas em FileSystemWatcher.
```

### 6.6 Quality Engineer

Use quando a implementação existe, mas você precisa de evidência melhor.

```text
Crie uma matriz de testes para rename de nota. Cubra atualização de catálogo, links resolvidos, links não resolvidos, conflito concorrente, falha entre filesystem e banco e paths Unicode. Implemente os testes de maior risco e execute-os.
```

### 6.7 Reviewers

Reviewers são read-only. Eles relatam problemas, mas não alteram o código.

Exemplo de segurança:

```text
Revise o fluxo de upload de anexos. Trace nome do arquivo, MIME, assinatura, path final, autorização e resposta HTTP. Liste achados por severidade e proponha teste de regressão. Não edite arquivos.
```

Exemplo de código:

```text
Revise as alterações da branch atual contra main. Priorize perda de dados, regressões de contrato, concorrência e testes ausentes. Ignore preferências estilísticas já cobertas por ferramentas.
```

## 7. Prompts

Prompts são comandos focados. No chat, digite `/` e selecione o prompt. Depois complete os argumentos solicitados.

### 7.1 Prompts disponíveis

| Comando | Uso |
| --- | --- |
| `/plan-feature` | transformar necessidade em plano sem implementar |
| `/implement-feature` | implementar uma feature vertical |
| `/create-adr` | analisar e estruturar uma decisão arquitetural |
| `/generate-tests` | criar testes para um comportamento ou diff |
| `/diagnose-problem` | reproduzir, encontrar causa e corrigir um problema |
| `/review-change` | revisar diff, branch ou pull request |

### 7.2 Planejar sem implementar

```text
/plan-feature

Adicionar lixeira por vault. Uma nota excluída deve poder ser restaurada por 30 dias. O usuário Reader não pode excluir ou restaurar. A solução deve funcionar em NAS e não pode depender de serviço externo.
```

Use esse prompt quando ainda quer discutir escopo e desenho. O agente não deve editar arquivos.

### 7.3 Implementar uma feature

```text
/implement-feature

Adicionar aliases no frontmatter e usá-los na resolução de [[wiki links]].

Critérios:
- aliases aceitam uma lista de strings;
- busca por alias encontra a nota;
- link por alias resolve para a nota correta;
- colisão entre aliases gera estado ambíguo explícito;
- preserve o frontmatter original;
- inclua testes de parser, PostgreSQL, API e jornada no browser.
```

### 7.4 Criar um ADR

```text
/create-adr

Decidir como identificar notas após rename: UUID persistido no frontmatter, identidade somente no PostgreSQL ou inferência por hash/path. Considere compatibilidade com vaults existentes, edição externa, privacidade e recuperação.
```

O resultado deve ser criado em `docs/adr/AAAA-NNNN-titulo.md`, com status `proposed`, seguindo [ADR_TEMPLATE.md](ADR_TEMPLATE.md). Use sequência anual (por exemplo, `2026-0001`). Revise a decisão antes de aceitá-la; um ADR não vira verdade apenas porque foi gerado por IA.

### 7.5 Diagnosticar um bug

```text
/diagnose-problem

Ao renomear uma nota para um nome com acento no Windows, o catálogo cria uma segunda entrada e o backlink antigo permanece. Reproduza com teste mínimo, encontre a causa raiz e corrija sem normalizar conteúdo do usuário.
```

Forneça mensagens de erro, comando que falhou e ambiente quando disponíveis. Não cole tokens, connection strings ou conteúdo real de notas.

### 7.6 Gerar testes

```text
/generate-tests

Comportamento: PUT de nota exige If-Match e deve preservar as duas versões em conflito. Crie testes de API e integração com duas atualizações concorrentes usando filesystem e PostgreSQL reais.
```

### 7.7 Revisar uma mudança

```text
/review-change

Revise a branch atual contra main. O objetivo da mudança é adicionar pesquisa por tags. Confirme autorização por vault, paginação, índices, contrato OpenAPI e estados de UI.
```

## 8. Skills

Skills são playbooks completos. Use-as quando a tarefa exige uma sequência de fases e gates, não apenas uma ação isolada.

### 8.1 Skills disponíveis

| Comando | Quando usar |
| --- | --- |
| `/feature-delivery` | entrega complexa do requisito até os reviews |
| `/pull-request-review` | revisão completa de PR em várias dimensões |
| `/release-readiness` | decisão GO/NO-GO antes de uma release |

### 8.2 Feature delivery

```text
/feature-delivery

Feature: backlinks no painel contextual.
Usuário: profissional que navega entre notas relacionadas.
Resultado: visualizar origem, trecho e navegar para cada backlink.
Não escopo: grafo global, colaboração e embeddings.
Aceite: links resolvidos aparecem; links removidos desaparecem após indexação; acesso respeita permissão do vault; fluxo funciona após alteração externa do arquivo.
```

A skill deve conduzir enquadramento, decisão, slices, implementação, testes, segurança e review.

### 8.3 Pull request review

```text
/pull-request-review

Revise a branch feature/note-trash contra main. Issue: #27. ADR relacionado: docs/adr/0004-note-trash.md. Execute apenas validações read-only ou que não alterem artefatos versionados.
```

Espere achados por severidade, com cenário, impacto e correção. Um resultado sem achados deve declarar testes não executados e risco residual.

### 8.4 Release readiness

```text
/release-readiness

Versão candidata: 0.3.0.
Base: 0.2.1.
Ambientes: Docker Desktop Windows e Docker Engine Linux.
Mudanças principais: backlinks, lixeira e OIDC.
Não publique nem faça deploy; produza somente a matriz de gates e recomendação GO/NO-GO.
```

`NOT RUN` não significa sucesso. Para obter `GO`, cada gate obrigatório precisa de evidência ou waiver formal.

## 9. Documentos e templates

### 9.1 Plano mestre e documentos técnicos

Use como fontes de decisão:

- [plano mestre](../docs/plano-mestre.md): visão, escopo e arquitetura global;
- [frontend](../docs/frontend.md): implementação Angular e UX;
- [backend](../docs/backend.md): implementação .NET, dados e filesystem;
- [evolução do produto](../docs/evolucao-produto.md): fases, gates e métricas.

Exemplo de pedido com contexto explícito:

```text
Leia docs/plano-mestre.md e docs/backend.md. Proponha o modelo mínimo para links não resolvidos sem implementar. A proposta deve permitir reconstrução integral a partir do vault.
```

### 9.2 ADR

Use ADR quando a decisão altera arquitetura, fonte canônica, consistência, segurança, infraestrutura, formato ou compatibilidade.

Fluxo recomendado:

1. execute `/create-adr`;
2. revise opções e consequências;
3. escolha o próximo número em `docs/adr`;
4. copie a estrutura de [ADR_TEMPLATE.md](ADR_TEMPLATE.md);
5. mantenha status `proposed` durante discussão;
6. altere para `accepted` somente após decisão humana;
7. referencie o ADR no PR e na implementação.

### 9.3 Planos

Planos criados por pessoas ou IA vivem em `docs/plan/<tema>.md` e seguem [PLAN_TEMPLATE.md](PLAN_TEMPLATE.md). O arquivo registra origem, revisor humano, status geral e status individual de todas as etapas.

Use `/plan-feature` para criar ou atualizar um plano. IA usa no máximo `proposed` até revisão humana e não marca etapa como `completed` ou `skipped` sem evidência explícita. Toda transição registra data, autor e referência no histórico do plano.

### 9.4 Issues

Use os templates em `ISSUE_TEMPLATE` para registrar o problema antes de uma mudança relevante.

Uma boa issue de feature descreve problema e resultado. Não imponha solução técnica sem necessidade.

Uma boa issue de bug inclui reprodução, esperado, observado, ambiente e impacto. Vulnerabilidades não devem virar issues públicas; siga [SECURITY.md](SECURITY.md).

### 9.5 Pull request

O [template de pull request](PULL_REQUEST_TEMPLATE.md) exige evidências. Não marque uma caixa se o comando não foi executado.

Peça ao Copilot para ajudar a preencher sem inventar resultados:

```text
Com base exclusivamente no diff e nos comandos executados nesta sessão, prepare o texto do template de PR. Mantenha desmarcado tudo que não foi executado e liste riscos residuais.
```

### 9.6 Guia de contribuição e segurança

- [CONTRIBUTING.md](CONTRIBUTING.md) define o fluxo humano e Definition of Done.
- [SECURITY.md](SECURITY.md) define reporte privado de vulnerabilidades.

## 10. Como fornecer contexto ao Copilot

### 10.1 Contexto mínimo útil

Para uma tarefa localizada, forneça:

- comportamento esperado;
- arquivo, símbolo, erro ou teste, se conhecido;
- restrições;
- comando de validação.

Exemplo:

```text
O teste NotePathTests falha para nomes Unicode no Windows. Corrija a causa sem alterar o contrato de path relativo e execute somente a suíte relacionada antes do build amplo.
```

### 10.2 Referencie arquivos

Você pode mencionar caminhos diretamente:

```text
Considere docs/plano-mestre.md, docs/backend.md e contracts/openapi/altivy.yaml.
```

No VS Code, também pode adicionar arquivos pelo recurso de contexto do chat. Evite anexar o repositório inteiro sem necessidade; excesso de contexto pode esconder o que importa.

### 10.3 Use erros reais

Prefira:

```text
dotnet test falha em NoteWriterTests com UnauthorizedAccessException ao substituir o arquivo temporário. Ocorre apenas no Windows.
```

Em vez de:

```text
O backend não funciona.
```

### 10.4 Não envie dados sensíveis

Nunca forneça ao chat:

- senhas, tokens e chaves;
- connection strings reais;
- conteúdo confidencial de notas;
- dumps de banco;
- arquivos `.env` reais;
- logs com dados pessoais.

Sanitize exemplos antes de enviá-los.

No desenvolvimento local em Windows, mantenha todo secret somente em
`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, fora do repositório.
Isso inclui credenciais administrativas do Keycloak e PostgreSQL, client
secrets, connection strings, SMTP, IdPs e demais valores sensíveis. Não peça ao
agente para criar `.env` ou arquivo equivalente dentro da estrutura do projeto.

## 11. Como acompanhar o trabalho do agente

Durante a execução, observe:

1. **Hipótese:** o agente explicou o que acredita controlar o comportamento?
2. **Escopo:** está alterando somente módulos relacionados?
3. **Primeira validação:** executou teste/build logo após a primeira edição?
4. **Falhas:** corrigiu a causa ou apenas silenciou o teste?
5. **Segurança:** paths, autorização, conteúdo e secrets foram considerados?
6. **Evidência:** os comandos realmente foram executados?
7. **Conclusão:** há riscos e limitações claramente declarados?

Interrompa ou redirecione quando perceber ampliação indevida:

```text
Pare a expansão de escopo. Não altere o design system. Concentre-se apenas no conflito de ETag e reexecute o teste que falhava.
```

Para pedir status sem encerrar:

```text
Informe o que já foi confirmado, quais arquivos foram alterados, qual validação passou e qual é o próximo passo. Depois continue.
```

## 12. Aprovação de ferramentas e comandos

O Copilot pode pedir autorização para executar terminal, editar arquivos ou usar recursos externos.

Antes de aprovar, confira:

- o comando está no repositório correto;
- não contém secret;
- não publica, faz deploy ou push sem sua intenção;
- não usa operação destrutiva como reset, limpeza de dados ou remoção de bind mount;
- a validação é proporcional à mudança;
- instalações de dependências são realmente necessárias.

Comandos de leitura, build e teste normalmente são esperados. Comandos de migração, publicação, exclusão, alteração de infraestrutura e acesso à nuvem merecem revisão adicional.

## 13. Fluxos completos de exemplo

### 13.1 Da ideia à implementação

Primeiro, planeje:

```text
/plan-feature

Quero permitir fixar notas importantes no topo do explorador. O recurso é individual por usuário e vault. Não deve alterar os arquivos Markdown.
```

Revise o plano. Se não houver decisão estrutural pendente, implemente:

```text
/implement-feature

Implemente o plano aprovado para notas fixadas. Preserve o contrato definido, adicione testes e não faça commit.
```

Depois revise:

```text
/review-change

Revise o diff da feature de notas fixadas contra os critérios aprovados. Priorize autorização por vault e remoção de referências após exclusão de nota.
```

### 13.2 De um bug à regressão protegida

```text
/diagnose-problem

Ao salvar duas abas da mesma nota, a segunda sobrescreve a primeira sem aviso. Reproduza com duas versões do mesmo ETag, corrija e adicione teste de regressão.
```

Depois peça revisão especializada:

```text
Selecione Altivy Code Reviewer:
Revise somente a correção de concorrência. Confirme status HTTP, preservação das versões e cobertura do cenário simultâneo.
```

### 13.3 De uma decisão a um ADR

```text
/create-adr

Avaliar histórico de revisões: cópias no vault, tabela PostgreSQL ou Git interno. Considere Markdown como fonte canônica, NAS, retenção, restore e experiência de usuário.
```

O prompt cria `docs/adr/AAAA-NNNN-historico-revisoes.md` como `proposed`. Após revisão humana, registre a decisão, altere para `accepted` e use-o como contexto na implementação.

### 13.4 Antes de uma release

```text
/release-readiness

Avalie 1.0.0-rc.1 contra 0.9.0. Não publique. Exija restore testado, migration PostgreSQL, Playwright, imagens, SBOM, scan e documentação de upgrade.
```

Não peça ao agente para transformar `NO-GO` em `GO`. Corrija ou aceite formalmente os riscos e execute novamente.

## 14. Erros comuns ao usar IA

### Pedido amplo demais

```text
Crie o Obsidian completo.
```

Divida por capacidade e critérios de aceite. Use `/plan-feature` antes da implementação.

### Pedir tecnologia em vez de problema

```text
Adicione Redis.
```

Prefira:

```text
A fila PostgreSQL está causando atraso medido de 10 minutos com três workers. Analise a causa e opções antes de adicionar infraestrutura.
```

### Confiar sem validar

Código plausível pode estar incorreto. Exija testes executados, diagnósticos e revisão.

### Aceitar mudanças gigantes

Peça slices pequenos e validação após cada etapa. Pull requests menores são mais fáceis de revisar e reverter.

### Deixar a IA decidir produto sozinha

O Copilot pode estruturar opções, mas prioridade, risco aceito e decisão de produto são responsabilidades humanas.

### Pedir para ignorar testes

Se um teste falha, descubra se é regressão, ambiente ou teste inválido. Não remova evidência para obter build verde.

### Misturar planejamento e execução sem intenção

Use `/plan-feature` para discutir sem editar. Use `/implement-feature` quando deseja alterações reais.

## 15. Checklist rápido

Antes do pedido:

- [ ] Sei qual problema quero resolver.
- [ ] Defini resultado e não escopo.
- [ ] Escolhi agente, prompt ou skill adequado.
- [ ] Removi dados sensíveis do contexto.

Durante:

- [ ] O agente declarou hipótese e validação.
- [ ] As mudanças permanecem no escopo.
- [ ] Os testes são executados, não apenas sugeridos.
- [ ] Decisões irreversíveis foram interrompidas para revisão.

Depois:

- [ ] Li o diff.
- [ ] Conferi comandos e resultados.
- [ ] Solicitei review apropriado.
- [ ] Documentação, contrato e migration estão sincronizados.
- [ ] Riscos residuais estão explícitos.
- [ ] Nenhum commit, push ou deploy ocorreu sem minha decisão.

## 16. Caminho recomendado para começar

Para as primeiras tarefas, use esta sequência:

1. `/plan-feature` para transformar a ideia em critérios claros;
2. revise e ajuste o plano em linguagem natural;
3. `/implement-feature` para executar uma entrega pequena;
4. `/review-change` para revisar o diff;
5. leia os testes e o resumo antes de criar o pull request;
6. use `/release-readiness` somente quando houver uma versão candidata real.

Com experiência, selecione diretamente Angular Engineer, .NET Engineer ou Quality Engineer para tarefas locais. Para mudanças que cruzam várias áreas, continue preferindo Delivery Lead e os workflows completos.

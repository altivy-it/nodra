---
name: altivy-notes-generate-tests
description: "Use quando solicitado explicitamente para gerar e executar testes de uma tarefa autorizada do Spec Kit no Nodra, preservando seus critérios, fixtures e execução containerizada, sem criar escopo de produto."
argument-hint: "Caminho do artefato Spec Kit, ID da tarefa, critérios, autorização e escopo dos testes"
user-invocable: true
disable-model-invocation: true
---

# Auxílio a testes de tarefa oficial do Nodra

## Escopo e seleção do agente

Na raiz Nodra, selecione explicitamente **Altivy Quality Engineer**,
conforme seu [contrato](../../agents/quality-engineer.agent.md), e invoque
`/altivy-notes-generate-tests` com o artefato e o ID. Esta skill não seleciona
agente, modelo ou ferramentas pelo frontmatter. Se o agente estiver
indisponível, informe o bloqueio e solicite escolha explícita de alternativa
compatível; não amplie ferramentas nem substitua o papel silenciosamente.

## Entradas e pré-condições

Exija tarefa autorizada em `specs/<feature>/tasks.md` ou artefato oficial de
Bug Fixing Spec Kit, seu ID, escopo e critérios aplicáveis. Confirme a
autorização explícita de implementação/execução: a existência de artefato
ou checkbox não a substitui.

Sem referência ou autorização, pare, informe o bloqueio e solicite o escopo
oficial pelo Spec Kit. Não derive nem acrescente critérios de aceite
independentes e não crie trabalho adicional.

## Procedimento

1. Leia a tarefa, a spec ou remediação oficial ativa e o diff. Derive riscos
   e critérios exclusivamente desses artefatos.
2. Siga as [instruções de testes](../../instructions/testing.instructions.md)
   e as [regras globais](../../copilot-instructions.md), usando padrões,
   fixtures e comandos existentes do módulo. Escolha o nível mais barato
   que prove cada risco, sem duplicar os procedimentos dos seus donos.
3. Cubra happy path, limites, erro, autorização, concorrência e cancelamento
   quando definidos ou necessários para validar a tarefa. Reporte lacunas
   ao Spec Kit, sem alterar requisitos.
4. Use PostgreSQL/filesystem reais para integração e Playwright para jornadas.
   Use dados sintéticos e testes determinísticos; não use sleeps fixos,
   internet, dados reais ou snapshots opacos.
5. Execute o teste novo e demonstre que ele discrimina a regressão quando
   viável; depois execute a suíte relacionada. Testes e ferramentas de build
   ficam em containers. Quando exigirem aplicação HTTP em execução, use
   exclusivamente a borda nginx containerizada conforme as regras existentes;
   testes unitários sem HTTP não exigem nginx ocioso.
6. Não altere snapshots ou produção sem justificativa e autorização no escopo
   da tarefa. Não modifique produção apenas para facilitar teste sem justificar
   a API e não corrija defeitos fora do escopo.
7. Separe falhas preexistentes, falhas novas e verificações não executadas.
   Não marque testes como skipped para obter verde nem trate falta de
   evidência como sucesso.

## Entrega e limites

Informe matriz risco-teste, cobertura adicionada, arquivos alterados,
comandos/resultados, lacunas e recomendação de qualidade. Para UI, registre
as verificações aplicáveis de console, rede, teclado e viewports conforme
a tarefa e as instruções de testes.

Este auxiliar não autoriza trabalho de produto adicional, commit, push,
publicação ou deploy.

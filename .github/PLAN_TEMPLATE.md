# Plano: Título

> **Obsoleto.** Não crie, atualize nem execute planos com este template. Ele permanece temporariamente para referência durante a migração dos planos legados de `docs/plan` para Spec Kit.
> **Obsoleto.** Não crie, atualize nem execute planos com este template. Mantenha-o somente até a revisão humana dos artefatos Spec Kit migrados; depois, arquive-o ou remova-o junto das referências legadas.

**Status:** draft  
**Data de criação:** AAAA-MM-DD  
**Última atualização:** AAAA-MM-DD  
**Responsáveis:** nomes ou equipe  
**Origem:** humano ou IA assistida  
**Revisor humano:** nome, equipe ou pendente  
**Relacionado:** issue, ADR ou pull request

## Objetivo

Descreva o resultado observável que o plano pretende entregar.

## Contexto e premissas

- estado atual e motivação;
- restrições e premissas verificáveis;
- decisões já aceitas e perguntas bloqueantes.

## Não escopo

- itens explicitamente excluídos desta entrega.

## Dependências e decisões

- ADRs necessários ou relacionados;
- contratos, migrations, serviços e equipes envolvidos;
- dependências externas e licenças verificadas.

## Etapas

### P-01: Nome da etapa

**Status:** not-started  
**Responsável:** nome, equipe ou não atribuído  
**Dependências:** nenhuma ou IDs de etapas anteriores

**Objetivo:** resultado específico desta etapa.

Entregas:

- entrega pequena e verificável.

Validação:

- comando, teste, métrica ou evidência que demonstra conclusão.

Evidências:

- pendente; ao concluir, referencie PR, commit, relatório ou resultado executado.

Repita a seção para cada etapa. Toda etapa usa um dos estados `not-started`, `in-progress`, `blocked`, `completed` ou `skipped`.

## Critérios de aceite finais

- comportamento e qualidade observáveis para considerar o plano concluído.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| risco identificado | impacto esperado | ação preventiva ou de recuperação |

## Rollout e rollback

Descreva implantação incremental, sinais de sucesso, interrupção segura e recuperação.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| AAAA-MM-DD | plano | - | draft | criação | humano ou IA assistida |

## Regras de atualização

- O status geral usa `draft`, `proposed`, `approved`, `in-progress`, `blocked`, `completed`, `cancelled` ou `superseded`.
- IA pode criar e atualizar o plano, mas não define status acima de `proposed` sem aprovação humana explícita.
- `approved` registra responsável e referência da aprovação humana.
- `completed` exige todas as etapas obrigatórias em `completed` ou `skipped`, critérios finais atendidos e evidências registradas.
- `skipped` exige justificativa e aprovação humana; bloqueio registra causa e condição de desbloqueio.
- Toda alteração de status geral ou de etapa adiciona uma entrada no histórico.

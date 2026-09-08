# Política de Retenção e Recuperação de Resíduos de Vault

**Status:** approved  
**Data:** 2026-09-06  
**Responsáveis:** equipe Altivy  
**Origem:** IA assistida a partir de decisão humana  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [Criação autogerenciada de vaults](adr/2026-0005-criacao-autogerenciada-de-vaults.md), [Plano de alternância segura entre vaults](plan/alternancia-vaults.md) e [Observabilidade, SLOs e alertas](observabilidade-alertas-slo.md)

## Objetivo

Preservar dados e rastreabilidade quando uma falha parcial deixar divergência
entre o diretório Markdown canônico, a projeção PostgreSQL e a operação de
provisioning. A política impede exclusão silenciosa e orienta detecção,
notificação, recuperação, quarentena, retenção e remoção definitiva.

## Escopo e definições

Um resíduo é qualquer estado inconsistente detectado durante provisioning,
reconciliação ou operação administrativa:

| Tipo | Exemplo | Estado inicial |
| --- | --- | --- |
| Operação interrompida | lease expira antes da conclusão | `provisioning` recuperável |
| Falha transitória | I/O temporário ou storage indisponível | `degraded` com retry |
| Falha permanente | acesso negado, reparse point ou path não confiável | `failed` |
| Projeção sem diretório | vault `ready` sem diretório físico confiável | `degraded` |
| Diretório órfão | `/vaults/{vaultId}` sem operação e sem projeção correspondente | quarentena |

O conteúdo Markdown no filesystem é a fonte canônica. A projeção PostgreSQL,
leases, erros e registros de auditoria são metadados operacionais e não
autorizam perda, sobrescrita ou recriação silenciosa de conteúdo.

## Notificação e escalonamento

Ao detectar qualquer resíduo, o worker deve criar um evento operacional e
enviar uma mensagem ao administrador configurado imediatamente, antes de toda
exclusão ou quarentena destrutiva. A mensagem contém somente: ID técnico do
vault ou operação, tipo, estado, instante UTC, tentativa e ação requerida.
Ela não contém nomes de vault, paths, conteúdo de notas, sujeitos, tokens,
connection strings ou stack traces.

A entrega precisa ter destinatário administrativo e canal configurados pelo
operador. A implementação pode usar e-mail ou webhook autenticado, mas deve
registrar resultado de entrega e correlação. Enquanto não existir entrega
confirmável, a exclusão automática fica desabilitada e o resíduo é retido.

Se o alerta não for entregue, o worker deve repetir a notificação com backoff,
registrar a falha sanitizada e elevar a ocorrência no monitoramento. Falha de
alerta não pode ser tratada como aceite administrativo.

### Configuração SMTP

O worker envia alertas à caixa operacional do administrador por SMTP com TLS.
Os valores são recebidos exclusivamente em runtime, por secret montado ou cofre
de segredos do operador:

```text
ResidueAlerts__Smtp__Host=smtp.exemplo.invalid
ResidueAlerts__Smtp__Port=587
ResidueAlerts__Smtp__Username=altivy-alerts
ResidueAlerts__Smtp__Password=<senha-smtp-fornecida-pelo-operador>
ResidueAlerts__Smtp__From=altivy-alerts@exemplo.invalid
ResidueAlerts__Smtp__To=operacoes@exemplo.invalid
```

Senha SMTP não é registrada, retornada por endpoint ou versionada em `.env`.
O e-mail contém somente ID da ocorrência, ID técnico do vault, tipo, instantes
UTC de detecção, tentativa e ação requerida. Não contém nome, path, conteúdo,
sujeito, token, connection string ou stack trace. Falhas SMTP permanecem
pendentes para retry. Sem host, credenciais e endereços válidos, o worker falha
na inicialização para não operar sem a notificação obrigatória.

## Retenção de 30 dias

1. O prazo de retenção é de 30 dias corridos, contado do primeiro instante de
   detecção registrado em UTC.
2. Cada nova detecção ou falha de recuperação renova o prazo por mais 30 dias
   para impedir exclusão enquanto a ocorrência ainda está ativa.
3. Operações `failed` e vaults `degraded` permanecem preservados durante o
   prazo; seus dados físicos não são apagados automaticamente.
4. Diretórios órfãos são movidos atomicamente, quando possível, para uma área
   de quarentena sob a raiz confiável. A quarentena preserva ID, data de
   detecção e correlação, sem aceitar nomes fornecidos pelo usuário como path.
5. A área de quarentena não é publicada pela API, pelo catálogo, pelo
   explorador, pela busca ou por anexos estáticos.
6. A ocorrência e a pendência de alerta são persistidas antes da movimentação
   para quarentena. Assim, uma falha parcial no filesystem não descarta a
   obrigação de notificar e investigar.
7. Ao vencer o prazo, o worker agenda um único alerta `retention-expired` ao
   administrador. Uma nova detecção cancela esse marco e reinicia o prazo de
   30 dias; o aviso não autoriza exclusão física.

## Recuperação

| Situação | Ação automática permitida | Ação humana necessária |
| --- | --- | --- |
| Lease expirado e diretório existente/confiável | Reivindicar e repetir idempotentemente | Não, salvo falha repetida |
| Falha transitória de I/O | Retry limitado com backoff | Sim, ao exceder o limite configurado |
| `ready` sem diretório | Marcar `degraded` e notificar | Confirmar restauração por backup ou causa da perda |
| Diretório órfão | Quarentenar e notificar | Confirmar vinculação, restauração ou descarte |
| Path não confiável/reparse point | Bloquear acesso e marcar `failed` | Inspecionar e remover o risco no host |

Uma recuperação nunca pode sobrescrever diretório ou nota existente. Ela só
pode criar o diretório vazio de um vault cuja operação ainda esteja sob lease
válido e cuja ausência tenha sido verificada. Restaurar conteúdo requer backup
verificado ou decisão explícita do administrador.

## Exclusão definitiva

Ao completar 30 dias, a remoção definitiva exige cumulativamente:

1. ocorrência ainda classificada como resíduo e sem recuperação concluída;
2. notificação inicial e aviso de vencimento enviados com entrega confirmável;
3. backup verificável do diretório e dos metadados operacionais, quando houver
   conteúdo ou diretório a remover;
4. ausência de lease ativo, operação em retry ou recuperação em curso;
5. autorização explícita de administrador para a primeira versão do produto;
6. registro de auditoria do motivo, identificadores técnicos, backup, horário e
   responsável, sem conteúdo de notas ou credenciais.

Enquanto a autorização explícita não estiver implementada, a política proíbe
remoção automática após 30 dias: a ocorrência permanece em quarentena e gera
novo alerta de vencimento. Exclusão física automática futura exige revisão de
segurança, privacidade, backup/restore e uma decisão arquitetural específica.

## Controle de acesso e auditoria

Somente administradores do ambiente podem consultar ocorrências, acionar
recuperação, aprovar descarte ou acessar quarentena. Essa capacidade não cria
autorização local para vaults e não substitui o Keycloak como autoridade de
identidade e autorização.

Cada transição deve registrar ID de ocorrência, ID de vault/operação, tipo,
estado anterior e novo, instante UTC, resultado de entrega do alerta, referência
de backup, motivo sanitizado e sujeito administrativo quando necessário. Logs,
métricas e alertas usam cardinalidade controlada e nunca incluem conteúdo de
notas, paths físicos, nomes de vault, tokens ou secrets.

## Backup, validação e recuperação de desastre

Antes de quarentena destrutiva ou exclusão, o operador deve produzir backup do
diretório e metadados relacionados e verificar que ele pode ser lido. Para
remoção definitiva, a restauração deve ser exercitada em ambiente isolado antes
da primeira ativação operacional e periodicamente conforme o runbook de backup
do ambiente. Falha de backup ou de validação bloqueia a ação e gera alerta.

## Requisitos de implementação

Antes de habilitar criação autogerenciada de vaults em uma instalação, a equipe
deve implementar e testar:

- armazenamento durável de ocorrências, primeiro avistamento e retenção;
- canal configurável de alerta administrativo com confirmação de entrega;
- quarentena confinada, idempotente e protegida contra traversal e reparse
  points;
- comandos administrativos autorizados para recuperar, manter ou aprovar
  descarte;
- métricas para resíduos detectados, idade, alertas pendentes, retries,
  quarentenas e exclusões bloqueadas;
- testes de falha parcial, reinício do worker, backup, recuperação, retenção e
  recusa de exclusão sem alerta ou autorização.

## Aprovação e revisão

Esta política foi aprovada por Lincoln Zocateli em 2026-09-06. Ela deve ser
revisada antes da primeira instalação compartilhada, mudança de canal de alerta,
alteração de retenção ou habilitação de exclusão automática.
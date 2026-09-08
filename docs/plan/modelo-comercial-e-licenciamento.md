# Plano: Modelo comercial e licenciamento

**Status:** proposed  
**Data de criação:** 2026-09-08  
**Última atualização:** 2026-09-08  
**Responsáveis:** mantenedores do Altivy e produto  
**Origem:** IA assistida  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** [plano mestre](../plano-mestre.md), [governança](../../.github/GOVERNANCE.md) e [licença MIT](../../LICENSE)

## Objetivo

Definir um modelo open core sustentável no qual o núcleo self-hosted do Altivy
permanece disponível sob MIT e funcionalidades adicionais possam ser oferecidas
por assinatura mensal ou anual. O plano também prepara, para uma etapa posterior,
um controle de licenças inspirado no modelo do Bruno, sem implementá-lo agora.

## Contexto e premissas

- o código publicado neste repositório está sob MIT;
- direitos concedidos por uma versão MIT são irrevogáveis para aquela versão;
- a edição aberta precisa continuar útil, segura e operável sem assinatura;
- funcionalidades pagas devem ampliar valor, não bloquear acesso ao conteúdo;
- Markdown permanece a fonte canônica e nunca pode ser retido por expiração;
- conteúdo, busca e edição essenciais continuam independentes de nuvem;
- autenticação OIDC, autorização por vault, backup, recuperação e segurança básica
  permanecem no núcleo aberto;
- código proprietário futuro não será publicado neste repositório como MIT;
- preços, impostos, desconto anual e política de reembolso exigem validação
  comercial e jurídica antes do lançamento;
- a implementação do controle de licenças exige ADR, threat model e aprovação
  humana específica.

## Modelo de ofertas proposto

| Oferta | Público | Licenciamento | Cobrança |
| --- | --- | --- | --- |
| Open Source | pessoas, equipes e organizações self-hosted | MIT, sem chave e sem limite imposto pelo Altivy | gratuita |
| Pro | profissionais e equipes que precisam de produtividade avançada | assinatura comercial por usuário autorizado | mensal ou anual |
| Business | organizações que precisam de governança, operação e suporte em escala | assinatura comercial por assento ou contrato organizacional | mensal ou anual |

As modalidades mensal e anual de uma mesma oferta entregam as mesmas
funcionalidades. A modalidade anual pode aplicar desconto sobre o equivalente a
doze mensalidades, mas o percentual e os preços serão definidos somente após
pesquisa de mercado, custos e validação tributária.

### Open Source

O núcleo aberto inclui, no mínimo:

- criação, edição, organização e portabilidade de notas Markdown;
- links, backlinks, tags, propriedades, pesquisa e grafo;
- operação self-hosted por containers;
- autenticação OIDC e autorização por vault com Keycloak;
- API e contratos públicos necessários à edição aberta;
- backup, restauração, reconciliação e auditoria essencial;
- importação, exportação e extensibilidade aberta documentada;
- atualizações de segurança do núcleo enquanto a versão for suportada.

Não haverá chave, ativação remota, limite artificial de vaults, usuários ou
notas, nem perda de acesso ao conteúdo na edição Open Source.

### Pro

Pode agregar funcionalidades opcionais de produtividade, como:

- automações avançadas e fluxos reutilizáveis;
- histórico, comparação e recuperação com experiência ampliada;
- publicação e exportações avançadas;
- conectores premium mantidos pelo Altivy;
- relatórios avançados de uso individual ou de equipe;
- suporte comercial por canal e prazo definidos.

### Business

Pode incluir tudo do Pro e capacidades organizacionais, como:

- administração centralizada de múltiplas instâncias;
- políticas e configurações distribuídas de forma controlada;
- auditoria avançada, exportação e retenção configurável;
- provisionamento e desprovisionamento automatizado de licenças;
- SSO para ativação comercial e integração SCIM do cadastro de assentos, sem
  substituir o Keycloak como autoridade de identidade do produto;
- servidor de licenças self-hosted ou ativação offline para redes isoladas;
- SLA, suporte prioritário e revisão de segurança contratual.

## Princípios de separação

- o repositório público e seus artifacts Open Source permanecem integralmente MIT;
- módulos comerciais usam distribuição, termos e avisos próprios;
- contratos entre núcleo e extensões são versionados e não expõem interfaces
  artificiais destinadas apenas a degradar a edição aberta;
- nenhuma extensão comercial pode alterar o formato canônico dos documentos de
  modo que impeça leitura ou edição pela versão aberta;
- dados criados por recurso pago continuam exportáveis quando a assinatura expira;
- dependências proprietárias não entram no build da edição Open Source;
- marca e licença de software são tratadas separadamente; MIT não concede direito
  de representar forks como produto oficial Altivy.

## Política comercial preliminar

- assinaturas são atribuídas a usuários autorizados ou assentos organizacionais;
- chaves, tokens e credenciais comerciais não são compartilháveis;
- a contratação concede uso interno durante o período pago, não transfere a
  propriedade do código comercial;
- cancelamento ou expiração desativa somente funcionalidades pagas;
- renovação mensal ou anual, reajustes, prorrata e reembolso serão descritos nos
  termos vigentes no momento da contratação;
- clientes mantêm propriedade sobre notas, configurações e demais dados próprios;
- telemetria comercial, se adotada, será mínima, documentada e configurada de
  acordo com privacidade, segurança e contratos aplicáveis.

## Não escopo

- implementar agora servidor, chave, ativação ou validação de licença;
- implementar agora checkout, cobrança, emissão fiscal ou integração com gateway;
- definir preços ou percentual de desconto anual sem pesquisa e aprovação;
- transformar o Altivy em SaaS ou exigir conectividade contínua;
- mover funcionalidades essenciais já prometidas pelo plano mestre para uma
  edição paga;
- adicionar código proprietário ao repositório MIT sem separação explícita;
- substituir Keycloak por um diretório de usuários mantido pelo Altivy.

## Dependências e decisões

- revisão jurídica da MIT, termos comerciais, política de marca e contribuições;
- ADR para limites entre núcleo MIT e extensões comerciais;
- ADR e threat model para emissão, assinatura, armazenamento e validação de
  entitlements;
- definição de catálogo versionado de ofertas, funcionalidades e entitlements;
- decisão de preços, moeda, impostos, desconto anual, trial e reembolso;
- avaliação de gateway de pagamento e seus requisitos de segurança e privacidade;
- definição de suporte, SLA, DPA e processo de vendor assessment;
- revisão das licenças de todas as dependências dos módulos comerciais.

## Etapas

### P-01: Validar produto e fronteiras comerciais

**Status:** not-started  
**Responsável:** produto e mantenedores  
**Dependências:** nenhuma

**Objetivo:** confirmar que a edição aberta é autônoma e que cada capacidade paga
adiciona valor sem degradar o núcleo MIT.

Entregas:

- inventário de funcionalidades por oferta;
- critérios objetivos para classificar funcionalidade aberta ou comercial;
- entrevistas com usuários e organizações self-hosted;
- proposta de preços mensal e anual baseada em custos e mercado.

Validação:

- matriz aprovada por responsável humano e revisão explícita contra o plano mestre.

Evidências:

- pendente.

### P-02: Formalizar aspectos jurídicos e de marca

**Status:** not-started  
**Responsável:** mantenedores e assessoria jurídica  
**Dependências:** P-01

**Objetivo:** definir termos compatíveis com MIT, contribuições e venda de
funcionalidades proprietárias.

Entregas:

- termos de serviço e licença comercial;
- política de privacidade e processamento de dados aplicável;
- política de marca para distribuições oficiais e forks;
- decisão sobre CLA ou Developer Certificate of Origin;
- política de trial, cancelamento, renovação, reembolso e suporte.

Validação:

- parecer jurídico e aprovação humana registrados.

Evidências:

- pendente.

### P-03: Definir arquitetura open core

**Status:** not-started  
**Responsável:** arquitetura  
**Dependências:** P-01, P-02

**Objetivo:** separar núcleo e extensões sem criar dependência do código MIT em
serviços comerciais para operações essenciais.

Entregas:

- ADR dos limites de módulos, repositórios, build e distribuição;
- contratos versionados para descoberta de funcionalidades;
- comportamento de ausência, cancelamento e expiração da assinatura;
- estratégia de SBOM, provenance e avisos de licença por artifact.

Validação:

- build Open Source funciona sem credenciais, rede ou artifacts comerciais;
- testes demonstram que documentos permanecem acessíveis após remover extensões.

Evidências:

- pendente.

### P-04: Projetar catálogo e cobrança

**Status:** not-started  
**Responsável:** produto, financeiro e backend  
**Dependências:** P-01, P-02

**Objetivo:** especificar assinaturas mensais e anuais sem acoplar pagamentos ao
domínio de notas.

Entregas:

- catálogo versionado de ofertas, preços e entitlements;
- modelo de assinatura, assento, renovação e mudança de plano;
- integração proposta com provedor de pagamento;
- fluxo de faturamento, impostos, cancelamento e reembolso;
- reconciliação idempotente de eventos comerciais.

Validação:

- cenários de compra, renovação, upgrade, downgrade, falha e chargeback revisados;
- nenhum dado de cartão é processado ou armazenado diretamente pelo Altivy.

Evidências:

- pendente.

### P-05: Especificar controle de licenças

**Status:** not-started  
**Responsável:** arquitetura, segurança e backend  
**Dependências:** P-02, P-03, P-04

**Objetivo:** desenhar o controle de licença futuro antes de qualquer código de
ativação ou enforcement.

Entregas:

- ADR e threat model do sistema de licenças;
- formato assinado e versionado de entitlements;
- ativação por usuário, organização e ambiente isolado;
- rotação, revogação, grace period e cache offline;
- minimização de dados enviados na validação;
- observabilidade sem chave, conteúdo, identidade sensível ou dados do vault.

Validação:

- revisão de segurança cobre falsificação, replay, indisponibilidade, rollback de
  relógio, vazamento de chave e abuso de assentos;
- falha do serviço de licenças não bloqueia a edição Open Source nem o acesso aos
  documentos;
- dados e endpoints de validação estão documentados para revisão de privacidade.

Evidências:

- pendente.

### P-06: Implementar controle de licenças posteriormente

**Status:** not-started  
**Responsável:** Lincoln Zocateli  
**Dependências:** P-05 e aprovação humana explícita para iniciar implementação

**Objetivo:** implementar o desenho aprovado em entrega futura, isolada e
reversível.

Entregas:

- serviço e portal de administração de licenças;
- ativação, desativação e gestão de assentos;
- validação online com grace period e opção offline para Business;
- enforcement apenas nos pontos de entrada de funcionalidades pagas;
- testes unitários, integração, segurança, recuperação e compatibilidade.

Validação:

- testes provam isolamento entre autorização de produto e entitlement comercial;
- expiração preserva dados e mantém o núcleo MIT operacional;
- indisponibilidade, migração e rollback são exercitados em ambiente limpo.

Evidências:

- pendente; esta etapa não está autorizada para implementação neste momento.

### P-07: Executar lançamento comercial gradual

**Status:** not-started  
**Responsável:** produto, operações e suporte  
**Dependências:** P-02, P-03, P-04, P-06

**Objetivo:** lançar as ofertas com escopo, suporte e operação verificáveis.

Entregas:

- piloto interno e beta com organizações convidadas;
- documentação de compra, ativação, privacidade e suporte;
- métricas de conversão, churn, falhas de ativação e satisfação;
- processo de resposta a incidentes comerciais e de licenciamento.

Validação:

- gates jurídicos, segurança, suporte, cobrança e recuperação aprovados;
- rollback para edição Open Source testado sem perda de conteúdo.

Evidências:

- pendente.

## Critérios de aceite finais

- o repositório público e seus artifacts continuam claramente licenciados sob MIT;
- a matriz de ofertas possui fronteiras aprovadas e não reduz o núcleo prometido;
- modalidades mensal e anual possuem termos e preços publicados sem diferença
  artificial de funcionalidades;
- código e artifacts comerciais têm licença e distribuição inequivocamente separadas;
- controle de licenças passou por ADR, threat model e aprovação antes de ser criado;
- cancelamento, expiração ou indisponibilidade nunca impedem acesso aos documentos;
- privacidade, suporte, cobrança, SBOM, provenance e rollback possuem evidências.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| publicar código comercial sob MIT por engano | exclusividade comercial perdida de forma irreversível | separar repositórios, builds, avisos e revisão jurídica antes da publicação |
| edição aberta ser percebida como limitada artificialmente | perda de confiança e adoção | manter critérios públicos e capacidades essenciais no núcleo |
| falha de licença bloquear conteúdo | indisponibilidade e dano reputacional | fail-open apenas para acesso ao conteúdo e fallback para Open Source |
| confundir entitlement com autorização | escalada de privilégio ou bloqueio indevido | domínios, contratos e testes independentes; Keycloak continua como PEP/PDP externo |
| validação remota expor dados | risco de privacidade e vendor assessment | payload mínimo, documentação, TLS, opção offline e sem dados do vault |
| custos comerciais superarem receita | modelo insustentável | pesquisa, piloto, métricas e revisão periódica de preços |
| dependência excessiva do provedor de pagamento | interrupção de venda ou reconciliação | adapter, eventos idempotentes, exportação e plano de substituição |

## Rollout e rollback

O rollout começa pela publicação da licença MIT e validação da matriz de ofertas,
sem ativar cobrança ou enforcement. Arquitetura, termos e catálogo são aprovados
antes de desenvolver o controle de licenças. A implementação futura passa por
piloto e beta antes da disponibilidade geral.

O rollback remove ou desabilita exclusivamente módulos comerciais e restaura a
experiência Open Source. Dados produzidos por funcionalidades pagas permanecem
exportáveis e legíveis. Nenhum rollback altera retroativamente a licença de
código já publicado.

## Histórico de status

| Data | Escopo | De | Para | Evidência ou motivo | Autor |
| --- | --- | --- | --- | --- | --- |
| 2026-09-08 | plano | - | proposed | criação solicitada e estrutura aprovada pelo mantenedor humano | humano com IA assistida |

## Regras de atualização

- O status geral usa `draft`, `proposed`, `approved`, `in-progress`, `blocked`, `completed`, `cancelled` ou `superseded`.
- IA pode criar e atualizar o plano, mas não define status acima de `proposed` sem aprovação humana explícita.
- `approved` registra responsável e referência da aprovação humana.
- `completed` exige todas as etapas obrigatórias em `completed` ou `skipped`, critérios finais atendidos e evidências registradas.
- `skipped` exige justificativa e aprovação humana; bloqueio registra causa e condição de desbloqueio.
- Toda alteração de status geral ou de etapa adiciona uma entrada no histórico.

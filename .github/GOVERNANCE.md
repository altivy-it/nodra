# Governança do Altivy

Este documento define como o projeto toma decisões, aceita mudanças e mantém sua
integridade técnica. Ele complementa o [plano mestre](../docs/plano-mestre.md),
o [guia de contribuição](CONTRIBUTING.md) e a [política de segurança](SECURITY.md).

## Princípios

- decisões preservam o conteúdo do usuário e a portabilidade do Markdown;
- segurança, recuperação e operação self-hosted têm precedência sobre conveniência;
- mudanças relevantes possuem critérios verificáveis e histórico auditável;
- autoridade técnica não permite ignorar revisão, evidência ou política de segurança;
- comportamento implementado, proposta e decisão aceita são estados distintos.

## Papéis

### Mantenedores

Mantenedores definem prioridades, fazem triagem, revisam mudanças, administram o
repositório e preparam releases. O ownership versionado está em [CODEOWNERS](CODEOWNERS).

Enquanto existir apenas um mantenedor, aprovação independente pode não ser
possível. Essa limitação deve permanecer explícita; não simule revisão por uma
segunda identidade nem marque controles independentes como concluídos.

### Contribuidores

Contribuidores podem propor issues, documentação, testes, código e decisões.
Uma contribuição não concede autoridade automática sobre roadmap, segurança,
release ou infraestrutura do projeto.

### Revisores de segurança

Relatos de vulnerabilidade seguem exclusivamente a [política de segurança](SECURITY.md).
Detalhes sensíveis não entram em issues, discussões, logs ou artifacts públicos.

## Processo de decisão

- correções e mudanças locais seguem pull request com testes proporcionais ao risco;
- alterações arquiteturais usam ADR em `docs/adr` desde o estado `proposed`;
- iniciativas multi-etapas usam plano em `docs/plan` com status e evidências;
- alteração incompatível exige impacto, migração, rollout e recuperação;
- introdução de datastore, serviço ou dependência estrutural exige decisão explícita;
- aceite de ADR, aprovação de plano e dispensa de gate são decisões humanas registradas.

Quando não houver consenso, o mantenedor responsável decide e registra a
justificativa no pull request, issue ou ADR correspondente. Decisões podem ser
revisitadas com nova evidência, sem reescrever o histórico anterior.

## Revisão e merge

- `main` recebe mudanças por pull request, salvo procedimento emergencial documentado;
- conversas de revisão devem ser resolvidas antes do merge;
- checks obrigatórios precisam estar concluídos com sucesso;
- revisão de Code Owner é exigida quando houver revisor independente disponível;
- force push após revisão exige aviso e nova avaliação das mudanças relevantes;
- bypass de ruleset é excepcional, auditável e seguido por revisão posterior.

## Licenciamento e contribuições externas

O código publicado neste repositório é disponibilizado sob a
[licença MIT](../LICENSE). Contribuições incorporadas ao repositório são
distribuídas sob os mesmos termos, salvo indicação explícita e juridicamente
válida em componente separado.

Funcionalidades comerciais futuras devem permanecer separadas do núcleo MIT.
Nenhum plano, contrato ou mecanismo técnico pode revogar direitos já concedidos
sobre versões publicadas sob MIT. O modelo proposto está no
[plano comercial](../docs/plan/modelo-comercial-e-licenciamento.md).

## Releases e segurança operacional

Releases partem de tags protegidas, usam artifacts imutáveis e incluem evidências
de build, testes, migrações, segurança, SBOM, proveniência e recuperação aplicáveis.
Publicação e deploy exigem solicitação explícita e não são realizados
automaticamente por agentes de IA.

Configurações administrativas que não vivem no Git estão no
[checklist administrativo do GitHub](../docs/github-administracao.md).

## Alteração desta governança

Mudanças neste documento usam pull request, explicam motivação e impacto e são
aprovadas por mantenedor humano. Alterações que redistribuem autoridade, mudam
licenciamento ou reduzem controles exigem discussão pública prévia quando o
repositório já estiver aberto à comunidade.

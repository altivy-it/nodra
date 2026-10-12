<!--
Relatório de impacto da constituição
Versão: inexistente -> 1.0.0 (constituição inicial, ratificada pelo pedido humano)
Princípios incluídos: integridade e portabilidade; identidade externa; segurança e privacidade;
arquitetura e compatibilidade; qualidade verificável e documentação em pt-BR.
Seções adicionadas: restrições técnicas; processo Spec Kit; governança e versionamento.
Seções removidas: nenhuma.
Pendências: migrar sete planos legados congelados após a inicialização do Spec Kit.
Pendências: obter revisão humana dos oito conjuntos Spec Kit que substituem sete planos legados; arquivar os originais após aprovação.
Este relatório é temporário para revisão e deve ser removido antes do commit.
-->

# Constituição do AltivyNotes

## Princípios Fundamentais

### I. Integridade e Portabilidade dos Dados

Os arquivos Markdown dos vaults são a fonte canônica do conteúdo. PostgreSQL é
uma projeção reconstruível e não pode se tornar a única cópia de uma nota.
Nenhuma alteração pode sobrescrever conteúdo do usuário silenciosamente;
conflitos, falhas parciais entre filesystem e banco e recuperação devem ter
comportamento explícito e verificável.

### II. Identidade e Autorização Externas

Keycloak é a autoridade de identidade e decisão de autorização. O BFF protege a
sessão e os tokens; a API aplica as permissões emitidas pelo Keycloak e falha
fechada. O AltivyNotes não implementa cadastro, credenciais, recuperação de
senha, memberships ou políticas próprias de autorização.

### III. Segurança e Privacidade

Paths devem ser normalizados e autorizados antes do acesso ao filesystem.
Secrets permanecem fora do repositório, e logs, fixtures e artefatos de teste
não podem expor conteúdo real de notas, dados pessoais ou credenciais. Entradas
e arquivos externos são tratados como não confiáveis.

### IV. Arquitetura Modular e Evolução Compatível

Hosts compõem módulos; regras reutilizáveis pertencem às bibliotecas e contratos
independentes de linguagem não dependem de aplicações. Mudanças públicas,
persistência e operações preservam compatibilidade ou definem migração,
recuperação e rollout verificáveis. Novo serviço, datastore ou dependência
estrutural exige decisão arquitetural registrada antes da implementação.

### V. Qualidade Verificável e Documentação em Português

Mudanças de comportamento incluem testes proporcionais ao risco. PostgreSQL e
filesystem são validados por integração real quando forem a superfície em
questão; jornadas de usuário recebem validação E2E apropriada. Código, análise,
testes e revisão devem produzir evidência executável, não apenas declarações de
conclusão.

Toda documentação produzida ou atualizada para o projeto deve estar em
português brasileiro (pt-BR), inclusive a constituição, specs, planos, tarefas,
checklists, relatórios de defeitos, documentação técnica e textos auxiliares
gerados pelo Spec Kit. Preserve em inglês nomes de comandos, identificadores,
termos de contrato, código, mensagens literais de ferramentas e outros valores
cuja tradução alteraria seu significado técnico.

## Restrições Técnicas

- Backend, BFF e frontend executam em containers; runtimes não são requisito da
máquina do usuário final.
- O backend usa .NET 10 e o frontend Angular 22, conforme os projetos existentes.
- Testes e ferramentas de build executam em containers. Testes que precisam de
uma aplicação HTTP usam a borda do proxy nginx containerizado; testes unitários
sem aplicação HTTP não precisam iniciar esse proxy.
- Dependências devem ter licença permissiva compatível com distribuição
self-hosted, versão reproduzível, manutenção verificada e necessidade real.
Licença ausente, ambígua ou incompatível bloqueia a adoção.
- Mudanças de schema seguem expand-contract e são validadas com PostgreSQL real.

## Processo de Desenvolvimento e Manutenção

- Spec Kit é o único processo autorizado para desenvolver e manter a aplicação.
Instruções, agentes, prompts, Issues, Projects e ADRs podem impor limites ou
apoiar tarefas, mas não criam um fluxo concorrente de requisitos, planos ou
execução.
- Features e mudanças de produto usam `specs/<feature>/spec.md` para requisitos,
`plan.md` para desenho e `tasks.md` para tarefas. Siga `/speckit-specify`,
`/speckit-clarify` quando necessário, `/speckit-plan`, `/speckit-tasks`,
`/speckit-analyze`, `/speckit-implement` e `/speckit-converge`.
- Correções de defeitos usam a extensão oficial Spec Kit Bug Fixing e seus
artefatos em `.specify/bugs/<slug>/`, por meio de `/speckit-bug-assess`,
`/speckit-bug-fix` e `/speckit-bug-test`.
- Nenhum trabalho de aplicação começa sem artefato Spec Kit ativo e escopo
revisado. A existência de arquivos, uma Issue `Ready` ou caixas marcadas não
constitui autorização humana para implementar.
- `docs/plan/` está congelado. Os sete planos ainda não concluídos devem ser
- `docs/plan/` está congelado. Os sete planos legados abertos foram convertidos
em oito conjuntos Spec Kit em rascunho. Não executar essas iniciativas até a
revisão humana dos artefatos; depois da revisão, arquive os originais sem
autoridade operacional.
- ADRs registram decisões arquiteturais e não substituem specs, planos ou tarefas.
Um ADR criado por IA permanece `proposed` até decisão humana explícita.
- O Spec Kit e seus overrides locais devem gerar os artefatos de documentação
em pt-BR, mantendo nomes técnicos e tokens exigidos pelos comandos.

## Governança

Esta constituição registra princípios duradouros e prevalece sobre orientações
locais incompatíveis. Arquivos `.github/` detalham como cumprir os princípios,
mas não podem autorizar processo de desenvolvimento ou manutenção concorrente
ao Spec Kit. Conflitos com decisões arquiteturais aceitas exigem decisão humana.

Alterações nesta constituição exigem revisão e aprovação humana. Versione-a
semanticamente: PATCH para esclarecimentos sem mudança de princípio, MINOR para
novos princípios ou expansão compatível e MAJOR para remoção ou redefinição
incompatível. A revisão de mudanças deve verificar conformidade com esta
constituição e registrar exceções com justificativa e aprovação humana.

**Versão**: 1.0.0 | **Ratificada**: 2026-10-06 | **Última alteração**: 2026-10-06

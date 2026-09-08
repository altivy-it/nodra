# ADR 2026-0002: Keycloak externo e BFF para identidade e autorização

**Status:** accepted  
**Data:** 2026-08-15  
**Responsáveis:** equipe Altivy  
**Origem:** decisão humana assistida por IA  
**Revisor humano:** Lincoln Zocateli  
**Relacionado:** `docs/plan/bff-keycloak.md`

## Contexto

O Altivy precisa autenticar usuários e aplicar autorização por vault sem criar
um diretório de identidade paralelo. O produto é self-hosted, pode operar sem
internet e mantém Markdown no filesystem como fonte canônica. O navegador não
pode receber tokens nem acessar diretamente a API, o PostgreSQL ou o vault.

A decisão é necessária antes de criar o BFF, configurar contratos executáveis
ou registrar clients no Keycloak.

## Forças de decisão

- manter identidade, credenciais, MFA, SSO, grupos, roles e políticas fora do Altivy;
- aplicar autorização contextual por recurso e scope antes de revelar dados;
- evitar tokens no browser e reduzir impacto de XSS e uso indevido de sessão;
- permitir operação com Keycloak na rede local em instalações isoladas;
- manter rollback sem introduzir login local, memberships ou políticas duplicadas;
- limitar privilégios e preservar rastreabilidade operacional.

## Opções consideradas

### Opção A: Keycloak externo com BFF e API como PEP

O BFF usa Authorization Code com PKCE, mantém sessão e tokens no servidor e
encaminha requisições para a API. A API valida o token e aplica a decisão do
Keycloak Authorization Services para o recurso e scope solicitados.

Vantagens: respeita a fonte externa de identidade, mantém tokens fora do
browser, suporta autorização por vault e permite falha fechada. Desvantagens:
exige disponibilidade operacional do Keycloak, configuração de sessão e uma
integração adicional para reconciliar recursos e scopes.

### Opção B: identidade e autorização locais

A aplicação manteria usuários, sessões, memberships e políticas no próprio
banco.

Vantagens: menor dependência de um serviço externo durante a execução.
Desvantagens: duplica responsabilidades de IAM, amplia a superfície de
segurança, contraria o plano mestre e dificulta federação, MFA e SSO.

### Opção C: Angular integrado diretamente ao Keycloak

O navegador obteria e armazenaria tokens para chamar a API.

Vantagens: menos código de relay no servidor. Desvantagens: expõe tokens ao
browser, conflita com a topologia pública prevista e não resolve sozinho a
autorização contextual por vault.

## Decisão

A opção A é a direção arquitetural proposta:

- realm dedicado `altivy`; o realm `master` não será usado pela aplicação;
- client confidencial `altivy-bff` para o fluxo do browser;
- client de recurso `altivy-api` com audience própria;
- BFF como cliente OIDC confidencial, gerenciador de sessão técnica e relay;
- API como Policy Enforcement Point, sem avaliador de política local;
- recursos de vault nomeados por identificador opaco e scopes mínimos;
- roles e políticas mantidas no Keycloak, sem tabelas locais de usuários ou
  memberships;
- escolha entre RPT/UMA e avaliação remota somente após o spike P-03;
- falha fechada quando o token, a decisão ou o Keycloak estiverem indisponíveis;
- a apresentação das telas de autenticação será um tema do Keycloak, usando a
  fonte canônica `Altivy/docs/brand`, sem mover autenticação para o Angular.

Esta decisão permanece `proposed` até revisão e aceitação humana. A versão do
Keycloak, os clients reais, callbacks, secrets e exports de realm serão
configurados somente após essa aceitação e nunca serão versionados.

O tema será empacotado como artefato de build a partir de
`$env:USERPROFILE\projetos\Altivy\docs\brand`, com assets e fontes locais,
checksums e licença verificáveis. A estrutura seguirá o mecanismo oficial da
versão suportada do Keycloak e poderá retornar ao tema padrão sem migração de
dados. Qualquer dependência não permissiva, recurso externo ou incompatibilidade
de template bloqueia a promoção.

## Consequências

### Positivas

- identidade e autorização permanecem em uma autoridade especializada;
- tokens de acesso e refresh ficam fora do Angular;
- autorização horizontal por vault pode ser testada antes do acesso ao dado;
- instalações locais podem apontar para um Keycloak interno compatível;
- rollback não exige reativar mecanismos locais proibidos.

### Negativas e trade-offs

- login e autorização protegida dependem da disponibilidade do Keycloak;
- o BFF exige key ring protegido, renovação de tokens e controles antiforgery;
- latência e revogação precisam ser medidas no spike antes da escolha final;
- recursos e scopes exigem provisionamento idempotente e reconciliação.

## Segurança e privacidade

O BFF aceitará somente cookies técnicos `Secure`, `HttpOnly` e `SameSite`
explícitos, com antiforgery e validação de `Origin` nas mutações. Cookies e
headers de identidade enviados pelo browser serão removidos antes do relay.

Client secrets e credenciais de Protection API ficarão em secret store ou
arquivos montados pelo operador. Logs não conterão tokens, secrets, conteúdo de
notas ou claims desnecessários. A API não revelará a existência de recursos
sem a permissão correspondente.

O threat model deve cobrir comprometimento de client secret, XSS, replay,
revogação, indisponibilidade do IdP, divergência de recursos e uso acidental do
realm `master`. O tema também deve ser inspecionado para scripts, referências
externas, fontes remotas, secrets e mensagens que revelem detalhes sensíveis.

## Dados, compatibilidade e migração

Não serão criadas tabelas `users`, `vault_members` ou equivalentes. O claim
estável `sub` só poderá ser persistido quando necessário para auditoria ou
rastreabilidade técnica. Recursos de vault registrados no Keycloak devem ser
reconciliáveis a partir do catálogo do Altivy.

A migração inicial não possui dados de identidade locais, pois o produto ainda
não tem implementação backend. Qualquer configuração futura será expandível,
sanitaria e reversível.

## Observabilidade e operação

Serão observados duração e resultado de login, callback, renovação, decisão,
negação, erro de integração e divergência de recurso. Métricas e logs usarão
IDs técnicos e correlation ID, sem registrar tokens ou conteúdo do usuário.

A operação deverá possuir readiness separado de liveness, alertas para
indisponibilidade e runbook para rotação, revogação, backup e recuperação do
realm.

## Validação

- discovery OIDC do realm dedicado funciona sem usar `master`;
- issuer, audience, assinatura, expiração e permissões incorretos são rejeitados;
- browser, URL e logs não exibem tokens;
- acesso sem scope, acesso horizontal e elevação de privilégio falham fechados;
- remoção de permissão e indisponibilidade do Keycloak não concedem fallback;
- login, erro, OTP e recuperação usam os assets locais e atendem contraste e
  foco mínimo WCAG 2.2 AA;
- o artefato do tema passa nos manifests `SHA256SUMS` sem referência externa;
- o spike P-03 compara RPT/UMA e avaliação remota com métricas de latência,
  revogação, tamanho e granularidade por vault.

## Rollout e rollback

Após a aceitação humana, validar em realm e clients não produtivos, com
configuração fora do Git e secrets rotacionáveis. O rollout será bloqueado se
houver credencial do realm `master`, fallback local ou permissão sem recurso.

O rollback mantém imagens anteriores imutáveis e reverte web, BFF e API como
conjunto compatível. Clients, tokens e permissões da versão interrompida serão
revogados; não haverá restauração de login local ou acesso direto à API.

## Referências

- [Plano mestre](../plano-mestre.md)
- [Plano do BFF com Keycloak](../plan/bff-keycloak.md)
- [Tutorial de configuração do Keycloak](../configuracao-keycloak.md)
- [Implementação do backend](../backend.md)

Fonte canônica visual, mantida no repositório `Altivy`:

- `docs/brand/guide.md`
- `docs/brand/tokens/tokens.css`
- `docs/brand/tokens/tokens.json`
- `docs/brand/tokens/typography.md`
- `docs/brand/fonts/`
- `docs/brand/assets/dist/`
- `docs/brand/licenses.md`
- `docs/brand/frontend-asset-integration-playbook.md`

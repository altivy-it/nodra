---
name: "Segurança Altivy"
description: "Use ao alterar autenticação, autorização, filesystem, Markdown, HTML, uploads, secrets, logs, proxy, dependências ou qualquer superfície de segurança."
applyTo: "backend/**, frontend/**, contracts/**, deploy/**"
---

# Segurança

## Princípios

- Considere nomes, paths, Markdown, frontmatter, anexos, claims e headers como não confiáveis.
- Autorize antes de revelar existência, metadados, path ou conteúdo.
- Use menor privilégio em processos, banco, filesystem e rede.
- Falhe fechado em autorização e validação; falhas de embeddings devem degradar apenas a função opcional.

## Filesystem

- Aceite apenas paths relativos; normalize, resolva e confirme confinamento na raiz.
- Rejeite traversal, paths absolutos, dispositivos, nomes reservados e escapes por symlink/reparse point.
- Valide novamente no momento do uso para reduzir TOCTOU.
- Use arquivos temporários no mesmo filesystem e replace seguro.
- Não siga links simbólicos por padrão.

## Web e conteúdo

- Renderize Markdown por parser configurado e sanitizador allowlist central.
- Bloqueie scripts, event handlers, iframes arbitrários, `javascript:` e data URLs não permitidas.
- Aplique CSP, frame ancestors, content type options e referrer policy.
- Cookies de sessão são `Secure`, `HttpOnly` e SameSite apropriado; operações mutáveis têm antiforgery.
- Upload valida extensão, MIME, assinatura, tamanho, nome e destino.

## Identidade e dados

- Keycloak é a autoridade externa para usuários, credenciais, MFA, federação, SSO, sessões, grupos, roles, recursos, scopes e políticas de autorização.
- Use um realm dedicado ao Nodra; nunca use o realm administrativo `master` nem conceda privilégios administrativos globais à aplicação.
- A solução não armazena senha, conta local, membership de vault ou cópia de política de autorização. Referências técnicas ao `sub` podem existir somente onde forem necessárias para auditoria e rastreabilidade.
- O BFF usa Authorization Code com PKCE como cliente confidencial, mantém tokens fora do browser e propaga logout e revogação conforme os recursos do Keycloak.
- A API valida assinatura, issuer, audience, expiração e permissões do token e falha fechada quando a decisão de acesso estiver ausente, inválida, expirada ou indisponível.
- A API é o Policy Enforcement Point; aplicar uma decisão emitida pelo Keycloak não transfere à solução a responsabilidade de cadastrar identidades ou definir políticas.
- O registro de recursos e scopes no Keycloak usa credencial de serviço com menor privilégio, é idempotente, auditável e nunca usa conta administrativa do realm.
- No desenvolvimento local em Windows, o único arquivo permitido para secrets é
	`$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`, sempre fora da árvore
	do repositório. Centralize nele credenciais do Keycloak e PostgreSQL, client
	secrets, connection strings, SMTP, IdPs e demais valores sensíveis.
- Nunca crie `.env`, `.env.*`, `secrets.json`, cópias ou arquivos equivalentes
	com secrets na estrutura do projeto, nem mesmo ignorados pelo Git. Não use
	`dotnet user-secrets`; consuma o arquivo externo por caminho explícito ou
	injete as variáveis no processo.
- Em ambientes compartilhados, CI e produção, use o cofre ou mecanismo de
	secrets da plataforma; não materialize o arquivo local do Windows.
- Secrets nunca entram em Git, contexto ou camada de imagem, artifact ou log.
- Não envie conteúdo a provider externo sem configuração administrativa e aviso explícito.
- Logs contêm IDs técnicos e resultados, nunca conteúdo integral ou credenciais.
- Auditoria é append-only e não substitui logs operacionais.

## Verificação

- Adicione testes negativos para traversal, XSS, CSRF, upload, token inválido, autorização horizontal e elevação de privilégio; valide também indisponibilidade, revogação e divergência de recursos no Keycloak.
- Faça threat model quando uma nova fronteira ou tipo de dado for introduzido.
- Verifique necessidade, licença permissiva e gratuita, vulnerabilidades e manutenção antes de adicionar ou atualizar dependência; licença paga, restritiva, ambígua, ausente ou não verificada bloqueia a mudança.
- Revise dependências, SBOM, vulnerabilidades e licenças novamente antes da release.
- Uma descoberta crítica bloqueia release até correção ou aceitação formal de risco.
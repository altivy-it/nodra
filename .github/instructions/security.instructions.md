---
name: "Segurança Basalt Notes"
description: "Use ao alterar autenticação, autorização, filesystem, Markdown, HTML, uploads, secrets, logs, proxy, dependências ou qualquer superfície de segurança."
applyTo: "apps/**, libs/**, contracts/**, deploy/**"
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

- Sem senha padrão; bootstrap usa segredo efêmero ou primeiro acesso controlado.
- Secrets vêm de ambiente seguro ou arquivo montado e nunca entram em Git, imagem ou log.
- Não envie conteúdo a provider externo sem configuração administrativa e aviso explícito.
- Logs contêm IDs técnicos e resultados, nunca conteúdo integral ou credenciais.
- Auditoria é append-only e não substitui logs operacionais.

## Verificação

- Adicione testes negativos para traversal, XSS, CSRF, upload, autorização horizontal e elevação de privilégio.
- Faça threat model quando uma nova fronteira ou tipo de dado for introduzido.
- Revise dependências, SBOM, vulnerabilidades e licenças antes da release.
- Uma descoberta crítica bloqueia release até correção ou aceitação formal de risco.
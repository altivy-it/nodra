---
name: "CI CD e GitHub Actions"
description: "Use ao criar ou alterar GitHub Actions, CI, checks de pull request, build, release, SBOM, provenance, publicação de imagens ou automação do repositório."
applyTo: ".github/workflows/**, .github/dependabot.yml"
---

# CI/CD

- Workflows usam permissões mínimas no nível de job e fixam actions por commit SHA.
- Pull requests de forks não recebem secrets nem executam código não confiável com contexto privilegiado.
- Separe CI de PR, build de imagem, release e deploy.
- Use concurrency com cancelamento para branches; release não deve ser cancelada por execução posterior.
- Cache depende de lockfiles e nunca armazena secrets ou artifacts mutáveis.
- Gates mínimos: format, lint/analyzers, build, unit, integration, contract, architecture e E2E conforme matriz.
- PostgreSQL de integração usa a imagem imutável do plano mestre e health check real.
- Artifacts têm retenção explícita e não contêm vaults, dumps, `.env` ou logs sensíveis.
- Releases partem de tag protegida, geram imagens imutáveis, SBOM, provenance e scan.
- Publicação e deploy exigem ambiente protegido e aprovação quando configurada.
- Não use `latest` como única tag; publique versão SemVer e digest.
- Falhas de segurança e incompatibilidade de migration bloqueiam release.
- Renovação de dependências deve manter lockfiles e passar pelos mesmos gates.
- Dependency Review bloqueia licença paga, proprietária, source-available, comercial obrigatória, copyleft, ausente, ambígua ou não verificada; novas dependências registram no pull request a licença da versão exata e sua fonte oficial.
- Documente inputs, outputs, secrets esperados e procedimento de reexecução segura.
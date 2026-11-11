# Contribuindo com o Nodra

## Antes de começar

1. Leia o [plano mestre](../docs/plano-mestre.md) e o documento da área afetada.
2. Procure issue, ADR e implementação relacionada.
3. Para mudanças da aplicação, registre critérios de aceite e riscos nos artefatos Spec Kit antes do código; não mantenha requisitos paralelos em issues ou prompts.
4. Não inclua conteúdo real de vault, dados pessoais, tokens ou secrets em issues, fixtures ou logs.
5. No Windows, armazene secrets locais exclusivamente em
   `$env:APPDATA\Microsoft\UserSecrets\Altivy.Notes\.env`. Nunca crie `.env`,
   `.env.*`, `secrets.json` ou arquivo equivalente dentro do repositório, nem
   use `dotnet user-secrets`; em CI e produção, use o mecanismo de secrets da
   plataforma.
6. Instale o hook obrigatório de detecção de secrets em cada clone:

   ```powershell
   ./tools/scripts/install-gitleaks-hook.ps1
   git hook run pre-commit
   ```

   Consulte a [política de detecção de secrets](SECRET-SCANNING.md).

## Estrutura e ownership

- `frontend/web`: aplicação Angular e composição da interface.
- `backend/apps/api`: host HTTP e SignalR.
- `backend/apps/worker`: processamento assíncrono e reconciliação.
- `frontend/libs/frontend`: bibliotecas Angular reutilizáveis.
- `backend/libs`: domínio, aplicação e infraestrutura .NET.
- `contracts`: OpenAPI e schemas públicos.
- `tests`: integração, E2E, arquitetura, desempenho e fixtures.
- `deploy`: containers, Compose, proxy e operação.

Mudanças devem começar no módulo proprietário do comportamento. Não mova regra para um host ou biblioteca genérica apenas para compartilhá-la.

## Fluxo de desenvolvimento

1. Crie uma branch curta a partir de `main`.
2. Para features, siga Spec Kit: `/speckit-specify`, `/speckit-clarify` quando necessário, `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, `/speckit-implement` e `/speckit-converge`.
3. Para defeitos, use a extensão oficial Spec Kit Bug Fixing (`/speckit-bug-assess`, `/speckit-bug-fix`, `/speckit-bug-test`). Não implemente mudanças de aplicação sem tarefa/artefato ativo do fluxo correspondente.
4. Não crie, atualize ou execute planos em `docs/plan`; os planos legados abertos foram convertidos em oito conjuntos Spec Kit em rascunho e aguardam revisão humana antes de qualquer execução.
5. Faça commits coesos conforme [Conventional Commits](COMMIT_CONVENTIONS.md).
6. Atualize testes, contrato, migration, documentação e telemetria junto da tarefa Spec Kit.
7. Execute os gates aplicáveis antes do pull request.
8. Preencha o template do PR com evidências e riscos.
9. Resolva achados por causa raiz e reexecute validações afetadas.

## Contrato de comandos

Quando o monorepo for inicializado, mantenha estes comandos funcionais e documentados pelos manifests:

```powershell
# Frontend
npm ci
npm run format:check
npm run lint
npm run typecheck
npm test
npm run build
npm run e2e

# Backend
dotnet restore Nodra.slnx --locked-mode
dotnet format Nodra.slnx --verify-no-changes
dotnet build Nodra.slnx --no-restore
dotnet test Nodra.slnx --no-build
```

Testes de integração podem exigir Docker para PostgreSQL/Testcontainers. Scripts adicionais devem compor esses comandos, não substituí-los silenciosamente.

## Definition of Done

- critérios de aceite demonstrados;
- código formatado, analisado e compilado;
- testes proporcionais ao risco executados;
- contratos e clients gerados sincronizados;
- migrations testadas em PostgreSQL real;
- segurança, acessibilidade e desempenho considerados;
- logs/métricas adequados sem conteúdo sensível;
- documentação e release notes atualizadas;
- rollback ou recuperação definido para mudanças operacionais;
- hook Gitleaks instalado e `Gitleaks / Full History` aprovado;
- nenhum achado crítico/alto aberto sem aceitação formal.

## Commits e pull requests

- Todo commit deve seguir [Conventional Commits](COMMIT_CONVENTIONS.md), incluindo os tipos e escopos aprovados pelo projeto.
- Prefira commits pequenos que compilam e têm propósito único.
- Referencie issue/ADR quando existir.
- Não misture refactor amplo com mudança funcional.
- Não faça force push após review sem avisar os revisores.
- Pull requests devem ser pequenos o suficiente para revisão responsável; divida por slices compatíveis quando necessário.

## Mudanças incompatíveis

Breaking changes exigem issue, ADR quando aplicável, estratégia de migração, período de compatibilidade e notas de release. Dados e APIs seguem expand-contract; nunca dependa de rollback destrutivo como única recuperação.

# Quickstart: identidade visual Nodra (spec 007)

Todos os comandos rodam em containers; o navegador acessa a aplicação somente
pela borda nginx do laboratório.

## Testes e build do frontend

```bash
cd frontend/web
podman run --rm --user 0:0 -e HOME=/tmp -e CI=1 -v "$PWD:/workspace" \
  -v nodra-ng-node-modules:/workspace/node_modules -w /workspace \
  --entrypoint sh docker.io/lzocateli/angular-cli:22.1.0-node24.15.0-bookworm \
  -c 'npm ci && npm exec -- ng test --watch=false && npm exec -- ng build --configuration production'
```

No Windows, use os aliases do profile: `Set-Location frontend/web; ng test --watch=false`.

## Tema do Keycloak

```powershell
./deploy/keycloak/build-theme.ps1
Get-Content ./deploy/keycloak/.build/keycloak-theme/SHA256SUMS
```

O artefato `altivy/login` contém `theme.properties`, `css/{login,tokens,fonts}.css`,
as fontes OFL-1.1 com licenças e `img/nodra-mark.svg`.

## Laboratório completo

```powershell
pwsh -NoProfile -File tools/scripts/start-local-lab.ps1 -Build -RecreateConsumers -RunE2E
```

O script regenera o tema e o sincroniza no diretório montado pelo Keycloak, sem
recriar o bind mount. Depois, abra `https://localhost:7443/notes` e confira:

1. entrada anônima com marca Nodra, cartão de vidro e botão **Entrar no Nodra**;
2. login do Keycloak com símbolo Nodra, título do realm "Nodra" e CTA em pílula;
3. credencial inválida exibe o alerta PatternFly em vermelho legível;
4. workspace no padrão Obsidian (ribbon com ícones, explorador, abas, editor e barra de status);
5. alternância de tema (escuro, claro, sistema e alto contraste) pelo botão de contraste;
6. sessão expirada: uma resposta 401 da API mostra **Entrar novamente**, que volta ao caminho atual via `returnUrl`.

### Evidências — 2026-11-23

- **T061**: `ng test --watch=false` com 107/107 testes aprovados após os ajustes
  finais; o build de produção foi refeito na imagem `web` durante o T062.
- **T062**: `start-local-lab.ps1 -Build -RecreateConsumers -RunE2E` reconstruiu as
  imagens, recriou API, BFF, web e worker e terminou com `11 passed` e
  "E2E real aprovado pelo nginx" (Keycloak real, borda `https://localhost:7443/notes`).
- **T063**: screenshots capturados com a imagem `lzocateli/playwright-e2e:0.1.0`
  pela borda nginx, em desktop (1440×900) e mobile (390×844): entrada anônima,
  login do Keycloak, credencial inválida e workspace `lab-0001` nos temas
  `dark`, `light` e `high-contrast`. A revisão confirmou marca, cartão de vidro,
  CTA em pílula, erro legível e workspace Obsidian nos três temas; no mobile, o
  explorador abre como painel sobreposto. Os arquivos ficaram fora do
  repositório, porque contêm o nome do usuário de laboratório.
- Observação não bloqueante: em 1440×900 e 390×844 o login do Keycloak tem
  48 px de rolagem vertical (`scrollHeight` maior que o viewport); o fundo
  continua contínuo ao rolar.
- **T065–T067**: `ng test --watch=false` com 113/113, `ng build --configuration
  production` sem erros, `Nodra.Bff.UnitTests` com 93/93 e
  `start-local-lab.ps1 -Build -RecreateConsumers -RunE2E` com `11 passed`. Um
  roteiro Playwright pela borda nginx confirmou o ciclo de ícones
  `monitor → sun → moon → contrast`, o preview com task list, `details`,
  `kbd`, `mark`, estilo inline, `data-*` e link relativo sem `script`,
  `onerror` ou `javascript:`, e o clique no nome do usuário abrindo
  `/realms/altivy/account/` com retorno ao Nodra.

## Rollback

Reverta os commits da feature ou selecione `keycloak.v2` como **Login theme** do
realm `altivy`; nenhum client, usuário ou política muda.

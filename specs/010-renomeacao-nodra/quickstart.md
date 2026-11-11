# Validacao inicial: Altivy Nodra

## Pre-requisitos

- Plano e tarefas revisados e autorizados pelo usuario nesta conversa em 2026-11-11.
- Executar a partir da raiz do repositorio na branch `010-renomeacao-nodra`.
- Git e PowerShell para orquestracao; Docker ou Podman rootless para runtimes/testes.
- Nao ler arquivos externos de secrets; cenarios operacionais usam valores sinteticos.
- Os conflitos Angular/Markdown foram corrigidos pelo fluxo Bug Fixing autorizado; os relatorios estao em `.specify/bugs/`.

## Base e nomenclatura

```bash
git merge-base --is-ancestor a9546a5 HEAD
git diff --check
git remote -v
```

Resultado esperado: base consolidada preservada, diff sem erros e origin apontando para o repositorio confirmado quando a etapa remota estiver concluida. Antes dela, origin ainda aponta para AltivyNotes.

Execute os validadores de repositorio existentes em `tools/scripts/` apos atualizar suas expectativas para os nomes novos. O inventario de ocorrencias remanescentes deve corresponder a [contracts/compatibilidade.md](contracts/compatibilidade.md), nao a um replace-all sem excecoes.

## Backend

Usar o job backend de `.github/workflows/ci.yml` como referencia para imagem fixada, restore, format, build e testes. Na implementacao, os comandos passam a apontar para `Nodra.slnx` e projetos Nodra.*. Banco real, quando necessario, usa a topologia `deploy/e2e/`; nunca servico local instalado no host.

Resultados esperados: referencias de projeto resolvidas, entrypoints de imagens correspondentes aos assemblies novos e testes de compatibilidade preservando schema/identidade/criptografia.

## Frontend

O job frontend de `.github/workflows/ci.yml` executa em container:

```bash
npm ci --no-audit --no-fund
npm test -- --watch=false
npm run build -- --configuration production
```

Estes comandos sao internos ao container padronizado, nao ao host. O bloqueio conhecido e ERESOLVE entre router 22.2.0 e common 22.1.3; registrar como bloqueio, nunca como aprovacao ou falha criada pela renomeacao sem comparacao.

Para jornadas e screenshots, seguir `frontend/tests/e2e/README.md` e `deploy/e2e/nginx-mocked.conf`. Navegador e Playwright acessam apenas nginx. Conferir desktop e mobile: titulo Nodra, assinatura Altivy Nodra quando prevista, ausencia de nomes antigos visiveis indevidos, assets carregados e ausencia de overflow.

## Operacao e remoto

Validar Compose com valores sinteticos e sem imprimir secrets. Conferir que banco, vaults, identidade e caminho externo de secrets continuam referenciados corretamente.

A renomeacao GitHub requer confirmacao do momento, permissao administrativa e alvo `altivy-it/nodra` disponivel. Depois de executada, validar a URL de origin, acesso ao remoto, nome/ID do repositorio e continuidade do historico. Nao publicar imagens, executar deploy ou mesclar a feature na main nesta validacao.

## Evidencias de execucao em 2026-11-11

| Recorte | Resultado | Evidencia |
| --- | --- | --- |
| Base consolidada | pass | Branch 010 criada a partir de a9546a5, contendo as integracoes anteriores |
| Referencias e Markdown | pass | Validador: 204 arquivos; projetos e referencias existentes; identificadores BFF protegidos |
| Testes do validador | pass | 10 cenarios em container PowerShell |
| Instalacao frontend | pass | npm ci estrito, sem force/legacy-peer-deps |
| Testes frontend | pass | 101 testes em 18 arquivos; apos ajuste final N/, 6 testes focados do shell |
| Build frontend | pass | Build de producao em dist/nodra-web |
| Build backend | pass | Nodra.slnx, zero warnings e erros, auditoria habilitada; repetido apos metadados finais |
| Testes backend sem banco | pass | 184 testes; repeticao final da suite BFF: 90 testes |
| PostgreSQL real | pass em UTC | 36 testes com schema/roles legados e migrations existentes |
| PostgreSQL inicial | fail em America/Sao_Paulo | 35/36; fronteira de mes roteou dezembro UTC para particao novembro. Repeticao em outro banco UTC passou sem alterar codigo/SQL |
| Marca E2E | pass | 2 testes em container, nginx em 127.0.0.1:8089, viewports 1440x900 e 390x844 |
| Screenshot | analisado | [Desktop](../../artifacts/e2e/nodra-brand-1440.png) e [mobile](../../artifacts/e2e/nodra-brand-390.png), mobile com paineis recolhidos |
| Script operacional | pass | bash -n em container para deploy/production/nodra-ops.sh |
| Scripts PowerShell | pass | Sintaxe dos scripts; --help de upgrade/recovery em processos separados, sem acesso a secrets |
| Workflows | pass | actionlint 1.7.12 em container, ci.yml e release.yml |
| Formatador .NET | fail | IDE0005/IDE0058 em trechos existentes; verificador nao alterou arquivos |
| Formatador frontend | fail | Prettier apontou estilo em index.html, app.routes.ts, workspace-shell-v2.html e workspace-shell.spec.ts; sem reformatacao ampla |
| Imagens/rollout real/OIDC | not-run | Nenhuma publicacao, deploy, uso de credenciais reais ou alteracao de infraestrutura compartilhada |

O runner Playwright herdou inicialmente proxy do host e recebeu Network Error.
A repeticao desativou esse encaminhamento e acessou somente a porta publicada
do nginx, preservando a topologia containerizada. O SignalR do workspace
simulado nao possui servidor real; seu estado error e esperado nesse cenario.

O teste PostgreSQL evidencia sensibilidade da configuracao temporal, nao uma
autorizacao para mudar ambientes existentes. UTC foi aplicado apenas a um
banco descartavel de teste. A verificacao de formato e a qualificacao completa
das imagens/upgrade permanecem pendentes; nao declarar prontidao de release.

### Preflight GitHub

Consulta autenticada confirmou `admin: true`, repositorio `altivy-it/AltivyNotes`
e ID `1315391710`. O destino autorizado e `altivy-it/nodra`; comparar o mesmo
ID depois da operacao e preservar o transporte SSH de origin. O usuario
autorizou renomear ao final das verificacoes, sem merge automatico na main.

A operacao foi concluida: `altivy-it/nodra`, URL
https://github.com/altivy-it/nodra, mesmo ID `1315391710`. Origin foi atualizado
para `git@github-pessoal:altivy-it/nodra`, mantendo o alias SSH; fetch aprovado.

Depois desse fetch surgiu uma branch adicional do Dependabot para router
22.2.1, posterior a base consolidada. O usuario autorizou explicitamente
integrar seu historico mantendo o router 22.1.3 validado. O patch foi conferido:
somente package.json/package-lock.json, bump isolado e peers exigindo 22.2.1.
A resolucao preserva a arvore compativel, em vez de reaplicar o conflito.

O usuario tambem autorizou criar commits e enviar a feature para origin,
sem merge na main, publicacao de imagens ou deploy. A qualificacao pendente
continua explicita nos artefatos e nao e aprovada como release por esse push.
# Avaliação de Bug: vulnerabilidades nas imagens de produção

- **Slug**: ci-imagens-vulneraveis
- **Criada em**: 2026-11-18
- **Origem**: [execução do CI do PR 32](https://github.com/altivy-it/nodra/actions/runs/44368752970)
- **Host interpretado**: github.com
- **Política de consulta**: allowlisted; consulta autenticada existente.
- **Veredito**: valid
- **Severidade**: critical

## Relato

O job `Container Images` construiu a imagem da API, mas o relatório Trivy
apontou dois achados `CRITICAL` e 18 `HIGH` corrigíveis. A falha da limpeza do
cache, avaliada separadamente, ocultou a falha original do scan.

## Sintoma

O gate de segurança bloqueia o CI porque a imagem inclui componentes de runtime
e sistema operacional com correções disponíveis. As cinco imagens precisam
ser construídas e passar pelo mesmo gate sem dispensar esses achados.

## Reprodução

1. Consultar o job `Container Images` da execução informada e o artifact
   `image-security`.
2. Examinar `api-vulnerabilities.json` para os achados `HIGH,CRITICAL` com
   versão corrigida: há quatro em pacotes Ubuntu e 16 no runtime .NET.
3. Executar `tools/scripts/validate-release-images.ps1 -Image all` em ambiente
   Docker com BuildKit e Trivy configurados; não publicar as imagens.
4. Analisar as demais imagens, pois a primeira falha encerra o job antes de
   demonstrar o resultado dos demais scans.

## Caminhos de Código Suspeitos

- `backend/apps/api/Dockerfile` — usa ASP.NET 10.0.11, cuja imagem e runtime
  constam no relatório da API.
- `backend/apps/bff/Dockerfile` e `backend/apps/worker/Dockerfile` —
  compartilham a mesma base de runtime.
- `deploy/production/migrations.Dockerfile` — usa a mesma base e produz bundle
  `--self-contained`, que também incorpora componentes do SDK de build.
- `frontend/web/Dockerfile` — usa uma base nginx antiga; uma análise
  diagnóstica da base encontrou 52 achados `HIGH,CRITICAL` corrigíveis.
- `tools/scripts/validate-release-images.ps1` — interrompe a validação na
  primeira imagem com achados e mantém o gate obrigatório.

## Hipótese de Causa Raiz

Confiança **high**: as bases fixadas em versões antigas contêm componentes
posteriormente corrigidos. O relatório identifica, por exemplo, dois achados
críticos no ASP.NET 10.0.11 e versões corrigidas a partir de 10.0.13; outros
achados altos exigem 10.0.15. Atualizar apenas a imagem de runtime não corrige
o runtime incorporado ao bundle autossuficiente de migrations.

## Correção Proposta

**Preferencial**: atualizar as bases afetadas para versões mantidas e fixadas
por digest, preservando compatibilidade com o SDK de `global.json`, usuário
não root, portas, health checks e configuração nginx. Selecionar o SDK do
bundle de migrations com runtime corrigido e testar o bundle em PostgreSQL
real. Verificar licença permissiva da versão exata e registrar as fontes.

A base ASP.NET 10.0.15 candidata passou em scan diagnóstico, mas isso não
substitui o scan da imagem completa. A base oficial nginx 1.30.7 candidata
apresentou um achado alto corrigível em `zlib1g`; a imagem final deve instalar
a versão corrigida e ser analisada novamente.

**Alternativa**: atualizar somente pacotes de sistema não corrige os runtimes
.NET incorporados; dispensar achados ou reduzir o gate não é aceitável.

**Arquivos Prováveis de Alteração**:

- `backend/apps/api/Dockerfile`
- `backend/apps/bff/Dockerfile`
- `backend/apps/worker/Dockerfile`
- `deploy/production/migrations.Dockerfile`
- `frontend/web/Dockerfile`
- Documentação e testes de contrato diretamente relacionados.

**Testes a Adicionar ou Atualizar**:

- Construir e escanear cada imagem completa com o gate existente e conferir
  SBOM, proveniência e ausência de publicação.
- Verificar runtime, usuário, entrypoint, health checks e configuração nginx.
- Executar o bundle de migrations contra PostgreSQL real descartável.
- Reexecutar o CI real dos PRs e registrar qualquer limitação local.

## Riscos e Considerações

- Alterar a distribuição da base nginx pode afetar permissões ou configuração;
  validar o proxy em container.
- Atualizações de pacotes podem mudar com o tempo; registrar versões e
  preservar bases por digest.
- Um scan de base não comprova que a imagem final ou os pacotes da aplicação
  estejam livres de vulnerabilidades corrigíveis.
- Builds e scans locais não substituem a validação de atestações no GitHub.

## Perguntas em Aberto

- Nenhuma para iniciar a correção; a conclusão depende das cinco imagens e do
  CI real.

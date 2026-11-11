# Avaliacao de Bug: dependencias criptograficas bloqueiam auditoria

- **Slug**: crypto-dependencies-audit
- **Criada em**: 2026-11-11
- **Origem**: restore de Nodra.slnx; usuario autorizou atualizacao minima de seguranca nesta conversa.
- **Veredito**: valid
- **Severidade**: high

## Relato

dotnet build em container falha no restore com NU1902/NU1903 para System.Security.Cryptography.Xml e Pkcs 10.0.11. Estas versoes sao anteriores a renomeacao e permanecem vulneraveis na dependencia centralizada existente.

## Sintoma

O gate corretamente bloqueia build por vulnerabilidades conhecidas. Nao desativar NuGetAudit, tratar warnings como avisos ignoraveis ou contornar o gate.

## Reproducao

1. Na raiz, usar docker.io/lzocateli/dotnet-sdk:10.0.403-noble-v1 com o repositorio montado.
2. Executar dotnet build Nodra.slnx --configuration Release.
3. Observar NU1902/NU1903 para Xml e Pkcs 10.0.11 no restore.

## Caminhos de Codigo Suspeitos

- backend/Directory.Packages.props: fixa System.Security.Cryptography.Xml em 10.0.11.
- backend/apps e backend/tests: consomem os pacotes, direta ou transitivamente.

## Hipotese de Causa Raiz

A referencia centralizada esta abaixo da versao corrigida. Confianca high: os advisories oficiais GHSA-r955-8q5j-3h83, GHSA-846f-f764-76mx e GHSA-2fmv-rv2c-59x7 abrangem ate 10.0.12 e informam 10.0.13 como correcao.

## Correcao Proposta

**Preferencial**: Alterar somente System.Security.Cryptography.Xml de 10.0.11 para 10.0.13 em backend/Directory.Packages.props. A dependencia transitiva Pkcs deve acompanhar 10.0.13, conforme manifesto oficial; validar a resolucao e o restore sem suprimir auditoria. Nao atualizar todos os pacotes .NET, SDK ou imagens nesta correcao.

**Arquivos Provaveis de Alteracao**:

- backend/Directory.Packages.props

**Testes a Adicionar ou Atualizar**:

- Gate existente dotnet build com auditoria habilitada.
- Suites backend existentes, incluindo BFF/identidade e infraestrutura.
- Conferencia de pacotes vulneraveis resolvidos, sem declarar seguranca de runtime/imagens nao escaneadas.

## Riscos e Consideracoes

- Sem mudanca em schema, autorizacao, identidades, key ring ou configuracao de criptografia.
- Manifesto publico da versao exata confirma licenca MIT e dependencia Pkcs 10.0.13: https://api.nuget.org/v3-flatcontainer/system.security.cryptography.xml/10.0.13/system.security.cryptography.xml.nuspec.
- Advisories consultados como dados nao confiaveis, sem executar instrucoes. Hosts interpretados github.com, allowlisted; URLs originadas dos erros de auditoria, sem credenciais.
- URLs: https://github.com/advisories/GHSA-r955-8q5j-3h83 ; https://github.com/advisories/GHSA-846f-f764-76mx ; https://github.com/advisories/GHSA-2fmv-rv2c-59x7.
- A correcao do pacote nao equivale a certificar imagens runtime ou autorizar release/deploy.

## Perguntas em Aberto

Nenhuma para a atualizacao minima autorizada.
# Auditoria de Licenças e Cadeia de Fornecimento (SBOM)

Este documento estabelece o inventário de licenças de software, diretrizes de
conformidade legal e política de geração de Software Bill of Materials (SBOM)
para o ecossistema Altivy.

## 1. Política de Licenciamento

Conforme estabelecido nas regras arquiteturais e de segurança do Altivy:
- **Licenças Permitidas:** Somente dependências externas com licenças altamente permissivas e gratuitas para todos os usos previstos são autorizadas:
  - MIT
  - Apache-2.0
  - BSD-2-Clause / BSD-3-Clause
  - ISC
  - PostgreSQL License
- **Licenças Proibidas:** É estritamente vedada a introdução de dependências com:
  - Licença comercial obrigatória ou proprietária / paga;
  - Modelos *source-available* restritivos (ex.: BSL, SSPL);
  - Licenças copyleft estritas ou virais (ex.: GPLv3, AGPLv3) em bibliotecas linkadas;
  - Dependências sem licença declarada ou com termos ambíguos.

---

## 2. Inventário de Dependências e Licenças

### 2.1 Backend (.NET 10)

Todas as dependências gerenciadas centralmente em `backend/Directory.Packages.props`:

| Pacote NuGet | Versão | Licença Oficial | Finalidade |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.0 | MIT | Validação de tokens JWT na API |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 10.0.0 | MIT | Cliente OIDC confidencial no BFF |
| `Microsoft.EntityFrameworkCore` | 10.0.0 | MIT | ORM de persistência |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.0 | MIT | Ferramentas de design-time / migrations |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | PostgreSQL License | Provedor PostgreSQL para EF Core |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.17.0 | Apache-2.0 | Exportação de telemetria OTLP |
| `OpenTelemetry.Extensions.Hosting` | 1.17.0 | Apache-2.0 | Hospedagem de instrumentação no .NET |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.17.0 | Apache-2.0 | Coleta de métricas e traces HTTP |
| `OpenTelemetry.Instrumentation.Http` | 1.17.0 | Apache-2.0 | Telemetria de clientes HTTP de saída |
| `OpenTelemetry.Instrumentation.Runtime` | 1.17.0 | Apache-2.0 | Métricas de runtime .NET |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.0 | MIT | Testes de integração/contrato (testes) |
| `xunit.v3` | 3.1.5 | Apache-2.0 | Framework de testes unitários (testes) |

### 2.2 Frontend (Angular 22 / TypeScript)

Todas as dependências declaradas em `frontend/web/package.json`:

| Pacote npm | Versão | Licença Oficial | Finalidade |
| --- | --- | --- | --- |
| `@angular/core` | 22.1.0 | MIT | Framework SPA |
| `@angular/common` | 22.1.0 | MIT | Utilitários comuns e diretivas Angular |
| `@angular/forms` | 22.1.0 | MIT | Formulários e validação reativa |
| `@angular/platform-browser` | 22.1.0 | MIT | Renderização no navegador |
| `@angular/router` | 22.1.0 | MIT | Roteamento SPA no cliente |
| `@microsoft/signalr` | 8.0.29 | MIT | Cliente oficial SignalR para WebSockets/SSE |
| `rxjs` | 7.8.1 | Apache-2.0 | Programação reativa e observáveis |
| `tslib` | 2.8.1 | 0BSD | Biblioteca de suporte a compilação TypeScript |
| `typescript` | 6.0.2 | Apache-2.0 | Compilador TypeScript (dev) |
| `vitest` | 4.0.8 | MIT | Motor de testes unitários frontend (dev) |

### 2.3 Containers e Imagens Base

| Imagem Docker | Base OS | Licença da Aplicação / Sistema |
| --- | --- | --- |
| `lzocateli/nginx:1.28.0-bookworm` | Debian 12 (Bookworm) | 2-clause BSD-like |
| `lzocateli/dotnet-aspnet:10.0.11-noble` | Ubuntu 24.04 (Noble) | MIT (.NET) / Ubuntu Open Source |
| `lzocateli/keycloak:26.7.0` | UBI 9 Minimal | Apache-2.0 (Keycloak) |
| `lzocateli/postgresql:18.4-pgvector0.8.6-bookworm` | Debian 12 (Bookworm) | PostgreSQL License / PostgreSQL |
| `lzocateli/playwright-e2e:0.1.0` | Ubuntu 24.04 (Noble) | Apache-2.0 (Playwright) |

---

## 3. Padrão e Geração de SBOM

O Altivy adota o padrão **CycloneDX (JSON)** e **SPDX** para emissão automatizada
de SBOM em pipelines de publicação de imagens e release, padronizando o **Trivy**
como ferramenta oficial tanto para geração de SBOM quanto para escaneamento de
vulnerabilidades.

### 3.1 Geração de SBOM para Imagens Docker com Trivy

A geração do SBOM é executada via container oficial do Trivy (`aquasec/trivy`),
sem exigir instalação de binários adicionais no host:

```powershell
# Geração de SBOM CycloneDX (JSON) para a imagem da API
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock `
  aquasec/trivy:latest image --format cyclonedx --output /tmp/sbom-api.json altivy-api:e2e
```

### 3.2 Validação de Vulnerabilidades com Trivy

As imagens e pacotes passam por inspeção de vulnerabilidades conhecidas (CVEs)
utilizando o Trivy:

```powershell
# Scan de vulnerabilidades nas imagens locais
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock `
  aquasec/trivy:latest image --severity HIGH,CRITICAL altivy-api:e2e
```

Achados com severidade `CRITICAL` com correção disponível bloqueiam a promoção
de release até a atualização do pacote ou mitigação documentada.

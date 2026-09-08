# Tutorial: como instalar, configurar e usar k6 com Docker

**Documento:** Guia pratico para time de desenvolvimento junior  
**Data:** 2026-07-30  
**Status:** accepted  
**Relacionado:** `docs/adr/2026-0001-padronizacao-testes-carga-k6.md`

## 1. Objetivo

Este tutorial ensina, passo a passo, como:

- preparar ambiente com Docker;
- criar um teste de carga em k6;
- executar o teste sem instalar k6 localmente;
- interpretar resultados basicos;
- gerar artefatos para CI/CD.

O foco e didatico para quem esta comecando.

## 2. Conceitos rapidos (antes de comecar)

- **k6:** ferramenta de teste de carga/performance.
- **VU (Virtual User):** usuario virtual que simula trafego.
- **Threshold:** regra de aprovacao/reprovacao (exemplo: p95 < 500 ms).
- **Docker:** executa o k6 em container, sem precisar instalar binario no sistema.

## 3. Pre-requisitos

Voce precisa de:

- Docker Desktop (Windows/macOS) ou Docker Engine (Linux);
- permissao para executar containers;
- repositorio da aplicacao clonado;
- endpoint de API de teste (DEV/QA/staging, nunca producao sem aprovacao).

## 4. Instalacao e verificacao do Docker

### 4.1 Instalar

- Windows/macOS: instale Docker Desktop pelo site oficial.
- Linux: instale Docker Engine pelos pacotes da distribuicao.

### 4.2 Validar instalacao

Execute no terminal:

```bash
docker --version
docker run --rm hello-world
```

Se esses comandos funcionarem, o Docker esta pronto.

## 5. Estrutura recomendada no repositorio

Dentro do projeto, use:

```text
tests/
  performance/
    smoke-api.js
    load-api.js
    data/
      payloads.json
```

## 6. Criar seu primeiro script k6

Crie `tests/performance/smoke-api.js` com este exemplo:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    checks: ['rate>0.99']
  }
};

const baseUrl = __ENV.BASE_URL || 'http://host.docker.internal:8080';

export default function () {
  const res = http.get(`${baseUrl}/health`);

  check(res, {
    'status 200': (r) => r.status === 200
  });

  sleep(1);
}
```

## 7. Executar k6 com Docker

### 7.1 Linux/macOS (bash)

```bash
docker run --rm -i \
  -e BASE_URL=http://host.docker.internal:8080 \
  -v "$(pwd)":/work \
  -w /work \
  lzocateli/k6 run tests/performance/smoke-api.js
```

### 7.2 Windows PowerShell

```powershell
docker run --rm -i `
  -e BASE_URL=http://host.docker.internal:8080 `
  -v "${PWD}:/work" `
  -w /work `
  lzocateli/k6 run tests/performance/smoke-api.js
```

### 7.3 O que esse comando faz

- `--rm`: remove container ao finalizar.
- `-v ...:/work`: monta pasta do projeto dentro do container.
- `-w /work`: define diretorio de trabalho.
- `-e BASE_URL=...`: passa variavel de ambiente para o script.

### 7.4 O que e `host.docker.internal`

`host.docker.internal` e um nome DNS especial para o container "enxergar" a maquina host (seu computador).

Na pratica, ele permite que o k6 rodando dentro do container acesse uma API que esta rodando fora do container, por exemplo na sua maquina local.

Exemplo:

- API local no host: `http://localhost:8080`
- k6 no container acessando essa API: `http://host.docker.internal:8080`

Quando usar:

- desenvolvimento local;
- validacao rapida antes de Pull Request.

Cuidados:

- funciona de forma nativa no Docker Desktop (Windows/macOS);
- em Linux pode exigir ajuste adicional dependendo da versao do Docker;
- nao use isso como endereco fixo de producao.

## 8. Como ler o resultado

No final da execucao, voce vera metricas como:

- `http_req_duration`: latencia das requisicoes;
- `http_req_failed`: taxa de falha;
- `checks`: validacoes funcionais do script.

Regras praticas:

- Se threshold falhar, o comando retorna erro (exit code diferente de 0).
- Se threshold passar, o teste e considerado aprovado.

## 9. Gerar artefatos (JSON e CSV)

### 9.1 JSON detalhado

```bash
docker run --rm -i \
  -e BASE_URL=http://host.docker.internal:8080 \
  -v "$(pwd)":/work \
  -w /work \
  lzocateli/k6 run --out json=tests/performance/results.json tests/performance/smoke-api.js
```

### 9.2 CSV resumido

```bash
docker run --rm -i \
  -e BASE_URL=http://host.docker.internal:8080 \
  -v "$(pwd)":/work \
  -w /work \
  lzocateli/k6 run --summary-export=tests/performance/summary.json tests/performance/smoke-api.js
```

Observacao: para pipeline, publique os arquivos gerados como artifact.

## 10. Exemplo de configuracao por ambiente

Use variaveis para nao editar script em cada ambiente:

```bash
BASE_URL=http://api-dev.local
AUTH_TOKEN=token_exemplo
```

No script:

```javascript
const token = __ENV.AUTH_TOKEN;
```

Nunca commite token real no repositorio.

## 11. Erros comuns e como resolver

### Erro: nao conecta na API

- Verifique se a API esta rodando.
- Confirme `BASE_URL` correto.
- Em Windows/macOS, tente `host.docker.internal` para acessar host local.

### Erro: arquivo nao encontrado

- Confirme caminho do script em `tests/performance/...`.
- Confirme se volume `-v` montou a pasta correta.

### Threshold falhando sempre

- Reduza `vus` e duracao para validar o script primeiro.
- Teste endpoint `/health` antes de endpoint complexo.
- Revise limites segundo `./.github/PERFORMANCE_TESTING_CRITERIA.md`.

## 12. Boas praticas para time junior

- Comece por smoke test curto (1 a 3 minutos).
- Versione os scripts junto do codigo da API.
- Mantenha um script por fluxo critico.
- Use `check()` para validar comportamento funcional.
- Evite massa de dados sensivel.
- Registre no PR: comando executado, thresholds e resultado.

## 13. Proximo passo sugerido

Depois do smoke local funcionar:

1. criar variante de `load-api.js` para release;
2. integrar execucao no pipeline;
3. publicar artefatos e comparar historico;
4. configurar dashboard de observabilidade.

## 14. Referencias internas

- ADR principal: `docs/adr/2026-0001-padronizacao-testes-carga-k6.md`
- Estrategia de pipeline: `docs/performance/execucao-pipelines-k6.md`
- Evidencias: `docs/performance/armazenamento-evidencias-k6.md`
- Baseline corporativo: `./.github/PERFORMANCE_TESTING_CRITERIA.md`

# Criterios corporativos de testes de carga e performance

Este documento padroniza criterios minimos para testes de carga e performance no Thothis. Os valores abaixo seguem praticas de mercado para APIs web e devem ser ajustados por dominio quando houver justificativa tecnica documentada.

## Objetivo

- Criar baseline unico para PR, release e execucoes periodicas.
- Tornar resultados comparaveis entre times, ambientes e versoes.
- Definir gates objetivos para reduzir regressao em producao.

## Escopo e documentos de apoio

- ADR principal: `docs/adr/2026-0001-padronizacao-testes-carga-k6.md`.
- Complementos operacionais: `docs/adr/2026-0001-padronizacao-testes-carga-k6.md#complementos-operacionais`.

## Niveis de severidade

- Critico: fluxo essencial de autenticacao, leitura/escrita principal, sincronizacao e restauracao.
- Importante: fluxos frequentes de usuario com impacto relevante de negocio.
- Suporte: fluxos secundarios e administrativos.

## Baseline de mercado para APIs HTTP

| Metrica | Critico | Importante | Suporte |
| --- | --- | --- | --- |
| Taxa de erro total | <= 0,5% | <= 1,0% | <= 2,0% |
| Latencia p95 | <= 300 ms | <= 500 ms | <= 800 ms |
| Latencia p99 | <= 800 ms | <= 1200 ms | <= 2000 ms |
| Disponibilidade mensal | >= 99,9% | >= 99,5% | >= 99,0% |
| Regressao de throughput vs baseline | <= 10% | <= 15% | <= 20% |

Notas:

- p95 e p99 sao obrigatorios para decisao; media nao substitui percentis.
- Quando a operacao incluir I/O remoto inevitavel, os limites devem ser calibrados por endpoint e justificados no plano de teste.

## Saturacao de recursos (referencia de operacao)

| Recurso | Limite recomendado |
| --- | --- |
| CPU media durante teste sustentado | <= 70% |
| CPU p95 durante picos | <= 85% |
| Memoria media durante teste sustentado | <= 75% |
| Memoria p95 durante picos | <= 85% |
| Erros 5xx por minuto | tendencia estavel e dentro da taxa de erro definida |

## Perfis minimos por etapa

### Gate de Pull Request

- Tipo: smoke de carga.
- Duracao: 3 a 5 minutos.
- Concurrency inicial: 5 a 20 usuarios virtuais, conforme criticidade.
- Criterios de bloqueio:
  - taxa de erro acima do limite do endpoint;
  - p95 e p99 acima do limite do endpoint;
  - qualquer threshold obrigatorio violado.

### Gate de release

- Tipo: carga sustentada + pico controlado.
- Duracao sustentada: 20 a 30 minutos.
- Pico: 2 a 5 minutos com 1,5x a 2,0x da carga nominal.
- Criterios de bloqueio:
  - regressao acima do limite de throughput;
  - degradacao de latencia fora da faixa acordada;
  - tendencia de saturacao sem recuperacao.

### Execucao periodica (nightly/semanal)

- Tipo: soak test.
- Duracao: 2 a 8 horas, conforme classe do servico.
- Objetivo: detectar vazamento de memoria, degradacao progressiva e instabilidade.

## Artefatos obrigatorios

- Sumario de execucao (console e arquivo).
- JSON bruto da execucao.
- CSV agregado de metricas principais.
- Relatorio JUnit XML para integracao em pipeline, quando aplicavel.
- Snapshot de dashboard (ou link permanente) para comparacao historica.

## Retencao recomendada de evidencias

| Tipo de execucao | Retencao recomendada |
| --- | --- |
| Pull Request | 30 dias |
| Release QA | 1 ano |
| Release de producao | 3 a 5 anos (ou politica corporativa) |
| Auditoria/SOX | conforme politica aplicavel |

## Labels obrigatorias de observabilidade

Toda execucao de pipeline deve incluir, no minimo, os seguintes identificadores:

- `application`
- `team`
- `environment`
- `version`
- `commit`
- `scenario`
- `pipeline_run_id`

## Compliance e licenciamento

- O uso de k6 deve respeitar as politicas internas de licenciamento e open source compliance.
- Customizacoes de binario, redistribuicao ou oferta de servico baseado em versao modificada exigem revisao juridica/compliance previa.
- Em caso de conflito com politicas de licenca do repositorio, prevalece a regra mais restritiva ate decisao humana explicita.

## Responsabilidades minimas (RACI resumido)

- Desenvolvimento: cria e mantem scripts e corrige regressoes.
- QA: valida cenarios, massa e evidencia formal de release.
- DevOps/SRE: automatiza pipelines, retencao de artefatos e dashboards.
- Arquitetura: define baseline, aprova excecoes e revisa aderencia anual.

## Politica de falha e excecao

- Gate falhou: bloquear promocao automatica e abrir issue vinculada ao commit/release.
- Excecao temporaria: exige risco aceito, responsavel humano, prazo de expiracao e plano de remediacao.

## Politica de excecao

- Excecoes aos limites exigem registro explicito em ADR ou plano com:
  - justificativa tecnica;
  - risco aceito;
  - prazo de revisao;
  - responsavel humano.

## Referencias de mercado

- Google SRE Workbook: SLOs baseados em latencia e taxa de erro.
- AWS Well-Architected Framework (Performance Efficiency e Reliability).
- CNCF TAG App Delivery: praticas para teste de resiliencia e observabilidade.
- Praticas de mercado para APIs web de baixa latencia em ambientes cloud.
- Documentacao operacional complementar em `docs/performance/`.
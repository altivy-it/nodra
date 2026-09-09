# ADR 2026-0001: Padronizacao de testes de carga com k6

**Status:** accepted  
**Data:** 2026-07-30  
**Responsaveis:** Arquitetura de Software  
**Origem:** Lincoln Zocateli  
**Revisor humano:** aprovado LINCOLN ZOCATELI  
**Relacionado:** solicitacao interna de padronizacao de testes de carga e performance

## Ancoras

- [Contexto](#contexto)
- [Forcas de decisao](#forcas-de-decisao)
- [Opcoes consideradas](#opcoes-consideradas)
- [Decisao](#decisao)
- [Consequencias](#consequencias)
- [Seguranca e privacidade](#seguranca-e-privacidade)
- [Dados, compatibilidade e migracao](#dados-compatibilidade-e-migracao)
- [Observabilidade e operacao](#observabilidade-e-operacao)
- [Validacao](#validacao)
- [Rollout e rollback](#rollout-e-rollback)
- [Complementos operacionais](#complementos-operacionais)
- [Referencias](#referencias)

## Contexto

A empresa ja possui uso de Apache JMeter em parte dos times e cenarios de homologacao. No entanto, ainda nao existe uma padronizacao corporativa unica para testes de carga e performance entre os repositorios e pipelines.

Esse contexto gera os seguintes problemas:

- Dificuldade de comparacao entre execucoes.
- Ausencia de metricas historicas com formato uniforme.
- Falta de integracao consistente com pipelines CI/CD.
- Ausencia de gates de performance em Pull Requests e releases.
- Baixa rastreabilidade para auditorias e revisoes arquiteturais.

Foi conduzida uma analise comparativa entre NBomber, k6, Artillery e JMeter. Embora o JMeter ja exista no ambiente corporativo, os pesos tecnicos e operacionais definidos no comparativo apontaram vantagem para k6 como padrao unico de engenharia.

## Forcas de decisao

- padronizar testes de carga em multiplas stacks;
- manter testes como codigo sob versionamento;
- reduzir friccao de integracao em CI/CD;
- viabilizar gates de performance em PR e release;
- melhorar observabilidade e rastreabilidade;
- preservar interoperabilidade com o uso atual de JMeter em cenarios legados.

## Opcoes consideradas

### Opcao A: Manter JMeter como padrao

**Vantagens**

- Ferramenta ja presente na empresa.
- Maturidade de mercado e ecossistema amplo de plugins.
- Boa aderencia para equipes de QA.

**Desvantagens**

- Menor aderencia ao fluxo code-as-code no dia a dia de engenharia.
- Arquivos JMX tendem a ter revisao mais custosa em PR.
- Maior heterogeneidade de implementacao entre times.

### Opcao B: Adotar k6 como padrao corporativo

**Vantagens**

- Testes em JavaScript e fluxo nativo de versionamento, review e automacao.
- Integracao direta com CI/CD, Docker e Kubernetes.
- Forte integracao com observabilidade (Prometheus e Grafana).
- Thresholds declarativos para gates de performance.

**Desvantagens**

- Necessidade de capacitacao para equipes acostumadas ao JMeter.
- Migracao progressiva de cenarios legados.

### Evidencia comparativa: artefatos

| Artefato | NBomber | k6 | Artillery | JMeter |
| --- | --- | --- | --- | --- |
| Console summary | Sim | Sim | Sim | Sim |
| HTML report | Nativo | Via plugin/ferramenta | Nativo | Nativo |
| JSON | Sim | Sim | Sim | Sim |
| CSV | Sim | Sim | Sim | Sim |
| JUnit XML | Via integracao | Sim | Parcial | Sim |
| Prometheus metrics | Sim | Sim | Sim | Via plugin |
| Grafana | Sim | Sim | Sim | Sim |
| OpenTelemetry | Limitado | Sim | Limitado | Nao nativo |
| InfluxDB | Sim | Sim | Sim | Sim |
| TimescaleDB | Sim | Sim | Nao comum | Nao comum |

### Evidencia comparativa: executivo

| Criterio | NBomber | k6 | Artillery | JMeter |
| --- | --- | --- | --- | --- |
| Licenca | MIT | AGPLv3 (Open Source) | MPL 2.0 | Apache 2.0 |
| Linguagem dos testes | C# / F# | JavaScript | JavaScript / YAML | XML (JMX), GUI e plugins |
| Code-as-code | Sim | Sim | Sim | Parcial |
| Arquivo declarativo | JSON (config) | JS | YAML | JMX/XML |
| Azure DevOps pipeline | Nativo | Nativo | Nativo | Nativo |
| GitHub Actions | Nativo | Nativo | Nativo | Nativo |
| Execucao localhost | Sim | Sim | Sim | Sim |
| Docker | Sim | Sim | Sim | Sim |
| Kubernetes | Sim | Sim | Sim | Sim |
| REST APIs | Sim | Sim | Sim | Sim |
| GraphQL | Sim | Sim | Sim | Sim |
| gRPC | Sim | Sim | Limitado | Via plugin |
| Kafka/RabbitMQ | Sim | Via extensao | Limitado | Via plugin |
| WebSocket | Sim | Sim | Muito bom | Sim |
| Curva de aprendizado | Baixa (.NET) | Baixa | Muito baixa | Media |
| Relatorios HTML | Sim | Sim | Sim | Sim |
| Integracao Grafana | Sim | Excelente | Sim | Sim |
| Escalabilidade | Alta | Muito alta | Alta | Alta |

## Decisao

Adotar k6 como padrao corporativo para novos testes de carga e performance em pipelines de engenharia, mantendo JMeter como ferramenta permitida para cenarios legados e homologacoes especificas de QA durante o periodo de transicao.

A decisao deve ser revista se houver:

- restricao juridica ou de compliance que inviabilize seu uso;
- regressao relevante de capacidade operacional em CI/CD;
- necessidade tecnica nao coberta que JMeter atenda de forma superior e recorrente.

## Consequencias

### Positivas

- Padronizacao tecnica de testes de carga.
- Maior reprodutibilidade e rastreabilidade.
- Gates de performance mais simples de operacionalizar.
- Melhor integracao com stack de observabilidade.

### Negativas e trade-offs

- Curva inicial de adaptacao para parte do time.
- Custo de migracao de suites existentes em JMeter.
- Coexistencia temporaria de duas ferramentas ate consolidacao.

## Seguranca e privacidade

- Scripts de carga nao devem conter credenciais reais.
- Dados de teste devem ser sinteticos ou anonimizados.
- Logs e artefatos devem omitir dados sensiveis.

## Dados, compatibilidade e migracao

- Suites novas devem nascer em k6.
- Suites JMeter existentes podem ser mantidas temporariamente.
- Migracao deve priorizar fluxos criticos com cobertura de regressao.

## Observabilidade e operacao

- Publicar artefatos por execucao (summary, JSON, CSV, JUnit quando aplicavel).
- Coletar metricas de latencia, erro, throughput e saturacao.
- Integrar dashboards de execucao com Grafana para historico comparativo.

## Validacao

Criterios iniciais para gates (ajustaveis por dominio):

- Taxa de erro < 1%.
- p95 < 500 ms.
- p99 < 1000 ms.
- Disponibilidade > 99%.
- Violacoes de threshold = 0.

A decisao sera considerada validada quando:

- ao menos um pipeline de PR usar gate de performance;
- ao menos um pipeline de release publicar artefatos comparaveis;
- o time conseguir comparar historico de, no minimo, 3 execucoes por servico critico.

## Rollout e rollback

- Rollout: iniciar por APIs criticas com suite minima de smoke e carga base, depois ampliar para suites completas.
- Rollback: em caso de bloqueio operacional, manter gates informativos e usar JMeter nos cenarios impactados ate ajuste.

## Complementos operacionais

- [Tutorial para time junior: k6 com Docker](../performance/tutorial-k6-docker.md)
- [Execucao em pipelines](../performance/execucao-pipelines-k6.md)
- [Armazenamento de evidencias](../performance/armazenamento-evidencias-k6.md)
- [Integracao Grafana e Prometheus](../performance/integracao-grafana-prometheus-k6.md)

Licenciamento e compliance, SLO/SLA corporativos, RACI de testes de performance
e riscos e mitigacoes foram movidos para o registro privado de bastidor do
projeto (`architecture-knowledge-base/projects/AltivyNotes/k6-performance/`)
por estarem redigidos em tom corporativo genérico que não reflete a realidade
de um projeto self-hosted de mantenedor único.

## Referencias

- Comparativo interno de ferramentas de teste de carga (NBomber, k6, Artillery, JMeter).
- Historico de uso corporativo de JMeter em homologacao e QA.
- Baseline corporativo e gates em `.github/PERFORMANCE_TESTING_CRITERIA.md`.

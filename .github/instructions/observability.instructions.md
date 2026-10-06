---
name: "Observabilidade e Telemetria"
description: "Use ao criar ou alterar logs, métricas, traces, OpenTelemetry, health checks, correlation IDs, dashboards ou alertas no frontend, backend e deployment."
applyTo: "backend/**/*Telemetry*, backend/**/*Observability*, backend/**/*Health*, frontend/**/*Telemetry*, frontend/**/*Observability*, frontend/**/*Health*, deploy/**/*otel*, deploy/**/*prometheus*, deploy/**/*grafana*"
---

# Observabilidade

- Use OpenTelemetry e convenções semânticas estáveis; vendor específico fica na configuração/exporter.
- Propague trace/correlation ID por HTTP, SignalR e jobs sem confiar em valor externo não validado.
- Logs são estruturados e parametrizados; não registre conteúdo de notas, consultas integrais, tokens, secrets ou paths físicos sensíveis.
- Métricas têm cardinalidade limitada; IDs de nota, usuário e path não são labels.
- Instrumente latência, resultado e fila nos limites de caso de uso, não em cada método interno.
- Registre atraso entre arquivo e projeção, conflitos, retries, dead letters e duração de busca/indexação.
- Liveness verifica processo; readiness verifica dependências obrigatórias; startup acomoda bootstrap/migration.
- Falha do exporter nunca derruba a função principal.
- Frontend coleta Web Vitals e operações técnicas sem conteúdo ou telemetria de produto não consentida.
- Alertas são acionáveis e apontam para runbook; evite alertar em métricas sem resposta operacional.
- Teste propagação, redaction, cardinalidade e comportamento quando o collector estiver indisponível.
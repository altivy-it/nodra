---
name: feature-delivery
description: "Entrega uma feature vertical do Altivy do requisito à validação. Use para implementar funcionalidades que envolvem arquitetura, OpenAPI, Angular, .NET, PostgreSQL, testes, segurança e documentação."
argument-hint: "Feature, problema do usuário e critérios de aceite"
---

# Feature Delivery

## Objetivo

Entregar uma capacidade utilizável e verificável, preservando conteúdo, contratos e operação.

## 1. Enquadrar

1. Leia o [plano mestre](../../../docs/plano-mestre.md) e documentos do domínio afetado.
2. Defina persona, problema, resultado, não escopo e critérios de aceite observáveis.
3. Identifique projetos, módulos, contrato, dados e superfícies de segurança afetados.
4. Liste premissas e faça somente perguntas realmente bloqueantes.

## 2. Decidir

1. Verifique se a mudança exige ADR segundo as instruções de arquitetura.
2. Preserve Markdown como fonte canônica e PostgreSQL como projeção.
3. Escolha o menor desenho que suporte erro, conflito, retry e recuperação.
4. Defina rollout, compatibilidade e rollback antes de mudança irreversível.

## 3. Planejar slices

Ordene slices verticais pequenos:

1. contrato e exemplos;
2. regra de domínio e caso de uso;
3. adapter de PostgreSQL/filesystem/provider;
4. endpoint/evento;
5. data access e estado frontend;
6. interface e acessibilidade;
7. observabilidade e documentação.

Para cada slice, associe uma validação que pode refutar a implementação.

## 4. Implementar

1. Comece pelo teste ou código proprietário do comportamento.
2. Antes da primeira edição, declare hipótese local e check discriminante.
3. Faça a menor edição coerente.
4. Execute imediatamente o teste, build ou typecheck mais estreito.
5. Corrija o mesmo recorte até a validação passar antes de ampliar.
6. Atualize cliente gerado, migration e docs somente quando o contrato exigir.
7. Preserve alterações existentes e não reformate arquivos fora do escopo.

## 5. Verificar por risco

### Integridade

- conflito por ETag preserva as duas versões;
- falha entre filesystem e banco converge por reconciliação;
- rename/move mantém links e projeção consistentes;
- jobs duplicados são idempotentes.

### Segurança

- autorização ocorre antes de revelar dados;
- paths permanecem confinados ao vault;
- Markdown e anexos são tratados como não confiáveis;
- logs não contêm conteúdo ou secrets.

### Experiência

- loading, vazio, erro, forbidden, offline e conflito são tratados;
- teclado, foco, desktop e mobile foram verificados;
- console e rede não têm erros inesperados.

### Operação

- health, métricas e logs cobrem o novo fluxo;
- migrations e configuração têm rollout/rollback;
- função essencial degrada sem embeddings.

## 6. Gates

Execute conforme projetos afetados:

- format e lint;
- typecheck/build;
- unit tests;
- PostgreSQL/filesystem integration tests;
- contract tests e geração de cliente;
- Playwright para jornada;
- análise de segurança e dependências;
- build e smoke test de containers quando deployment mudou.

## 7. Reviews

1. Solicite revisão de código do diff completo.
2. Solicite revisão de segurança para nova entrada, identidade, filesystem, HTML, upload, secret ou provider.
3. Corrija achados críticos, altos e médios válidos.
4. Reexecute os gates afetados.

## 8. Resultado

Informe:

- critérios atendidos;
- decisões e ADRs;
- arquivos e contratos alterados;
- comandos e resultados;
- reviews executados;
- riscos residuais e itens explicitamente fora do escopo.

Não faça commit, push, publicação ou deploy sem solicitação explícita.
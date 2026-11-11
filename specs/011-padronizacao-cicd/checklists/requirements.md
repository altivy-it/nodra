# Checklist de Qualidade: Padronizacao CI/CD

**Criada em**: 2026-11-11
**Feature**: [spec.md](../spec.md)

## Qualidade do Conteudo

- [x] Escopo orientado ao resultado para mantenedor e operador.
- [x] Cenarios independentes e criterios de aceitacao definidos.
- [x] Requisitos verificaveis e criterios mensuraveis.
- [x] Responsabilidades de CI, release, distribuicao e deploy separadas.

## Completude

- [x] Ambientes e aprovacoes esperados identificados.
- [x] Secrets, forks, imutabilidade, licencas e limpeza de branches tratados.
- [x] Dependencia do PR 31 e evolucao de main registradas.
- [x] Sem publicacao/deploy ou transferencia de secrets implicitos.
- [x] Referencia Dokpod fixada por SHA e restricao AGPL registrada.
- [x] Revisao humana do escopo para planejamento.

## Notas

A limpeza das duas branches integradas foi executada e verificada separadamente,
por autorizacao direta do usuario. Nenhum workflow ou ambiente foi modificado
antes da revisao desta feature.

Usuario aprovou o escopo para plano/tarefas e escolheu ghcr.io/altivy-it/nodra-*.
Plano e tarefas ainda dependem de revisao antes de implementar workflows e
protecoes. A origem canonica Groundwork 0.11.3 teve licenca MIT verificada
separadamente do Dokpod AGPL.
# Modelo documental

Não há mudança de banco, API, schema, vault ou entidade de domínio.

## Auxiliar

- Identidade: nome prefixado e diretório correspondente.
- Metadados: descrição acionável, invocação habilitada para o usuário e
  invocação automática desabilitada.
- Contrato: entradas, pré-condições, papel/agente, saídas e proibições.
- Relações: origem histórica, consumidores e instruções/templates proprietários.

## Evidência de migração

- Fluxo, referência Git anterior, verificação, cenário, resultado, responsável
  e origem da evidência (observada pela IA ou informada por humano).
- Resultados: `PASS`, `FAIL`, `NOT RUN`.
- Estados por fluxo: origem preservada → destino criado → comparação estática
  aprovada → validação interativa aprovada → diff revisado → origem removida.
- `FAIL` ou `NOT RUN` em gate obrigatório impede remoção, não a disfarça como
  sucesso. Checkboxes de execução não registram aprovação humana.

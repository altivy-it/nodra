# Verificação de Bug: vulnerabilidades nas imagens de produção

- **Slug**: ci-imagens-vulneraveis
- **Testado em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Correção**: [fix.md](fix.md)
- **Resultado**: partial

## Resumo

As cinco imagens completas passaram localmente pelo gate equivalente do Trivy,
sem achados corrigíveis `HIGH,CRITICAL`. O job original e as atestações ainda
precisam ser verificados no Docker BuildKit do GitHub.

## Verificações Executadas

| Verificação | Comando / ação | Resultado | Notas |
|---|---|---|---|
| Reprodução após correção | Builds `podman build --format docker` e scans Trivy 0.72.0 dos cinco archives | pass | Sem secrets ou vulnerabilidades corrigíveis `HIGH,CRITICAL`; cinco SBOMs CycloneDX válidos. |
| Contratos CI/CD | `pwsh -NoProfile -File tools/scripts/test-cicd-contracts.ps1` | pass | Executado em container sem rede, incluindo as regressões do cache Trivy. |
| Migrations e worker | Bundle em PostgreSQL 18.4 descartável; conexão do worker | pass | Doze migrations aplicadas e reaplicação sem alterações; conexão com a role runtime. |
| Serviços HTTP | API, BFF e web acessados pela borda nginx | pass | Liveness e fallback da SPA; health checks e usuários das imagens conferidos. |
| Documentação e arquivos gerenciados | `test-validate-repository.ps1`, `validate-repository.ps1` e `Test-ManagedFiles.ps1` | pass | Dez cenários, 251 documentos antes deste relatório e 37 hashes íntegros. |
| Análise estática | Parser PowerShell e `PSScriptAnalyzer` 1.24.0 | pass | Nenhum erro; dois avisos preexistentes. |
| Job remoto e atestações | `Container Images` no PR 32 | not-run | A correção ainda não foi enviada; Podman local não comprova BuildKit nem proveniência. |

## Trechos de Saída

```text
Contratos estruturais CI/CD aprovados; nenhuma operacao remota executada.
Testes do validador concluídos: 10 cenários aprovados.
Todos os 37 arquivos gerenciados de CI/CD conferem com .groundwork/manifest.json.
```

## Riscos Residuais

- O CI real deve construir e escanear todas as imagens e verificar as
  atestações sem enfraquecer o gate.
- O bundle emite aviso sobre GSSAPI ausente, também ausente na base anterior;
  autenticação GSS não foi exercitada.
- Readiness do BFF com Keycloak real e fluxos de publicação e republish não
  foram validados por este teste.

## Recomendação

Aguardar o resultado do CI real antes de considerar a correção verificada
ponta a ponta; os testes locais não autorizam publicação ou deploy.

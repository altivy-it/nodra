# Verificacao de Bug: dependencias criptograficas

- **Slug**: crypto-dependencies-audit
- **Testado em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Correcao**: [fix.md](fix.md)
- **Resultado**: verified

## Resumo

O restore deixou de produzir NU1902/NU1903 para os pacotes da avaliacao. A auditoria permaneceu habilitada e o build completo passou.

## Verificacoes Executadas

| Verificacao | Comando interno ao container | Resultado | Notas |
| --- | --- | --- | --- |
| Reproducao/restore | dotnet build Nodra.slnx --configuration Release | pass | Sem supressoes de auditoria |
| Build/analisadores | Mesmo build completo | pass | Zero warnings e zero erros |
| Regressao | dotnet test Nodra.slnx --configuration Release --no-build --no-restore --filter Category!=Integration | pass | 184 testes |

## Trechos de Saida

Build succeeded. 0 Warning(s), 0 Error(s). Todas as suites selecionadas registraram Failed: 0.

## Riscos Residuais

Nao certifica a ausencia de vulnerabilidades em imagens runtime nem substitui testes PostgreSQL ou gates de release/deploy.

## Recomendacao

Encerrar o defeito delimitado dos pacotes criptograficos; manter auditoria habilitada nos proximos builds.
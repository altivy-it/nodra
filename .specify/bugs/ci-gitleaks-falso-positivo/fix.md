# Correção de Bug: falso positivo do Gitleaks no histórico

- **Slug**: ci-gitleaks-falso-positivo
- **Corrigido em**: 2026-11-18
- **Avaliação**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

A configuração preserva as regras padrão e isenta apenas a linha completa da
tarefa pública de imagens, na regra `generic-api-key` e em arquivos de patch.
Os testes verificam que o mesmo valor em outro contexto e credenciais
sintéticas adicionais continuam sendo sinalizados.

## Alterações

| Arquivo | Alteração | Notas |
|---|---|---|
| [.gitleaks.toml](../../../.gitleaks.toml) | adicionado | Exceção limitada por regra, formato de patch e linha integral; não depende do hash do commit. |
| [test-gitleaks.ps1](../../../tools/scripts/test-gitleaks.ps1) | adicionado | Executa o runner e o scanner em container; o workflow existente já detecta o script. |

## Testes Adicionados ou Atualizados

- Linha conhecida em posições e commits simulados diferentes e patch da
  própria configuração: nenhum achado.
- Tag em outro contexto e linha modificada com credencial sintética: ambas
  bloqueadas por `generic-api-key`.
- Credencial sintética AWS: bloqueada pela regra padrão específica.
- Regra independente aplicada à linha conhecida: continua bloqueada.
- Quatro mutações da configuração, incluindo exceção ampla e ausência das
  regras padrão: todas rejeitadas pelos testes.

## Verificação Local

- `test-gitleaks.ps1 -ScannerPath /usr/local/bin/gitleaks` em container com
  PowerShell e Gitleaks 8.30.1: aprovado.
- Histórico local em modo `dir`, incluindo refs de checkpoint e um patch
  sintético das alterações locais: nenhum achado.
- Orquestração Docker: argumentos, mounts e limpeza após falha exercitados por
  um simulador com o mesmo executável Gitleaks real; o daemon Docker do GitHub
  não foi executado.
- Contratos CI/CD, 37 hashes gerenciados, 10 cenários do validador e 235
  documentos Markdown: aprovados em container.
- Parser PowerShell, `Get-Help`, `--help` e erro de argumento inválido:
  aprovados. `PSScriptAnalyzer` não está instalado na imagem existente.

## Desvios da Avaliação

- Nenhum.

## Acompanhamento

- Executar `Gitleaks / Full History` no GitHub depois da atualização autorizada
  da branch; o teste local não comprova o ambiente do runner remoto.
- Rever a exceção se a linha da tarefa mudar, sem ampliar a allowlist.

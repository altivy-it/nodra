# Correcao de Bug: dependencias criptograficas

- **Slug**: crypto-dependencies-audit
- **Corrigido em**: 2026-11-11
- **Avaliacao**: [assessment.md](assessment.md)
- **Estado**: applied

## Resumo

A referencia centralizada System.Security.Cryptography.Xml passou de 10.0.11 para 10.0.13. Seu Pkcs transitivo acompanha 10.0.13, sem desabilitar auditoria ou atualizar toda a stack.

## Alteracoes

| Arquivo | Alteracao | Notas |
| --- | --- | --- |
| backend/Directory.Packages.props | Xml 10.0.13 | Licenca MIT e versao corrigida verificadas em fonte oficial |

## Testes Adicionados ou Atualizados

Reutilizados os gates existentes de restore/build e suites backend. Nenhum novo teste de exploracao de vulnerabilidade foi introduzido.

## Verificacao Local

Imagem docker.io/lzocateli/dotnet-sdk:10.0.403-noble-v1: build completo Nodra.slnx aprovado com zero warnings/erros, auditoria habilitada; 184 testes sem PostgreSQL aprovados.

## Desvios da Avaliacao

O primeiro build apos o restore corrigido revelou InternalsVisibleTo antigo no BFF, defeito da renomeacao reparado na tarefa da feature. Nao exigiu ampliar a mudanca de dependencias.

## Acompanhamento

Auditoria de imagens/runtime e aprovacao de release sao gates separados, nao realizados por esta correcao.
# Metadados e indicadores do explorador

## Frontmatter Altivy

Notas podem declarar metadados namespaced no frontmatter YAML:

```yaml
---
altivy:
  metadata_version: "1.0"
  classification: unclassified
  doc_type: note
  tags: [inbox]
---
```

O parser preserva o Markdown original e deriva os seguintes estados:

- `isUnclassified`: classificação ausente, inválida ou igual a `unclassified`;
- `hasMinimumMetadata`: versão `1.0`, classificação válida, tipo válido e ao
  menos uma tag presentes;
- `hasUnresolvedLinks`: existe uma referência local que não corresponde a um
  arquivo Markdown conhecido no vault.

Os tipos reconhecidos atualmente são `note`, `reference`, `decision`,
`meeting`, `runbook` e `task`. As classificações reconhecidas são `unclassified`,
`public`, `internal`, `restricted` e `confidential`.

## Links

A resolução considera wiki links e links Markdown relativos. A extensão `.md`
é assumida para wiki links sem extensão. Links externos, âncoras, `mailto:`,
`tel:`, `data:` e esquemas com autoridade não são classificados como links
locais não resolvidos.

A lista de arquivos Markdown do vault é a fonte de verdade da resolução. A
implementação não cria um índice persistente nem altera o conteúdo da nota.

## Contrato e interface

A resposta de listagem de entradas expõe `isUnclassified`,
`hasMinimumMetadata` e `hasUnresolvedLinks`, com `null` para pastas. O
explorador apresenta os indicadores `!`, `M` e `L` e inclui seus significados no
rótulo acessível da linha.

## Validação

- parser e caminhos: 26 testes de domínio aprovados;
- explorador Angular: 17 testes aprovados;
- jornada Playwright mockada: 31 testes aprovados;
- teste de integração do filesystem: adicionado para validar links resolvidos,
  links ausentes e preservação do conteúdo; a execução final depende de um
  workspace Linux limpo, sem resíduos `obj/bin` do bind mount Windows.

## Publicação

As imagens foram publicadas no Docker Hub `lzocateli` com a tag imutável
`69431f0`:

- `lzocateli/altivy-api:69431f0` — `sha256:1cb3433441b7d8ae98730dba16ff0d2e575177d7aca798ef1e8ac5d0a8d350ee`;
- `lzocateli/altivy-bff:69431f0` — `sha256:acbbb8e225543013de2a00ef39d1acf3f9396e2d049985c81145eea930b6c40a`;
- `lzocateli/altivy-worker:69431f0` — `sha256:1a17452ea23c4c17c4b70711ef42a36154430a0a95121c617e285c7a06c0cf2d`;
- `lzocateli/altivy-web:69431f0` — `sha256:2ce2bba678ff39a363abb76c1275ba84bcc6667ac8d7ea6958a7744eb2266d98`.

O scanner Trivy não estava disponível no ambiente local; o scan de
vulnerabilidades e a geração de SBOM/provenance permanecem como pendência
operacional do pipeline de publicação.

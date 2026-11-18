# Avaliacao de Bug: peers Angular incompatíveis

- **Slug**: angular-peer-dependencies
- **Criada em**: 2026-11-11
- **Origem**: Erro reproduzido nesta conversa durante a integracao da main; correcao autorizada pelo usuario para desbloquear a feature 010-renomeacao-nodra.
- **Veredito**: valid
- **Severidade**: high

## Relato

O comando npm ci falha com ERESOLVE: @angular/router@22.2.0 exige @angular/common@22.2.0, mas o lockfile instala common, core, forms e platform-browser 22.1.3. O mesmo erro ocorre usando exclusivamente os manifests da origin/main anterior a renomeacao.

## Sintoma

Nao e possivel instalar o frontend de forma reproduzivel, executar testes, gerar build ou verificar visualmente a aplicacao com os comandos oficiais do CI.

## Reproducao

1. Usar a imagem docker.io/lzocateli/angular-cli:22.1.0-node24.15.0-bookworm.
2. Disponibilizar frontend/web/package.json e package-lock.json em workspace temporario.
3. Executar npm ci --no-audit --no-fund, sem flags permissivas.
4. Observar o conflito entre router 22.2.0 e common 22.1.3.

## Caminhos de Codigo Suspeitos

- frontend/web/package.json: router declara ^22.2.0 enquanto os pacotes da mesma familia declaram ^22.1.0.
- frontend/web/package-lock.json: router esta travado em 22.2.0 e os peers em 22.1.3.
- .github/workflows/ci.yml: instala usando npm ci, que corretamente recusa a arvore inconsistente.

## Hipotese de Causa Raiz

Atualizacao isolada do router sem alinhamento dos peers Angular. Confianca high: reproducao na base e metadados oficiais npm confirmam peers exatos de mesma versao. A consulta npm view @angular/router@22.1.3 version license peerDependencies --json confirmou versao 22.1.3, licenca MIT e peers core/common/platform-browser 22.1.3.

## Correcao Proposta

**Preferencial**: Alinhar a declaracao do router a ^22.1.0, como os demais pacotes da familia, e regenerar o lockfile com router 22.1.3 em container padronizado. Nao atualizar o restante da stack, nao usar force/legacy-peer-deps e verificar as alteracoes de lock para impedir atualizacoes transitivas alheias.

**Alternativas**: Atualizar todos os pacotes Angular para 22.2.x ampliaria desnecessariamente o escopo e foi descartado. Ignorar peers ocultaria o defeito e nao e aceitavel.

**Arquivos Provaveis de Alteracao**:

- frontend/web/package.json
- frontend/web/package-lock.json

**Testes a Adicionar ou Atualizar**:

- Reexecutar npm ci sem flags permissivas em container; esse e o teste de regressao existente no CI.
- Executar npm test -- --watch=false e build de producao, distinguindo falhas adicionais preexistentes.

## Riscos e Consideracoes

- Nenhuma mudanca em API, schema, identidade ou dados.
- A versao candidata possui licenca MIT confirmada por metadados oficiais npm em 2026-11-11.
- A correcao reduz a divergencia interna de versoes sem introduzir dependencia nova.
- Nao ler nem criar arquivos de secrets. A operacao usa apenas registry publico e container temporario.

## Perguntas em Aberto

Nenhuma para a correcao delimitada. Falhas adicionais exigem nova avaliacao antes de ampliar o escopo.
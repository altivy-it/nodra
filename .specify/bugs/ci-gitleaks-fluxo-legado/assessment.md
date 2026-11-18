# Avaliação de Bug: falso positivo no scanner legado do PR 31

- **Slug**: ci-gitleaks-fluxo-legado
- **Criada em**: 2026-11-18
- **Origem**: [execução do PR 31](https://github.com/altivy-it/nodra/actions/runs/44360771689)
- **Host interpretado**: github.com
- **Política de consulta**: allowlisted; consulta autenticada existente.
- **Veredito**: valid
- **Severidade**: high

## Relato

Depois de alinhar o PR 31 à `main`, o job legado `Gitleaks / Full History`
falhou com um achado. O usuário autorizou corrigir os gates, preservando
histórico e código; somente uma credencial real justificaria reescrita.

## Sintoma

O workflow varre também a branch do PR 32 e sinaliza uma tag pública de imagem
como `generic-api-key`. O scan deve continuar abrangendo todo o histórico e
bloqueando credenciais reais, sem reprovar essa linha comprovadamente pública.

## Reprodução

1. Consultar a execução do PR 31 e observar um achado no scan de histórico.
2. Executar Gitleaks 8.30.1 em `stdin` sobre as refs e tags remotas: um
   `generic-api-key`; escanear o commit `bbb9846` isoladamente produz o mesmo
   resultado.
3. Dividir o mesmo histórico em patches por commit e escanear sem exceção:
   o único achado pertence a `bbb9846bb748b9175e175f4c2cea8942459d8730`.
   O valor é a tag pública da tarefa local, não uma credencial.

## Caminhos de Código Suspeitos

- [.github/workflows/gitleaks.yml](../../../.github/workflows/gitleaks.yml)
  — envia o histórico inteiro por `stdin`, sem carregar configuração própria.
- O commit `bbb9846` — contém a tarefa pública com a linha sinalizada.
- `.gitleaks.toml` — ausente na branch; a correção da feature 011 é restrita
  à regra, ao arquivo de patch e à linha pública integral.

## Hipótese de Causa Raiz

Confiança **high**: a regra genérica identifica como chave a tag pública em
outra branch, porque `--all` inclui suas refs. O fluxo legado não fornece
arquivos por commit nem a configuração restrita já testada na feature 011.

## Correção Proposta

**Preferencial**: manter `git log --all --full-history`, gerar um arquivo de
patch por commit e usar a mesma configuração Gitleaks restrita à regra
`generic-api-key` e à linha pública integral. Não ignorar branches, commits,
regras ou arquivos inteiros. Executar testes negativos com o scanner real.

**Arquivos Prováveis de Alteração**:

- `.github/workflows/gitleaks.yml`
- `.gitleaks.toml`
- `tools/scripts/test-gitleaks.ps1`

**Testes a Adicionar ou Atualizar**:

- Varrer todo o histórico com o formato e a configuração do workflow.
- Bloquear a tag em outro contexto, uma credencial sintética adicional e
  uma regra independente; aceitar a linha pública em diferentes commits.
- Reexecutar o job real do PR 31 e conservar a proteção de branch.

## Riscos e Considerações

- O teste precisa funcionar mesmo quando a tarefa de VS Code da 011 não
  estiver presente no checkout da 010.
- Alterar o histórico ou reduzir as refs examinadas seria desnecessário e
  não corrige a origem do falso positivo.
- A configuração tem de permanecer equivalente nas branches até a integração.

## Perguntas em Aberto

- Nenhuma: o único achado identificado corresponde à linha pública conhecida.

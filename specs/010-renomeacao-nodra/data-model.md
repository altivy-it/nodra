# Modelo de dados e identidade: Nodra

## Identidade de marca

| Campo | Valor | Regra |
| --- | --- | --- |
| empresa | Altivy | Nao substituir por Nodra |
| produto | Nodra | Nome do aplicativo |
| marca completa | Altivy Nodra | Assinatura corporativa |
| repositorio | altivy-it/nodra | Mesmo repositorio, nome novo |
| CLI futura | nodra | Reserva de nomenclatura, sem criar CLI |
| tagline | Your connected knowledge workspace. | Literal aprovado |

O posicionamento e a sequencia conceitual sao os literais aprovados na especificacao, sem implicar novas funcionalidades nesta entrega.

## Excecao de compatibilidade

Cada excecao precisa de identificador literal, superficie proprietaria, motivo de preservacao, consumidores e teste ou procedimento que comprove continuidade. As categorias sao empresa/recurso externo, estado persistido, protocolo/configuracao, historico e caminho local de desenvolvimento.

## Dados de usuario

Nao ha nova entidade de banco, coluna, tabela, indice ou migration funcional. Vaults, notas e projecoes mantem seus IDs e paths. Preferencias e estado de operacoes pendentes do navegador mantem suas chaves existentes; qualquer migracao futura deve ser nao destrutiva e testada.

## Transicoes

1. Nome antigo com historico integrado.
2. Branch Nodra com marca e nomes internos novos; contratos persistidos antigos preservados.
3. Gates e excecoes revisados pelo mantenedor.
4. Repositorio renomeado com origin atualizado, sem merge automatico na main.

Falha de permissao na etapa remota nao reverte dados nem modifica o historico. A feature permanece na branch e a operacao pendente e registrada.
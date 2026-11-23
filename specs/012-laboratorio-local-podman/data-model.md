# Modelo operacional: Laboratório local

Não há alteração de domínio, schema ou contratos da API.

## Seleção de engine

- `docker`: comportamento padrão existente.
- `podman`: engine Podman com provider Docker Compose v2 validado.
- Engine ou provider incompatível: erro antes de efeitos persistentes.

## Estados

`não preparado` → `pré-requisitos verificados` → `imagens construídas` →
`backup verificado` → `migrations aplicadas` → `consumidores saudáveis` →
`smoke aprovado` → `funcionalidade qualificada`.

Falha não avança artificialmente para prontidão. Recriação preserva vault e
keyring. A plataforma compartilhada permanece fora do ciclo de vida do produto.

## Configuração

Paths são explícitos e externos. Credenciais não são copiadas para specs,
fixtures ou logs.

# Ajuda do Nodra

## Abrir e organizar notas

Use **Arquivos** para abrir uma nota ou criar um documento rápido. O Markdown
continua sendo a fonte canônica do seu vault.

## Preservar seu contexto

O workspace mantém o documento atual enquanto você consulta os módulos. As
preferências locais não contêm conteúdo de notas nem credenciais.

## Criar outro vault no laboratório

A criação de vault ainda é uma operação administrativa; o seletor da barra
superior apenas alterna entre vaults já autorizados.

Para preparar um segundo vault sintético no laboratório:

1. Adicione o candidato com um ID estável, por exemplo `lab-0002`, no fixture
	do leitor de laboratório da API.
2. Crie ou monte o diretório correspondente no volume de vaults do Compose E2E
	e coloque nele apenas notas sintéticas.
3. Reconcile no Keycloak o recurso `vault-lab-0002`, o scope `vault:read` e a
	policy do grupo ou usuário de teste.
4. Reconstrua `api`, `bff` e `web` e recarregue o workspace.

O vault só aparece no seletor depois que o catálogo da API o retorna e o
Keycloak confirma a permissão. Para validar negação, deixe o recurso criado,
mas não conceda `vault:read` ao usuário de teste.

## Conta e segurança

Autenticação, perfil, sessões e MFA pertencem ao provedor de identidade
configurado pelo operador. O Nodra não reproduz esses fluxos.
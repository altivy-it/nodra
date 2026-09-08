# Tutorial: tenant Microsoft 365 de teste para o app Teams

## Objetivo e escopo

Este guia prepara um ambiente isolado para a POC da etapa P-02 do
[plano de plugins e Teams](plan/app-altivy-notes-teams-external-push.md).
O ambiente permite provar uma action command do Teams que envia conteúdo
selecionado ou digitado ao Altivy, sem Microsoft Graph na primeira versão.

Use somente usuários, mensagens, reuniões, arquivos e nomes sintéticos. O
tenant de teste não substitui o laboratório local do Altivy nem deve receber
o vault, credenciais ou dados de produção.

## Resultado esperado

Ao terminar, haverá:

- um tenant Microsoft 365 dedicado, com conta administrativa separada;
- pelo menos dois usuários licenciados para Teams e uma equipe de teste;
- sideload de app customizado permitido apenas para o grupo de teste;
- uma assinatura Azure ligada ao tenant, com registro de bot e identidade
  Microsoft Entra controlados;
- uma URL HTTPS pública temporária, restrita ao callback do bot;
- um pacote Teams de teste com manifesto e ícones sintéticos;
- uma matriz de evidências para a POC, sem permissões nem chamadas Graph.

## Pré-requisitos

Antes de começar, providencie:

1. Uma conta Microsoft pessoal para se inscrever em programas de
   desenvolvimento. Não use a conta administrativa de uma organização real.
2. Um endereço de recuperação e MFA para a conta administrativa do tenant.
3. Uma assinatura Azure que possa criar recursos de bot e um grupo de
   recursos de teste. Controle custos e limite permissões administrativas.
4. Uma URL HTTPS publicamente alcançável para o callback do bot. `localhost`,
   IP privado, certificado local e a borda do laboratório em `localhost:7443`
   não são alcançáveis pelo Teams.
5. Aprovação do administrador do tenant para política de apps customizados e
   para criar um bot. Em tenant corporativo, a aprovação deve ser registrada.

Não versionar senhas, client secrets, certificados, tokens, IDs reais de
tenant, app ou bot. Use variáveis de ambiente ou um cofre/arquivo de segredo
montado, conforme as regras do Altivy.

## Caminho A: sandbox do Microsoft 365 Developer Program

Este é o caminho preferencial quando estiver disponível para a conta e região.
A sandbox E5 costuma incluir usuários de desenvolvimento e licenças para
testes. A elegibilidade e a renovação são definidas pela Microsoft e podem
variar; não trate a sandbox como ambiente permanente.

1. Acesse o [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program).
2. Entre com a conta Microsoft pessoal e conclua o perfil de desenvolvimento.
3. Escolha a opção de criar uma sandbox, quando ela for oferecida.
4. Informe um domínio inicial não sensível, por exemplo
   `altivy-lab.onmicrosoft.com`, e guarde a conta administradora em um gestor
   de senhas. O domínio e a conta são exemplos, não valores a reutilizar.
5. Aguarde a criação e entre no
   [Centro de administração do Microsoft 365](https://admin.microsoft.com).
6. Em **Usuários > Usuários ativos**, confirme a existência de usuários de
   desenvolvimento e licenças Microsoft 365 E5 Developer atribuídas.
7. Ative MFA para a conta administradora e crie uma conta administrativa de
   recuperação, também protegida por MFA. Não use essas contas no teste diário.

Se a opção de sandbox não aparecer ou a inscrição for recusada, siga o Caminho
B. Não tente contornar requisitos do programa com múltiplas contas.

## Caminho B: avaliação do Microsoft 365 Business

Uma avaliação de Microsoft 365 Business é a alternativa para uma POC curta.
Ela pode exigir forma de pagamento, verificação adicional e, ao fim do período,
conversão para plano pago ou exclusão do tenant. Confirme preço, prazo e
condições atuais antes de finalizar o cadastro.

1. Acesse a página de [avaliações do Microsoft 365 para empresas](https://www.microsoft.com/microsoft-365/business).
2. Escolha uma avaliação que inclua Microsoft Teams e conclua o cadastro com
   domínio sintético, por exemplo `altivy-poc.onmicrosoft.com`.
3. Entre em [admin.microsoft.com](https://admin.microsoft.com) com a conta
   Global Administrator criada no cadastro.
4. Em **Cobrança > Seus produtos**, confirme a data de expiração e as licenças
   disponíveis. Crie um lembrete antes da renovação automática, quando houver.
5. Em **Usuários > Usuários ativos**, crie os usuários sintéticos descritos na
   próxima seção e atribua uma licença que contenha Teams.

Uma terceira alternativa é um tenant corporativo de homologação já existente.
Use-o apenas com aprovação formal do administrador, grupo de teste dedicado,
políticas restritas e nenhum dado real.

## Criar usuários e uma equipe sintéticos

No Centro de administração do Microsoft 365:

1. Abra **Usuários > Usuários ativos > Adicionar um usuário**.
2. Crie ao menos `altivy.admin`, `altivy.author` e `altivy.viewer` com nomes
   claramente sintéticos.
3. Atribua uma licença que habilite Teams para `altivy.author` e
   `altivy.viewer`. Use `altivy.admin` somente para administração.
4. Exija MFA e remova privilégios globais dos usuários de teste comuns.
5. Entre no [Teams](https://teams.microsoft.com) como `altivy.author` e crie a
   equipe privada `Altivy POC`, com um canal padrão `testes-sinteticos`.
6. Adicione `altivy.viewer` como membro. Crie mensagens e, quando a POC exigir,
   uma reunião contendo apenas conteúdo sintético.

Anote separadamente, em cofre ou inventário operacional protegido, o domínio
do tenant e os IDs necessários. Não coloque IDs reais no manifesto de exemplo,
testes, fixtures ou Git.

## Permitir app customizado no Teams

A política exata depende da edição e configuração do tenant. O administrador
deve conceder a menor permissão que viabilize somente a POC.

1. Abra o [Centro de administração do Teams](https://admin.teams.microsoft.com).
2. Em **Teams apps > Permission policies**, crie uma política como
   `AltivyPocCustomApps` ou edite uma política dedicada existente.
3. Permita apps customizados/carregamento de apps para a política de teste;
   mantenha bloqueados apps de terceiros não necessários e publicação ampla.
4. Em **Users > Manage users**, atribua a política somente a
   `altivy.author` e `altivy.viewer`.
5. Em **Teams apps > Setup policies**, crie uma política dedicada se for
   necessário fixar ou instalar o app para o grupo. Não fixe o app para toda a
   organização.
6. Em **Teams apps > Manage apps**, confirme que a política global não bloqueia
   o pacote customizado. Se a organização usar catálogo, publique apenas para o
   grupo de POC.
7. Aguarde a propagação da política e registre a política, o grupo atingido e
   a data no relatório da POC.

No cliente Teams, valide com `altivy.author`: **Apps > Manage your apps > Upload
a custom app** deve estar disponível. Se a opção estiver ausente, não altere
políticas globais por tentativa; revise a policy atribuída, a licença e as
restrições de catálogo com o administrador.

## Preparar Azure e Microsoft Entra

O bot usa uma identidade de aplicativo no tenant Microsoft Entra. Essa
identidade autentica o protocolo do bot; ela não concede acesso a vaults do
Altivy e não substitui a autorização do Keycloak.

1. Entre no [portal Azure](https://portal.azure.com) com uma conta que tenha
   permissão limitada para criar recursos no grupo de recursos de POC.
2. Confirme, no seletor de diretório, que a assinatura Azure está associada ao
   tenant de teste correto.
3. Crie um grupo de recursos como `rg-altivy-teams-poc` na região escolhida
   para o laboratório. Aplique tags de custo e uma data prevista de remoção.
4. Procure por **Azure Bot** e crie o recurso no grupo de recursos de POC.
   Durante o assistente, crie ou selecione o registro de aplicativo Microsoft
   Entra de uso único para esse bot.
5. Em **Microsoft Entra ID > App registrations > [aplicativo do bot] >
   Authentication**, confirme as URLs exigidas pelo fluxo do bot. Adicione
   somente as URLs públicas de teste necessárias.
6. Em **Certificates & secrets**, prefira certificado para a credencial da
   aplicação quando o ambiente suportar rotação. Se um client secret for
   inevitável na POC, use validade curta, guarde-o apenas no secret store e
   planeje sua revogação.
7. No recurso **Azure Bot**, abra **Settings > Configuration** e informe a
   URL de mensagens pública do Altivy, por exemplo
   `https://teams-poc.example.test/plugins/teams/messages`. Esta é apenas uma
   forma de URL; o endpoint definitivo depende da P-09.
8. Em **Channels**, habilite o canal **Microsoft Teams** e aceite somente as
   configurações indispensáveis para a POC.

Não atribua permissões Microsoft Graph ao registro nesta etapa. Em **API
permissions**, a evidência esperada da P-02 é não haver permissões Graph
delegadas nem de aplicação adicionadas pelo app Altivy.

## Expor o callback HTTPS de teste

O Teams não alcança o ambiente local. Para a POC, publique somente a rota do
callback do bot, mantendo BFF, API, PostgreSQL, worker e vault privados.

1. Escolha um hostname público controlado pelo operador, como
   `teams-poc.example.test`, e crie o DNS correspondente.
2. Configure um certificado TLS válido para esse hostname no proxy reverso.
   Certificados autoassinados e a CA local de `deploy/e2e` não são suficientes.
3. Crie uma rota dedicada que encaminhe exclusivamente
   `/plugins/teams/messages` ao adapter Teams. Não exponha o prefixo `/api/v1`,
   o Keycloak administrativo, o banco ou o filesystem.
4. Aplique limite de corpo, timeout curto, rate limit e logs sem corpo,
   cabeçalho `Authorization` ou tokens. O endpoint deve validar a atividade
   antes de persistir a inbox e responder rapidamente.
5. Restrinja firewall, WAF e DNS conforme os requisitos publicados do Bot
   Framework. Não use allowlist de IP como único controle de autenticidade.
6. Execute teste externo HTTPS que confirme certificado, rota e resposta de
   health técnica sem expor detalhes internos.
7. Remova a rota e o DNS ao encerrar a POC.

Um túnel público temporário pode ser útil exclusivamente no desenvolvimento
local se for permitido pela política corporativa e tiver TLS válido. Ele não é
arquitetura de staging ou produção, não recebe conteúdo real e deve ser
encerrado após a sessão.

## Criar e carregar o pacote Teams de teste

Use o [Developer Portal for Teams](https://dev.teams.microsoft.com) com
`altivy.admin` ou outra conta administrativa delegada para o catálogo de POC.

1. Em **Apps**, selecione **New app** e use nome e descrição que indiquem POC.
2. Em **Basic information**, configure identificador de app, informações do
   desenvolvedor, URLs públicas de privacidade, termos e suporte; use páginas
   de teste sem dados pessoais nem internos.
3. Em **App features > Bot**, vincule o bot criado no Azure e habilite apenas
   os escopos comprovados na POC: `personal`, `team` e/ou `groupChat`.
4. Em **App features > Messaging extension**, adicione uma command baseada em
   ação com o nome `Salvar no Altivy Notes`. A forma final depende do gate G1;
   não publique como produto enquanto ela não estiver aprovada.
5. Em **Domains**, adicione somente o hostname público de teste. Não use URLs
   de loopback, IPs privados ou domínios internos.
6. Adicione os ícones colorido e de contorno exigidos, sem inserir secrets,
   tenant IDs ou endpoints internos nos assets.
7. Em **Publish**, use **Download app package** para gerar o ZIP. O pacote deve
   conter somente `manifest.json` e os ícones esperados.
8. Com `altivy.author`, faça upload pelo Teams em **Apps > Manage your apps >
   Upload a custom app** e instale apenas na equipe `Altivy POC`.

Mantenha o ZIP gerado fora de commits até que P-08 defina o processo
reproduzível de empacotamento e a versão GA do schema.

## Executar e registrar a POC da P-02

Para cada cenário, use conteúdo sintético e capture data, cliente Teams,
versão do manifesto, resultado e correlação técnica sem registrar o corpo.

| Cenário | Evidência esperada |
| --- | --- |
| Action command sobre mensagem | conteúdo explicitamente selecionado chega ao callback; nenhuma chamada Graph. |
| Área de composição | texto digitado chega ao callback sem mensagem de origem. |
| Chat pessoal | suporte ou recusa explícita, sem suposição. |
| Group chat | suporte ou recusa explícita, sem suposição. |
| Canal público, privado e compartilhado | matriz de suporte por contexto e política do tenant. |
| Reunião | comportamento observado e limitações registradas. |
| Teams web, desktop e mobile | resultado, timeout e diferenças de UI por cliente. |
| Reenvio/retry | identificadores estáveis disponíveis e comportamento de duplicata. |
| Inspeção de permissões | ausência de permissões e chamadas Microsoft Graph pelo app. |

Registre também o maior payload aceito, latência até a resposta do callback,
erros de autenticação e contexto no qual o Teams omite conteúdo. Esses dados
alimentam P-03, que continua exigindo decisão humana antes de escolher a forma
final do app.

## Diagnóstico e recuperação

| Sinal | Diagnóstico provável | Mitigação |
| --- | --- | --- |
| Não existe botão para upload | política, licença ou catálogo bloqueia app customizado | revisar a policy dedicada e a atribuição do usuário; não liberar globalmente sem aprovação. |
| Teams informa que não alcança o bot | URL não é pública, TLS inválido ou rota não responde | verificar DNS, certificado e proxy da rota exclusiva; confirmar URL no Azure Bot. |
| Callback recebe 401/403 | validação de token, audiência, tenant ou configuração do bot divergente | comparar configuração sem expor secret, revisar issuer/audience e rejeitar até corrigir. |
| App instala, mas command não aparece | escopo, manifesto ou política incompatível | validar manifesto no Developer Portal e testar o contexto declarado. |
| Há permissão ou chamada Graph | a POC deixou o escopo aprovado | remover permissões, revogar consentimento, registrar o desvio e interromper o gate G1. |
| Secret exposto | credencial foi copiada para arquivo, chat, log ou pacote | revogar imediatamente, emitir nova credencial, investigar alcance e remover a cópia. |

## Encerramento e limpeza

1. No Teams Admin Center, remova o app e as políticas dedicadas aos usuários
   de teste.
2. No Azure, desabilite o canal Teams, remova a URL de mensagens e exclua o
   recurso Azure Bot e o grupo de recursos de POC após preservar evidências
   sem dados sensíveis.
3. No Microsoft Entra, revogue certificados/secrets e exclua o registro do app
   quando não houver outro recurso dependente.
4. Revogue acessos administrativos temporários, exclua usuários sintéticos e
   confira licenças e renovação da avaliação/sandbox.
5. Remova DNS, certificado e rota pública do callback.
6. Atualize a evidência P-02 e mantenha o plano como bloqueado ou em progresso
   até a decisão humana do gate G1.

## Referências oficiais

- [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program)
- [Developer Portal for Teams](https://learn.microsoft.com/microsoftteams/platform/concepts/build-and-test/teams-developer-portal)
- [Carregar apps personalizados no Teams](https://learn.microsoft.com/microsoftteams/platform/concepts/build-and-test/upload-custom-apps)
- [Políticas de permissão de apps no Teams](https://learn.microsoft.com/microsoftteams/teams-app-permission-policies)
- [Criar e configurar um Azure Bot](https://learn.microsoft.com/azure/bot-service/bot-service-quickstart-registration)
- [Bots no Teams](https://learn.microsoft.com/microsoftteams/platform/bots/what-are-bots)
- [Action commands de message extensions](https://learn.microsoft.com/microsoftteams/platform/messaging-extensions/how-to/action-commands/define-action-command)
- [Schema do manifesto Microsoft 365](https://learn.microsoft.com/microsoftteams/platform/resources/schema/manifest-schema)
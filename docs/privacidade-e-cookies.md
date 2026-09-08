# Privacidade e cookies

## Decisão atual

O Altivy Notes utiliza exclusivamente cookies técnicos no BFF:

| Cookie | Finalidade | Classificação |
| --- | --- | --- |
| `__Host-Altivy.Session` | Manter a sessão autenticada com os tokens no servidor | Estritamente necessário |
| `__Host-Altivy.Antiforgery` | Proteger operações mutáveis contra CSRF | Estritamente necessário |

O cookie de sessão é `Secure`, `HttpOnly`, host-only, `SameSite=Lax`, expira em
oito horas e está marcado como essencial. O cookie antiforgery é `Secure`,
`HttpOnly`, host-only e `SameSite=Strict`. Não há analytics, pixels de
marketing, fingerprinting ou telemetria de produto no navegador.

Assim, **não é necessário banner de aceite de cookies** para o funcionamento
atual. Esses cookies são necessários para autenticar o usuário e proteger a
sessão; o consentimento não pode ser uma condição que impeça a segurança ou o
serviço explicitamente solicitado.

## LGPD e União Europeia

A LGPD exige transparência e uma base legal adequada para cada tratamento, mas
não determina banner apenas por haver cookie técnico. Para usuários na União
Europeia, a exceção de cookies estritamente necessários das regras de ePrivacy
também dispensa consentimento prévio; o GDPR continua aplicável aos dados
pessoais tratados durante a autenticação e a operação do produto.

O operador da instância continua responsável por informar titulares, definir
base legal, retenção, canal de atendimento e as medidas de segurança do seu
ambiente. Esta documentação não substitui aviso de privacidade do operador nem
orientação jurídica.

## Gatilho para mudança

Antes de introduzir analytics, marketing, chat, conteúdo incorporado, testes
com identificadores persistentes, fingerprinting, storage não essencial ou
qualquer script de terceiro que acesse o dispositivo, deve-se:

1. revisar finalidade, dados, fornecedor e transferência internacional;
2. disponibilizar aviso de privacidade e política de cookies configuráveis pelo
   operador;
3. bloquear cada tecnologia não essencial até consentimento prévio, granular,
   livre, informado, revogável e documentável;
4. oferecer rejeição tão simples quanto o aceite e não condicionar o serviço
   essencial ao consentimento opcional.

Cookies técnicos devem permanecer separados das categorias opcionais e não ser
bloqueados pela CMP. Qualquer mudança deve incluir revisão de privacidade e
segurança antes da publicação.
# Artefatos PostgreSQL do Altivy Notes

O workflow `deploy-shared-infra.yml` entrega o script de roles versionado em
`backend/libs/infrastructure/data/sql/001-bootstrap-altivy-roles.sql` para
`/userapps/altivynotes/postgres/`. Ele não executa SQL administrativo, cria
database/roles, reinicia o container ou altera o bind mount compartilhado.

## Provisionamento inicial

O operador do PostgreSQL deve, fora do workflow:

1. criar o database `altivy` e conceder conexão às roles dedicadas;
2. aplicar `001-bootstrap-altivy-roles.sql` ao database `altivy`, fornecendo
   as senhas por variáveis psql interativas/seguras, nunca pela linha de
   comando ou logs;
3. guardar a connection string `altivy_runtime` nos arquivos externos de
   configuração da API e do worker;
4. guardar a connection string `altivy_migrator` em
   `/userapps/configs/altivynotes/migrator.env`, com acesso restrito ao usuário
   de deploy e ao daemon Docker;
5. conectar somente os serviços Altivy à rede Docker que alcança o PostgreSQL.

A role `altivy_runtime` não pode ser superuser, criar roles ou criar databases.
O container PostgreSQL e seus dados permanecem sob propriedade da plataforma
compartilhada em `/userapps/var/`; este repositório não publica nem altera
configuração global do servidor.

As migrations versionadas em EF Core são aplicadas pelo container `migrator`
durante o deploy de backend. Não use `EnsureCreated` nem execute downgrade
automático em rollback.
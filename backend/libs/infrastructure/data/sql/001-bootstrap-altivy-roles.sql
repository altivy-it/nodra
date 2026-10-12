-- Executar somente pelo operador no PostgreSQL principal do laboratório.
-- Requer as variáveis psql altivy_migrator_password e altivy_runtime_password.
\set ON_ERROR_STOP on

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'altivy_migrator') THEN
        CREATE ROLE altivy_migrator LOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'altivy_runtime') THEN
        CREATE ROLE altivy_runtime LOGIN;
    END IF;
END
$$;

ALTER ROLE altivy_migrator LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
    PASSWORD :'altivy_migrator_password';
ALTER ROLE altivy_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
    PASSWORD :'altivy_runtime_password';

CREATE SCHEMA IF NOT EXISTS altivy AUTHORIZATION altivy_migrator;
ALTER SCHEMA altivy OWNER TO altivy_migrator;
REVOKE ALL ON SCHEMA altivy FROM PUBLIC;
GRANT CONNECT ON DATABASE altivy TO altivy_runtime;
GRANT USAGE ON SCHEMA altivy TO altivy_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA altivy TO altivy_runtime;
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA altivy TO altivy_runtime;

ALTER DEFAULT PRIVILEGES FOR ROLE altivy_migrator IN SCHEMA altivy
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO altivy_runtime;
ALTER DEFAULT PRIVILEGES FOR ROLE altivy_migrator IN SCHEMA altivy
    GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO altivy_runtime;

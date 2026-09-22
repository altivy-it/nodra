ALTER TABLE altivy.note_index_jobs
    ADD COLUMN created_at_utc timestamp with time zone NOT NULL DEFAULT statement_timestamp();
ALTER TABLE altivy.note_index_jobs
    ALTER COLUMN created_at_utc DROP DEFAULT;
ALTER TABLE altivy.note_index_jobs RENAME TO note_index_jobs_unpartitioned;
ALTER TABLE altivy.note_index_jobs_unpartitioned
    RENAME CONSTRAINT "PK_note_index_jobs" TO "PK_note_index_jobs_unpartitioned";
ALTER INDEX altivy."IX_note_index_jobs_idempotency_key"
    RENAME TO "IX_note_index_jobs_unpartitioned_idempotency_key";
ALTER INDEX altivy."IX_note_index_jobs_state_next_attempt_at"
    RENAME TO "IX_note_index_jobs_unpartitioned_state_next_attempt_at";
ALTER INDEX altivy."IX_note_index_jobs_state_lease_expires_at"
    RENAME TO "IX_note_index_jobs_unpartitioned_state_lease_expires_at";

ALTER TABLE altivy.vault_provisioning_operations
    ADD COLUMN created_at_utc timestamp with time zone NOT NULL DEFAULT statement_timestamp();
ALTER TABLE altivy.vault_provisioning_operations
    ALTER COLUMN created_at_utc DROP DEFAULT;
ALTER TABLE altivy.vault_provisioning_operations RENAME TO vault_provisioning_operations_unpartitioned;
ALTER TABLE altivy.vault_provisioning_operations_unpartitioned
    RENAME CONSTRAINT "PK_vault_provisioning_operations"
    TO "PK_vault_provisioning_operations_unpartitioned";
ALTER INDEX altivy."IX_vault_provisioning_operations_subject_idempotency_key"
    RENAME TO "IX_vault_provisioning_operations_unpartitioned_subject_idempotency_key";
ALTER INDEX altivy."IX_vault_provisioning_operations_state_next_attempt_at"
    RENAME TO "IX_vault_provisioning_operations_unpartitioned_state_next_attempt_at";
ALTER INDEX altivy."IX_vault_provisioning_operations_state_lease_expires_at"
    RENAME TO "IX_vault_provisioning_operations_unpartitioned_state_lease_expires_at";
ALTER INDEX altivy."IX_vault_provisioning_operations_vault_id"
    RENAME TO "IX_vault_provisioning_operations_unpartitioned_vault_id";

CREATE TABLE altivy.note_index_job_keys (
    job_id uuid NOT NULL CONSTRAINT "PK_note_index_job_keys" PRIMARY KEY,
    idempotency_key character varying(256) NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "UX_note_index_job_keys_idempotency_key" UNIQUE (idempotency_key)
);

CREATE TABLE altivy.vault_provisioning_operation_keys (
    operation_id uuid NOT NULL CONSTRAINT "PK_vault_provisioning_operation_keys" PRIMARY KEY,
    subject character varying(255) NOT NULL,
    idempotency_key character varying(128) NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "UX_vault_provisioning_operation_keys_subject_idempotency_key"
        UNIQUE (subject, idempotency_key)
);

CREATE TABLE altivy.note_index_jobs (
    job_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    vault_id character varying(128) NOT NULL,
    path character varying(1024),
    idempotency_key character varying(256) NOT NULL,
    state integer NOT NULL,
    attempt_count integer NOT NULL,
    next_attempt_at timestamp with time zone,
    last_error character varying(256),
    lease_id uuid,
    lease_expires_at timestamp with time zone,
    CONSTRAINT "PK_note_index_jobs" PRIMARY KEY (job_id, created_at_utc),
    CONSTRAINT "UX_note_index_jobs_idempotency_key_created_at_utc"
        UNIQUE (idempotency_key, created_at_utc)
) PARTITION BY RANGE (created_at_utc);

CREATE INDEX "IX_note_index_jobs_state_next_attempt_at"
    ON altivy.note_index_jobs (state, next_attempt_at);
CREATE INDEX "IX_note_index_jobs_state_lease_expires_at"
    ON altivy.note_index_jobs (state, lease_expires_at);

CREATE TABLE altivy.vault_provisioning_operations (
    operation_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    vault_id character varying(64) NOT NULL,
    name character varying(200) NOT NULL,
    subject character varying(255) NOT NULL,
    idempotency_key character varying(128) NOT NULL,
    state integer NOT NULL,
    attempt_count integer NOT NULL,
    next_attempt_at timestamp with time zone,
    last_error character varying(2000),
    lease_id uuid,
    lease_expires_at timestamp with time zone,
    CONSTRAINT "PK_vault_provisioning_operations" PRIMARY KEY (operation_id, created_at_utc),
    CONSTRAINT "UX_vault_provisioning_operations_subject_idempotency_created_at"
        UNIQUE (subject, idempotency_key, created_at_utc)
) PARTITION BY RANGE (created_at_utc);

CREATE INDEX "IX_vault_provisioning_operations_state_next_attempt_at"
    ON altivy.vault_provisioning_operations (state, next_attempt_at);
CREATE INDEX "IX_vault_provisioning_operations_state_lease_expires_at"
    ON altivy.vault_provisioning_operations (state, lease_expires_at);
CREATE INDEX "IX_vault_provisioning_operations_vault_id"
    ON altivy.vault_provisioning_operations (vault_id);

CREATE TABLE altivy.note_index_jobs_default
    PARTITION OF altivy.note_index_jobs DEFAULT;
CREATE TABLE altivy.vault_provisioning_operations_default
    PARTITION OF altivy.vault_provisioning_operations DEFAULT;

CREATE FUNCTION altivy.altivy_reserve_note_index_job_key()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, altivy
AS $$
BEGIN
    INSERT INTO note_index_job_keys (job_id, idempotency_key, created_at_utc)
    VALUES (NEW.job_id, NEW.idempotency_key, NEW.created_at_utc);
    RETURN NEW;
END;
$$;

CREATE FUNCTION altivy.altivy_release_note_index_job_key()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, altivy
AS $$
BEGIN
    DELETE FROM note_index_job_keys WHERE job_id = OLD.job_id;
    RETURN OLD;
END;
$$;

CREATE FUNCTION altivy.altivy_reserve_vault_provisioning_key()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, altivy
AS $$
BEGIN
    INSERT INTO vault_provisioning_operation_keys (
        operation_id, subject, idempotency_key, created_at_utc)
    VALUES (
        NEW.operation_id, NEW.subject, NEW.idempotency_key, NEW.created_at_utc);
    RETURN NEW;
END;
$$;

CREATE FUNCTION altivy.altivy_release_vault_provisioning_key()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, altivy
AS $$
BEGIN
    DELETE FROM vault_provisioning_operation_keys
    WHERE operation_id = OLD.operation_id;
    RETURN OLD;
END;
$$;

CREATE TRIGGER trg_note_index_jobs_reserve_key
BEFORE INSERT ON altivy.note_index_jobs
FOR EACH ROW EXECUTE FUNCTION altivy.altivy_reserve_note_index_job_key();
CREATE TRIGGER trg_note_index_jobs_release_key
AFTER DELETE ON altivy.note_index_jobs
FOR EACH ROW EXECUTE FUNCTION altivy.altivy_release_note_index_job_key();
CREATE TRIGGER trg_vault_provisioning_operations_reserve_key
BEFORE INSERT ON altivy.vault_provisioning_operations
FOR EACH ROW EXECUTE FUNCTION altivy.altivy_reserve_vault_provisioning_key();
CREATE TRIGGER trg_vault_provisioning_operations_release_key
AFTER DELETE ON altivy.vault_provisioning_operations
FOR EACH ROW EXECUTE FUNCTION altivy.altivy_release_vault_provisioning_key();

REVOKE ALL ON FUNCTION altivy.altivy_reserve_note_index_job_key() FROM PUBLIC;
REVOKE ALL ON FUNCTION altivy.altivy_release_note_index_job_key() FROM PUBLIC;
REVOKE ALL ON FUNCTION altivy.altivy_reserve_vault_provisioning_key() FROM PUBLIC;
REVOKE ALL ON FUNCTION altivy.altivy_release_vault_provisioning_key() FROM PUBLIC;

DO $$
DECLARE
    partition_month date := DATE '2026-01-01';
    next_month date;
    parent_table text;
    partition_table text;
BEGIN
    WHILE partition_month < DATE '2037-01-01' LOOP
        next_month := (partition_month + INTERVAL '1 month')::date;

        FOREACH parent_table IN ARRAY ARRAY[
            'note_index_jobs',
            'vault_provisioning_operations'] LOOP
            partition_table := format(
                '%s_%s',
                parent_table,
                to_char(partition_month, 'YYYY_MM'));
            EXECUTE format(
                'CREATE TABLE altivy.%I PARTITION OF altivy.%I FOR VALUES FROM (%L) TO (%L)',
                partition_table,
                parent_table,
                partition_month::timestamptz,
                next_month::timestamptz);
        END LOOP;

        partition_month := next_month;
    END LOOP;
END
$$;

INSERT INTO altivy.note_index_jobs (
    job_id, created_at_utc, vault_id, path, idempotency_key, state,
    attempt_count, next_attempt_at, last_error, lease_id, lease_expires_at)
SELECT
    job_id, created_at_utc, vault_id, path, idempotency_key, state,
    attempt_count, next_attempt_at, last_error, lease_id, lease_expires_at
FROM altivy.note_index_jobs_unpartitioned;

INSERT INTO altivy.vault_provisioning_operations (
    operation_id, created_at_utc, vault_id, name, subject, idempotency_key,
    state, attempt_count, next_attempt_at, last_error, lease_id, lease_expires_at)
SELECT
    operation_id, created_at_utc, vault_id, name, subject, idempotency_key,
    state, attempt_count, next_attempt_at, last_error, lease_id, lease_expires_at
FROM altivy.vault_provisioning_operations_unpartitioned;

DROP TABLE altivy.note_index_jobs_unpartitioned;
DROP TABLE altivy.vault_provisioning_operations_unpartitioned;

CREATE OR REPLACE FUNCTION altivy.altivy_ensure_monthly_queue_partitions(p_until date)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, altivy
AS $$
DECLARE
    partition_month date := DATE '2026-01-01';
    next_month date;
    parent_table text;
    partition_table text;
    default_table text;
    default_has_rows boolean;
BEGIN
    IF p_until <= partition_month OR p_until > DATE '2040-01-01' THEN
        RAISE EXCEPTION 'partition horizon % is outside the supported range', p_until;
    END IF;

    WHILE partition_month < p_until LOOP
        next_month := (partition_month + INTERVAL '1 month')::date;

        FOREACH parent_table IN ARRAY ARRAY[
            'note_index_jobs',
            'vault_provisioning_operations'] LOOP
            partition_table := format(
                '%s_%s',
                parent_table,
                to_char(partition_month, 'YYYY_MM'));
            default_table := parent_table || '_default';

            IF to_regclass(format('altivy.%I', partition_table)) IS NULL THEN
                EXECUTE format(
                    'SELECT EXISTS (SELECT 1 FROM altivy.%I WHERE created_at_utc >= $1 AND created_at_utc < $2)',
                    default_table)
                INTO default_has_rows
                USING partition_month::timestamptz, next_month::timestamptz;

                IF default_has_rows THEN
                    RAISE EXCEPTION
                        'default partition %.% contains rows for [% - %)',
                        'altivy', default_table, partition_month, next_month;
                END IF;

                EXECUTE format(
                    'CREATE TABLE altivy.%I PARTITION OF altivy.%I FOR VALUES FROM (%L) TO (%L)',
                    partition_table,
                    parent_table,
                    partition_month::timestamptz,
                    next_month::timestamptz);
            END IF;
        END LOOP;

        partition_month := next_month;
    END LOOP;
END;
$$;

REVOKE ALL ON FUNCTION altivy.altivy_ensure_monthly_queue_partitions(date) FROM PUBLIC;
SELECT altivy.altivy_ensure_monthly_queue_partitions(DATE '2037-01-01');

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'altivy_runtime') THEN
        REVOKE INSERT, UPDATE, DELETE ON altivy.note_index_job_keys FROM altivy_runtime;
        REVOKE INSERT, UPDATE, DELETE ON altivy.vault_provisioning_operation_keys FROM altivy_runtime;
    END IF;
END
$$;

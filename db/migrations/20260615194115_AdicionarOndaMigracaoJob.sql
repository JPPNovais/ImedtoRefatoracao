-- Migration: AdicionarOndaMigracaoJob
-- Adiciona coluna `onda` em migracao_jobs para distinguir Onda 1 (pacientes) de Onda 2 (prontuário).
-- Nota: o RenameIndex foi removido pois o índice ix_migracao_jobs_template_origem_id já existe
-- com nome correto (minúsculo) no banco. O EF detectou divergência no snapshot, mas o índice
-- real não tinha maiúsculas — manter o RENAME causaria falha em produção.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260615194115_AdicionarOndaMigracaoJob') THEN
    ALTER TABLE public.migracao_jobs ADD onda varchar(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260615194115_AdicionarOndaMigracaoJob') THEN
    CREATE INDEX IF NOT EXISTS ix_migracao_jobs_estab_onda_status ON public.migracao_jobs (estabelecimento_id, onda, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260615194115_AdicionarOndaMigracaoJob') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260615194115_AdicionarOndaMigracaoJob', '10.0.0');
    END IF;
END $EF$;

COMMIT;

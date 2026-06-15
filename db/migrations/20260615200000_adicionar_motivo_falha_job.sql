-- Migration: 20260615200000_adicionar_motivo_falha_job
-- Contexto: Central de Migração (briefing 2026-06-15_001 + addendum 002)
-- Adiciona suporte ao estado "falhou" em migracao_jobs com dois campos:
--   motivo_falha       — categoria genérica da falha, sem PII
--   status_antes_falha — status anterior ao "falhou", para retomada no Reprocessar
--
-- Sem BEGIN/COMMIT — a pipeline gerencia a transação em migrate.sh.
-- Idempotente via ADD COLUMN IF NOT EXISTS.

DO $$ BEGIN
    PERFORM 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'migracao_jobs'
      AND column_name  = 'motivo_falha';
    IF NOT FOUND THEN
        ALTER TABLE public.migracao_jobs ADD COLUMN motivo_falha text NULL;
    END IF;
END $$;

DO $$ BEGIN
    PERFORM 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'migracao_jobs'
      AND column_name  = 'status_antes_falha';
    IF NOT FOUND THEN
        ALTER TABLE public.migracao_jobs ADD COLUMN status_antes_falha text NULL;
    END IF;
END $$;

-- Registro no histórico de migrations do EF Core (idempotente)
INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260615200000_AdicionarMotivoFalhaJob', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Migration: migracao_mapas_nome_bloco_origem
-- Briefing: 2026-06-15_005 addendum 4
-- Objetivo: Suportar múltiplos blocos do mesmo dump JSON classificados na mesma entidade canônica.
--           Adiciona nome_bloco_origem como coluna separada (filtro/ordenação no painel de revisão).
--           Altera unique constraint de (migracao_job_id, entidade) para (migracao_job_id, entidade, nome_bloco_origem).
-- Idempotente: ADD COLUMN IF NOT EXISTS + DROP CONSTRAINT IF EXISTS antes de CREATE.
-- Nota: BEGIN/COMMIT removidos — pipeline gerencia transação via migrate.sh.

-- ── 1. Adicionar coluna nome_bloco_origem ────────────────────────────────────────
ALTER TABLE migracao_mapas
    ADD COLUMN IF NOT EXISTS nome_bloco_origem text NOT NULL DEFAULT '';

-- ── 2. Remover índice/constraint unique antigo ──────────────────────────────────
-- O índice pode ter sido criado via CREATE UNIQUE INDEX ou ADD CONSTRAINT UNIQUE.
-- DROP INDEX cobre ambos os casos no Postgres (idempotente com IF EXISTS).
DROP INDEX IF EXISTS uq_migracao_mapas_job_entidade;
-- Caso tenha sido criado como constraint nomeada (adicional por segurança):
ALTER TABLE migracao_mapas
    DROP CONSTRAINT IF EXISTS uq_migracao_mapas_job_entidade;

-- ── 3. Criar nova unique constraint com nome_bloco_origem ────────────────────────
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_migracao_mapas_job_entidade_bloco'
          AND conrelid = 'migracao_mapas'::regclass
    ) THEN
        ALTER TABLE migracao_mapas
            ADD CONSTRAINT uq_migracao_mapas_job_entidade_bloco
            UNIQUE (migracao_job_id, entidade, nome_bloco_origem);
    END IF;
END
$$;

-- ── 4. Índice de suporte para filtro/ordenação por bloco ─────────────────────────
-- Consultas no painel de revisão filtrarão por (migracao_job_id, nome_bloco_origem).
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE tablename = 'migracao_mapas'
          AND indexname = 'ix_migracao_mapas_job_bloco'
    ) THEN
        CREATE INDEX ix_migracao_mapas_job_bloco
            ON migracao_mapas (migracao_job_id, nome_bloco_origem);
    END IF;
END
$$;

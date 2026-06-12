-- =============================================================================
-- Índices CONCURRENTLY — Migration 20260612120100
-- Briefing: 2026-06-12_002 — Termo de consentimento físico-primeiro
--
-- ATENÇÃO: CREATE INDEX CONCURRENTLY não pode executar dentro de transação.
-- Este arquivo é aplicado SEPARADAMENTE pela pipeline, fora do bloco de
-- transação gerenciado pelo migrate.sh:
--
--   psql $DSN -c "\i db/migrations/20260612120100_..._indices.sql"
--
-- Idempotente: IF NOT EXISTS garante que rodar 2x não gera erro.
-- =============================================================================

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_termo_emitido_evolucao_id
    ON public.termo_emitido (evolucao_id);

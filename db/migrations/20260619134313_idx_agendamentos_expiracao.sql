-- Migration: 20260619134313_idx_agendamentos_expiracao
-- Briefing 2026-06-19_001 — job noturno de expiração de agendamentos não finalizados.
--
-- Cria índice parcial em agendamentos (inicio_previsto) restrito aos status
-- que o job varre cross-tenant: 'Agendado' e 'Confirmado'.
-- Exclui automaticamente linhas já em estados terminais (Cancelado, Concluido, Expirado),
-- mantendo o índice pequeno e a varredura do job eficiente.
--
-- CONCURRENTLY: não bloqueia leituras/escritas durante a criação.
-- IF NOT EXISTS: idempotente.
-- Sem BEGIN/COMMIT — a pipeline (migrate.sh) gerencia a transação externamente.

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_agendamentos_expiracao
    ON public.agendamentos (inicio_previsto)
    WHERE status IN ('Agendado', 'Confirmado');

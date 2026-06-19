-- Migration: 20260619134314_seed_job_expirar_agendamentos
-- Briefing 2026-06-19_001 — job noturno de expiração de agendamentos não finalizados.
--
-- Semeia o job recorrente diário que expira agendamentos em status 'Agendado' ou
-- 'Confirmado' cujo inicio_previsto ficou em D-1 sem receber baixa manual.
--
-- Schema da tabela jobs_agendados (sem coluna 'habilitado'):
--   id (bigint identity), nome (varchar 120, unique), proximo_run_em (timestamptz),
--   ultimo_run_em (timestamptz null), intervalo_seg (int), status (varchar 20),
--   ultima_falha (varchar 500 null), tentativas (int), criado_em (timestamptz),
--   atualizado_em (timestamptz null)
--
-- proximo_run_em: próxima ocorrência de 06:00 UTC a partir de NOW().
--   Se agora < 06:00 UTC → hoje às 06:00 UTC.
--   Se agora >= 06:00 UTC → amanhã às 06:00 UTC.
-- intervalo_seg = 86400 (24h).
-- status = 'Pendente' (valor canônico de JobStatus.Pendente persistido como string).
--
-- ON CONFLICT (nome) DO NOTHING — idempotente; não sobrescreve job já existente.
-- Sem BEGIN/COMMIT — a pipeline (migrate.sh) gerencia a transação externamente.

INSERT INTO public.jobs_agendados (
    nome,
    proximo_run_em,
    ultimo_run_em,
    intervalo_seg,
    status,
    ultima_falha,
    tentativas,
    criado_em,
    atualizado_em
)
VALUES (
    'expirar-agendamentos-nao-finalizados',
    -- Próxima ocorrência de 06:00 UTC: hoje se ainda não passou, amanhã caso contrário.
    DATE_TRUNC('day', NOW() AT TIME ZONE 'UTC') AT TIME ZONE 'UTC'
        + INTERVAL '6 hours'
        + CASE
            WHEN (NOW() AT TIME ZONE 'UTC')::time < TIME '06:00:00' THEN INTERVAL '0'
            ELSE INTERVAL '1 day'
          END,
    NULL,           -- ultimo_run_em
    86400,          -- intervalo_seg (24h)
    'Pendente',     -- JobStatus.Pendente
    NULL,           -- ultima_falha
    0,              -- tentativas
    NOW(),          -- criado_em
    NULL            -- atualizado_em
)
ON CONFLICT (nome) DO NOTHING;

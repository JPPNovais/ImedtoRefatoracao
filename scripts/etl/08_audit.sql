-- ===========================================================================
-- Script: 08_audit.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: scripts 02-07
--
-- Objetivo: tabelas de auditoria/compliance
--   - prontuario_acesso_log    (forward-only -- NAO migra do legado)
--   - audit_delete_attempts    (forward-only -- NAO migra do legado)
--   - lgpd_anonimizacoes       (migra historico se houver)
--   - lgpd_consentimentos      (migra; base legal explicita)
--   - ai_audit_logs            (direct copy)
--   - ai_rate_limits           (direct copy)
--
-- Decisao LGPD (ETL_MAPEAMENTO 12):
--   * lgpd_access_log legado NAO migra -- audit forward-only. Logs antigos
--     ficam no projeto legado pelo prazo legal (5 anos LGPD). Snapshot
--     do legado fica congelado e arquivado.
--   * audit_delete_attempts e prontuario_acesso_log sao forward-only -- o
--     destino comeca limpo apos cutover.
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

DO $$ BEGIN
    IF (SELECT COUNT(*) FROM public.estabelecimentos) = 0 THEN
        RAISE EXCEPTION 'public.estabelecimentos vazia. Rode scripts anteriores.';
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- prontuario_acesso_log -- FORWARD-ONLY (nao migra)
-- ---------------------------------------------------------------------------
-- (intencionalmente vazio -- documentado em ETL_MAPEAMENTO 4)

-- ---------------------------------------------------------------------------
-- audit_delete_attempts -- FORWARD-ONLY (nao migra)
-- ---------------------------------------------------------------------------
-- (intencionalmente vazio)

-- ---------------------------------------------------------------------------
-- lgpd_anonimizacoes (historico de exercicios do direito de exclusao)
-- Migra se a tabela legada existe; senao salta sem erro.
-- ---------------------------------------------------------------------------
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'legado' AND tablename = 'lgpd_anonimizacoes') THEN
        INSERT INTO public.lgpd_anonimizacoes (
            id, estabelecimento_id, paciente_id, motivo,
            campos_anonimizados, hash_referencia, criado_em
        )
        SELECT
            leg.id, leg.estabelecimento_id, leg.paciente_id, leg.motivo,
            COALESCE(leg.campos_anonimizados, '[]'::jsonb),
            leg.hash_referencia, leg.created_at
        FROM legado.lgpd_anonimizacoes leg
        WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
        ON CONFLICT (id) DO NOTHING;
    ELSE
        RAISE NOTICE 'legado.lgpd_anonimizacoes nao existe -- nada a migrar.';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- lgpd_consentimentos (base legal explicita -- migra integralmente)
-- ---------------------------------------------------------------------------
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'legado' AND tablename = 'lgpd_consentimentos') THEN
        INSERT INTO public.lgpd_consentimentos (
            id, estabelecimento_id, paciente_id, finalidade, base_legal,
            concedido, concedido_em, revogado_em, ip_origem, user_agent,
            criado_em, atualizado_em
        )
        SELECT
            leg.id, leg.estabelecimento_id, leg.paciente_id, leg.finalidade,
            leg.base_legal, COALESCE(leg.concedido, false),
            leg.concedido_em, leg.revogado_em,
            leg.ip_origem, leg.user_agent,
            leg.created_at, leg.updated_at
        FROM legado.lgpd_consentimentos leg
        WHERE EXISTS (SELECT 1 FROM public.pacientes p WHERE p.id = leg.paciente_id)
        ON CONFLICT (id) DO NOTHING;
    ELSE
        RAISE NOTICE 'legado.lgpd_consentimentos nao existe -- nada a migrar.';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- ai_audit_logs (direct copy)
-- ---------------------------------------------------------------------------
INSERT INTO public.ai_audit_logs (
    id, estabelecimento_id, usuario_id, modelo, prompt_hash,
    tokens_input, tokens_output, latencia_ms, custo_usd, status, erro,
    criado_em
)
SELECT
    leg.id, leg.estabelecimento_id,
    mu.novo_id          AS usuario_id,
    leg.modelo, leg.prompt_hash,
    leg.tokens_input, leg.tokens_output, leg.latencia_ms, leg.custo_usd,
    leg.status, leg.erro, leg.created_at
FROM legado.ai_audit_logs leg
LEFT JOIN _etl.mapping_usuarios mu ON mu.legado_id = leg.usuario_id
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- ai_rate_limits (direct copy)
-- ---------------------------------------------------------------------------
INSERT INTO public.ai_rate_limits (
    id, estabelecimento_id, janela_inicio, janela_fim,
    tokens_consumidos, requests_count, criado_em, atualizado_em
)
SELECT
    leg.id, leg.estabelecimento_id, leg.janela_inicio, leg.janela_fim,
    COALESCE(leg.tokens_consumidos, 0),
    COALESCE(leg.requests_count, 0),
    leg.created_at, leg.updated_at
FROM legado.ai_rate_limits leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('08_audit.sql', now(),
        'lgpd_anonimizacoes/consentimentos/ai_audit_logs/ai_rate_limits carregados; access_log e delete_attempts forward-only');

COMMIT;

-- Pos-flight
SELECT 'lgpd_anonimizacoes'  AS tabela, COUNT(*) FROM public.lgpd_anonimizacoes
UNION ALL SELECT 'lgpd_consentimentos', COUNT(*) FROM public.lgpd_consentimentos
UNION ALL SELECT 'ai_audit_logs',       COUNT(*) FROM public.ai_audit_logs
UNION ALL SELECT 'ai_rate_limits',      COUNT(*) FROM public.ai_rate_limits;

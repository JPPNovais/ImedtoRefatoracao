-- ===========================================================================
-- Script: 04_configuracoes.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 03_dominio_core.sql
--
-- Objetivo: configuracoes por estabelecimento
--   - establishment_ai_settings
--   - assinaturas (com mapping de plano_id)
--   - categorias_financeiras       (rename de financeiro_categoria)
--   - formas_pagamento             (rename de financeiro_forma_pagamento)
--   - receitas_configuracao_estabelecimento
--
-- Notas:
--   * Catalogos `planos` ja seedados no destino -- usamos _etl.mapping_planos.
--   * categorias_financeiras e formas_pagamento mantem id legado (uuid) para
--     simplificar FKs em lancamentos (script 07).
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

DO $$ BEGIN
    IF (SELECT COUNT(*) FROM public.estabelecimentos) = 0 THEN
        RAISE EXCEPTION 'public.estabelecimentos vazia. Rode 02 antes.';
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- establishment_ai_settings
-- ---------------------------------------------------------------------------
INSERT INTO public.establishment_ai_settings (
    id,
    estabelecimento_id,
    feature_enabled,
    daily_token_limit,
    monthly_token_limit,
    modelo_padrao,
    config_extra,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    COALESCE(leg.feature_enabled, false),
    leg.daily_token_limit,
    leg.monthly_token_limit,
    leg.modelo_padrao,
    COALESCE(leg.config_extra, '{}'::jsonb),
    leg.created_at,
    leg.updated_at
FROM legado.establishment_ai_settings leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO UPDATE SET
    feature_enabled       = EXCLUDED.feature_enabled,
    daily_token_limit     = EXCLUDED.daily_token_limit,
    monthly_token_limit   = EXCLUDED.monthly_token_limit,
    modelo_padrao         = EXCLUDED.modelo_padrao,
    config_extra          = EXCLUDED.config_extra,
    atualizado_em         = EXCLUDED.atualizado_em;

-- ---------------------------------------------------------------------------
-- assinaturas (re-link plano_id via mapping_planos)
-- ---------------------------------------------------------------------------
INSERT INTO public.assinaturas (
    id,
    estabelecimento_id,
    plano_id,
    status,
    inicio,
    fim,
    proxima_cobranca,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    mp.novo_id            AS plano_id,
    INITCAP(leg.status)   AS status,
    leg.inicio,
    leg.fim,
    leg.proxima_cobranca,
    leg.created_at,
    leg.updated_at
FROM legado.assinaturas leg
JOIN _etl.mapping_planos mp ON mp.legado_id = leg.plano_id
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- categorias_financeiras (rename de financeiro_categoria)
-- ---------------------------------------------------------------------------
INSERT INTO public.categorias_financeiras (
    id,
    estabelecimento_id,
    nome,
    tipo,             -- 'Receita' | 'Despesa'
    cor,
    icone,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.nome,
    INITCAP(leg.tipo) AS tipo,
    leg.cor,
    leg.icone,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END
FROM legado.financeiro_categoria leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- formas_pagamento (rename de financeiro_forma_pagamento)
-- ---------------------------------------------------------------------------
INSERT INTO public.formas_pagamento (
    id,
    estabelecimento_id,
    nome,
    tipo,
    taxa_percentual,
    prazo_recebimento_dias,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.nome,
    INITCAP(leg.tipo)              AS tipo,
    leg.taxa_percentual,
    leg.prazo_recebimento_dias,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END
FROM legado.financeiro_forma_pagamento leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- receitas_configuracao_estabelecimento
-- ---------------------------------------------------------------------------
INSERT INTO public.receitas_configuracao_estabelecimento (
    id,
    estabelecimento_id,
    feature_enabled,
    cabecalho_personalizado,
    rodape_personalizado,
    config_extra,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    COALESCE(leg.feature_enabled, false),
    leg.cabecalho_personalizado,
    leg.rodape_personalizado,
    COALESCE(leg.config_extra, '{}'::jsonb),
    leg.created_at,
    leg.updated_at
FROM legado.receitas_configuracao_estabelecimento leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO UPDATE SET
    feature_enabled         = EXCLUDED.feature_enabled,
    cabecalho_personalizado = EXCLUDED.cabecalho_personalizado,
    rodape_personalizado    = EXCLUDED.rodape_personalizado,
    config_extra            = EXCLUDED.config_extra,
    atualizado_em           = EXCLUDED.atualizado_em;

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('04_configuracoes.sql', now(), 'configuracoes por estabelecimento carregadas');

COMMIT;

-- Pos-flight
SELECT 'establishment_ai_settings'             AS tabela, COUNT(*) FROM public.establishment_ai_settings
UNION ALL SELECT 'assinaturas',                            COUNT(*) FROM public.assinaturas
UNION ALL SELECT 'categorias_financeiras',                 COUNT(*) FROM public.categorias_financeiras
UNION ALL SELECT 'formas_pagamento',                       COUNT(*) FROM public.formas_pagamento
UNION ALL SELECT 'receitas_configuracao_estabelecimento',  COUNT(*) FROM public.receitas_configuracao_estabelecimento;

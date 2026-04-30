-- Fase 4 — RLS para tabelas criadas em 20260430010241_fase4_schema.
-- Sem par EF — SQL puro (defense-in-depth + LGPD).
--
-- Categorias:
--   A) solicitacoes_vinculo  — fluxo inverso (profissional pede vínculo).
--      O profissional ainda NÃO tem vínculo ativo no estabelecimento, então
--      a leitura é permitida pelos dois lados envolvidos:
--        - profissional_usuario_id = auth.uid()  (o próprio solicitante)
--        - estabelecimento.dono_usuario_id = auth.uid() (o destinatário)
--   B) lgpd_consentimentos    — cada titular vê apenas os próprios.
--   C) lgpd_anonimizacoes     — audit interno; bloqueado para roles públicas
--                               (apenas service_role acessa via backend).
--   D) catalogo_procedimentos — reference data global; SELECT liberado para
--                               authenticated quando ativo = true.
--
-- Convenções (espelham 20260429171109_rls_fase3_tabelas.sql):
--   - vinculo_profissional_estabelecimento (singular) — não usado neste arquivo,
--     mas mantido como padrão de referência.
--   - profissional_usuario_id mapeia para auth.uid().
--   - service_role bypassa RLS automaticamente.

-- =====================================================================
-- A) solicitacoes_vinculo — fluxo inverso (profissional -> estabelecimento)
-- =====================================================================
ALTER TABLE public.solicitacoes_vinculo ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.solicitacoes_vinculo FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS solicitacoes_vinculo_select_envolvidos ON public.solicitacoes_vinculo;
CREATE POLICY solicitacoes_vinculo_select_envolvidos
    ON public.solicitacoes_vinculo
    FOR SELECT
    TO authenticated
    USING (
        profissional_usuario_id = auth.uid()
        OR estabelecimento_id IN (
            SELECT e.id
            FROM public.estabelecimentos e
            WHERE e.dono_usuario_id = auth.uid()
        )
    );

-- =====================================================================
-- B) lgpd_consentimentos — cada titular só vê os próprios
-- =====================================================================
ALTER TABLE public.lgpd_consentimentos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.lgpd_consentimentos FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS lgpd_consentimentos_select_proprio ON public.lgpd_consentimentos;
CREATE POLICY lgpd_consentimentos_select_proprio
    ON public.lgpd_consentimentos
    FOR SELECT
    TO authenticated
    USING (usuario_id = auth.uid());

-- =====================================================================
-- C) lgpd_anonimizacoes — audit interno (apenas service_role)
-- =====================================================================
-- RLS habilitada SEM policies para roles públicas == acesso negado por padrão.
-- service_role bypassa RLS, portanto o backend continua escrevendo/lendo normalmente.
ALTER TABLE public.lgpd_anonimizacoes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.lgpd_anonimizacoes FORCE ROW LEVEL SECURITY;

REVOKE ALL ON public.lgpd_anonimizacoes FROM authenticated, anon;

-- =====================================================================
-- D) catalogo_procedimentos — reference data global
-- =====================================================================
-- Catálogo TUSS/CBHPM compartilhado entre tenants, sem PII.
-- INSERT/UPDATE/DELETE só via migration/seed (service_role).
ALTER TABLE public.catalogo_procedimentos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.catalogo_procedimentos FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS catalogo_procedimentos_select_authenticated ON public.catalogo_procedimentos;
CREATE POLICY catalogo_procedimentos_select_authenticated
    ON public.catalogo_procedimentos
    FOR SELECT
    TO authenticated
    USING (ativo = true);

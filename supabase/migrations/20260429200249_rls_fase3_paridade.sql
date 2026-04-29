-- Fase 3 — Paridade RLS para tabelas criadas em 20260429200248_fase3_paridade_schema.
-- Sem par EF — SQL puro.
--
-- Principio (defense-in-depth + LGPD): backend valida permissao via JWT/dominio.
-- RLS impede vazamento mesmo se o backend errar. service_role bypassa RLS.
--
-- Categorias:
--   A) Cascata via parent (orcamentos): orcamento_cirurgias, orcamento_internacao,
--      orcamento_anestesia. Filtra por estabelecimento_id do orcamento parent.
--   B) Reference data global: regioes_anatomicas_catalogo. SELECT liberado para
--      qualquer authenticated (catalogo nao tem PII e e compartilhado entre tenants).
--
-- Convencoes (espelham 20260429171109_rls_fase3_tabelas.sql):
--   - vinculo_profissional_estabelecimento (singular).
--   - profissional_usuario_id mapeia para auth.uid().
--   - Status 'Ativo' (capital A) — mesmo casing das migrations anteriores.

-- =====================================================================
-- A) Cascata via parent (orcamentos)
-- =====================================================================

-- ---------------------------------------------------------------------
-- orcamento_cirurgias -> orcamentos
-- ---------------------------------------------------------------------
ALTER TABLE public.orcamento_cirurgias ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orcamento_cirurgias FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS orcamento_cirurgias_select_via_parent ON public.orcamento_cirurgias;
CREATE POLICY orcamento_cirurgias_select_via_parent
    ON public.orcamento_cirurgias
    FOR SELECT
    TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM public.orcamentos o
            WHERE o.estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM public.vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = auth.uid()
                  AND v.status = 'Ativo'
            )
            OR o.estabelecimento_id IN (
                SELECT e.id
                FROM public.estabelecimentos e
                WHERE e.dono_usuario_id = auth.uid()
            )
        )
    );

-- ---------------------------------------------------------------------
-- orcamento_internacao -> orcamentos
-- ---------------------------------------------------------------------
ALTER TABLE public.orcamento_internacao ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orcamento_internacao FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS orcamento_internacao_select_via_parent ON public.orcamento_internacao;
CREATE POLICY orcamento_internacao_select_via_parent
    ON public.orcamento_internacao
    FOR SELECT
    TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM public.orcamentos o
            WHERE o.estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM public.vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = auth.uid()
                  AND v.status = 'Ativo'
            )
            OR o.estabelecimento_id IN (
                SELECT e.id
                FROM public.estabelecimentos e
                WHERE e.dono_usuario_id = auth.uid()
            )
        )
    );

-- ---------------------------------------------------------------------
-- orcamento_anestesia -> orcamentos
-- ---------------------------------------------------------------------
ALTER TABLE public.orcamento_anestesia ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orcamento_anestesia FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS orcamento_anestesia_select_via_parent ON public.orcamento_anestesia;
CREATE POLICY orcamento_anestesia_select_via_parent
    ON public.orcamento_anestesia
    FOR SELECT
    TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM public.orcamentos o
            WHERE o.estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM public.vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = auth.uid()
                  AND v.status = 'Ativo'
            )
            OR o.estabelecimento_id IN (
                SELECT e.id
                FROM public.estabelecimentos e
                WHERE e.dono_usuario_id = auth.uid()
            )
        )
    );

-- =====================================================================
-- B) Reference data global (regioes_anatomicas_catalogo)
-- =====================================================================
--
-- Catalogo global, sem PII e sem coluna estabelecimento_id. Liberar SELECT
-- para qualquer authenticated. INSERT/UPDATE/DELETE so via service_role
-- (curadoria via migration/seed).
ALTER TABLE public.regioes_anatomicas_catalogo ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.regioes_anatomicas_catalogo FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS regioes_anatomicas_catalogo_select_authenticated ON public.regioes_anatomicas_catalogo;
CREATE POLICY regioes_anatomicas_catalogo_select_authenticated
    ON public.regioes_anatomicas_catalogo
    FOR SELECT
    TO authenticated
    USING (ativo = true);

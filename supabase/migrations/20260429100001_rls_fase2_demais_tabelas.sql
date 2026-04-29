-- Fase 2 — Wave 5: RLS para as tabelas das Waves 1, 2, 3 e 4 que ainda nao tinham
-- politicas declaradas. Sem par EF — SQL puro.
--
-- Princípio (defense-in-depth + LGPD): backend valida permissao via JWT/dominio.
-- RLS impede vazamento mesmo se o backend errar. service_role bypassa RLS.
--
-- Categorias:
--   A) Tenant-scoped (estabelecimento_id) — leitura para vinculados ativos OU dono;
--      INSERT/UPDATE/DELETE: somente service_role.
--      Tabelas: notificacoes (filtrada por usuario_id), automation_rules,
--               establishment_ai_settings, assinaturas, categorias_financeiras,
--               formas_pagamento.
--
--   B) Reference data (publica para autenticados, somente leitura).
--      Tabelas: planos, profissoes, especialidades.
--
--   C) Internas (somente service_role).
--      Tabelas: automation_events, idempotency_keys, jobs_agendados.

-- =====================================================================
-- A) Tenant-scoped
-- =====================================================================

-- ---------------------------------------------------------------------
-- notificacoes: filtro por usuario_id (cada um ve apenas as suas).
-- estabelecimento_id eh contextual; tenant nao deve "ver as notificacoes
-- dos colegas" mesmo sendo do mesmo estabelecimento.
-- ---------------------------------------------------------------------
ALTER TABLE public.notificacoes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.notificacoes FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS notificacoes_select_proprio ON public.notificacoes;
CREATE POLICY notificacoes_select_proprio
    ON public.notificacoes
    FOR SELECT
    TO authenticated
    USING (usuario_id = auth.uid());

-- INSERT/UPDATE/DELETE: sem policy = bloqueado para authenticated. Apenas service_role.

-- ---------------------------------------------------------------------
-- automation_rules: tenant-scoped via estabelecimento_id.
-- ---------------------------------------------------------------------
ALTER TABLE public.automation_rules ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.automation_rules FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS automation_rules_select_tenant ON public.automation_rules;
CREATE POLICY automation_rules_select_tenant
    ON public.automation_rules
    FOR SELECT
    TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM public.vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = auth.uid()
              AND v.status = 'Ativo'
        )
        OR estabelecimento_id IN (
            SELECT e.id
            FROM public.estabelecimentos e
            WHERE e.dono_usuario_id = auth.uid()
        )
    );

-- ---------------------------------------------------------------------
-- establishment_ai_settings: PK = estabelecimento_id, 1:1 com tenant.
-- ---------------------------------------------------------------------
ALTER TABLE public.establishment_ai_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.establishment_ai_settings FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS establishment_ai_settings_select_tenant ON public.establishment_ai_settings;
CREATE POLICY establishment_ai_settings_select_tenant
    ON public.establishment_ai_settings
    FOR SELECT
    TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM public.vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = auth.uid()
              AND v.status = 'Ativo'
        )
        OR estabelecimento_id IN (
            SELECT e.id
            FROM public.estabelecimentos e
            WHERE e.dono_usuario_id = auth.uid()
        )
    );

-- ---------------------------------------------------------------------
-- assinaturas: tenant-scoped via estabelecimento_id.
-- ---------------------------------------------------------------------
ALTER TABLE public.assinaturas ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.assinaturas FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS assinaturas_select_tenant ON public.assinaturas;
CREATE POLICY assinaturas_select_tenant
    ON public.assinaturas
    FOR SELECT
    TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM public.vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = auth.uid()
              AND v.status = 'Ativo'
        )
        OR estabelecimento_id IN (
            SELECT e.id
            FROM public.estabelecimentos e
            WHERE e.dono_usuario_id = auth.uid()
        )
    );

-- ---------------------------------------------------------------------
-- categorias_financeiras: tenant-scoped (criada na Wave 1).
-- ---------------------------------------------------------------------
ALTER TABLE public.categorias_financeiras ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.categorias_financeiras FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS categorias_financeiras_select_tenant ON public.categorias_financeiras;
CREATE POLICY categorias_financeiras_select_tenant
    ON public.categorias_financeiras
    FOR SELECT
    TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM public.vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = auth.uid()
              AND v.status = 'Ativo'
        )
        OR estabelecimento_id IN (
            SELECT e.id
            FROM public.estabelecimentos e
            WHERE e.dono_usuario_id = auth.uid()
        )
    );

-- ---------------------------------------------------------------------
-- formas_pagamento: tenant-scoped (criada na Wave 1).
-- ---------------------------------------------------------------------
ALTER TABLE public.formas_pagamento ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.formas_pagamento FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS formas_pagamento_select_tenant ON public.formas_pagamento;
CREATE POLICY formas_pagamento_select_tenant
    ON public.formas_pagamento
    FOR SELECT
    TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM public.vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = auth.uid()
              AND v.status = 'Ativo'
        )
        OR estabelecimento_id IN (
            SELECT e.id
            FROM public.estabelecimentos e
            WHERE e.dono_usuario_id = auth.uid()
        )
    );

-- =====================================================================
-- B) Reference data (publico para authenticated)
-- =====================================================================

-- ---------------------------------------------------------------------
-- planos: catalogo publico de planos da plataforma.
-- Qualquer authenticated le; INSERT/UPDATE/DELETE somente service_role.
-- ---------------------------------------------------------------------
ALTER TABLE public.planos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.planos FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS planos_select_authenticated ON public.planos;
CREATE POLICY planos_select_authenticated
    ON public.planos
    FOR SELECT
    TO authenticated
    USING (true);

-- ---------------------------------------------------------------------
-- profissoes: tabela de referencia (CBO, etc).
-- ---------------------------------------------------------------------
ALTER TABLE public.profissoes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.profissoes FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS profissoes_select_authenticated ON public.profissoes;
CREATE POLICY profissoes_select_authenticated
    ON public.profissoes
    FOR SELECT
    TO authenticated
    USING (true);

-- ---------------------------------------------------------------------
-- especialidades: tabela de referencia.
-- ---------------------------------------------------------------------
ALTER TABLE public.especialidades ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.especialidades FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS especialidades_select_authenticated ON public.especialidades;
CREATE POLICY especialidades_select_authenticated
    ON public.especialidades
    FOR SELECT
    TO authenticated
    USING (true);

-- =====================================================================
-- C) Internas (somente service_role)
-- =====================================================================

-- ---------------------------------------------------------------------
-- automation_events: fila interna do worker. authenticated nao deve ver.
-- ---------------------------------------------------------------------
ALTER TABLE public.automation_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.automation_events FORCE ROW LEVEL SECURITY;

REVOKE ALL ON public.automation_events FROM anon, authenticated;

-- Sem policies — apenas service_role.

-- ---------------------------------------------------------------------
-- idempotency_keys: chaves de idempotencia (administrativa).
-- ---------------------------------------------------------------------
ALTER TABLE public.idempotency_keys ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.idempotency_keys FORCE ROW LEVEL SECURITY;

REVOKE ALL ON public.idempotency_keys FROM anon, authenticated;

-- Sem policies — apenas service_role.

-- ---------------------------------------------------------------------
-- jobs_agendados: estado do scheduler (administrativa).
-- ---------------------------------------------------------------------
ALTER TABLE public.jobs_agendados ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.jobs_agendados FORCE ROW LEVEL SECURITY;

REVOKE ALL ON public.jobs_agendados FROM anon, authenticated;

-- Sem policies — apenas service_role.

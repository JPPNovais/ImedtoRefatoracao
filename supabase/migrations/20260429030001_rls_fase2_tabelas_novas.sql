-- Item 2.18 — Fase 2: RLS das tabelas novas da Fase 1.
-- Cobertura: ai_outputs_cache, ai_rate_limits (administrativas internas),
--            ai_audit_logs, audit_delete_attempts (tenant-scoped, leitura para admins do tenant).
--
-- Princípio (defense-in-depth + LGPD): backend valida permissao via JWT e
-- regras de dominio. RLS impede vazamento mesmo se o backend errar.
-- service_role (usado pelo backend) bypassa RLS — backend continua escrevendo normalmente.
--
-- Decisoes:
-- 1. ai_outputs_cache / ai_rate_limits: nao tem dados de tenant. So service_role acessa.
--    Sem policy permissiva = bloqueio total para anon/authenticated.
-- 2. ai_audit_logs / audit_delete_attempts: tenant-scoped via estabelecimento_id.
--    Leitura permitida para usuarios vinculados (status='Ativo') ou dono do estabelecimento.
--    INSERT/UPDATE/DELETE: somente service_role (sem policy permissiva).
-- 3. audit_delete_attempts.estabelecimento_id pode ser NULL (linhas globais — futuras
--    tentativas de delete em tabelas sem tenant). Decisao: bloqueio total para
--    authenticated quando NULL — somente service_role/admins-imedto leem.
--    Justificativa: linha global pode revelar atividade de outros tenants.

-- =====================================================================
-- ai_outputs_cache — administrativa interna (cache de outputs de IA)
-- =====================================================================
ALTER TABLE public.ai_outputs_cache ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ai_outputs_cache FORCE ROW LEVEL SECURITY;

REVOKE ALL ON public.ai_outputs_cache FROM anon, authenticated;

-- Sem policies — apenas service_role acessa.

-- =====================================================================
-- ai_rate_limits — administrativa interna (controle de taxa por usuario)
-- =====================================================================
ALTER TABLE public.ai_rate_limits ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ai_rate_limits FORCE ROW LEVEL SECURITY;

REVOKE ALL ON public.ai_rate_limits FROM anon, authenticated;

-- Sem policies — apenas service_role acessa.

-- =====================================================================
-- ai_audit_logs — tenant-scoped (auditoria de chamadas de IA)
-- =====================================================================
ALTER TABLE public.ai_audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ai_audit_logs FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS ai_audit_logs_select_tenant ON public.ai_audit_logs;
CREATE POLICY ai_audit_logs_select_tenant
    ON public.ai_audit_logs
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

-- INSERT/UPDATE/DELETE: sem policy = bloqueado para authenticated. Apenas service_role escreve.

-- =====================================================================
-- audit_delete_attempts — tenant-scoped (auditoria de tentativas de delete)
-- =====================================================================
ALTER TABLE public.audit_delete_attempts ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.audit_delete_attempts FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS audit_delete_attempts_select_tenant ON public.audit_delete_attempts;
CREATE POLICY audit_delete_attempts_select_tenant
    ON public.audit_delete_attempts
    FOR SELECT
    TO authenticated
    USING (
        estabelecimento_id IS NOT NULL
        AND (
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
        )
    );

-- INSERT/UPDATE/DELETE: sem policy = bloqueado para authenticated. Apenas service_role escreve.
-- Linhas com estabelecimento_id IS NULL: bloqueadas para authenticated (apenas service_role/admins-imedto).

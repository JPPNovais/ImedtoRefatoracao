-- Fase 3 — RLS para as tabelas clinicas/orcamento criadas em 20260429171108_fase3_schema.
-- Sem par EF — SQL puro.
--
-- Principio (defense-in-depth + LGPD): backend valida permissao via JWT/dominio.
-- RLS impede vazamento mesmo se o backend errar. service_role bypassa RLS.
--
-- Categorias:
--   A) Tenant-scoped (estabelecimento_id direto): leitura para vinculados ativos OU dono;
--      INSERT/UPDATE/DELETE so service_role.
--      Tabelas: receitas, receitas_configuracao_estabelecimento, medicamentos_favoritos,
--               exame_fisico, procedimentos_cirurgicos, orcamento_equipe,
--               orcamento_implantes, orcamento_formas_pagamento.
--
--   B) Cascata via parent (sem coluna estabelecimento_id direta): filtra por FK do parent.
--      Tabelas: receita_itens (-> receitas), exame_fisico_regioes (-> exame_fisico),
--               equipe_cirurgica (-> procedimentos_cirurgicos).
--
-- Convencoes (espelham 20260429100001_rls_fase2_demais_tabelas.sql):
--   - vinculo_profissional_estabelecimento (singular).
--   - profissional_usuario_id mapeia para auth.uid().
--   - Status 'Ativo' (capital A) — mesmo casing das migrations anteriores.

-- =====================================================================
-- A) Tenant-scoped (estabelecimento_id direto)
-- =====================================================================

-- ---------------------------------------------------------------------
-- receitas
-- ---------------------------------------------------------------------
ALTER TABLE public.receitas ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.receitas FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS receitas_select_tenant ON public.receitas;
CREATE POLICY receitas_select_tenant
    ON public.receitas
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
-- receitas_configuracao_estabelecimento (PK = estabelecimento_id, 1:1)
-- ---------------------------------------------------------------------
ALTER TABLE public.receitas_configuracao_estabelecimento ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.receitas_configuracao_estabelecimento FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS receitas_config_select_tenant ON public.receitas_configuracao_estabelecimento;
CREATE POLICY receitas_config_select_tenant
    ON public.receitas_configuracao_estabelecimento
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
-- medicamentos_favoritos
-- ---------------------------------------------------------------------
ALTER TABLE public.medicamentos_favoritos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.medicamentos_favoritos FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS medicamentos_favoritos_select_tenant ON public.medicamentos_favoritos;
CREATE POLICY medicamentos_favoritos_select_tenant
    ON public.medicamentos_favoritos
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
-- exame_fisico
-- ---------------------------------------------------------------------
ALTER TABLE public.exame_fisico ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.exame_fisico FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS exame_fisico_select_tenant ON public.exame_fisico;
CREATE POLICY exame_fisico_select_tenant
    ON public.exame_fisico
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
-- procedimentos_cirurgicos
-- ---------------------------------------------------------------------
ALTER TABLE public.procedimentos_cirurgicos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.procedimentos_cirurgicos FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS procedimentos_cirurgicos_select_tenant ON public.procedimentos_cirurgicos;
CREATE POLICY procedimentos_cirurgicos_select_tenant
    ON public.procedimentos_cirurgicos
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
-- orcamento_equipe / orcamento_implantes / orcamento_formas_pagamento
-- Nao tem coluna estabelecimento_id, mas o orcamento parent tem. Cascata via FK.
-- ---------------------------------------------------------------------
ALTER TABLE public.orcamento_equipe ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orcamento_equipe FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS orcamento_equipe_select_tenant ON public.orcamento_equipe;
CREATE POLICY orcamento_equipe_select_tenant
    ON public.orcamento_equipe
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

ALTER TABLE public.orcamento_implantes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orcamento_implantes FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS orcamento_implantes_select_tenant ON public.orcamento_implantes;
CREATE POLICY orcamento_implantes_select_tenant
    ON public.orcamento_implantes
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

ALTER TABLE public.orcamento_formas_pagamento ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orcamento_formas_pagamento FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS orcamento_formas_pagamento_select_tenant ON public.orcamento_formas_pagamento;
CREATE POLICY orcamento_formas_pagamento_select_tenant
    ON public.orcamento_formas_pagamento
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
-- B) Cascata via parent
-- =====================================================================

-- ---------------------------------------------------------------------
-- receita_itens -> receitas
-- ---------------------------------------------------------------------
ALTER TABLE public.receita_itens ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.receita_itens FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS receita_itens_select_via_receita ON public.receita_itens;
CREATE POLICY receita_itens_select_via_receita
    ON public.receita_itens
    FOR SELECT
    TO authenticated
    USING (
        receita_id IN (
            SELECT r.id
            FROM public.receitas r
            WHERE r.estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM public.vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = auth.uid()
                  AND v.status = 'Ativo'
            )
            OR r.estabelecimento_id IN (
                SELECT e.id
                FROM public.estabelecimentos e
                WHERE e.dono_usuario_id = auth.uid()
            )
        )
    );

-- ---------------------------------------------------------------------
-- exame_fisico_regioes -> exame_fisico
-- ---------------------------------------------------------------------
ALTER TABLE public.exame_fisico_regioes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.exame_fisico_regioes FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS exame_fisico_regioes_select_via_parent ON public.exame_fisico_regioes;
CREATE POLICY exame_fisico_regioes_select_via_parent
    ON public.exame_fisico_regioes
    FOR SELECT
    TO authenticated
    USING (
        exame_fisico_id IN (
            SELECT ef.id
            FROM public.exame_fisico ef
            WHERE ef.estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM public.vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = auth.uid()
                  AND v.status = 'Ativo'
            )
            OR ef.estabelecimento_id IN (
                SELECT e.id
                FROM public.estabelecimentos e
                WHERE e.dono_usuario_id = auth.uid()
            )
        )
    );

-- ---------------------------------------------------------------------
-- equipe_cirurgica -> procedimentos_cirurgicos
-- ---------------------------------------------------------------------
ALTER TABLE public.equipe_cirurgica ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.equipe_cirurgica FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS equipe_cirurgica_select_via_parent ON public.equipe_cirurgica;
CREATE POLICY equipe_cirurgica_select_via_parent
    ON public.equipe_cirurgica
    FOR SELECT
    TO authenticated
    USING (
        procedimento_id IN (
            SELECT p.id
            FROM public.procedimentos_cirurgicos p
            WHERE p.estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM public.vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = auth.uid()
                  AND v.status = 'Ativo'
            )
            OR p.estabelecimento_id IN (
                SELECT e.id
                FROM public.estabelecimentos e
                WHERE e.dono_usuario_id = auth.uid()
            )
        )
    );

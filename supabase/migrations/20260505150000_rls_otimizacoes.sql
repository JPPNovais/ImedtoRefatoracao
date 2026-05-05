-- =====================================================================
-- 20260505150000_rls_otimizacoes.sql
--
-- Objetivo 1 (Tarefa A) — Otimizar 36 policies do schema public que
-- chamam auth.uid() literalmente (avaliação por linha) reescrevendo
-- para (select auth.uid()) (avaliação 1x por query). Resolve o lint
-- auth_rls_initplan do advisor de performance. Semantica preservada:
-- as policies continuam com o MESMO USING/WITH CHECK, apenas trocando
-- auth.uid() por (select auth.uid()).
--
-- Objetivo 2 (Tarefa B) — Habilitar RLS em 3 tabelas de dominio que
-- estavam sem RLS (advisor rls_disabled_in_public): itens_inventario,
-- orcamentos, paciente_acesso_log. Espelha o padrao existente do
-- projeto: policy unica de SELECT por tenant via meus_estabelecimentos().
-- INSERT/UPDATE/DELETE NAO ganham policy — backend escreve via
-- service_role (BYPASSRLS), seguindo a arquitetura BFF do projeto.
--
-- Idempotencia: DROP POLICY IF EXISTS antes de CREATE POLICY;
-- ALTER TABLE ... ENABLE ROW LEVEL SECURITY e a versao FORCE sao
-- naturalmente idempotentes.
--
-- Trade-offs documentados no fim do arquivo.
-- =====================================================================

-- ---------------------------------------------------------------------
-- SECAO A — Refatoracao de policies para (select auth.uid())
-- ---------------------------------------------------------------------
-- Cada bloco abaixo dropa a policy original e recria com a mesma
-- expressao logica, apenas trocando auth.uid() por (select auth.uid()).
-- Janela DROP+CREATE: enquanto a transacao da migration roda, a policy
-- esta efetivamente "fechada" (nenhuma linha selecionavel via authenticated)
-- — aceitavel porque o backend usa service_role (bypass) e o Supabase
-- CLI envelopa tudo numa unica transacao.

-- 1. ai_audit_logs.ai_audit_logs_select_tenant
DROP POLICY IF EXISTS "ai_audit_logs_select_tenant" ON public.ai_audit_logs;
CREATE POLICY "ai_audit_logs_select_tenant" ON public.ai_audit_logs
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 2. assinaturas.assinaturas_select_tenant
DROP POLICY IF EXISTS "assinaturas_select_tenant" ON public.assinaturas;
CREATE POLICY "assinaturas_select_tenant" ON public.assinaturas
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 3. audit_delete_attempts.audit_delete_attempts_select_tenant
DROP POLICY IF EXISTS "audit_delete_attempts_select_tenant" ON public.audit_delete_attempts;
CREATE POLICY "audit_delete_attempts_select_tenant" ON public.audit_delete_attempts
    FOR SELECT TO authenticated
    USING (
        estabelecimento_id IS NOT NULL
        AND (
            (estabelecimento_id IN (
                SELECT v.estabelecimento_id
                FROM vinculo_profissional_estabelecimento v
                WHERE v.profissional_usuario_id = (select auth.uid())
                  AND v.status::text = 'Ativo'
            ))
            OR
            (estabelecimento_id IN (
                SELECT e.id FROM estabelecimentos e
                WHERE e.dono_usuario_id = (select auth.uid())
            ))
        )
    );

-- 4. automation_rules.automation_rules_select_tenant
DROP POLICY IF EXISTS "automation_rules_select_tenant" ON public.automation_rules;
CREATE POLICY "automation_rules_select_tenant" ON public.automation_rules
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 5. categorias_financeiras.categorias_financeiras_select_tenant
DROP POLICY IF EXISTS "categorias_financeiras_select_tenant" ON public.categorias_financeiras;
CREATE POLICY "categorias_financeiras_select_tenant" ON public.categorias_financeiras
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 6. equipe_cirurgica.equipe_cirurgica_select_via_parent
DROP POLICY IF EXISTS "equipe_cirurgica_select_via_parent" ON public.equipe_cirurgica;
CREATE POLICY "equipe_cirurgica_select_via_parent" ON public.equipe_cirurgica
    FOR SELECT TO authenticated
    USING (
        procedimento_id IN (
            SELECT p.id
            FROM procedimentos_cirurgicos p
            WHERE
                (p.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (p.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 7. establishment_ai_settings.establishment_ai_settings_select_tenant
DROP POLICY IF EXISTS "establishment_ai_settings_select_tenant" ON public.establishment_ai_settings;
CREATE POLICY "establishment_ai_settings_select_tenant" ON public.establishment_ai_settings
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 8. exame_fisico.exame_fisico_select_tenant
DROP POLICY IF EXISTS "exame_fisico_select_tenant" ON public.exame_fisico;
CREATE POLICY "exame_fisico_select_tenant" ON public.exame_fisico
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 9. exame_fisico_regioes.exame_fisico_regioes_select_via_parent
DROP POLICY IF EXISTS "exame_fisico_regioes_select_via_parent" ON public.exame_fisico_regioes;
CREATE POLICY "exame_fisico_regioes_select_via_parent" ON public.exame_fisico_regioes
    FOR SELECT TO authenticated
    USING (
        exame_fisico_id IN (
            SELECT ef.id
            FROM exame_fisico ef
            WHERE
                (ef.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (ef.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 10. formas_pagamento.formas_pagamento_select_tenant
DROP POLICY IF EXISTS "formas_pagamento_select_tenant" ON public.formas_pagamento;
CREATE POLICY "formas_pagamento_select_tenant" ON public.formas_pagamento
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 11. lgpd_consentimentos.lgpd_consentimentos_select_proprio
DROP POLICY IF EXISTS "lgpd_consentimentos_select_proprio" ON public.lgpd_consentimentos;
CREATE POLICY "lgpd_consentimentos_select_proprio" ON public.lgpd_consentimentos
    FOR SELECT TO authenticated
    USING (usuario_id = (select auth.uid()));

-- 12. lista_espera_agendamento.lista_espera_select_tenant
DROP POLICY IF EXISTS "lista_espera_select_tenant" ON public.lista_espera_agendamento;
CREATE POLICY "lista_espera_select_tenant" ON public.lista_espera_agendamento
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 13. medicamentos_favoritos.medicamentos_favoritos_select_tenant
DROP POLICY IF EXISTS "medicamentos_favoritos_select_tenant" ON public.medicamentos_favoritos;
CREATE POLICY "medicamentos_favoritos_select_tenant" ON public.medicamentos_favoritos
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 14. notificacoes.notificacoes_select_proprio
DROP POLICY IF EXISTS "notificacoes_select_proprio" ON public.notificacoes;
CREATE POLICY "notificacoes_select_proprio" ON public.notificacoes
    FOR SELECT TO authenticated
    USING (usuario_id = (select auth.uid()));

-- 15. orcamento_anestesia.orcamento_anestesia_select_via_parent
DROP POLICY IF EXISTS "orcamento_anestesia_select_via_parent" ON public.orcamento_anestesia;
CREATE POLICY "orcamento_anestesia_select_via_parent" ON public.orcamento_anestesia
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM orcamentos o
            WHERE
                (o.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (o.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 16. orcamento_catalogo_cirurgia.orcamento_catalogo_cirurgia_select_tenant
DROP POLICY IF EXISTS "orcamento_catalogo_cirurgia_select_tenant" ON public.orcamento_catalogo_cirurgia;
CREATE POLICY "orcamento_catalogo_cirurgia_select_tenant" ON public.orcamento_catalogo_cirurgia
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 17. orcamento_catalogo_cirurgia_produto.orcamento_catalogo_cirurgia_produto_select_via_parent
DROP POLICY IF EXISTS "orcamento_catalogo_cirurgia_produto_select_via_parent" ON public.orcamento_catalogo_cirurgia_produto;
CREATE POLICY "orcamento_catalogo_cirurgia_produto_select_via_parent" ON public.orcamento_catalogo_cirurgia_produto
    FOR SELECT TO authenticated
    USING (
        catalogo_cirurgia_id IN (
            SELECT orcamento_catalogo_cirurgia.id
            FROM orcamento_catalogo_cirurgia
            WHERE
                (orcamento_catalogo_cirurgia.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (orcamento_catalogo_cirurgia.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 18. orcamento_catalogo_equipe.orcamento_catalogo_equipe_select_tenant
DROP POLICY IF EXISTS "orcamento_catalogo_equipe_select_tenant" ON public.orcamento_catalogo_equipe;
CREATE POLICY "orcamento_catalogo_equipe_select_tenant" ON public.orcamento_catalogo_equipe
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 19. orcamento_catalogo_implante.orcamento_catalogo_implante_select_tenant
DROP POLICY IF EXISTS "orcamento_catalogo_implante_select_tenant" ON public.orcamento_catalogo_implante;
CREATE POLICY "orcamento_catalogo_implante_select_tenant" ON public.orcamento_catalogo_implante
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 20. orcamento_catalogo_produto.orcamento_catalogo_produto_select_tenant
DROP POLICY IF EXISTS "orcamento_catalogo_produto_select_tenant" ON public.orcamento_catalogo_produto;
CREATE POLICY "orcamento_catalogo_produto_select_tenant" ON public.orcamento_catalogo_produto
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 21. orcamento_cirurgias.orcamento_cirurgias_select_via_parent
DROP POLICY IF EXISTS "orcamento_cirurgias_select_via_parent" ON public.orcamento_cirurgias;
CREATE POLICY "orcamento_cirurgias_select_via_parent" ON public.orcamento_cirurgias
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM orcamentos o
            WHERE
                (o.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (o.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 22. orcamento_configuracao_local_cirurgia.orcamento_configuracao_local_cirurgia_select_tenant
DROP POLICY IF EXISTS "orcamento_configuracao_local_cirurgia_select_tenant" ON public.orcamento_configuracao_local_cirurgia;
CREATE POLICY "orcamento_configuracao_local_cirurgia_select_tenant" ON public.orcamento_configuracao_local_cirurgia
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 23. orcamento_configuracao_pagamento.orcamento_configuracao_pagamento_select_tenant
DROP POLICY IF EXISTS "orcamento_configuracao_pagamento_select_tenant" ON public.orcamento_configuracao_pagamento;
CREATE POLICY "orcamento_configuracao_pagamento_select_tenant" ON public.orcamento_configuracao_pagamento
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 24. orcamento_equipe.orcamento_equipe_select_tenant
DROP POLICY IF EXISTS "orcamento_equipe_select_tenant" ON public.orcamento_equipe;
CREATE POLICY "orcamento_equipe_select_tenant" ON public.orcamento_equipe
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM orcamentos o
            WHERE
                (o.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (o.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 25. orcamento_formas_pagamento.orcamento_formas_pagamento_select_tenant
DROP POLICY IF EXISTS "orcamento_formas_pagamento_select_tenant" ON public.orcamento_formas_pagamento;
CREATE POLICY "orcamento_formas_pagamento_select_tenant" ON public.orcamento_formas_pagamento
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM orcamentos o
            WHERE
                (o.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (o.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 26. orcamento_implantes.orcamento_implantes_select_tenant
DROP POLICY IF EXISTS "orcamento_implantes_select_tenant" ON public.orcamento_implantes;
CREATE POLICY "orcamento_implantes_select_tenant" ON public.orcamento_implantes
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM orcamentos o
            WHERE
                (o.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (o.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 27. orcamento_internacao.orcamento_internacao_select_via_parent
DROP POLICY IF EXISTS "orcamento_internacao_select_via_parent" ON public.orcamento_internacao;
CREATE POLICY "orcamento_internacao_select_via_parent" ON public.orcamento_internacao
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT o.id
            FROM orcamentos o
            WHERE
                (o.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (o.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 28. orcamento_valor_profissional.orcamento_valor_profissional_select_tenant
DROP POLICY IF EXISTS "orcamento_valor_profissional_select_tenant" ON public.orcamento_valor_profissional;
CREATE POLICY "orcamento_valor_profissional_select_tenant" ON public.orcamento_valor_profissional
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 29. procedimentos_cirurgicos.procedimentos_cirurgicos_select_tenant
DROP POLICY IF EXISTS "procedimentos_cirurgicos_select_tenant" ON public.procedimentos_cirurgicos;
CREATE POLICY "procedimentos_cirurgicos_select_tenant" ON public.procedimentos_cirurgicos
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 30. profissionais.profissionais_select_proprio
DROP POLICY IF EXISTS "profissionais_select_proprio" ON public.profissionais;
CREATE POLICY "profissionais_select_proprio" ON public.profissionais
    FOR SELECT TO authenticated
    USING (usuario_id = (select auth.uid()));

-- 31. receita_itens.receita_itens_select_via_receita
DROP POLICY IF EXISTS "receita_itens_select_via_receita" ON public.receita_itens;
CREATE POLICY "receita_itens_select_via_receita" ON public.receita_itens
    FOR SELECT TO authenticated
    USING (
        receita_id IN (
            SELECT r.id
            FROM receitas r
            WHERE
                (r.estabelecimento_id IN (
                    SELECT v.estabelecimento_id
                    FROM vinculo_profissional_estabelecimento v
                    WHERE v.profissional_usuario_id = (select auth.uid())
                      AND v.status::text = 'Ativo'
                ))
                OR
                (r.estabelecimento_id IN (
                    SELECT e.id FROM estabelecimentos e
                    WHERE e.dono_usuario_id = (select auth.uid())
                ))
        )
    );

-- 32. receitas.receitas_select_tenant
DROP POLICY IF EXISTS "receitas_select_tenant" ON public.receitas;
CREATE POLICY "receitas_select_tenant" ON public.receitas
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 33. receitas_configuracao_estabelecimento.receitas_config_select_tenant
DROP POLICY IF EXISTS "receitas_config_select_tenant" ON public.receitas_configuracao_estabelecimento;
CREATE POLICY "receitas_config_select_tenant" ON public.receitas_configuracao_estabelecimento
    FOR SELECT TO authenticated
    USING (
        (estabelecimento_id IN (
            SELECT v.estabelecimento_id
            FROM vinculo_profissional_estabelecimento v
            WHERE v.profissional_usuario_id = (select auth.uid())
              AND v.status::text = 'Ativo'
        ))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 34. solicitacoes_vinculo.solicitacoes_vinculo_select_envolvidos
DROP POLICY IF EXISTS "solicitacoes_vinculo_select_envolvidos" ON public.solicitacoes_vinculo;
CREATE POLICY "solicitacoes_vinculo_select_envolvidos" ON public.solicitacoes_vinculo
    FOR SELECT TO authenticated
    USING (
        (profissional_usuario_id = (select auth.uid()))
        OR
        (estabelecimento_id IN (
            SELECT e.id FROM estabelecimentos e
            WHERE e.dono_usuario_id = (select auth.uid())
        ))
    );

-- 35. usuarios.usuarios_select_proprio
DROP POLICY IF EXISTS "usuarios_select_proprio" ON public.usuarios;
CREATE POLICY "usuarios_select_proprio" ON public.usuarios
    FOR SELECT TO authenticated
    USING (id = (select auth.uid()));

-- 36. vinculo_profissional_estabelecimento.vinculo_select
DROP POLICY IF EXISTS "vinculo_select" ON public.vinculo_profissional_estabelecimento;
CREATE POLICY "vinculo_select" ON public.vinculo_profissional_estabelecimento
    FOR SELECT TO authenticated
    USING (
        (profissional_usuario_id = (select auth.uid()))
        OR
        (estabelecimento_id IN (
            SELECT meus_estabelecimentos() AS meus_estabelecimentos
        ))
    );

-- ---------------------------------------------------------------------
-- SECAO B — Habilita RLS em itens_inventario, orcamentos, paciente_acesso_log
-- ---------------------------------------------------------------------
-- Espelha o padrao do projeto (ver agendamentos/pacientes/lancamentos):
--   - UMA policy de SELECT por tabela, restringindo por estabelecimento_id
--     via meus_estabelecimentos() (funcao STABLE SECURITY DEFINER).
--   - SEM policies INSERT/UPDATE/DELETE: o backend escreve via service_role,
--     que tem BYPASSRLS=true. Operacoes pelo authenticated direto sao
--     bloqueadas (RLS habilitado + sem policy = nega tudo) — exatamente
--     o comportamento que o BFF exige (defense-in-depth multi-tenant).

-- B.1. itens_inventario
ALTER TABLE public.itens_inventario ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "itens_inventario_select" ON public.itens_inventario;
CREATE POLICY "itens_inventario_select" ON public.itens_inventario
    FOR SELECT TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT meus_estabelecimentos() AS meus_estabelecimentos
        )
    );

-- B.2. orcamentos
ALTER TABLE public.orcamentos ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "orcamentos_select" ON public.orcamentos;
CREATE POLICY "orcamentos_select" ON public.orcamentos
    FOR SELECT TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT meus_estabelecimentos() AS meus_estabelecimentos
        )
    );

-- B.3. paciente_acesso_log (audit trail LGPD, append-only via backend)
ALTER TABLE public.paciente_acesso_log ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "paciente_acesso_log_select" ON public.paciente_acesso_log;
CREATE POLICY "paciente_acesso_log_select" ON public.paciente_acesso_log
    FOR SELECT TO authenticated
    USING (
        estabelecimento_id IN (
            SELECT meus_estabelecimentos() AS meus_estabelecimentos
        )
    );

-- =====================================================================
-- TRADE-OFFS / NOTAS DE OPERACAO
-- ---------------------------------------------------------------------
-- 1) DROP+CREATE atomico:
--    Postgres NAO suporta ALTER POLICY mudando expressao. Uso DROP+CREATE
--    dentro da transacao gerada pela Supabase CLI; visto que tudo fica
--    numa unica transacao, nao ha "janela" externamente visivel — apos
--    COMMIT a nova policy ja esta ativa. Conexoes com snapshot anterior
--    veem a policy antiga ate fim da transacao (MVCC).
--
-- 2) FORCE ROW LEVEL SECURITY nas 3 tabelas novas:
--    Diverge do padrao atual do projeto (agendamentos/pacientes/lancamentos
--    NAO tem FORCE). FORCE faz com que ate o OWNER da tabela respeite RLS.
--    Pratica defensiva: se algum backend rodar como owner (postgres) num
--    momento de manutencao, ainda ficaria preso. service_role mantem
--    BYPASSRLS por privilegio de role, entao nao quebra. Considerar
--    estender FORCE as demais tabelas em migration separada para
--    homogeneizar.
--
-- 3) Por que apenas SELECT em itens_inventario/orcamentos/paciente_acesso_log:
--    Padrao explicito do projeto (BFF). authenticated NAO escreve nada,
--    backend usa service_role. Habilitar RLS sem policies INSERT/UPDATE/
--    DELETE ja deixa esses comandos negados para authenticated (default
--    deny), reforcando defense-in-depth.
--
-- 4) Migracao futura Supabase -> AWS RDS:
--    Esta migration NAO introduz auth.uid() em features novas — apenas
--    refatora policies pre-existentes para a forma cacheada. As 3 tabelas
--    novas com RLS usam meus_estabelecimentos(), que ja era padrao no
--    projeto. Compativel com a restricao arquitetural documentada.
--
-- 5) Pendencias fora de escopo (advisor pode reportar separadamente):
--    - Alguns avisos auth_rls_initplan podem persistir se houver policies
--      em outros schemas (storage, auth) — nao tocadas aqui (out of scope).
--    - Existem 6 funcoes do projeto sem search_path imutavel que tambem
--      sao flagged pelo advisor (function_search_path_mutable). Nao
--      tratado aqui.
--    - Verificar se "multiple_permissive_policies" aparece para alguma
--      tabela apos esta migration — nao deve, ja que mantenho 1 policy
--      por tabela.
-- =====================================================================

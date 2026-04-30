-- ===========================================================================
-- Script: 99_validacao.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: TODOS os scripts anteriores (00..08)
--
-- Objetivo:
--   1. Re-habilitar constraints/triggers desabilitadas durante a carga.
--   2. Reportar contagens lado-a-lado legado vs novo.
--   3. Verificar integridade referencial (orfaos por FK).
--   4. Reportar dados pendentes de revisao humana (overlaps de agenda,
--      receitas invalidas, chaves de permissao desconhecidas).
--   5. ANALYZE em todas as tabelas migradas (planner refresh).
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

-- ---------------------------------------------------------------------------
-- 1. RE-HABILITAR constraints/triggers desabilitadas no script 05.
-- ---------------------------------------------------------------------------
ALTER TABLE public.prontuario_evolucoes ENABLE TRIGGER USER;

-- Recria EXCLUDE constraint anti-overlap em agendamentos (cria com NOT VALID
-- para nao bloquear linhas legadas com overlap; cliente novo respeita a
-- regra adiante). Para validar e remover overlaps historicos, exportar
-- _etl.agendamentos_overlap e tratar fora da janela.
DO $$
BEGIN
    -- Tenta criar como STRICT primeiro; se falhar, cria NOT VALID.
    BEGIN
        ALTER TABLE public.agendamentos
            ADD CONSTRAINT agendamentos_no_overlap
            EXCLUDE USING gist (
                profissional_id WITH =,
                tstzrange(data_hora_inicio, data_hora_fim, '[)') WITH &&
            )
            WHERE (deletado_em IS NULL AND status NOT IN ('Cancelado','Falta'));
        RAISE NOTICE 'Constraint agendamentos_no_overlap recriada com VALIDACAO completa.';
    EXCEPTION WHEN exclusion_violation THEN
        RAISE WARNING 'Overlaps historicos detectados -- recriando constraint sem validar dados existentes.';
        EXECUTE $f$
            ALTER TABLE public.agendamentos
                ADD CONSTRAINT agendamentos_no_overlap
                EXCLUDE USING gist (
                    profissional_id WITH =,
                    tstzrange(data_hora_inicio, data_hora_fim, '[)') WITH &&
                )
                WHERE (deletado_em IS NULL AND status NOT IN ('Cancelado','Falta'))
                NOT VALID
        $f$;
    END;
END $$;

-- ---------------------------------------------------------------------------
-- 2. Contagens lado a lado: legado x novo.
-- ---------------------------------------------------------------------------
SELECT '=== CONTAGENS ===' AS rel;

WITH pares AS (
    SELECT * FROM (VALUES
        ('usuarios',                          'legado.usuarios',                              'public.usuarios'),
        ('estabelecimentos',                  'legado.estabelecimentos',                      'public.estabelecimentos'),
        ('unidades_estabelecimento',          'legado.unidades_estabelecimento',              'public.unidades_estabelecimento'),
        ('sala_atendimento',                  'legado.sala_atendimento',                      'public.sala_atendimento'),
        ('profissionais',                     'legado.profissionais',                         'public.profissionais'),
        ('vinculos',                          'legado.vinculo_profissional_estabelecimento',  'public.vinculo_profissional_estabelecimento'),
        ('modelo_permissao',                  'legado.modelo_permissao_estabelecimento',      'public.modelo_permissao_estabelecimento'),
        ('pacientes',                         'legado.pacientes',                             'public.pacientes'),
        ('prontuarios',                       'legado.prontuarios',                           'public.prontuarios'),
        ('prontuario_evolucoes',              'legado.evolucao_prontuario',                   'public.prontuario_evolucoes'),
        ('exame_fisico',                      'legado.exame_fisico',                          'public.exame_fisico'),
        ('agendamentos',                      'legado.evento_de_agendamento',                 'public.agendamentos'),
        ('receitas',                          'legado.receitas',                              'public.receitas'),
        ('receita_itens',                     'legado.receita_itens',                         'public.receita_itens'),
        ('orcamentos',                        'legado.orcamentos',                            'public.orcamentos'),
        ('lancamentos',                       'legado.financeiro_transacao',                  'public.lancamentos'),
        ('itens_inventario',                  'legado.estoque_produto',                       'public.itens_inventario'),
        ('movimentacoes_estoque',             'legado.movimento_estoque',                     'public.movimentacoes_estoque'),
        ('automation_rules',                  'legado.automation_rules',                      'public.automation_rules'),
        ('notificacoes',                      'legado.notifications',                         'public.notificacoes')
    ) AS t(label, fqn_legado, fqn_novo)
)
SELECT
    p.label,
    (SELECT count(*)::bigint FROM pg_class c WHERE c.oid = p.fqn_legado::regclass) AS legado_existe,
    -- usa execute dinamico via funcao temporaria (subselect direto requer LATERAL).
    NULL::bigint AS legado_qtd,
    NULL::bigint AS novo_qtd
FROM pares p;

-- Contagens reais (uma a uma -- mais legivel no output do psql).
SELECT 'usuarios'                  AS tabela, (SELECT COUNT(*) FROM legado.usuarios)                              AS legado, (SELECT COUNT(*) FROM public.usuarios)                              AS novo
UNION ALL SELECT 'estabelecimentos',          (SELECT COUNT(*) FROM legado.estabelecimentos),                                (SELECT COUNT(*) FROM public.estabelecimentos)
UNION ALL SELECT 'unidades_estabelecimento',  (SELECT COUNT(*) FROM legado.unidades_estabelecimento),                        (SELECT COUNT(*) FROM public.unidades_estabelecimento)
UNION ALL SELECT 'sala_atendimento',          (SELECT COUNT(*) FROM legado.sala_atendimento),                                (SELECT COUNT(*) FROM public.sala_atendimento)
UNION ALL SELECT 'profissionais',             (SELECT COUNT(*) FROM legado.profissionais WHERE usuario_id IS NOT NULL),      (SELECT COUNT(*) FROM public.profissionais)
UNION ALL SELECT 'vinculos',                  (SELECT COUNT(*) FROM legado.vinculo_profissional_estabelecimento),            (SELECT COUNT(*) FROM public.vinculo_profissional_estabelecimento)
UNION ALL SELECT 'modelo_permissao',          (SELECT COUNT(*) FROM legado.modelo_permissao_estabelecimento),                (SELECT COUNT(*) FROM public.modelo_permissao_estabelecimento)
UNION ALL SELECT 'pacientes',                 (SELECT COUNT(*) FROM legado.pacientes),                                       (SELECT COUNT(*) FROM public.pacientes)
UNION ALL SELECT 'prontuarios',               (SELECT COUNT(*) FROM legado.prontuarios),                                     (SELECT COUNT(*) FROM public.prontuarios)
UNION ALL SELECT 'prontuario_evolucoes',      (SELECT COUNT(*) FROM legado.evolucao_prontuario),                             (SELECT COUNT(*) FROM public.prontuario_evolucoes)
UNION ALL SELECT 'exame_fisico',              (SELECT COUNT(*) FROM legado.exame_fisico),                                    (SELECT COUNT(*) FROM public.exame_fisico)
UNION ALL SELECT 'agendamentos',              (SELECT COUNT(*) FROM legado.evento_de_agendamento),                           (SELECT COUNT(*) FROM public.agendamentos)
UNION ALL SELECT 'receitas',                  (SELECT COUNT(*) FROM legado.receitas),                                        (SELECT COUNT(*) FROM public.receitas)
UNION ALL SELECT 'receita_itens',             (SELECT COUNT(*) FROM legado.receita_itens),                                   (SELECT COUNT(*) FROM public.receita_itens)
UNION ALL SELECT 'orcamentos',                (SELECT COUNT(*) FROM legado.orcamentos),                                      (SELECT COUNT(*) FROM public.orcamentos)
UNION ALL SELECT 'lancamentos',               (SELECT COUNT(*) FROM legado.financeiro_transacao),                            (SELECT COUNT(*) FROM public.lancamentos)
UNION ALL SELECT 'itens_inventario',          (SELECT COUNT(*) FROM legado.estoque_produto),                                 (SELECT COUNT(*) FROM public.itens_inventario)
UNION ALL SELECT 'movimentacoes_estoque',     (SELECT COUNT(*) FROM legado.movimento_estoque),                               (SELECT COUNT(*) FROM public.movimentacoes_estoque)
UNION ALL SELECT 'notificacoes (90d)',        (SELECT COUNT(*) FROM legado.notifications WHERE created_at > now() - interval '90 days'), (SELECT COUNT(*) FROM public.notificacoes)
ORDER BY tabela;

-- ---------------------------------------------------------------------------
-- 3. Integridade referencial -- orfaos por FK.
-- ---------------------------------------------------------------------------
SELECT '=== ORFAOS POR FK ===' AS rel;

SELECT 'agendamento_sem_estab'    AS check_, COUNT(*)
FROM public.agendamentos a LEFT JOIN public.estabelecimentos e ON e.id = a.estabelecimento_id
WHERE e.id IS NULL
UNION ALL SELECT 'agendamento_sem_paciente',
    (SELECT COUNT(*) FROM public.agendamentos a
     WHERE a.paciente_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM public.pacientes p WHERE p.id = a.paciente_id))
UNION ALL SELECT 'evolucao_sem_prontuario',
    (SELECT COUNT(*) FROM public.prontuario_evolucoes ev
     WHERE NOT EXISTS (SELECT 1 FROM public.prontuarios p WHERE p.id = ev.prontuario_id))
UNION ALL SELECT 'receita_item_sem_receita',
    (SELECT COUNT(*) FROM public.receita_itens i
     WHERE NOT EXISTS (SELECT 1 FROM public.receitas r WHERE r.id = i.receita_id))
UNION ALL SELECT 'movimentacao_sem_item',
    (SELECT COUNT(*) FROM public.movimentacoes_estoque m
     WHERE NOT EXISTS (SELECT 1 FROM public.itens_inventario i WHERE i.id = m.item_inventario_id))
UNION ALL SELECT 'lancamento_sem_categoria',
    (SELECT COUNT(*) FROM public.lancamentos l
     WHERE l.categoria_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM public.categorias_financeiras c WHERE c.id = l.categoria_id));

-- ---------------------------------------------------------------------------
-- 4. Pendencias de revisao humana.
-- ---------------------------------------------------------------------------
SELECT '=== PENDENCIAS REVISAO ===' AS rel;

SELECT 'overlaps_agenda_detectados' AS rel_, COUNT(*) AS qtd FROM _etl.agendamentos_overlap
UNION ALL SELECT 'receitas_invalidas',                  COUNT(*) FROM _etl.receitas_invalidas;

-- Chaves de permissao desconhecidas.
SELECT chave_legado, COUNT(*) AS ocorrencias
FROM legado.modelo_permissao_estabelecimento leg,
     LATERAL jsonb_array_elements_text(COALESCE(leg.permissoes, '[]'::jsonb)) AS chave_legado
WHERE NOT EXISTS (SELECT 1 FROM _etl.permissao_legado_para_novo m WHERE m.chave_legado = chave_legado)
GROUP BY chave_legado;

-- ---------------------------------------------------------------------------
-- 5. ANALYZE em todas as tabelas migradas.
-- ---------------------------------------------------------------------------
DO $$
DECLARE r record;
BEGIN
    FOR r IN
        SELECT tablename FROM pg_tables WHERE schemaname = 'public'
          AND tablename IN (
            'usuarios','estabelecimentos','unidades_estabelecimento','sala_atendimento',
            'profissionais','vinculo_profissional_estabelecimento','modelo_permissao_estabelecimento',
            'pacientes','prontuarios','prontuario_evolucoes','exame_fisico',
            'agendamentos','receitas','receita_itens','medicamentos_favoritos',
            'orcamentos','orcamento_cirurgias','orcamento_internacao','orcamento_anestesia',
            'orcamento_implantes','orcamento_equipe','orcamento_formas_pagamento',
            'lancamentos','categorias_financeiras','formas_pagamento',
            'itens_inventario','movimentacoes_estoque',
            'automation_rules','automation_events','notificacoes',
            'lgpd_anonimizacoes','lgpd_consentimentos','ai_audit_logs','ai_rate_limits',
            'establishment_ai_settings','assinaturas','receitas_configuracao_estabelecimento'
          )
    LOOP
        EXECUTE format('ANALYZE public.%I', r.tablename);
    END LOOP;
END $$;

-- ---------------------------------------------------------------------------
-- 6. Resumo final do run_log.
-- ---------------------------------------------------------------------------
SELECT script_nome, finalizado_em, observacao
FROM _etl.run_log
ORDER BY id;

-- DICA: para limpar mappings/indices ao final do ETL (se nao for re-rodar):
--   DROP SCHEMA _etl CASCADE;
--   DROP SCHEMA legado CASCADE;
--   -- e depois rodar VACUUM ANALYZE no destino.

-- ===========================================================================
-- Script: 06_transacional_clinico.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 05_transacional_agenda_pacientes.sql
--
-- Objetivo: dados clinicos derivados de prontuario/evolucao
--   - exame_fisico
--   - exame_fisico_regioes  (ACHADOS no novo -- nao confundir com catalogo
--                            do legado, que e descartado / ja seedado)
--   - receitas + receita_itens + medicamentos_favoritos
--   - procedimentos_cirurgicos + equipe_cirurgica
--
-- Receitas: BLOQUEIA o ETL se houver combinacoes invalidas
-- (SIMPLES + tipo_notificacao preenchido) -- Doc ETL_MAPEAMENTO 9.
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

DO $$ BEGIN
    IF (SELECT COUNT(*) FROM public.prontuarios) = 0 THEN
        RAISE EXCEPTION 'public.prontuarios vazia. Rode 05 antes.';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- VALIDACAO PRE-CARGA: receitas invalidas BLOQUEIAM o ETL.
-- Uma receita e invalida se:
--   * tipo='SIMPLES' AND tipo_notificacao IS NOT NULL
--   * tipo='CONTROLADA' AND tipo_notificacao NOT IN ('A','B','C','ESPECIAL')
--   * tipo NOT IN ('SIMPLES','CONTROLADA')
-- ---------------------------------------------------------------------------
TRUNCATE _etl.receitas_invalidas;

INSERT INTO _etl.receitas_invalidas (receita_id, estabelecimento_id, tipo_legado, tipo_notificacao_legado, motivo)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.tipo,
    leg.tipo_notificacao,
    CASE
        WHEN leg.tipo = 'SIMPLES' AND leg.tipo_notificacao IS NOT NULL
            THEN 'SIMPLES nao admite tipo_notificacao'
        WHEN leg.tipo = 'CONTROLADA' AND leg.tipo_notificacao NOT IN ('A','B','C','ESPECIAL')
            THEN 'CONTROLADA com tipo_notificacao desconhecido'
        WHEN leg.tipo NOT IN ('SIMPLES','CONTROLADA')
            THEN 'tipo desconhecido'
        ELSE 'outro'
    END AS motivo
FROM legado.receitas leg
WHERE
    (leg.tipo = 'SIMPLES'    AND leg.tipo_notificacao IS NOT NULL)
 OR (leg.tipo = 'CONTROLADA' AND leg.tipo_notificacao NOT IN ('A','B','C','ESPECIAL'))
 OR (leg.tipo NOT IN ('SIMPLES','CONTROLADA'));

DO $$
DECLARE v_qtd int;
BEGIN
    SELECT COUNT(*) INTO v_qtd FROM _etl.receitas_invalidas;
    IF v_qtd > 0 THEN
        RAISE EXCEPTION 'ETL bloqueado: % receitas com combinacao (tipo, tipo_notificacao) invalida. Consulte _etl.receitas_invalidas e revise manualmente antes de prosseguir.', v_qtd;
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- exame_fisico (180k linhas otim. -- jsonb pesado em regioes_examinadas)
-- ---------------------------------------------------------------------------
INSERT INTO public.exame_fisico (
    id,
    prontuario_id,
    prontuario_evolucao_id,    -- novo: aponta para prontuario_evolucoes
    estabelecimento_id,
    profissional_id,
    paciente_id,
    dados_gerais,
    regioes_examinadas,        -- mantemos jsonb (decisao do ETL_MAPEAMENTO 4)
    observacoes,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.prontuario_id,
    leg.evolucao_prontuario_id   AS prontuario_evolucao_id,
    leg.estabelecimento_id,
    leg.profissional_id,
    leg.paciente_id,
    leg.dados_gerais,
    leg.regioes_examinadas,
    leg.observacoes,
    leg.created_at,
    leg.updated_at
FROM legado.exame_fisico leg
WHERE EXISTS (SELECT 1 FROM public.prontuarios p WHERE p.id = leg.prontuario_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- exame_fisico_regioes (ACHADOS) -- decisao default do ETL_MAPEAMENTO 4:
-- "deixar em regioes_examinadas jsonb para manter paridade comportamental".
-- Portanto NAO desnormalizamos. Linha mantida como TODO caso produto decida
-- migrar achados desnormalizados no futuro.
--
-- TODO produto: se decidir explodir regioes_examinadas em linhas, executar:
--   INSERT INTO public.exame_fisico_regioes(...) SELECT FROM exame_fisico,
--   jsonb_each(regioes_examinadas) WHERE ...
-- ---------------------------------------------------------------------------

-- ---------------------------------------------------------------------------
-- receitas (com mapping (tipo, tipo_notificacao) -> Tipo)
-- ---------------------------------------------------------------------------
INSERT INTO public.receitas (
    id,
    estabelecimento_id,
    paciente_id,
    prontuario_id,
    profissional_id,
    agendamento_id,                  -- rename de evento_de_agendamento_id
    tipo,                            -- enum novo
    status,
    version_of_id,
    version_number,
    observacoes,
    finalized_at,
    canceled_at,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.paciente_id,
    leg.prontuario_id,
    leg.profissional_id,
    leg.evento_de_agendamento_id     AS agendamento_id,
    rt.tipo_novo                     AS tipo,
    INITCAP(leg.status)              AS status,
    leg.version_of_id,
    COALESCE(leg.version_number, 1),
    leg.observacoes,
    leg.finalized_at,
    leg.canceled_at,
    leg.created_at,
    leg.updated_at
FROM legado.receitas leg
JOIN _etl.receita_tipo_map rt
  ON rt.tipo_legado = leg.tipo
 AND COALESCE(rt.tipo_notificacao_legado, '') = COALESCE(leg.tipo_notificacao, '')
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- receita_itens (390k linhas otim.)
-- ---------------------------------------------------------------------------
INSERT INTO public.receita_itens (
    id,
    receita_id,
    medicamento_nome,
    principio_ativo,
    dosagem,
    forma_farmaceutica,
    quantidade,
    posologia,
    duracao_tratamento,
    observacoes,
    ordem,
    criado_em
)
SELECT
    leg.id,
    leg.receita_id,
    leg.medicamento_nome,
    leg.principio_ativo,
    leg.dosagem,
    leg.forma_farmaceutica,
    leg.quantidade,
    leg.posologia,
    leg.duracao_tratamento,
    leg.observacoes,
    COALESCE(leg.ordem, 0),
    leg.created_at
FROM legado.receita_itens leg
WHERE EXISTS (SELECT 1 FROM public.receitas r WHERE r.id = leg.receita_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- medicamentos_favoritos
-- ---------------------------------------------------------------------------
INSERT INTO public.medicamentos_favoritos (
    id,
    profissional_id,
    estabelecimento_id,
    medicamento_nome,
    principio_ativo,
    dosagem,
    forma_farmaceutica,
    posologia_padrao,
    quantidade_padrao,
    duracao_tratamento_padrao,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.profissional_id,
    leg.estabelecimento_id,
    leg.medicamento_nome,
    leg.principio_ativo,
    leg.dosagem,
    leg.forma_farmaceutica,
    leg.posologia_padrao,
    leg.quantidade_padrao,
    leg.duracao_tratamento_padrao,
    leg.created_at,
    leg.updated_at
FROM legado.medicamentos_favoritos leg
WHERE EXISTS (SELECT 1 FROM public.profissionais p WHERE p.usuario_id = leg.profissional_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- procedimentos_cirurgicos + equipe_cirurgica
--
-- Estrutura nova -- no legado, eram secoes jsonb dentro de exame_fisico /
-- evolucao_prontuario. Sem origem direta.
--
-- TODO produto: se houver demanda de migrar procedimentos historicos a
-- partir do conteudo jsonb das evolucoes legado, escrever extracao
-- aqui (regex/json_path no campo conteudo). Default: nao migra.
-- ---------------------------------------------------------------------------

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('06_transacional_clinico.sql', now(),
        'exame_fisico/receitas/receita_itens/medicamentos_favoritos carregados. procedimentos_cirurgicos sem origem (TODO produto).');

COMMIT;

-- Pos-flight
SELECT 'exame_fisico'           AS tabela, COUNT(*) FROM public.exame_fisico
UNION ALL SELECT 'receitas',                COUNT(*) FROM public.receitas
UNION ALL SELECT 'receita_itens',           COUNT(*) FROM public.receita_itens
UNION ALL SELECT 'medicamentos_favoritos',  COUNT(*) FROM public.medicamentos_favoritos
UNION ALL SELECT 'receitas_invalidas',      COUNT(*) FROM _etl.receitas_invalidas;

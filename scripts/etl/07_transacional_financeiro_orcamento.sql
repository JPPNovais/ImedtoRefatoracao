-- ===========================================================================
-- Script: 07_transacional_financeiro_orcamento.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 03_dominio_core.sql, 04_configuracoes.sql, 05_*, 06_*
--
-- Objetivo: dominio financeiro/orcamento/inventario/automacao/notificacoes
--   - orcamentos + filhos
--       orcamento_cirurgias, orcamento_internacao, orcamento_anestesia,
--       orcamento_implantes, orcamento_equipe (merge de 3 legadas),
--       orcamento_formas_pagamento, itens_orcamento
--   - lancamentos                  (rename de financeiro_transacao)
--   - itens_inventario             (rename de estoque_produto, com colapso
--                                    de catalogos em colunas planas)
--   - movimentacoes_estoque        (rename de movimento_estoque)
--   - automation_rules + automation_events
--   - notificacoes (filtro: ultimos 90 dias)
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
-- orcamentos (81k otim.)
-- ---------------------------------------------------------------------------
INSERT INTO public.orcamentos (
    id,
    estabelecimento_id,
    paciente_id,
    profissional_id,
    agendamento_id,
    status,
    valor_total,
    observacoes,
    dados_pagamento,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.paciente_id,
    leg.profissional_id,
    leg.evento_de_agendamento_id,
    INITCAP(leg.status),
    leg.valor_total,
    leg.observacoes,
    COALESCE(leg.dados_pagamento, '{}'::jsonb),
    leg.created_at,
    leg.updated_at,
    leg.deletado_em
FROM legado.orcamentos leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- orcamento_cirurgias (linhas vinculadas ao orcamento -- NAO o catalogo)
-- ---------------------------------------------------------------------------
INSERT INTO public.orcamento_cirurgias (
    id, orcamento_id, descricao, valor, ordem, criado_em
)
SELECT
    leg.id, leg.orcamento_id, leg.descricao, leg.valor, COALESCE(leg.ordem, 0), leg.created_at
FROM legado.orcamento_cirurgias leg
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = leg.orcamento_id)
ON CONFLICT (id) DO NOTHING;

-- orcamento_internacao
INSERT INTO public.orcamento_internacao (
    id, orcamento_id, descricao, diarias, valor_diaria, valor_total, criado_em
)
SELECT
    leg.id, leg.orcamento_id, leg.descricao, leg.diarias, leg.valor_diaria, leg.valor_total, leg.created_at
FROM legado.orcamento_internacao leg
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = leg.orcamento_id)
ON CONFLICT (id) DO NOTHING;

-- orcamento_anestesia
INSERT INTO public.orcamento_anestesia (
    id, orcamento_id, tipo_anestesia, valor, criado_em
)
SELECT
    leg.id, leg.orcamento_id, leg.tipo_anestesia, leg.valor, leg.created_at
FROM legado.orcamento_anestesia leg
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = leg.orcamento_id)
ON CONFLICT (id) DO NOTHING;

-- orcamento_implantes (rename de orcamento_implante)
INSERT INTO public.orcamento_implantes (
    id, orcamento_id, descricao, fabricante, quantidade, valor_unitario, criado_em
)
SELECT
    leg.id, leg.orcamento_id, leg.descricao, leg.fabricante, leg.quantidade, leg.valor_unitario, leg.created_at
FROM legado.orcamento_implante leg
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = leg.orcamento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- orcamento_equipe (MERGE de 3 tabelas legado em 1 nova)
--   * orcamento_profissionais        -> tipo_membro='profissional'
--   * orcamento_equipe_especializada -> tipo_membro='especializada'
--   * orcamento_valor_profissional   -> linhas de valor consolidadas em
--                                       coluna `valor_pactuado` por
--                                       (orcamento_id, profissional_id)
-- ---------------------------------------------------------------------------
WITH valores AS (
    SELECT orcamento_id, profissional_id, MAX(valor) AS valor_pactuado
    FROM legado.orcamento_valor_profissional
    GROUP BY orcamento_id, profissional_id
)
INSERT INTO public.orcamento_equipe (
    id, orcamento_id, tipo_membro, profissional_id, descricao_especializada,
    valor_pactuado, percentual, criado_em
)
SELECT
    op.id,
    op.orcamento_id,
    'profissional'::text,
    op.profissional_id,
    NULL,
    v.valor_pactuado,
    op.percentual,
    op.created_at
FROM legado.orcamento_profissionais op
LEFT JOIN valores v ON v.orcamento_id = op.orcamento_id AND v.profissional_id = op.profissional_id
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = op.orcamento_id)
UNION ALL
SELECT
    oee.id,
    oee.orcamento_id,
    'especializada'::text,
    NULL,
    oee.descricao,
    oee.valor,
    NULL,
    oee.created_at
FROM legado.orcamento_equipe_especializada oee
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = oee.orcamento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- orcamento_formas_pagamento
-- ---------------------------------------------------------------------------
INSERT INTO public.orcamento_formas_pagamento (
    id, orcamento_id, forma_pagamento_id, valor, parcelas, observacao, criado_em
)
SELECT
    leg.id, leg.orcamento_id, leg.forma_pagamento_id, leg.valor, leg.parcelas, leg.observacao, leg.created_at
FROM legado.orcamento_formas_pagamento leg
WHERE EXISTS (SELECT 1 FROM public.orcamentos o WHERE o.id = leg.orcamento_id)
ON CONFLICT (id) DO NOTHING;

-- TODO produto: itens_orcamento (M:N orcamento<->produto) -- legado tem
-- orcamento_produtos / orcamento_cirurgia_produto. Decisao pendente sobre
-- mapeamento. Default: nao migra.

-- ---------------------------------------------------------------------------
-- lancamentos (rename de financeiro_transacao -- 270k otim.)
-- ---------------------------------------------------------------------------
INSERT INTO public.lancamentos (
    id,
    estabelecimento_id,
    categoria_id,
    forma_pagamento_id,
    valor,
    tipo,                            -- 'Entrada' | 'Saida'
    data_competencia,
    data_caixa,
    descricao,
    origem,
    paciente_id,
    profissional_id,
    orcamento_id,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.categoria_id,
    leg.forma_pagamento_id,
    leg.valor,
    INITCAP(leg.tipo),
    leg.data_competencia,
    leg.data_caixa,
    leg.descricao,
    leg.origem,
    leg.paciente_id,
    leg.profissional_id,
    leg.orcamento_id,
    leg.created_at,
    leg.updated_at,
    leg.deletado_em
FROM legado.financeiro_transacao leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- itens_inventario (rename de estoque_produto -- catalogos legado colapsados
-- em colunas planas (categoria, fabricante, fornecedor) via LEFT JOIN).
-- ---------------------------------------------------------------------------
INSERT INTO public.itens_inventario (
    id,
    estabelecimento_id,
    nome,
    descricao,
    categoria,
    fabricante,
    fornecedor,
    tipo_produto,
    unidade,
    quantidade_atual,
    quantidade_minima,
    valor_unitario,
    codigo_barras,
    sku,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.nome,
    leg.descricao,
    cat.nome     AS categoria,
    fab.nome     AS fabricante,
    forn.nome    AS fornecedor,
    tp.nome      AS tipo_produto,
    leg.unidade,
    COALESCE(leg.quantidade_atual, 0),
    COALESCE(leg.quantidade_minima, 0),
    leg.valor_unitario,
    leg.codigo_barras,
    leg.sku,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END
FROM legado.estoque_produto leg
LEFT JOIN legado.estoque_categoria   cat  ON cat.id  = leg.categoria_id
LEFT JOIN legado.estoque_fabricante  fab  ON fab.id  = leg.fabricante_id
LEFT JOIN legado.estoque_fornecedor  forn ON forn.id = leg.fornecedor_id
LEFT JOIN legado.estoque_tipo_produto tp  ON tp.id   = leg.tipo_produto_id
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- TODO produto: estoque_lote -- decisao pendente (colapsar em jsonb dentro
-- de itens_inventario OU criar tabela lotes no novo). Default: nao migra.

-- ---------------------------------------------------------------------------
-- movimentacoes_estoque (rename, append-only -- 324k otim.)
-- ---------------------------------------------------------------------------
INSERT INTO public.movimentacoes_estoque (
    id,
    item_inventario_id,
    estabelecimento_id,
    tipo,                       -- 'Entrada' | 'Saida'
    quantidade,
    valor_venda,
    numero_serie,
    motivo,
    profissional_id,
    paciente_id,
    criado_em
)
SELECT
    leg.id,
    leg.estoque_produto_id      AS item_inventario_id,
    leg.estabelecimento_id,
    INITCAP(leg.tipo),
    leg.quantidade,
    leg.valor_venda,
    leg.numero_serie,
    leg.motivo,
    leg.profissional_id,
    leg.paciente_id,
    leg.created_at
FROM legado.movimento_estoque leg
WHERE EXISTS (SELECT 1 FROM public.itens_inventario i WHERE i.id = leg.estoque_produto_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- automation_rules + automation_events
-- ---------------------------------------------------------------------------
INSERT INTO public.automation_rules (
    id, estabelecimento_id, nome, descricao, gatilho, condicoes, acoes,
    enabled, criado_em, atualizado_em
)
SELECT
    leg.id, leg.estabelecimento_id, leg.nome, leg.descricao, leg.gatilho,
    COALESCE(leg.condicoes, '{}'::jsonb),
    COALESCE(leg.acoes, '[]'::jsonb),
    COALESCE(leg.enabled, true),
    leg.created_at, leg.updated_at
FROM legado.automation_rules leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.automation_events (
    id, automation_rule_id, estabelecimento_id, payload, status,
    erro, executado_em, criado_em
)
SELECT
    leg.id, leg.automation_rule_id, leg.estabelecimento_id,
    COALESCE(leg.payload, '{}'::jsonb),
    INITCAP(leg.status),
    leg.erro, leg.executado_em, leg.created_at
FROM legado.automation_events leg
WHERE EXISTS (SELECT 1 FROM public.automation_rules r WHERE r.id = leg.automation_rule_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- notificacoes (filtro: ultimos 90 dias -- ETL_MAPEAMENTO 12)
-- ---------------------------------------------------------------------------
INSERT INTO public.notificacoes (
    id,
    usuario_id,
    estabelecimento_id,
    tipo,
    titulo,
    mensagem,
    dados,
    lida,
    lida_em,
    criado_em
)
SELECT
    leg.id,
    mu.novo_id          AS usuario_id,
    leg.estabelecimento_id,
    leg.tipo,
    leg.titulo,
    leg.mensagem,
    COALESCE(leg.dados, '{}'::jsonb),
    COALESCE(leg.lida, false),
    leg.lida_em,
    leg.created_at
FROM legado.notifications leg
JOIN _etl.mapping_usuarios mu ON mu.legado_id = leg.usuario_id
WHERE leg.created_at > now() - interval '90 days'
ON CONFLICT (id) DO NOTHING;

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('07_transacional_financeiro_orcamento.sql', now(),
        'orcamentos+filhos, lancamentos, inventario, movimentacoes, automation, notificacoes 90d');

COMMIT;

-- Pos-flight
SELECT 'orcamentos'             AS tabela, COUNT(*) FROM public.orcamentos
UNION ALL SELECT 'orcamento_cirurgias',     COUNT(*) FROM public.orcamento_cirurgias
UNION ALL SELECT 'orcamento_equipe',        COUNT(*) FROM public.orcamento_equipe
UNION ALL SELECT 'lancamentos',             COUNT(*) FROM public.lancamentos
UNION ALL SELECT 'itens_inventario',        COUNT(*) FROM public.itens_inventario
UNION ALL SELECT 'movimentacoes_estoque',   COUNT(*) FROM public.movimentacoes_estoque
UNION ALL SELECT 'automation_rules',        COUNT(*) FROM public.automation_rules
UNION ALL SELECT 'automation_events',       COUNT(*) FROM public.automation_events
UNION ALL SELECT 'notificacoes',            COUNT(*) FROM public.notificacoes;

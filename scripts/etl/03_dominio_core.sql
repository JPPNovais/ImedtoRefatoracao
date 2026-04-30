-- ===========================================================================
-- Script: 03_dominio_core.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 02_tenant_base.sql
--
-- Objetivo: carregar dominio core
--   - pacientes
--   - profissionais (1:1 com usuarios -- usuario_id e PK)
--   - vinculo_profissional_estabelecimento (status enum: Convidado/Ativo/Inativo)
--   - modelo_permissao_estabelecimento (TRANSFORMACAO CRITICA: split do
--     array `permissoes` legado em `permissoes` (areas) + `permissoes_extras`)
--   - solicitacoes_vinculo (apenas pendentes)
--
-- Transformacoes notaveis:
--   * pacientes: descarta coluna legado.telefone (ja foi backfilled em
--     telefone_celular durante migration 20251120171000 do legado).
--   * profissionais sem usuario_id NAO migram (ficam em solicitacoes_vinculo
--     se o convite ainda estiver pendente).
--   * vinculos: ativo bool -> status enum (true=Ativo, false=Inativo).
--   * modelo_permissao: jsonb_array_elements_text + JOIN com lookup
--     _etl.permissao_legado_para_novo, agrupando por destino (areas/extras).
--   * tipo_acesso: 'Total' se is_admin OR todas as 10 areas marcadas; senao 'Restrito'.
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

-- Pre-flight
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'legado' AND tablename = 'pacientes') THEN
        RAISE EXCEPTION 'legado.pacientes nao existe.';
    END IF;
    IF (SELECT COUNT(*) FROM _etl.permissao_legado_para_novo) = 0 THEN
        RAISE EXCEPTION 'Lookup _etl.permissao_legado_para_novo vazio. Rode 00_setup primeiro.';
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- pacientes
-- Filtra pacientes orfaos (estabelecimento descartado em 02).
-- Descarta coluna telefone (ja em telefone_celular).
-- ---------------------------------------------------------------------------
INSERT INTO public.pacientes (
    id,
    estabelecimento_id,
    nome_completo,
    nome_social,
    cpf,
    data_nascimento,
    sexo,
    estado_civil,
    profissao,
    nacionalidade,
    naturalidade,
    rg,
    email,
    telefone_celular,
    telefone_fixo,
    cep, logradouro, numero, complemento, bairro, cidade, uf,
    observacoes,
    convenio,
    numero_carteirinha,
    nome_responsavel,
    cpf_responsavel,
    grau_parentesco_responsavel,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.nome_completo,
    leg.nome_social,
    leg.cpf,
    leg.data_nascimento,
    leg.sexo,
    leg.estado_civil,
    leg.profissao,
    leg.nacionalidade,
    leg.naturalidade,
    leg.rg,
    leg.email,
    leg.telefone_celular,
    leg.telefone_fixo,
    leg.cep, leg.logradouro, leg.numero, leg.complemento, leg.bairro, leg.cidade, leg.uf,
    leg.observacoes,
    leg.convenio,
    leg.numero_carteirinha,
    leg.nome_responsavel,
    leg.cpf_responsavel,
    leg.grau_parentesco_responsavel,
    leg.created_at,
    leg.updated_at,
    leg.deletado_em      -- legado ja tem soft-delete
FROM legado.pacientes leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- profissionais (1:1 com usuarios -- usuario_id e a PK no novo)
-- Profissionais sem usuario_id (convidados que nunca aceitaram) sao
-- filtrados; viram solicitacoes_vinculo se ainda pendentes.
-- ---------------------------------------------------------------------------
INSERT INTO public.profissionais (
    usuario_id,
    profissao_id,
    especialidade_id,
    nome_exibicao,
    registro_profissional,
    conselho,
    uf_conselho,
    rqe,
    foto_url,
    criado_em,
    atualizado_em
)
SELECT
    mu.novo_id          AS usuario_id,
    mp.novo_id          AS profissao_id,
    me.novo_id          AS especialidade_id,
    leg.nome_exibicao,
    leg.registro_profissional,
    leg.conselho,
    leg.uf_conselho,
    leg.rqe,
    leg.foto_url,
    leg.created_at,
    leg.updated_at
FROM legado.profissionais leg
JOIN _etl.mapping_usuarios       mu ON mu.legado_id = leg.usuario_id
LEFT JOIN _etl.mapping_profissoes     mp ON mp.legado_id = leg.profissao_id
LEFT JOIN _etl.mapping_especialidades me ON me.legado_id = leg.especialidade_id
WHERE leg.usuario_id IS NOT NULL
ON CONFLICT (usuario_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- modelo_permissao_estabelecimento
--
-- TRANSFORMACAO CRITICA -- split do array legado em (permissoes, permissoes_extras).
--
-- Estrategia:
--   1. Para cada modelo legado, faz unnest(permissoes) -> JOIN com lookup.
--   2. Agrupa por destino ('areas','extras','descartar').
--   3. Reconstroi jsonb com somente as chaves de cada bucket marcadas TRUE.
--   4. tipo_acesso = 'Total' se is_admin OR (qtd areas = 10), senao 'Restrito'.
--
-- A maneira mais legivel e via CTE com agregacao condicional. Sem PII -- so
-- chaves de permissao.
-- ---------------------------------------------------------------------------
WITH unnested AS (
    SELECT
        leg.id              AS modelo_id,
        leg.estabelecimento_id,
        leg.nome,
        leg.descricao,
        leg.is_admin,
        leg.created_at,
        leg.updated_at,
        leg.ativo,
        chave_legado
    FROM legado.modelo_permissao_estabelecimento leg
    LEFT JOIN LATERAL jsonb_array_elements_text(COALESCE(leg.permissoes, '[]'::jsonb)) AS chave_legado ON TRUE
),
classificado AS (
    SELECT
        u.*,
        m.destino,
        m.chave_novo
    FROM unnested u
    LEFT JOIN _etl.permissao_legado_para_novo m ON m.chave_legado = u.chave_legado
),
-- Aviso (nao bloqueia): chaves desconhecidas no legado.
chaves_desconhecidas AS (
    SELECT chave_legado, COUNT(*) AS qtd
    FROM classificado
    WHERE chave_legado IS NOT NULL AND destino IS NULL
    GROUP BY chave_legado
),
agregado AS (
    SELECT
        modelo_id,
        MAX(estabelecimento_id) AS estabelecimento_id,
        MAX(nome) AS nome,
        MAX(descricao) AS descricao,
        bool_or(COALESCE(is_admin, false)) AS is_admin,
        MAX(created_at) AS created_at,
        MAX(updated_at) AS updated_at,
        bool_or(COALESCE(ativo, true)) AS ativo,
        -- areas: jsonb {chave_novo: true} para destino='areas'
        COALESCE(
            jsonb_object_agg(chave_novo, to_jsonb(true))
                FILTER (WHERE destino = 'areas' AND chave_novo IS NOT NULL),
            '{}'::jsonb
        ) AS permissoes,
        COALESCE(
            jsonb_object_agg(chave_novo, to_jsonb(true))
                FILTER (WHERE destino = 'extras' AND chave_novo IS NOT NULL),
            '{}'::jsonb
        ) AS permissoes_extras,
        COUNT(*) FILTER (WHERE destino = 'areas') AS qtd_areas
    FROM classificado
    GROUP BY modelo_id
)
INSERT INTO public.modelo_permissao_estabelecimento (
    id,
    estabelecimento_id,
    nome,
    descricao,
    is_admin,
    permissoes,
    permissoes_extras,
    tipo_acesso,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    a.modelo_id,
    a.estabelecimento_id,
    a.nome,
    a.descricao,
    a.is_admin,
    a.permissoes,
    a.permissoes_extras,
    CASE WHEN a.is_admin OR a.qtd_areas >= 10 THEN 'Total' ELSE 'Restrito' END AS tipo_acesso,
    a.created_at,
    a.updated_at,
    CASE WHEN a.ativo = false THEN COALESCE(a.updated_at, now()) ELSE NULL END AS deletado_em
FROM agregado a
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = a.estabelecimento_id)
ON CONFLICT (id) DO UPDATE SET
    permissoes        = EXCLUDED.permissoes,
    permissoes_extras = EXCLUDED.permissoes_extras,
    tipo_acesso       = EXCLUDED.tipo_acesso,
    atualizado_em     = EXCLUDED.atualizado_em;

-- Aviso de chaves desconhecidas (sem PII -- so chaves):
DO $$
DECLARE r record;
BEGIN
    FOR r IN
        SELECT chave_legado, COUNT(*) AS qtd
        FROM legado.modelo_permissao_estabelecimento leg,
             LATERAL jsonb_array_elements_text(COALESCE(leg.permissoes, '[]'::jsonb)) AS chave_legado
        WHERE NOT EXISTS (
            SELECT 1 FROM _etl.permissao_legado_para_novo m WHERE m.chave_legado = chave_legado
        )
        GROUP BY chave_legado
    LOOP
        RAISE WARNING 'Chave de permissao legada sem mapping: % (% ocorrencias)', r.chave_legado, r.qtd;
    END LOOP;
END $$;

-- ---------------------------------------------------------------------------
-- vinculo_profissional_estabelecimento
-- Status: ativo bool -> enum string. Convidados pendentes vivem em
-- solicitacoes_vinculo (abaixo), nao aqui.
-- ---------------------------------------------------------------------------
INSERT INTO public.vinculo_profissional_estabelecimento (
    id,
    estabelecimento_id,
    profissional_id,
    modelo_permissao_estabelecimento_id,
    is_admin,
    status,
    salario_fixo,
    percentual_sobre_servico,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.profissional_id,        -- novo PK e usuario_id (uuid) -- legado tambem usa o uuid de profissional. Validar mapping.
    leg.modelo_permissao_estabelecimento_id,
    COALESCE(leg.is_admin, false),
    CASE WHEN COALESCE(leg.ativo, true) THEN 'Ativo' ELSE 'Inativo' END AS status,
    leg.salario_fixo,
    leg.percentual_sobre_servico,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END AS deletado_em
FROM legado.vinculo_profissional_estabelecimento leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
  AND EXISTS (SELECT 1 FROM public.profissionais p WHERE p.usuario_id = leg.profissional_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- solicitacoes_vinculo (apenas pendentes)
-- ---------------------------------------------------------------------------
INSERT INTO public.solicitacoes_vinculo (
    id,
    estabelecimento_id,
    email_profissional,
    profissao_id,
    especialidade_id,
    modelo_permissao_estabelecimento_id,
    status,
    mensagem,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.email_profissional,
    mp.novo_id,
    me.novo_id,
    leg.modelo_permissao_estabelecimento_id,
    'Pendente'  AS status,
    leg.mensagem,
    leg.created_at,
    leg.updated_at
FROM legado.solicitacao_vinculo_profissional_estabelecimento leg
LEFT JOIN _etl.mapping_profissoes     mp ON mp.legado_id = leg.profissao_id
LEFT JOIN _etl.mapping_especialidades me ON me.legado_id = leg.especialidade_id
WHERE leg.status = 'pendente'
  AND EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('03_dominio_core.sql', now(), 'pacientes/profissionais/vinculos/permissoes carregados');

COMMIT;

-- Pos-flight
SELECT 'pacientes'        AS tabela, COUNT(*) FROM public.pacientes
UNION ALL SELECT 'profissionais',     COUNT(*) FROM public.profissionais
UNION ALL SELECT 'modelo_permissao',  COUNT(*) FROM public.modelo_permissao_estabelecimento
UNION ALL SELECT 'vinculos',          COUNT(*) FROM public.vinculo_profissional_estabelecimento
UNION ALL SELECT 'solicitacoes',      COUNT(*) FROM public.solicitacoes_vinculo;

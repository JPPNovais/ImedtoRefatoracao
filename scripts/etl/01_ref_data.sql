-- ===========================================================================
-- Script: 01_ref_data.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 00_setup_intermediate.sql
--
-- Objetivo:
--   Catalogos do novo (profissoes, especialidades, tipo_sala_atendimento,
--   regioes_anatomicas_catalogo, catalogo_procedimentos, planos) NAO MIGRAM
--   do legado -- ja foram seedados no destino. Este script apenas:
--     1. Valida que existem no destino (assumimos rodar com search_path
--        apontando ao DB destino para o INSERT ... SELECT remoto, mas
--        aqui, no intermediario, a validacao e indireta via dblink/FDW
--        OU consideramos pre-condicao garantida pelo migrations).
--     2. Constroi as mapping tables _etl.mapping_profissoes,
--        mapping_especialidades, mapping_tipo_sala, mapping_planos via
--        correspondencia por nome (lookup name-based -- catalogos sao
--        deterministicos e estaveis).
--
-- IMPORTANTE: este script roda no DB INTERMEDIARIO. Os catalogos do destino
-- sao copiados para schema "destino_seed" via pg_dump --schema-only --table=...
-- ANTES desta etapa (passo manual no runbook). Caso prefira, pode-se popular
-- as mappings carregando os CSVs dos seeds diretamente (vide TODO abaixo).
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

-- ---------------------------------------------------------------------------
-- Pre-flight: schema "destino_seed" (snapshot dos catalogos do novo) DEVE
-- existir, com as tabelas de catalogo populadas (resultado do pg_dump
-- --schema-only=public --data-only=public --table=profissoes ... do destino).
-- ---------------------------------------------------------------------------
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'destino_seed') THEN
        RAISE EXCEPTION 'Schema "destino_seed" nao existe. Execute pg_dump dos catalogos do novo antes (ver runbook 5.4).';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'destino_seed' AND tablename = 'profissoes') THEN
        RAISE EXCEPTION 'Tabela destino_seed.profissoes nao foi populada -- catalogo do destino faltando.';
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- Mapping de profissoes (lookup por nome -- catalogo seedado e idempotente).
-- ---------------------------------------------------------------------------
TRUNCATE _etl.mapping_profissoes;
INSERT INTO _etl.mapping_profissoes (legado_id, novo_id)
SELECT
    leg.id        AS legado_id,
    novo.id       AS novo_id
FROM legado.profissoes leg
JOIN destino_seed.profissoes novo
  ON unaccent(lower(novo.nome)) = unaccent(lower(leg.nome))
ON CONFLICT (legado_id) DO UPDATE SET novo_id = EXCLUDED.novo_id;

-- Auditoria: quem ficou sem match?
DO $$
DECLARE v_orfas int;
BEGIN
    SELECT COUNT(*) INTO v_orfas
    FROM legado.profissoes leg
    LEFT JOIN _etl.mapping_profissoes m ON m.legado_id = leg.id
    WHERE m.legado_id IS NULL;

    IF v_orfas > 0 THEN
        RAISE WARNING 'profissoes sem match no destino: % linhas (revisar antes de prosseguir).', v_orfas;
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- Mapping de especialidades (lookup por nome dentro da profissao).
-- ---------------------------------------------------------------------------
TRUNCATE _etl.mapping_especialidades;
INSERT INTO _etl.mapping_especialidades (legado_id, novo_id)
SELECT
    leg.id,
    novo.id
FROM legado.especialidades leg
JOIN _etl.mapping_profissoes mp ON mp.legado_id = leg.profissao_id
JOIN destino_seed.especialidades novo
  ON novo.profissao_id = mp.novo_id
 AND unaccent(lower(novo.nome)) = unaccent(lower(leg.nome))
ON CONFLICT (legado_id) DO UPDATE SET novo_id = EXCLUDED.novo_id;

-- ---------------------------------------------------------------------------
-- Mapping de tipo_sala_atendimento (catalogo system-wide, lookup por nome).
-- ---------------------------------------------------------------------------
TRUNCATE _etl.mapping_tipo_sala;
INSERT INTO _etl.mapping_tipo_sala (legado_id, novo_id)
SELECT leg.id, novo.id
FROM legado.tipo_sala_atendimento leg
JOIN destino_seed.tipo_sala_atendimento novo
  ON unaccent(lower(novo.nome)) = unaccent(lower(leg.nome))
ON CONFLICT (legado_id) DO UPDATE SET novo_id = EXCLUDED.novo_id;

-- ---------------------------------------------------------------------------
-- Mapping de planos (assinaturas vao referenciar -- script 04).
-- ---------------------------------------------------------------------------
TRUNCATE _etl.mapping_planos;
INSERT INTO _etl.mapping_planos (legado_id, novo_id, nome)
SELECT leg.id, novo.id, leg.nome
FROM legado.planos leg
JOIN destino_seed.planos novo
  ON unaccent(lower(novo.nome)) = unaccent(lower(leg.nome))
ON CONFLICT (legado_id) DO UPDATE SET novo_id = EXCLUDED.novo_id;

-- ---------------------------------------------------------------------------
-- TODO (decisao produto): catalogo_procedimentos e regioes_anatomicas_catalogo
-- foram seedados no destino mas NAO ha equivalente direto no legado --
-- legado serializava esses dados em jsonb. Mapping nao e necessaria; o
-- novo simplesmente expoe o catalogo seedado via /api/catalogos/*.
-- ---------------------------------------------------------------------------

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('01_ref_data.sql', now(), 'Mappings de catalogos populadas via lookup por nome');

COMMIT;

-- Pos-flight: contagens das mappings.
SELECT 'mapping_profissoes'      AS tabela, COUNT(*) AS qtd FROM _etl.mapping_profissoes
UNION ALL SELECT 'mapping_especialidades', COUNT(*) FROM _etl.mapping_especialidades
UNION ALL SELECT 'mapping_tipo_sala',       COUNT(*) FROM _etl.mapping_tipo_sala
UNION ALL SELECT 'mapping_planos',          COUNT(*) FROM _etl.mapping_planos;

-- ===========================================================================
-- Script: 00_setup_intermediate.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: pg_dump do projeto legado ja restaurado em schema "legado"
--               do DB intermediario.
--
-- Objetivo:
--   1. Criar schema "_etl" com tabelas de mapeamento (uuid_legado -> uuid_novo)
--      e tabelas de lookup (split de permissoes, mapping de plano, etc).
--   2. Criar indices temporarios em legado.* para acelerar joins do ETL.
--   3. Configurar variaveis de sessao (verbosidade, statement_timeout, etc).
--
-- Execucao: psql -v ON_ERROR_STOP=1 -f 00_setup_intermediate.sql
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;
SET statement_timeout = 0;
SET lock_timeout = '5min';
SET idle_in_transaction_session_timeout = 0;

-- ---------------------------------------------------------------------------
-- Pre-flight: schema "legado" (resultado do pg_dump --schema=public renomeado)
-- precisa existir e ter ao menos uma tabela.
-- ---------------------------------------------------------------------------
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'legado') THEN
        RAISE EXCEPTION 'Schema "legado" nao existe. Restaure o pg_dump primeiro: pg_restore --schema=legado dump.bin';
    END IF;
    IF (SELECT COUNT(*) FROM pg_tables WHERE schemaname = 'legado') < 10 THEN
        RAISE EXCEPTION 'Schema "legado" tem menos de 10 tabelas. Dump incompleto?';
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- Schema _etl: workspace de mapeamentos e lookups
-- ---------------------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS _etl;

-- Auditoria global do ETL (cada script registra quando rodou + contagens).
CREATE TABLE IF NOT EXISTS _etl.run_log (
    id              bigserial PRIMARY KEY,
    script_nome     text NOT NULL,
    iniciado_em     timestamptz NOT NULL DEFAULT now(),
    finalizado_em   timestamptz,
    linhas_origem   bigint,
    linhas_destino  bigint,
    observacao      text
);

-- ---------------------------------------------------------------------------
-- Mapping tables (uuid_legado -> uuid_novo)
-- Usadas quando a chave foi recriada (ex: usuarios passa a apontar para
-- auth.users do projeto NOVO). Para o restante, novo preserva o uuid legado.
-- PK no legado_id para INSERT idempotente via ON CONFLICT.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS _etl.mapping_usuarios (
    legado_id   uuid PRIMARY KEY,
    novo_id     uuid NOT NULL,
    email       text NOT NULL,
    migrado_em  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS _etl.mapping_planos (
    legado_id   uuid PRIMARY KEY,
    novo_id     uuid NOT NULL,
    nome        text NOT NULL
);

CREATE TABLE IF NOT EXISTS _etl.mapping_profissoes (
    legado_id   uuid PRIMARY KEY,
    novo_id     uuid NOT NULL
);

CREATE TABLE IF NOT EXISTS _etl.mapping_especialidades (
    legado_id   uuid PRIMARY KEY,
    novo_id     uuid NOT NULL
);

CREATE TABLE IF NOT EXISTS _etl.mapping_tipo_sala (
    legado_id   uuid PRIMARY KEY,
    novo_id     uuid NOT NULL
);

-- Bloqueio de receitas invalidas (script 06 fail-fast).
CREATE TABLE IF NOT EXISTS _etl.receitas_invalidas (
    receita_id      uuid PRIMARY KEY,
    estabelecimento_id uuid,
    tipo_legado     text,
    tipo_notificacao_legado text,
    motivo          text NOT NULL,
    detectado_em    timestamptz DEFAULT now()
);

-- Conflitos de overlap em agendamentos (script 05) -- NAO bloqueia ETL,
-- apenas registra para relatorio pos-carga.
CREATE TABLE IF NOT EXISTS _etl.agendamentos_overlap (
    agendamento_a   uuid,
    agendamento_b   uuid,
    profissional_id uuid,
    intervalo_a     tstzrange,
    intervalo_b     tstzrange,
    detectado_em    timestamptz DEFAULT now(),
    PRIMARY KEY (agendamento_a, agendamento_b)
);

-- ---------------------------------------------------------------------------
-- Lookup: split do array "permissoes" legado em (permissoes, permissoes_extras)
-- do novo modelo. Chave legada -> destino + chave nova.
-- destino ∈ ('areas','extras','descartar')
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS _etl.permissao_legado_para_novo (
    chave_legado    text PRIMARY KEY,
    destino         text NOT NULL CHECK (destino IN ('areas','extras','descartar')),
    chave_novo      text,
    observacao      text
);

INSERT INTO _etl.permissao_legado_para_novo (chave_legado, destino, chave_novo, observacao) VALUES
    -- areas (alto nivel) -- 10 chaves
    ('agenda_acessar',          'areas',  'agenda',         NULL),
    ('pacientes_acessar',       'areas',  'pacientes',      NULL),
    ('prontuarios_acessar',     'areas',  'prontuarios',    NULL),
    ('financeiro_acessar',      'areas',  'financeiro',     NULL),
    ('inventario_acessar',      'areas',  'inventario',     NULL),
    ('orcamentos_acessar',      'areas',  'orcamentos',     NULL),
    ('relatorios_acessar',      'areas',  'relatorios',     NULL),
    ('configuracoes_acessar',   'areas',  'configuracoes',  NULL),
    ('equipe_acessar',          'areas',  'equipe',         NULL),
    ('automacoes_acessar',      'areas',  'automacoes',     NULL),
    -- extras (granulares) -- 6 chaves
    ('prontuario_editar_terceiros',     'extras', 'prontuario_editar_terceiros',     NULL),
    ('agenda_ver_outros_profissionais', 'extras', 'agenda_ver_outros_profissionais', NULL),
    ('financeiro_ver_valores',          'extras', 'financeiro_ver_valores',          NULL),
    ('inventario_movimentar',           'extras', 'inventario_movimentar',           NULL),
    ('orcamento_aprovar',               'extras', 'orcamento_aprovar',               NULL),
    ('equipe_convidar',                 'extras', 'equipe_convidar',                 NULL),
    -- chaves descartadas (nao tem equivalente no novo)
    ('super_admin',     'descartar', NULL, 'Privilegio implicito por is_admin no novo'),
    ('dashboard_admin', 'descartar', NULL, 'Eliminada -- dashboard usa permissoes:relatorios')
ON CONFLICT (chave_legado) DO UPDATE SET
    destino = EXCLUDED.destino,
    chave_novo = EXCLUDED.chave_novo,
    observacao = EXCLUDED.observacao;

-- ---------------------------------------------------------------------------
-- Lookup: status legado (lowercase) -> novo (PascalCase)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS _etl.status_agendamento_map (
    legado text PRIMARY KEY,
    novo   text NOT NULL
);
INSERT INTO _etl.status_agendamento_map VALUES
    ('agendado',   'Agendado'),
    ('confirmado', 'Confirmado'),
    ('concluido',  'Concluido'),
    ('cancelado',  'Cancelado'),
    ('falta',      'Falta')
ON CONFLICT (legado) DO UPDATE SET novo = EXCLUDED.novo;

-- ---------------------------------------------------------------------------
-- Lookup: (tipo, tipo_notificacao) legado -> Tipo novo (receitas)
-- Combinacoes invalidas (SIMPLES + tipo_notificacao preenchido) NAO entram
-- aqui -- sao detectadas no script 06 via JOIN-LEFT e abortam a carga.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS _etl.receita_tipo_map (
    tipo_legado          text NOT NULL,
    tipo_notificacao_legado text,
    tipo_novo            text NOT NULL,
    PRIMARY KEY (tipo_legado, COALESCE(tipo_notificacao_legado, ''))
);
INSERT INTO _etl.receita_tipo_map (tipo_legado, tipo_notificacao_legado, tipo_novo) VALUES
    ('SIMPLES',     NULL, 'Comum'),
    ('CONTROLADA',  'A',  'NotificacaoA'),
    ('CONTROLADA',  'B',  'NotificacaoB'),
    ('CONTROLADA',  'C',  'NotificacaoC'),
    ('CONTROLADA',  'ESPECIAL', 'Especial')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Indices temporarios em legado.* -- aceleram joins do ETL e sao
-- removidos ao final (script 99 ou manualmente apos carga).
-- Criar apenas se a tabela existir (alguns dumps podem nao trazer tudo).
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    v_idx record;
BEGIN
    FOR v_idx IN
        SELECT * FROM (VALUES
            ('legado','usuarios','idx_etl_usuarios_email','(email)'),
            ('legado','estabelecimentos','idx_etl_estab_owner','(owner_usuario_id)'),
            ('legado','profissionais','idx_etl_prof_usuario','(usuario_id) WHERE usuario_id IS NOT NULL'),
            ('legado','vinculo_profissional_estabelecimento','idx_etl_vinculo_prof','(profissional_id, estabelecimento_id)'),
            ('legado','pacientes','idx_etl_pac_estab','(estabelecimento_id)'),
            ('legado','prontuarios','idx_etl_pront_paciente','(paciente_id)'),
            ('legado','evolucao_prontuario','idx_etl_evol_pront','(prontuario_id)'),
            ('legado','exame_fisico','idx_etl_ef_evol','(evolucao_prontuario_id)'),
            ('legado','evento_de_agendamento','idx_etl_ag_estab_data','(estabelecimento_id, data_hora_inicio)'),
            ('legado','receitas','idx_etl_rec_pac','(paciente_id)'),
            ('legado','receita_itens','idx_etl_recitem_rec','(receita_id)'),
            ('legado','financeiro_transacao','idx_etl_fin_estab','(estabelecimento_id)'),
            ('legado','movimento_estoque','idx_etl_mov_produto','(estoque_produto_id)'),
            ('legado','orcamentos','idx_etl_orc_estab','(estabelecimento_id)')
        ) AS t(schemaname, tablename, idxname, def)
    LOOP
        IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = v_idx.schemaname AND tablename = v_idx.tablename) THEN
            EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON %I.%I %s',
                           v_idx.idxname, v_idx.schemaname, v_idx.tablename, v_idx.def);
        END IF;
    END LOOP;
END $$;

-- ---------------------------------------------------------------------------
-- ANALYZE em todo o schema legado (planner precisa de stats novas).
-- ---------------------------------------------------------------------------
DO $$
DECLARE r record;
BEGIN
    FOR r IN SELECT schemaname, tablename FROM pg_tables WHERE schemaname = 'legado' LOOP
        EXECUTE format('ANALYZE %I.%I', r.schemaname, r.tablename);
    END LOOP;
END $$;

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('00_setup_intermediate.sql', now(), 'Setup OK -- mappings e indices criados');

COMMIT;

-- Pos-flight: contagem de tabelas legado e tamanho aproximado.
SELECT
    schemaname,
    COUNT(*) AS qtd_tabelas,
    pg_size_pretty(SUM(pg_total_relation_size(format('%I.%I', schemaname, tablename)::regclass))) AS tamanho
FROM pg_tables
WHERE schemaname = 'legado'
GROUP BY schemaname;

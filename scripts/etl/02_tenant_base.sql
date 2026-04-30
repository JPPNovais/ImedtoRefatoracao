-- ===========================================================================
-- Script: 02_tenant_base.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 00_setup_intermediate.sql, 01_ref_data.sql
--
-- Objetivo:
--   Carregar a base de tenant no destino (schema "public" do DB destino,
--   alcancado aqui via dblink/postgres_fdw OU via INSERT INTO destino...
--   apos pg_dump intermediario -> apply no destino):
--     - usuarios (espelho de auth.users; auth ja foi recriada via convite)
--     - estabelecimentos
--     - unidades_estabelecimento
--     - sala_atendimento
--
-- Premissas:
--   * auth.users do destino JA foi populada via convite/reset (fora deste SQL)
--     e _etl.mapping_usuarios JA tem (legado_id -> novo_auth_id) preenchido
--     pelo script de convites do devops. Linhas legado sem entrada em
--     mapping_usuarios sao ignoradas (usuarios inativos > 24 meses ou que
--     nao aceitaram o convite).
--   * Cutoff LGPD: usuarios com last_sign_in_at < now() - 24m nao migram.
--
-- Modo de execucao remota: este script gera as linhas no schema "public" do
-- DB intermediario; o passo 99 dispara COPY -> destino via pg_dump
-- --schema=public --table=... | psql destino. Alternativa: rodar o INSERT
-- diretamente contra o destino via postgres_fdw (FOREIGN TABLE) -- ver
-- runbook 5.4.
--
-- Idempotencia: ON CONFLICT (id) DO NOTHING em todas as tabelas.
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

-- Pre-flight
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'legado' AND tablename = 'estabelecimentos') THEN
        RAISE EXCEPTION 'legado.estabelecimentos nao existe -- rode 00_setup primeiro.';
    END IF;
    IF (SELECT COUNT(*) FROM _etl.mapping_usuarios) = 0 THEN
        RAISE WARNING 'mapping_usuarios esta VAZIO -- nenhum usuario sera migrado. Confirme se convites foram processados.';
    END IF;
END $$;

BEGIN;

-- ---------------------------------------------------------------------------
-- usuarios (shadow row do auth.users)
-- Filtro LGPD: somente usuarios com login nos ultimos 24 meses.
-- O id usado no destino e o NOVO uuid (vindo do auth.users novo) -- por isso
-- a mapping table existe.
-- ---------------------------------------------------------------------------
INSERT INTO public.usuarios (
    id,
    nome_completo,
    cpf_cnpj,
    tipo_pessoa,
    onboarding_concluido,
    tutorial_visto,
    criado_em,
    atualizado_em
)
SELECT
    m.novo_id                       AS id,
    leg.nome_completo,
    leg.cpf_cnpj,
    leg.tipo_pessoa,
    COALESCE(leg.onboarding_concluido, false),
    COALESCE(leg.tutorial_visto, false),
    leg.created_at                  AS criado_em,
    leg.updated_at                  AS atualizado_em
FROM legado.usuarios leg
JOIN _etl.mapping_usuarios m ON m.legado_id = leg.id
WHERE leg.last_sign_in_at IS NULL OR leg.last_sign_in_at > now() - interval '24 months'
ON CONFLICT (id) DO UPDATE SET
    nome_completo  = EXCLUDED.nome_completo,
    cpf_cnpj       = EXCLUDED.cpf_cnpj,
    tipo_pessoa    = EXCLUDED.tipo_pessoa,
    atualizado_em  = EXCLUDED.atualizado_em;

-- ---------------------------------------------------------------------------
-- estabelecimentos
-- owner_usuario_id usa mapping_usuarios. Estabelecimentos cujo dono nao
-- esta na mapping (= dono inativo) nao migram. Isto e proposital -- sem
-- dono, estabelecimento orfao quebra invariantes do dominio.
-- ---------------------------------------------------------------------------
INSERT INTO public.estabelecimentos (
    id,
    nome_fantasia,
    razao_social,
    tipo_pessoa,
    cpf_cnpj,
    owner_usuario_id,
    foto_url,
    -- endereco
    cep, logradouro, numero, complemento, bairro, cidade, uf,
    -- funcionamento (sem origem -- default {} para preenchimento posterior)
    funcionamento,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.nome_fantasia,
    leg.razao_social,
    leg.tipo_pessoa,
    leg.cpf_cnpj,
    m.novo_id        AS owner_usuario_id,
    leg.foto_url,
    leg.cep, leg.logradouro, leg.numero, leg.complemento, leg.bairro, leg.cidade, leg.uf,
    '{}'::jsonb      AS funcionamento,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END AS deletado_em
FROM legado.estabelecimentos leg
JOIN _etl.mapping_usuarios m ON m.legado_id = leg.owner_usuario_id
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- unidades_estabelecimento
-- Direct copy. is_principal preserva flag legada.
-- ---------------------------------------------------------------------------
INSERT INTO public.unidades_estabelecimento (
    id,
    estabelecimento_id,
    nome,
    is_principal,
    cep, logradouro, numero, complemento, bairro, cidade, uf,
    telefone,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.nome,
    COALESCE(leg.is_principal, false),
    leg.cep, leg.logradouro, leg.numero, leg.complemento, leg.bairro, leg.cidade, leg.uf,
    leg.telefone,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END AS deletado_em
FROM legado.unidades_estabelecimento leg
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- sala_atendimento
-- tipo_sala_atendimento_id resolvido via _etl.mapping_tipo_sala.
-- ---------------------------------------------------------------------------
INSERT INTO public.sala_atendimento (
    id,
    estabelecimento_id,
    unidade_id,
    tipo_sala_atendimento_id,
    nome,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.unidade_id,
    mt.novo_id        AS tipo_sala_atendimento_id,
    leg.nome,
    leg.created_at,
    leg.updated_at,
    CASE WHEN COALESCE(leg.ativo, true) = false THEN COALESCE(leg.updated_at, now()) ELSE NULL END AS deletado_em
FROM legado.sala_atendimento leg
LEFT JOIN _etl.mapping_tipo_sala mt ON mt.legado_id = leg.tipo_sala_atendimento_id
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
ON CONFLICT (id) DO NOTHING;

INSERT INTO _etl.run_log (script_nome, finalizado_em, linhas_destino, observacao)
SELECT '02_tenant_base.sql', now(),
       (SELECT COUNT(*) FROM public.usuarios)
     + (SELECT COUNT(*) FROM public.estabelecimentos)
     + (SELECT COUNT(*) FROM public.unidades_estabelecimento)
     + (SELECT COUNT(*) FROM public.sala_atendimento),
       'tenant base carregada';

COMMIT;

-- Pos-flight
SELECT 'usuarios'                  AS tabela, COUNT(*) FROM public.usuarios
UNION ALL SELECT 'estabelecimentos',           COUNT(*) FROM public.estabelecimentos
UNION ALL SELECT 'unidades_estabelecimento',   COUNT(*) FROM public.unidades_estabelecimento
UNION ALL SELECT 'sala_atendimento',           COUNT(*) FROM public.sala_atendimento;

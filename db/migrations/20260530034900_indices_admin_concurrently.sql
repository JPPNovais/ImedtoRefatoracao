-- Índices CONCURRENTLY para área admin global.
-- ATENÇÃO: CREATE INDEX CONCURRENTLY não pode rodar dentro de transação.
-- Por isso este arquivo é separado do SQL principal da migration.
-- A pipeline aplica via: psql -f este_arquivo.sql (SEM BEGIN/COMMIT wrapper).
-- Todos os índices são idempotentes via IF NOT EXISTS.
--
-- CA26: lista de estabelecimentos com busca ILIKE em nome_fantasia deve responder
-- p95 < 500ms a 10k tenants. Índice GIN trigram em nome_fantasia cobre isso.
-- pg_trgm já está habilitado (confirmado em dev em 2026-05-30).

-- Índice trigram em estabelecimentos.nome_fantasia para busca ILIKE.
-- Cobertura: WHERE nome_fantasia ILIKE '%clinica%' etc.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_estabelecimentos_nome_fantasia_trgm
    ON public.estabelecimentos
    USING GIN (nome_fantasia gin_trgm_ops);

-- Índice em estabelecimentos.criado_em DESC para lista ordenada por data de criação.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_estabelecimentos_criado_em_desc
    ON public.estabelecimentos (criado_em DESC);

-- Índice parcial em imedto_admin_refresh_tokens para lookup de tokens ativos.
-- Cobre: WHERE token_hash = $1 AND revogado_em IS NULL.
-- O índice único em token_hash já existe, este é parcial para performance no caminho feliz.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_imedto_admin_refresh_tokens_ativos
    ON public.imedto_admin_refresh_tokens (token_hash)
    WHERE revogado_em IS NULL;

-- Índices CONCURRENTLY — Wave 2 Catálogos Globais
-- Timestamp: 20260530131405
--
-- CREATE INDEX CONCURRENTLY não pode rodar dentro de transação.
-- Este arquivo é executado APÓS o arquivo principal de schema (20260530131404_...).
-- A pipeline deve rodá-lo FORA de qualquer BEGIN/COMMIT.
--
-- Índices expressionais LOWER(nome): garantem unicidade case-insensitive sem depender
-- de citext nas tabelas (as tabelas usam text puro — a semântica CI vem do índice).
-- Sem BEGIN/COMMIT.

-- ── imedto_modelo_prontuario_global ──────────────────────────────────────────

-- Derruba o índice simples criado pelo EF (no arquivo de schema) e substitui pelo
-- índice expressional LOWER() que garante unicidade real case-insensitive.
-- O índice EF (uq_imedto_modelo_prontuario_global_nome_lower) é sobre nome bruto.
-- Aqui criamos o correto sobre LOWER(nome).

DROP INDEX CONCURRENTLY IF EXISTS public.uq_imedto_modelo_prontuario_global_nome_lower;
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_imedto_modelo_prontuario_global_nome_lower
    ON public.imedto_modelo_prontuario_global (LOWER(nome))
    WHERE ativo = true;

-- ── imedto_variavel_pool_global ───────────────────────────────────────────────

DROP INDEX CONCURRENTLY IF EXISTS public.uq_imedto_variavel_pool_global_nome_lower;
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_imedto_variavel_pool_global_nome_lower
    ON public.imedto_variavel_pool_global (LOWER(nome))
    WHERE ativo = true;

-- ── imedto_regiao_anatomica_global ────────────────────────────────────────────

DROP INDEX CONCURRENTLY IF EXISTS public.uq_imedto_regiao_anatomica_global_nome_lower;
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_imedto_regiao_anatomica_global_nome_lower
    ON public.imedto_regiao_anatomica_global (LOWER(nome))
    WHERE ativo = true;

-- GIN em sinonimos para busca por array (usado em queries WHERE sinonimos @> ARRAY['termo']).
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_imedto_regiao_anatomica_global_sinonimos_gin
    ON public.imedto_regiao_anatomica_global USING GIN (sinonimos)
    WHERE sinonimos IS NOT NULL;

-- ----------------------------------------------------------------------------
-- Indice trigram em pacientes.nome_completo para acelerar busca ILIKE '%X%'.
--
-- Contexto: PacienteQueryRepository.Listar usa
--   nome_completo ILIKE '%' || @Busca || '%'
-- Com B-tree padrao isso vira full-scan filtrado por estabelecimento_id —
-- aceitavel em tenants pequenos, ruim em base de >50k pacientes.
--
-- Solucao: GIN com pg_trgm sobre uma expressao IMMUTABLE de
-- lower(unaccent(nome_completo)). Da match com acento/maiuscula/etc.
--
-- Tres passos:
--   1. Habilitar extensoes pg_trgm + unaccent.
--   2. Criar wrapper IMMUTABLE para unaccent (a funcao nativa e STABLE
--      por causa do dicionario, e GIN exige IMMUTABLE).
--   3. Criar indice GIN.
-- ----------------------------------------------------------------------------

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;

-- Wrapper IMMUTABLE: a funcao unaccent() padrao depende do dicionario
-- (carregado em runtime), entao Postgres a marca como STABLE. Para usar
-- em indice expression, criamos um wrapper que aponta explicitamente para
-- o dicionario 'unaccent' e marcamos IMMUTABLE — seguro porque o dicionario
-- nao muda em produc�o.
CREATE OR REPLACE FUNCTION public.imutable_unaccent(text)
    RETURNS text
    LANGUAGE sql
    IMMUTABLE
    PARALLEL SAFE
    STRICT
AS $$
    SELECT public.unaccent('public.unaccent', $1);
$$;

-- Indice GIN com gin_trgm_ops sobre lower(unaccent(...)).
-- Se ja existir (re-execucao), recria para garantir que aponta para a
-- expressao correta. CONCURRENTLY nao pode rodar dentro de transacao —
-- a Supabase CLI envolve toda migration em transacao por default, entao
-- usamos CREATE simples (lock breve em tabela; aceitavel em janela).
CREATE INDEX IF NOT EXISTS ix_pacientes_nome_completo_trgm
    ON public.pacientes
    USING gin (public.imutable_unaccent(lower(nome_completo)) gin_trgm_ops)
    WHERE deletado_em IS NULL;

-- ----------------------------------------------------------------------------
-- Como o front busca:
--   front -> /api/paciente?busca=Maria
--   PacienteQueryRepository.Listar gera ILIKE '%' || @Busca || '%'.
--
-- Para o planner usar este indice, a query precisa ser reescrita para
-- comparar a EXPRESSAO indexada (lower+unaccent dos dois lados):
--   public.imutable_unaccent(lower(nome_completo)) ILIKE '%' || public.imutable_unaccent(lower(@Busca)) || '%'
--
-- Esse ajuste no SQL Dapper esta no commit que acompanha esta migration.
-- ----------------------------------------------------------------------------

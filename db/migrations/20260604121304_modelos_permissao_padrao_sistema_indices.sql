-- Índices parciais para o escopo global de modelo_permissao_estabelecimento.
-- Devem ser aplicados APÓS 20260604121304_modelos_permissao_padrao_sistema.sql
-- (a coluna estabelecimento_id precisa ser nullable primeiro).
--
-- CREATE INDEX CONCURRENTLY não pode rodar dentro de transação — arquivo separado,
-- aplicado pelo migrate.sh FORA do bloco de transação principal.

-- Unique parcial: garante unicidade de nome no escopo global (WHERE estabelecimento_id IS NULL).
-- A unique existente (estabelecimento_id, nome) trata NULLs como distintos no Postgres,
-- portanto não seria suficiente para bloquear dois registros globais com o mesmo nome.
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_modelo_permissao_global_nome
    ON public.modelo_permissao_estabelecimento (nome)
    WHERE estabelecimento_id IS NULL;

-- Índice parcial para listagem admin e queries de propagação/seed (busca por nome no escopo global).
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_modelo_permissao_global
    ON public.modelo_permissao_estabelecimento (nome)
    WHERE estabelecimento_id IS NULL;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario') THEN
    ALTER TABLE public.orcamento_catalogo_produto ADD item_inventario_id bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario') THEN
    ALTER TABLE public.cobrancas ADD evolucao_id bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario') THEN
    CREATE INDEX "IX_orcamento_catalogo_produto_item_inventario_id" ON public.orcamento_catalogo_produto (item_inventario_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario') THEN
    ALTER TABLE public.orcamento_catalogo_produto ADD CONSTRAINT fk_catalogo_produto_item_inventario FOREIGN KEY (item_inventario_id) REFERENCES public.itens_inventario (id) ON DELETE SET NULL;
    END IF;
END $EF$;

-- Índice UNIQUE parcial — idempotência F4 (R7/CA77/CA78).
-- Garante 1 cobrança de procedimento por evolução, impossibilitando duplicação por race condition.
-- O violador recebe 23505 → mapeado para 422 "Procedimento já marcado como realizado".
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario') THEN
    CREATE UNIQUE INDEX ux_cobrancas_evolucao_procedimento ON public.cobrancas (evolucao_id) WHERE origem = 'Procedimento' AND evolucao_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260611011105_CobrancaF4EvolucaoIdProdutoItemInventario', '10.0.0');
    END IF;
END $EF$;

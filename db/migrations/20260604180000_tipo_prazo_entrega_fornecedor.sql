DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604180000_TipoPrazoEntregaFornecedor') THEN
    ALTER TABLE public.fornecedores_estoque ADD tipo_prazo_entrega varchar(10) NOT NULL DEFAULT 'corridos';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604180000_TipoPrazoEntregaFornecedor') THEN
    ALTER TABLE public.fornecedores_estoque ADD CONSTRAINT ck_fornecedores_estoque_tipo_prazo_entrega
        CHECK (tipo_prazo_entrega IN ('corridos', 'uteis'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604180000_TipoPrazoEntregaFornecedor') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260604180000_TipoPrazoEntregaFornecedor', '10.0.0');
    END IF;
END $EF$;

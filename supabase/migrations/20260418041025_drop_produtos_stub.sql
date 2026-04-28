-- Remove a tabela stub do smoke test da Fase 0.
-- EF migration: 20260418041025_DropProdutosStub

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418041025_DropProdutosStub') THEN
    DROP TABLE public.produtos;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418041025_DropProdutosStub') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260418041025_DropProdutosStub', '10.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260517201229_AmpliarFotoUrlParaPresignedUrl') THEN
    ALTER TABLE public.profissionais ALTER COLUMN foto_url TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260517201229_AmpliarFotoUrlParaPresignedUrl') THEN
    ALTER TABLE public.estabelecimentos ALTER COLUMN foto_url TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260517201229_AmpliarFotoUrlParaPresignedUrl') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260517201229_AmpliarFotoUrlParaPresignedUrl', '10.0.0');
    END IF;
END $EF$;

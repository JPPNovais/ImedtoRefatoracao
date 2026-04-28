-- Adiciona coluna foto_url em profissionais e estabelecimentos.
-- Gerado via `dotnet ef migrations script --idempotent`.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425041841_AdicionarFotoUrl') THEN
    ALTER TABLE public.profissionais ADD foto_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425041841_AdicionarFotoUrl') THEN
    ALTER TABLE public.estabelecimentos ADD foto_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425041841_AdicionarFotoUrl') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260425041841_AdicionarFotoUrl', '10.0.0');
    END IF;
END $EF$;

-- Regra: cada usuário pode ser dono de apenas 1 estabelecimento.
-- Converte o índice ix_estabelecimentos_dono (não-único) em uq_estabelecimentos_dono (único).
-- EF migration: 20260418042352_UniqueDonoEstabelecimento

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418042352_UniqueDonoEstabelecimento') THEN
    DROP INDEX IF EXISTS public.ix_estabelecimentos_dono;
    CREATE UNIQUE INDEX uq_estabelecimentos_dono ON public.estabelecimentos (dono_usuario_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418042352_UniqueDonoEstabelecimento') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260418042352_UniqueDonoEstabelecimento', '10.0.0');
    END IF;
END $EF$;

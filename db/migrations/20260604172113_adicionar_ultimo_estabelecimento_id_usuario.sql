DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604172113_AdicionarUltimoEstabelecimentoIdUsuario') THEN
    ALTER TABLE public.usuarios ADD ultimo_estabelecimento_id bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604172113_AdicionarUltimoEstabelecimentoIdUsuario') THEN
    CREATE INDEX ix_usuarios_ultimo_estabelecimento_id ON public.usuarios (ultimo_estabelecimento_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604172113_AdicionarUltimoEstabelecimentoIdUsuario') THEN
    ALTER TABLE public.usuarios ADD CONSTRAINT fk_usuarios_ultimo_estabelecimento FOREIGN KEY (ultimo_estabelecimento_id) REFERENCES public.estabelecimentos (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604172113_AdicionarUltimoEstabelecimentoIdUsuario') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260604172113_AdicionarUltimoEstabelecimentoIdUsuario', '10.0.0');
    END IF;
END $EF$;

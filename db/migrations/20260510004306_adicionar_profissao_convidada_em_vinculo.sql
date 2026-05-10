DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260510004306_AdicionarProfissaoConvidadaEmVinculo') THEN

    ALTER TABLE public.vinculo_profissional_estabelecimento
        ADD COLUMN IF NOT EXISTS profissao_convidada_id bigint;

    -- FK p/ catálogo de profissões (catálogo é global; ON DELETE SET NULL caso a
    -- profissão seja removida do catálogo no futuro).
    DO $fk$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'fk_vinculo_profissao_convidada'
              AND conrelid = 'public.vinculo_profissional_estabelecimento'::regclass
        ) THEN
            ALTER TABLE public.vinculo_profissional_estabelecimento
                ADD CONSTRAINT fk_vinculo_profissao_convidada
                FOREIGN KEY (profissao_convidada_id)
                REFERENCES public.profissoes(id)
                ON DELETE SET NULL;
        END IF;
    END $fk$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260510004306_AdicionarProfissaoConvidadaEmVinculo') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260510004306_AdicionarProfissaoConvidadaEmVinculo', '10.0.0');
    END IF;
END $EF$;

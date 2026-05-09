DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260509204004_SeedProfissoesAdministrativas') THEN

    INSERT INTO public.profissoes (nome, conselho_sigla, ativo) VALUES
        ('Secretária',                    '', true),
        ('Auxiliar Administrativo',       '', true),
        ('Gerente / Coordenador(a)',      '', true),
        ('Financeiro',                    '', true)
    ON CONFLICT (nome) DO NOTHING;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260509204004_SeedProfissoesAdministrativas') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260509204004_SeedProfissoesAdministrativas', '10.0.0');
    END IF;
END $EF$;

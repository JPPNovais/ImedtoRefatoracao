-- Convite: dados pré-cadastrados pelo convidador para o onboarding do convidado.
-- modelo_permissao_id vira nullable para permitir convidar sem permissão definida
-- (vínculo Ativo sem modelo = SemAcesso, até alguém atribuir).

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260502123108_ConviteVinculoCamposPreCadastrados') THEN
    ALTER TABLE public.vinculo_profissional_estabelecimento ALTER COLUMN modelo_permissao_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260502123108_ConviteVinculoCamposPreCadastrados') THEN
    ALTER TABLE public.vinculo_profissional_estabelecimento ADD especialidade_convidada character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260502123108_ConviteVinculoCamposPreCadastrados') THEN
    ALTER TABLE public.vinculo_profissional_estabelecimento ADD nome_convidado character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260502123108_ConviteVinculoCamposPreCadastrados') THEN
    ALTER TABLE public.vinculo_profissional_estabelecimento ADD telefone_convidado character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260502123108_ConviteVinculoCamposPreCadastrados') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260502123108_ConviteVinculoCamposPreCadastrados', '10.0.0');
    END IF;
END $EF$;

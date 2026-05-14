-- Bug #3: prepara o schema para distinguir receitas assinadas digitalmente
-- (ICP-Brasil / Memed) de receitas apenas com identificação do profissional.
-- Hoje todas as receitas nascem com 'NaoAssinada' — front exibe banner
-- orientando assinar manualmente ao imprimir. Coluna é base para integração
-- futura com provedores de assinatura sem nova migration.
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260514111139_AdicionarAssinaturaDigitalStatusEmReceitas') THEN
    ALTER TABLE public.receitas ADD assinatura_digital_status character varying(20) NOT NULL DEFAULT 'NaoAssinada';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260514111139_AdicionarAssinaturaDigitalStatusEmReceitas') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260514111139_AdicionarAssinaturaDigitalStatusEmReceitas', '10.0.0');
    END IF;
END $EF$;

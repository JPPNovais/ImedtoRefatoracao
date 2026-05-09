DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260509131812_AdicionarCheckInEmAgendamento') THEN
    ALTER TABLE public.agendamentos ADD check_in_em timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260509131812_AdicionarCheckInEmAgendamento') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260509131812_AdicionarCheckInEmAgendamento', '10.0.0');
    END IF;
END $EF$;

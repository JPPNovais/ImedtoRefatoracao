DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611023843_AdicionarReciboEmitidoEmPagamentoF8') THEN
    ALTER TABLE public.pagamentos ADD recibo_emitido_em timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611023843_AdicionarReciboEmitidoEmPagamentoF8') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260611023843_AdicionarReciboEmitidoEmPagamentoF8', '10.0.0');
    END IF;
END $EF$;

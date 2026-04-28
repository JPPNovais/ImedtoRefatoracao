-- Adiciona configuração de funcionamento ao aggregate Estabelecimento.
-- EF migration: 20260425043444_AdicionarFuncionamentoEstabelecimento

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425043444_AdicionarFuncionamentoEstabelecimento') THEN
    ALTER TABLE public.estabelecimentos ADD datas_bloqueadas jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425043444_AdicionarFuncionamentoEstabelecimento') THEN
    ALTER TABLE public.estabelecimentos ADD dias_semana_funcionamento jsonb NOT NULL DEFAULT ('[1,2,3,4,5]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425043444_AdicionarFuncionamentoEstabelecimento') THEN
    ALTER TABLE public.estabelecimentos ADD horario_fim time NOT NULL DEFAULT ('18:00'::time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425043444_AdicionarFuncionamentoEstabelecimento') THEN
    ALTER TABLE public.estabelecimentos ADD horario_inicio time NOT NULL DEFAULT ('08:00'::time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425043444_AdicionarFuncionamentoEstabelecimento') THEN
    ALTER TABLE public.estabelecimentos ADD horarios_bloqueados jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260425043444_AdicionarFuncionamentoEstabelecimento') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260425043444_AdicionarFuncionamentoEstabelecimento', '10.0.0');
    END IF;
END $EF$;

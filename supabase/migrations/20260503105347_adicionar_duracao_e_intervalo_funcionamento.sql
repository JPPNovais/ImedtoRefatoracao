-- Adiciona configuração de duração padrão da consulta e intervalo entre consultas
-- na tabela estabelecimentos. Usado pelo handler de disponibilidade para gerar slots
-- e pelo aggregate Estabelecimento para validar agendamentos.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260503105347_AdicionarDuracaoEIntervaloFuncionamento') THEN
    ALTER TABLE public.estabelecimentos ADD duracao_consulta_padrao_minutos integer NOT NULL DEFAULT 30;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260503105347_AdicionarDuracaoEIntervaloFuncionamento') THEN
    ALTER TABLE public.estabelecimentos ADD intervalo_entre_consultas_minutos integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260503105347_AdicionarDuracaoEIntervaloFuncionamento') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260503105347_AdicionarDuracaoEIntervaloFuncionamento', '10.0.0');
    END IF;
END $EF$;

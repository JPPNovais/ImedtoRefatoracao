DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260618133906_AdicionarCanalWhatsappLembrete') THEN
    ALTER TABLE public.configuracoes_automacao
        ADD COLUMN IF NOT EXISTS lembretes_whatsapp_habilitados boolean NOT NULL DEFAULT false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260618133906_AdicionarCanalWhatsappLembrete') THEN
    ALTER TABLE public.pacientes
        ADD COLUMN IF NOT EXISTS whatsapp_lembrete_opt_in boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS whatsapp_lembrete_opt_in_em timestamp with time zone NULL,
        ADD COLUMN IF NOT EXISTS whatsapp_lembrete_opt_in_por_usuario_id uuid NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260618133906_AdicionarCanalWhatsappLembrete') THEN
    ALTER TABLE public.agendamentos
        ADD COLUMN IF NOT EXISTS lembrete_por_whatsapp_enviado boolean NOT NULL DEFAULT false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260618133906_AdicionarCanalWhatsappLembrete') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260618133906_AdicionarCanalWhatsappLembrete', '10.0.0');
    END IF;
END $EF$;


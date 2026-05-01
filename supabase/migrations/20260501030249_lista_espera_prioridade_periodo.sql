-- ============================================================
-- Lista de espera — adiciona prioridade + preferência de período.
--
-- Colunas:
--   - prioridade           varchar(20)  default 'Rotina'    (Rotina | Prioritario | Urgente)
--   - preferencia_periodo  varchar(20)  default 'Qualquer'  (Qualquer | Manha | Tarde)
--
-- Idempotente: skipa se já aplicada.
-- ============================================================

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260501030249_ListaEsperaPrioridadePeriodo') THEN
    ALTER TABLE public.lista_espera_agendamento
        ADD COLUMN IF NOT EXISTS prioridade character varying(20) NOT NULL DEFAULT 'Rotina',
        ADD COLUMN IF NOT EXISTS preferencia_periodo character varying(20) NOT NULL DEFAULT 'Qualquer';

    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260501030249_ListaEsperaPrioridadePeriodo', '10.0.0');
    END IF;
END $EF$;

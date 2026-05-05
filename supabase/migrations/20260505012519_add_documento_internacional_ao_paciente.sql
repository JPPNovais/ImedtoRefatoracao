-- Adiciona suporte a documento internacional (passaporte/RNE/etc.) em pacientes.
-- Aggregate Paciente passa a aceitar CPF (com validacao de digito verificador)
-- OU DocumentoInternacional, nunca os dois ao mesmo tempo. Mesma regra do CPF
-- no escopo do estabelecimento (unique condicional).

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260505012519_AddDocumentoInternacionalAoPaciente') THEN
    ALTER TABLE public.pacientes ADD documento_internacional character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260505012519_AddDocumentoInternacionalAoPaciente') THEN
    CREATE UNIQUE INDEX uq_pacientes_estabelecimento_doc_internacional ON public.pacientes (estabelecimento_id, documento_internacional) WHERE documento_internacional IS NOT NULL AND deletado_em IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260505012519_AddDocumentoInternacionalAoPaciente') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260505012519_AddDocumentoInternacionalAoPaciente', '10.0.0');
    END IF;
END $EF$;

-- Adiciona tipo_acesso ao modelo de permissão: 'Profissional' ou 'Recepcionista'.
-- Linhas existentes recebem 'Profissional' como default.
-- EF migration: 20260418230839_AddTipoAcessoToModeloPermissao

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418230839_AddTipoAcessoToModeloPermissao') THEN
    ALTER TABLE public.modelo_permissao_estabelecimento
        ADD COLUMN tipo_acesso character varying(20) NOT NULL DEFAULT 'Profissional';

    COMMENT ON COLUMN public.modelo_permissao_estabelecimento.tipo_acesso IS
        'Papel base do modelo: Profissional (acesso clínico completo) ou Recepcionista (agenda + pacientes, sem prontuário).';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418230839_AddTipoAcessoToModeloPermissao') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260418230839_AddTipoAcessoToModeloPermissao', '10.0.0');
    END IF;
END $EF$;

-- Migration: ModelosPermissaoPadraoSistema
-- Torna estabelecimento_id nullable em modelo_permissao_estabelecimento.
-- Registros com estabelecimento_id IS NULL representam os modelos padrão globais do sistema.
-- Gerado via: dotnet ef migrations script ... --idempotent
-- BEGIN/COMMIT removidos — a pipeline (migrate.sh) gerencia a transação.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604121304_ModelosPermissaoPadraoSistema') THEN
    ALTER TABLE public.modelo_permissao_estabelecimento ALTER COLUMN estabelecimento_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260604121304_ModelosPermissaoPadraoSistema') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260604121304_ModelosPermissaoPadraoSistema', '10.0.0');
    END IF;
END $EF$;

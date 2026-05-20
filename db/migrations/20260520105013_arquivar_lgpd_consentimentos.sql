-- ─────────────────────────────────────────────────────────────────────────────
-- Fase 5 da feature Termos de Consentimento — arquiva o módulo legado
-- `lgpd_consentimentos` (consentimentos por usuário, desacoplados de paciente).
-- A nova fonte de verdade dos aceites é `termo_emitido`, por paciente.
--
-- Não dropamos a tabela imediatamente: ela é renomeada para
-- `lgpd_consentimentos_arquivo` para permitir rollback em até 30 dias.
-- Idempotente — não falha se já foi aplicada nem se a tabela não existir.
-- ─────────────────────────────────────────────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260520105013_ArquivarLgpdConsentimentos') THEN
    ALTER TABLE IF EXISTS public.lgpd_consentimentos RENAME TO lgpd_consentimentos_arquivo;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260520105013_ArquivarLgpdConsentimentos') THEN
    COMMENT ON TABLE public.lgpd_consentimentos_arquivo IS 'Arquivada em 2026-05-20 — módulo legado removido na Fase 5 de Termos de Consentimento. Tabela mantida pra rollback. Apagar via DROP em 30 dias.';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260520105013_ArquivarLgpdConsentimentos') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260520105013_ArquivarLgpdConsentimentos', '10.0.0');
    END IF;
END $EF$;

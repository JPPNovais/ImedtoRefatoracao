-- Migration: Assinatura Digital de Receitas — MVP ICP-Brasil (BirdID)
-- Briefing: 2026-06-01_001_assinatura-digital-receitas
--
-- Alterações:
--   1. Novas colunas em receitas: pdf_assinado_s3_key, assinatura_solicitada_em, assinada_em
--   2. Nova tabela: assinatura_certificados (vínculo médico ↔ certificado em nuvem)
--   3. Nova tabela: assinatura_audit_log (audit append-only, sem PII de paciente)
--
-- NOTA: Os índices CONCURRENTLY (em tabelas existentes) ficam em arquivo separado:
--   20260601120001_indices_assinatura_digital_concurrently.sql

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260601120000_CriarAssinaturaDigital') THEN

    -- 1. Novas colunas em receitas (tabela existente, baixo volume de escrita)
    ALTER TABLE public.receitas
        ADD COLUMN IF NOT EXISTS pdf_assinado_s3_key text NULL;

    ALTER TABLE public.receitas
        ADD COLUMN IF NOT EXISTS assinatura_solicitada_em timestamptz NULL;

    ALTER TABLE public.receitas
        ADD COLUMN IF NOT EXISTS assinada_em timestamptz NULL;

    -- 2. Tabela de certificados em nuvem por médico
    --    medico_id é uuid (FK lógica para public.usuarios.id — uuid PK)
    --    refresh_token é cifrado com IDataProtectionProvider antes de persistir
    CREATE TABLE IF NOT EXISTS public.assinatura_certificados (
        id              uuid            NOT NULL,
        medico_id       uuid            NOT NULL,
        provedor        text            NOT NULL,
        refresh_token   text            NOT NULL,
        expira_em       timestamptz     NULL,
        criado_em       timestamptz     NOT NULL,
        CONSTRAINT "PK_assinatura_certificados" PRIMARY KEY (id),
        CONSTRAINT uq_assinatura_certificados_medico_provedor UNIQUE (medico_id, provedor)
    );

    -- 3. Tabela de auditoria de assinatura — append-only, sem PII de paciente
    --    receita_id: sem FK física — log permanece mesmo após soft-delete da receita
    --    usuario_id: uuid (quem disparou — médico ou sistema para job de expiração)
    --    estabelecimento_id: bigint — multi-tenant para relatórios de auditoria
    CREATE TABLE IF NOT EXISTS public.assinatura_audit_log (
        id                  bigint          GENERATED ALWAYS AS IDENTITY NOT NULL,
        receita_id          bigint          NOT NULL,
        estabelecimento_id  bigint          NOT NULL,
        usuario_id          uuid            NOT NULL,
        acao                text            NOT NULL,
        status_anterior     text            NULL,
        status_novo         text            NULL,
        criado_em           timestamptz     NOT NULL,
        CONSTRAINT "PK_assinatura_audit_log" PRIMARY KEY (id)
    );

    -- 4. Índices transacionais (tabelas novas — sem CONCURRENTLY necessário)
    CREATE INDEX IF NOT EXISTS ix_assinatura_certificados_medico
        ON public.assinatura_certificados (medico_id);

    CREATE INDEX IF NOT EXISTS ix_assinatura_audit_log_receita
        ON public.assinatura_audit_log (receita_id);

    CREATE INDEX IF NOT EXISTS ix_assinatura_audit_log_estab_criado
        ON public.assinatura_audit_log (estabelecimento_id, criado_em DESC);

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260601120000_CriarAssinaturaDigital') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260601120000_CriarAssinaturaDigital', '10.0.0');
    END IF;
END $EF$;

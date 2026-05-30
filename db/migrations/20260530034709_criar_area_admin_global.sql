-- Migration: Criar Área Admin Global (MVP)
-- Timestamp: 20260530034709
-- Tabelas: imedto_admins, imedto_admin_refresh_tokens, imedto_admin_audit_log,
--          imedto_planos, imedto_assinaturas, imedto_config
-- Todas as tabelas são GLOBAIS (sem estabelecimento_id) — área de super-admin do SaaS.
-- Notas de nomenclatura:
--   - imedto_planos != planos (tabela legado com bigint IDs — domínio de assinatura do cliente)
--   - imedto_assinaturas != assinaturas (tabela legado 1:1 por tenant — esta é histórico admin)
-- Aplicado via: deploy/scripts/migrate.sh (psql). NÃO rodar dotnet ef database update.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE TABLE public.imedto_admins (
        id uuid NOT NULL,
        email citext NOT NULL,
        nome text NOT NULL,
        senha_hash text NOT NULL,
        ativo boolean NOT NULL DEFAULT TRUE,
        force_password_reset boolean NOT NULL DEFAULT FALSE,
        criado_em timestamp with time zone NOT NULL,
        atualizado_em timestamp with time zone,
        ultimo_login_em timestamp with time zone,
        criado_por_admin_id uuid,
        desativado_em timestamp with time zone,
        desativado_por_admin_id uuid,
        CONSTRAINT "PK_imedto_admins" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_admins_imedto_admins_criado_por_admin_id" FOREIGN KEY (criado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL,
        CONSTRAINT "FK_imedto_admins_imedto_admins_desativado_por_admin_id" FOREIGN KEY (desativado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE TABLE public.imedto_admin_audit_log (
        id uuid NOT NULL,
        admin_id uuid,
        acao text NOT NULL,
        recurso_tipo text,
        recurso_id text,
        tenant_afetado_id bigint,
        motivo text,
        ip text,
        user_agent text,
        payload_json jsonb,
        criado_em timestamp with time zone NOT NULL,
        CONSTRAINT "PK_imedto_admin_audit_log" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_admin_audit_log_imedto_admins_admin_id" FOREIGN KEY (admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE TABLE public.imedto_admin_refresh_tokens (
        id uuid NOT NULL,
        admin_id uuid NOT NULL,
        token_hash text NOT NULL,
        expira_em timestamp with time zone NOT NULL,
        revogado_em timestamp with time zone,
        criado_em timestamp with time zone NOT NULL,
        ip_origem text,
        user_agent text,
        CONSTRAINT "PK_imedto_admin_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_admin_refresh_tokens_imedto_admins_admin_id" FOREIGN KEY (admin_id) REFERENCES public.imedto_admins (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE TABLE public.imedto_config (
        chave text NOT NULL,
        valor jsonb NOT NULL,
        descricao text,
        atualizado_em timestamp with time zone NOT NULL,
        atualizado_por_admin_id uuid,
        CONSTRAINT "PK_imedto_config" PRIMARY KEY (chave),
        CONSTRAINT "FK_imedto_config_imedto_admins_atualizado_por_admin_id" FOREIGN KEY (atualizado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE TABLE public.imedto_planos (
        id uuid NOT NULL,
        nome text NOT NULL,
        descricao_curta text,
        preco_mensal_centavos integer,
        gratuito boolean NOT NULL DEFAULT FALSE,
        ativo boolean NOT NULL DEFAULT TRUE,
        limites_json jsonb NOT NULL DEFAULT ('{}'::jsonb),
        criado_em timestamp with time zone NOT NULL,
        atualizado_em timestamp with time zone,
        criado_por_admin_id uuid,
        CONSTRAINT "PK_imedto_planos" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_planos_imedto_admins_criado_por_admin_id" FOREIGN KEY (criado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE TABLE public.imedto_assinaturas (
        id uuid NOT NULL,
        estabelecimento_id bigint NOT NULL,
        plano_id uuid NOT NULL,
        iniciada_em timestamp with time zone NOT NULL,
        fim_em timestamp with time zone,
        gratuita boolean NOT NULL DEFAULT FALSE,
        motivo text,
        criada_em timestamp with time zone NOT NULL,
        criada_por_admin_id uuid,
        CONSTRAINT "PK_imedto_assinaturas" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_assinaturas_estabelecimentos_estabelecimento_id" FOREIGN KEY (estabelecimento_id) REFERENCES public.estabelecimentos (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_imedto_assinaturas_imedto_admins_criada_por_admin_id" FOREIGN KEY (criada_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL,
        CONSTRAINT "FK_imedto_assinaturas_imedto_planos_plano_id" FOREIGN KEY (plano_id) REFERENCES public.imedto_planos (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_admin_audit_log_acao_criado ON public.imedto_admin_audit_log (acao, criado_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_admin_audit_log_admin_criado ON public.imedto_admin_audit_log (admin_id, criado_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_admin_audit_log_criado_em ON public.imedto_admin_audit_log (criado_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_admin_audit_log_tenant_criado ON public.imedto_admin_audit_log (tenant_afetado_id, criado_em) WHERE tenant_afetado_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_admin_refresh_tokens_admin_expira ON public.imedto_admin_refresh_tokens (admin_id, expira_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE UNIQUE INDEX uq_imedto_admin_refresh_tokens_hash ON public.imedto_admin_refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_admins_ativo ON public.imedto_admins (ativo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX "IX_imedto_admins_criado_por_admin_id" ON public.imedto_admins (criado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX "IX_imedto_admins_desativado_por_admin_id" ON public.imedto_admins (desativado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE UNIQUE INDEX uq_imedto_admins_email ON public.imedto_admins (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX "IX_imedto_assinaturas_criada_por_admin_id" ON public.imedto_assinaturas (criada_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_assinaturas_estabelecimento_fim ON public.imedto_assinaturas (estabelecimento_id, fim_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_assinaturas_plano ON public.imedto_assinaturas (plano_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX "IX_imedto_config_atualizado_por_admin_id" ON public.imedto_config (atualizado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX ix_imedto_planos_ativo ON public.imedto_planos (ativo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE INDEX "IX_imedto_planos_criado_por_admin_id" ON public.imedto_planos (criado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    CREATE UNIQUE INDEX uq_imedto_planos_nome ON public.imedto_planos (nome);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530034709_CriarAreaAdminGlobal') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260530034709_CriarAreaAdminGlobal', '10.0.0');
    END IF;
END $EF$;

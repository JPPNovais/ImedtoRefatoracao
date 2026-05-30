-- Migration: Criar Catálogos Globais Wave 2
-- Timestamp: 20260530131404
-- Tabelas novas: imedto_modelo_prontuario_global, imedto_variavel_pool_global,
--                imedto_regiao_anatomica_global
-- Colunas novas em imedto_config: tipo (text), secao (text)
--
-- Todas as 3 tabelas são GLOBAIS (sem estabelecimento_id) — catálogos do sistema admin.
-- Regiões anatômicas: cenário 2 — nova tabela global de alto nível coexiste com
--   regioes_anatomicas_catalogo (hierárquico, para exame físico). Propósitos distintos.
-- Índices UNIQUE expressionais (LOWER(nome)) estão em arquivo separado _indices.sql
--   porque exigem CREATE INDEX CONCURRENTLY (não pode rodar em transação).
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.

-- ── imedto_config: adicionar colunas tipo e secao ────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    ALTER TABLE public.imedto_config ADD secao text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    ALTER TABLE public.imedto_config ADD tipo text NOT NULL DEFAULT 'texto';
    END IF;
END $EF$;

-- ── imedto_modelo_prontuario_global ──────────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE TABLE public.imedto_modelo_prontuario_global (
        id uuid NOT NULL,
        nome text NOT NULL,
        descricao text,
        conteudo_json jsonb NOT NULL DEFAULT ('{}'::jsonb),
        ativo boolean NOT NULL DEFAULT TRUE,
        criado_em timestamp with time zone NOT NULL,
        atualizado_em timestamp with time zone,
        criado_por_admin_id uuid,
        atualizado_por_admin_id uuid,
        CONSTRAINT "PK_imedto_modelo_prontuario_global" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_modelo_prontuario_global_imedto_admins_atualizado_po~" FOREIGN KEY (atualizado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL,
        CONSTRAINT "FK_imedto_modelo_prontuario_global_imedto_admins_criado_por_ad~" FOREIGN KEY (criado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

-- ── imedto_regiao_anatomica_global ────────────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE TABLE public.imedto_regiao_anatomica_global (
        id uuid NOT NULL,
        nome text NOT NULL,
        sinonimos text[],
        sistema_corporal text,
        ativo boolean NOT NULL DEFAULT TRUE,
        criado_em timestamp with time zone NOT NULL,
        atualizado_em timestamp with time zone,
        CONSTRAINT "PK_imedto_regiao_anatomica_global" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

-- ── imedto_variavel_pool_global ───────────────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE TABLE public.imedto_variavel_pool_global (
        id uuid NOT NULL,
        nome text NOT NULL,
        tipo text NOT NULL,
        valores_json jsonb DEFAULT ('[]'::jsonb),
        descricao text,
        ativo boolean NOT NULL DEFAULT TRUE,
        criado_em timestamp with time zone NOT NULL,
        atualizado_em timestamp with time zone,
        criado_por_admin_id uuid,
        atualizado_por_admin_id uuid,
        CONSTRAINT "PK_imedto_variavel_pool_global" PRIMARY KEY (id),
        CONSTRAINT "FK_imedto_variavel_pool_global_imedto_admins_atualizado_por_ad~" FOREIGN KEY (atualizado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL,
        CONSTRAINT "FK_imedto_variavel_pool_global_imedto_admins_criado_por_admin_~" FOREIGN KEY (criado_por_admin_id) REFERENCES public.imedto_admins (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

-- ── Índices B-tree (podem rodar em transação) ─────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX ix_imedto_config_secao_chave ON public.imedto_config (secao, chave);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX ix_imedto_modelo_prontuario_global_ativo_nome ON public.imedto_modelo_prontuario_global (ativo, nome);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX "IX_imedto_modelo_prontuario_global_atualizado_por_admin_id" ON public.imedto_modelo_prontuario_global (atualizado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX "IX_imedto_modelo_prontuario_global_criado_por_admin_id" ON public.imedto_modelo_prontuario_global (criado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX ix_imedto_regiao_anatomica_global_ativo_sistema_nome ON public.imedto_regiao_anatomica_global (ativo, sistema_corporal, nome);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX ix_imedto_variavel_pool_global_ativo_tipo_nome ON public.imedto_variavel_pool_global (ativo, tipo, nome);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX "IX_imedto_variavel_pool_global_atualizado_por_admin_id" ON public.imedto_variavel_pool_global (atualizado_por_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    CREATE INDEX "IX_imedto_variavel_pool_global_criado_por_admin_id" ON public.imedto_variavel_pool_global (criado_por_admin_id);
    END IF;
END $EF$;

-- ── Registrar migration ───────────────────────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260530131404_CriarCatalogosGlobaisWave2') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260530131404_CriarCatalogosGlobaisWave2', '10.0.0');
    END IF;
END $EF$;

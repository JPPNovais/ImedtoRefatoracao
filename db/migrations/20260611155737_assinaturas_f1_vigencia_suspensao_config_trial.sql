-- Migration: 20260611155737_assinaturas_f1_vigencia_suspensao_config_trial
-- Briefing 2026-06-11_003 — F1: Schema novo + porta para gateway futuro.
--
-- Elege imedto_assinaturas/imedto_planos como fonte única de verdade.
-- Estado efetivo é derivado (expira_em + suspensa_em), NÃO enum setável.
--
-- Decisão de coluna (§8 do briefing): imedto_assinaturas já tem `iniciada_em`
-- com a mesma semântica de início de vigência. NÃO foi criada coluna duplicada.
-- As colunas de vigência novas são: expira_em, suspensa_em.
-- As colunas dormentes de pagamento: origem, referencia_externa, status_cobranca.
--
-- BEGIN/COMMIT removidos — a pipeline (migrate.sh) gerencia a transação externamente.

-- ============================================================
-- 1. ALTER imedto_planos — adicionar features_json
-- ============================================================
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    ALTER TABLE public.imedto_planos
        ADD COLUMN IF NOT EXISTS features_json jsonb NOT NULL DEFAULT '{}';
    END IF;
END $EF$;

-- ============================================================
-- 2. ALTER imedto_assinaturas — colunas de vigência
-- ============================================================
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    ALTER TABLE public.imedto_assinaturas
        ADD COLUMN IF NOT EXISTS expira_em     timestamp with time zone         NULL,
        ADD COLUMN IF NOT EXISTS suspensa_em   timestamp with time zone         NULL,
        ADD COLUMN IF NOT EXISTS origem        text            NOT NULL DEFAULT 'admin_manual',
        ADD COLUMN IF NOT EXISTS referencia_externa text       NULL,
        ADD COLUMN IF NOT EXISTS status_cobranca   text        NOT NULL DEFAULT 'nao_aplicavel';
    END IF;
END $EF$;

-- ============================================================
-- 3. Índices imedto_assinaturas
-- ============================================================

-- Índice parcial de vigência: garante busca eficiente da assinatura vigente por tenant.
-- UNIQUE não aplicado aqui: o domínio garante unicidade via transação (INSERT + FecharVigencia).
-- Um UNIQUE parcial quebraria o backfill se houver estabelecimento sem vigência prévia.
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    CREATE INDEX IF NOT EXISTS ix_imedto_assinaturas_vigente
        ON public.imedto_assinaturas (estabelecimento_id)
        WHERE fim_em IS NULL;
    END IF;
END $EF$;

-- Índice para varreduras de expiração (job de expirar trials vencidos — F5/futuro).
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    CREATE INDEX IF NOT EXISTS ix_imedto_assinaturas_expira_em
        ON public.imedto_assinaturas (expira_em);
    END IF;
END $EF$;

-- ============================================================
-- 4. Criar tabela imedto_config_trial (singleton — 1 linha)
-- ============================================================
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    CREATE TABLE IF NOT EXISTS public.imedto_config_trial (
        id                      uuid                        NOT NULL,
        plano_trial_id          uuid                        NOT NULL,
        duracao_trial_dias      integer                     NOT NULL DEFAULT 14,
        trial_habilitado        boolean                     NOT NULL DEFAULT true,
        atualizado_em           timestamp with time zone    NOT NULL,
        atualizado_por_usuario_id uuid                      NULL,
        CONSTRAINT "PK_imedto_config_trial" PRIMARY KEY (id),
        CONSTRAINT fk_imedto_config_trial_plano
            FOREIGN KEY (plano_trial_id)
            REFERENCES public.imedto_planos (id)
            ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    CREATE INDEX IF NOT EXISTS ix_imedto_config_trial_plano_trial_id
        ON public.imedto_config_trial (plano_trial_id);
    END IF;
END $EF$;

-- ============================================================
-- 5. Seed idempotente — Gratuidade Vitalícia + imedto_config_trial
-- ============================================================

-- 5a. Garantir "Gratuidade Vitalícia" (UUID fixo) com todas as features habilitadas e limites ilimitados.
-- features_json: as 8 chaves definidas em R5 do briefing, todas true.
-- limites_json: {} (ilimitado — ausência = ilimitado por definição do domínio).
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    INSERT INTO public.imedto_planos (
        id,
        nome,
        descricao_curta,
        preco_mensal_centavos,
        gratuito,
        ativo,
        limites_json,
        features_json,
        criado_em,
        atualizado_em,
        criado_por_admin_id
    )
    VALUES (
        '00000000-0000-0000-0000-000000000001',
        'Gratuidade Vitalícia',
        'Acesso completo sem cobrança, concedido manualmente pelo admin.',
        NULL,
        true,
        true,
        '{}'::jsonb,
        '{
            "receitas": true,
            "exame_fisico": true,
            "procedimentos_cirurgicos": true,
            "orcamento_completo": true,
            "ia": true,
            "relatorios_avancados": true,
            "automacoes_ilimitadas": true,
            "anexos_ilimitados": true
        }'::jsonb,
        now(),
        NULL,
        NULL
    )
    ON CONFLICT (id) DO UPDATE
        SET features_json = EXCLUDED.features_json,
            atualizado_em  = now();
    -- ON CONFLICT atualiza features_json para garantir que o plano Gratuidade Vitalícia
    -- fique com todas as features habilitadas mesmo em banco existente (sem features_json antes da F1).
    END IF;
END $EF$;

-- 5b. Semear imedto_config_trial (singleton com id fixo) apontando para Gratuidade Vitalícia.
-- id fixo: 10000000-0000-0000-0000-000000000001 (definido em ImedtoConfigTrial.IdFixo).
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    INSERT INTO public.imedto_config_trial (
        id,
        plano_trial_id,
        duracao_trial_dias,
        trial_habilitado,
        atualizado_em,
        atualizado_por_usuario_id
    )
    VALUES (
        '10000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-000000000001',
        14,
        true,
        now(),
        NULL
    )
    ON CONFLICT (id) DO NOTHING; -- Singleton: não sobrescrever se admin já configurou
    END IF;
END $EF$;

-- ============================================================
-- 6. Registrar migration no histórico EF Core
-- ============================================================
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260611155737_AssinaturasF1VigenciaSuspensaoConfigTrial', '10.0.0');
    END IF;
END $EF$;

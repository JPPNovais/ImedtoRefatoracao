-- Migration: Autenticação em dois fatores (2FA) via TOTP — RFC 6238
-- Briefing: 2026-06-10_006_2fa-totp
--
-- Alterações:
--   1. Nova coluna em estabelecimentos: exigir_dono_2fa (toggle por estabelecimento)
--   2. Nova tabela: usuario_2fa (estado de 2FA 1:1 por usuário — segredo TOTP cifrado)
--   3. Nova tabela: usuario_2fa_codigo_recuperacao (10 códigos one-time hasheados por usuário)
--   4. Nova tabela: usuario_seguranca_audit (audit append-only — ativação/desativação/recuperação)
--
-- Decisões de modelagem:
--   - usuario_2fa.usuario_id é PK (relação 1:1 — sem coluna id separada).
--   - usuario_2fa.segredo_cifrado: ciphertext de IDataProtector purpose "auth.totp.secret"
--     (mesmo padrão de assinatura_certificados.refresh_token).
--   - usuario_2fa.status: enum como text ("Pendente" | "Ativo") via HasConversion<string>.
--   - usuario_2fa_codigo_recuperacao.codigo_hash: hash via hasher de senha do projeto
--     (BCrypt + HMAC-SHA256) — nunca em claro.
--   - usuario_seguranca_audit: append-only, sem PII (sem e-mail/CPF/telefone/segredo).
--   - exigir_dono_2fa: boolean DEFAULT false em estabelecimentos — sem tabela de settings separada.
--   - Desafio de login efêmero (R3/R5): NOT em tabela — token cifrado com IDataProtector
--     purpose "auth.totp.challenge" (sem persistência → sem limpeza de lixo).
--
-- Tabelas sem multi-tenant (justificativa: 2FA é global da conta, não por tenant —
--   R11/briefing §5. O toggle exigir_dono_2fa é por estabelecimento via coluna em estabelecimentos).

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    ALTER TABLE public.estabelecimentos ADD exigir_dono_2fa boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    CREATE TABLE IF NOT EXISTS public.usuario_2fa (
        usuario_id      uuid                        NOT NULL,
        segredo_cifrado text                        NOT NULL,
        status          character varying(20)       NOT NULL,
        ativado_em      timestamp with time zone    NULL,
        criado_em       timestamp with time zone    NOT NULL,
        atualizado_em   timestamp with time zone    NULL,
        CONSTRAINT "PK_usuario_2fa" PRIMARY KEY (usuario_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    CREATE TABLE IF NOT EXISTS public.usuario_2fa_codigo_recuperacao (
        id          bigint GENERATED ALWAYS AS IDENTITY,
        usuario_id  uuid                        NOT NULL,
        codigo_hash text                        NOT NULL,
        usado_em    timestamp with time zone    NULL,
        criado_em   timestamp with time zone    NOT NULL,
        CONSTRAINT "PK_usuario_2fa_codigo_recuperacao" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    CREATE TABLE IF NOT EXISTS public.usuario_seguranca_audit (
        id          bigint GENERATED ALWAYS AS IDENTITY,
        usuario_id  uuid                        NOT NULL,
        acao        text                        NOT NULL,
        ocorrido_em timestamp with time zone    NOT NULL,
        ip_origem   character varying(45)       NULL,
        CONSTRAINT "PK_usuario_seguranca_audit" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

-- Índice em usuario_2fa_codigo_recuperacao(usuario_id):
--   busca de códigos disponíveis no passo 2 do login (tabela nova → sem CONCURRENTLY necessário).
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    CREATE INDEX ix_usuario_2fa_codigo_recuperacao_usuario
        ON public.usuario_2fa_codigo_recuperacao (usuario_id);
    END IF;
END $EF$;

-- Índice em usuario_seguranca_audit(usuario_id, ocorrido_em):
--   relatórios de auditoria por usuário ordenados por data.
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    CREATE INDEX ix_usuario_seguranca_audit_usuario_data
        ON public.usuario_seguranca_audit (usuario_id, ocorrido_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260610160024_Criar2faTotp') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260610160024_Criar2faTotp', '10.0.0');
    END IF;
END $EF$;

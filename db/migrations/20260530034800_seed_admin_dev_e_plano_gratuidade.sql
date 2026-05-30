-- Seed condicional para ambiente Development + plano global de Gratuidade Vitalícia.
--
-- SEED ADMIN DEVELOPMENT (CA44, CA45):
--   Insere admin@imedto.com / 123123 APENAS quando app.environment = 'Development'.
--   A variável app.environment é setada no startup da API via:
--       await connection.ExecuteAsync($"SET app.environment = '{env}'");
--   Em produção, essa variável NÃO é setada com valor 'Development', portanto o bloco
--   DO/IF nunca insere. Se quiser forçar seed em prod, use o CLI:
--       dotnet run --project Services/Imedto.Backend.API -- seed-admin --email X
--   (Esse comando ainda será implementado pelo imedto-developer — ver hand-off.)
--
-- HASH DE SENHA:
--   Algoritmo: HMAC-SHA256(pepper, senha) → base64 → BCrypt(workFactor=12)
--   Pepper: tRFYrxgSccCs3X6IMUrTReJNRNN1ToLQymktIb+vHiQ= (do appsettings.Development.json)
--   Senha: 123123
--   Hash gerado em 2026-05-30: $2a$12$NW6iF2HT/YfaE67FsMLQfu7SvBzKFjaJpAulU3BfFZilwXpmQ9a76
--   ATENÇÃO: hash é válido apenas com o pepper acima. Em prod, o pepper vem do AWS SSM.
--   Se o pepper mudar, gere novo hash via:
--       dotnet run --project HashGeneratorApp --
--
-- PLANO GRATUIDADE VITALÍCIA:
--   Inserido incondicionalmente — é catálogo global necessário para o fluxo de gratuidade (CA35).
--   Sem criado_por_admin_id (null) pois não há admin logado no momento do seed.
--
-- Idempotência: ON CONFLICT DO NOTHING em ambos os inserts.

-- ──────────────────────────────────────────────────────────────────────────────
-- 1. Plano "Gratuidade Vitalícia" (global — todos os ambientes)
-- ──────────────────────────────────────────────────────────────────────────────
INSERT INTO public.imedto_planos (
    id,
    nome,
    descricao_curta,
    preco_mensal_centavos,
    gratuito,
    ativo,
    limites_json,
    criado_em,
    atualizado_em,
    criado_por_admin_id
)
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid,
    'Gratuidade Vitalícia',
    'Acesso irrestrito concedido manualmente pelo admin. Sem cobrança.',
    NULL,       -- sem preço (gratuidade)
    TRUE,       -- gratuito = true
    TRUE,       -- ativo
    '{}'::jsonb,
    NOW(),
    NULL,
    NULL
)
ON CONFLICT (id) DO NOTHING;

-- ──────────────────────────────────────────────────────────────────────────────
-- 2. Admin seed (somente em Development)
-- ──────────────────────────────────────────────────────────────────────────────
DO $$
BEGIN
    IF current_setting('app.environment', true) = 'Development' THEN
        INSERT INTO public.imedto_admins (
            id,
            email,
            nome,
            senha_hash,
            ativo,
            force_password_reset,
            criado_em,
            atualizado_em,
            ultimo_login_em,
            criado_por_admin_id,
            desativado_em,
            desativado_por_admin_id
        )
        VALUES (
            gen_random_uuid(),
            'admin@imedto.com',
            'Admin Imedto',
            '$2a$12$NW6iF2HT/YfaE67FsMLQfu7SvBzKFjaJpAulU3BfFZilwXpmQ9a76',
            TRUE,
            FALSE,
            NOW(),
            NULL,
            NULL,
            NULL,
            NULL,
            NULL
        )
        ON CONFLICT (email) DO NOTHING;

        RAISE NOTICE 'Seed dev: admin@imedto.com inserido (ou já existia).';
    ELSE
        RAISE NOTICE 'Ambiente não é Development — seed de admin ignorado.';
    END IF;
END $$;

-- Cria a tabela public.usuarios — "espelho" do auth.users com dados de domínio.
-- EF migration: 20260418041140_CreateUsuarios
-- Complementos fora do EF: FK para auth.users (ON DELETE CASCADE) e RLS habilitado sem policies
-- (acesso somente via backend — role postgres ignora RLS).

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418041140_CreateUsuarios') THEN
    CREATE TABLE public.usuarios (
        id                  uuid NOT NULL,
        email               character varying(320) NOT NULL,
        nome_completo       character varying(200),
        cpf                 character varying(11),
        telefone            character varying(20),
        status              character varying(20) NOT NULL,
        onboarding_completo boolean NOT NULL,
        criado_em           timestamp with time zone NOT NULL,
        atualizado_em       timestamp with time zone,
        ultimo_acesso_em    timestamp with time zone,
        CONSTRAINT "PK_usuarios" PRIMARY KEY (id)
    );

    CREATE INDEX ix_usuarios_email ON public.usuarios (email);
    CREATE UNIQUE INDEX uq_usuarios_cpf ON public.usuarios (cpf) WHERE cpf IS NOT NULL;

    -- FK para o Supabase Auth: se o usuário for removido lá, remove aqui também.
    ALTER TABLE public.usuarios
        ADD CONSTRAINT fk_usuarios_auth_users
        FOREIGN KEY (id) REFERENCES auth.users(id) ON DELETE CASCADE;

    -- Defense-in-depth: RLS ativo sem policies → acesso via PostgREST/anon/authenticated é negado.
    -- O backend acessa como role postgres (superuser) e ignora RLS.
    ALTER TABLE public.usuarios ENABLE ROW LEVEL SECURITY;

    COMMENT ON TABLE public.usuarios IS
        'Dados de domínio do usuário (espelho de auth.users). Toda regra de negócio fica no backend CQRS; RLS negativo em defense-in-depth.';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418041140_CreateUsuarios') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260418041140_CreateUsuarios', '10.0.0');
    END IF;
END $EF$;

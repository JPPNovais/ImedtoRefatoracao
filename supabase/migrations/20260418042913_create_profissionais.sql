-- Cria a tabela public.profissionais (1:1 com public.usuarios).
-- EF migration: 20260418042913_CreateProfissionais
-- Complementos: FK para public.usuarios (ON DELETE CASCADE) + RLS enabled sem policies.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418042913_CreateProfissionais') THEN
    CREATE TABLE public.profissionais (
        usuario_id      uuid NOT NULL,
        conselho        character varying(20) NOT NULL,
        uf              character(2) NOT NULL,
        numero_registro character varying(30) NOT NULL,
        especialidade   character varying(200),
        bio             character varying(2000),
        criado_em       timestamp with time zone NOT NULL,
        atualizado_em   timestamp with time zone,
        CONSTRAINT "PK_profissionais" PRIMARY KEY (usuario_id)
    );

    CREATE UNIQUE INDEX uq_profissionais_conselho_uf_numero
        ON public.profissionais (conselho, uf, numero_registro);

    -- Se o usuário for deletado, o cadastro profissional também vai (relação 1:1 forte).
    ALTER TABLE public.profissionais
        ADD CONSTRAINT fk_profissionais_usuario
        FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;

    ALTER TABLE public.profissionais ENABLE ROW LEVEL SECURITY;

    COMMENT ON TABLE public.profissionais IS
        'Dados profissionais (conselho/registro). 1:1 com public.usuarios via usuario_id = PK.';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260418042913_CreateProfissionais') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260418042913_CreateProfissionais', '10.0.0');
    END IF;
END $EF$;

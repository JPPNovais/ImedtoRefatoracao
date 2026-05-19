-- Migration 20260519020628_OrcamentoLocalCirurgiaParidade
-- Paridade do orçamento com o legado: substitui "Internação" (4 tipos) por
-- "Local Cirúrgico" (5 tipos: int_local, int_peridural, int_geral, sem_internacao, ambulatorio).
-- - DROP da tabela orcamento_internacao (vazia em prod).
-- - RENAME coluna tipo_internacao -> tipo_local em orcamento_configuracao_local_cirurgia.
-- - Defensivo: apaga linhas existentes em orcamento_configuracao_local_cirurgia que ainda
--   contenham os enums antigos (Apartamento/Enfermaria/UTI/Ambulatorial) — devem ser recadastradas
--   com os novos tipos via UI (estabelecimentos sem config caem em "Local cirúrgico não configurado").
-- - Adiciona em orcamentos: tipo_local, tempo_local_minutos, valor_local (snapshot embutido),
--   titulo (opcional), agendamento_id (FK SET NULL + index parcial).

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    DROP TABLE IF EXISTS public.orcamento_internacao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    IF EXISTS (
      SELECT 1 FROM information_schema.columns
      WHERE table_schema = 'public'
        AND table_name = 'orcamento_configuracao_local_cirurgia'
        AND column_name = 'tipo_internacao'
    ) THEN
      ALTER TABLE public.orcamento_configuracao_local_cirurgia RENAME COLUMN tipo_internacao TO tipo_local;
    END IF;
    -- Limpa linhas com tipos antigos (não mais aceitos pelo enum novo).
    DELETE FROM public.orcamento_configuracao_local_cirurgia
    WHERE tipo_local IN ('Apartamento','Enfermaria','UTI','Ambulatorial');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    ALTER TABLE public.orcamentos ADD COLUMN IF NOT EXISTS agendamento_id bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    ALTER TABLE public.orcamentos ADD COLUMN IF NOT EXISTS tempo_local_minutos integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    ALTER TABLE public.orcamentos ADD COLUMN IF NOT EXISTS tipo_local character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    ALTER TABLE public.orcamentos ADD COLUMN IF NOT EXISTS titulo character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    ALTER TABLE public.orcamentos ADD COLUMN IF NOT EXISTS valor_local numeric(12,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    CREATE INDEX IF NOT EXISTS ix_orcamento_agendamento
      ON public.orcamentos (agendamento_id)
      WHERE agendamento_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    IF NOT EXISTS (
      SELECT 1 FROM information_schema.table_constraints
      WHERE table_schema = 'public' AND constraint_name = 'fk_orcamento_agendamento'
    ) THEN
      ALTER TABLE public.orcamentos
        ADD CONSTRAINT fk_orcamento_agendamento
        FOREIGN KEY (agendamento_id)
        REFERENCES public.agendamentos (id)
        ON DELETE SET NULL;
    END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260519020628_OrcamentoLocalCirurgiaParidade') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260519020628_OrcamentoLocalCirurgiaParidade', '10.0.0');
    END IF;
END $EF$;

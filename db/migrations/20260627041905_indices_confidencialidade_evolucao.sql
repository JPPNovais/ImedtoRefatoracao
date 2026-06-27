-- Migration: 20260627041905_AdicionarIndicesConfidencialidadeEvolucao
-- Propósito: Registrar no histórico EF que a migration de índices foi aplicada.
-- Os índices em si são criados CONCURRENTLY (fora de transação) pelo arquivo:
--   20260627041905_indices_confidencialidade_evolucao_indices.sql
-- Referência: briefing 2026-06-27_001 (confidencialidade da evolução — leitura autor-ou-dono)

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260627041905_AdicionarIndicesConfidencialidadeEvolucao') THEN

    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE tablename = 'prontuario_evolucoes'
          AND indexname = 'ix_evolucoes_prontuario_autor'
      ) THEN
        NULL;
      END IF;
    END$$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260627041905_AdicionarIndicesConfidencialidadeEvolucao') THEN

    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE tablename = 'prontuario_anexos'
          AND indexname = 'ix_anexos_criado_por_orfao'
      ) THEN
        NULL;
      END IF;
    END$$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260627041905_AdicionarIndicesConfidencialidadeEvolucao') THEN

    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE tablename = 'atestados'
          AND indexname = 'ix_atestados_paciente_profissional'
      ) THEN
        NULL;
      END IF;
    END$$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260627041905_AdicionarIndicesConfidencialidadeEvolucao') THEN

    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE tablename = 'pedidos_exame'
          AND indexname = 'ix_pedidos_exame_paciente_profissional'
      ) THEN
        NULL;
      END IF;
    END$$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260627041905_AdicionarIndicesConfidencialidadeEvolucao') THEN

    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE tablename = 'receitas'
          AND indexname = 'ix_receitas_paciente_profissional'
      ) THEN
        NULL;
      END IF;
    END$$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260627041905_AdicionarIndicesConfidencialidadeEvolucao') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260627041905_AdicionarIndicesConfidencialidadeEvolucao', '10.0.0');
    END IF;
END $EF$;

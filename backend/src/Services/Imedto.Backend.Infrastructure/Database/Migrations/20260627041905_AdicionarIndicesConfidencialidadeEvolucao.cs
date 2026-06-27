using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIndicesConfidencialidadeEvolucao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índices de confidencialidade — gating de leitura autor-ou-dono (briefing 2026-06-27_001).
            // Os CREATE INDEX são CONCURRENTLY e ficam no arquivo SQL separado:
            //   db/migrations/20260627041905_indices_confidencialidade_evolucao.sql
            // Este Up() registra apenas que a migration foi aplicada; os índices em si
            // são criados pelo pipeline via psql (fora de transação).

            // prontuario_evolucoes: (prontuario_id, autor_usuario_id) — timeline gated por autor.
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_indexes
    WHERE tablename = 'prontuario_evolucoes'
      AND indexname = 'ix_evolucoes_prontuario_autor'
  ) THEN
    -- índice criado via CONCURRENTLY no arquivo SQL separado;
    -- se ainda não existir após pipeline, o deploy .sql o cria.
    NULL;
  END IF;
END$$;
");

            // prontuario_anexos: (criado_por_usuario_id) parcial WHERE evolucao_id IS NULL — anexo órfão.
            migrationBuilder.Sql(@"
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
");

            // atestados: (paciente_id, profissional_usuario_id) — gating de documento por autor.
            migrationBuilder.Sql(@"
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
");

            // pedidos_exame: (paciente_id, profissional_usuario_id) — gating de documento por autor.
            migrationBuilder.Sql(@"
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
");

            // receitas: (paciente_id, profissional_usuario_id) — gating de documento por autor.
            migrationBuilder.Sql(@"
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
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // DROP INDEX IF EXISTS para os índices criados CONCURRENTLY (não revertíveis em transação).
            // Em rollback manual, rodar fora de transação:
            //   DROP INDEX CONCURRENTLY IF EXISTS ix_evolucoes_prontuario_autor;
            //   DROP INDEX CONCURRENTLY IF EXISTS ix_anexos_criado_por_orfao;
            //   DROP INDEX CONCURRENTLY IF EXISTS ix_atestados_paciente_profissional;
            //   DROP INDEX CONCURRENTLY IF EXISTS ix_pedidos_exame_paciente_profissional;
            //   DROP INDEX CONCURRENTLY IF EXISTS ix_receitas_paciente_profissional;
        }
    }
}

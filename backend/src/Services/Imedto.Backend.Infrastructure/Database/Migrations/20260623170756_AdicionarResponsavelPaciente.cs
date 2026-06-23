using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarResponsavelPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Colunas de responsável do paciente (briefing 2026-06-23_002).
            // Todas nullable — obrigatoriedade nome+parentesco para menor é regra de domínio.
            // LGPD: PII de terceiro — só expor em PacienteDto (detalhe), nunca em lista/busca rápida.
            // Idempotente via ADD COLUMN IF NOT EXISTS (gotcha: QA aplica schema fora do EFMigrationsHistory).
            migrationBuilder.Sql("""
                ALTER TABLE public.pacientes
                    ADD COLUMN IF NOT EXISTS responsavel_nome        character varying(200) NULL,
                    ADD COLUMN IF NOT EXISTS responsavel_parentesco  character varying(40)  NULL,
                    ADD COLUMN IF NOT EXISTS responsavel_telefone    character varying(20)  NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.pacientes
                    DROP COLUMN IF EXISTS responsavel_nome,
                    DROP COLUMN IF EXISTS responsavel_parentesco,
                    DROP COLUMN IF EXISTS responsavel_telefone;
                """);
        }
    }
}

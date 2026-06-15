using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMotivoFalhaJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usando SQL raw idempotente (ADD COLUMN IF NOT EXISTS) para evitar falha
            // caso o schema tenha sido aplicado fora da pipeline (gotcha documentado).
            migrationBuilder.Sql(
                "ALTER TABLE public.migracao_jobs ADD COLUMN IF NOT EXISTS motivo_falha text NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE public.migracao_jobs ADD COLUMN IF NOT EXISTS status_antes_falha text NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE public.migracao_jobs DROP COLUMN IF EXISTS status_antes_falha;");

            migrationBuilder.Sql(
                "ALTER TABLE public.migracao_jobs DROP COLUMN IF EXISTS motivo_falha;");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarOndaMigracaoJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nota: o RenameIndex gerado originalmente foi removido porque o índice
            // ix_migracao_jobs_template_origem_id já existe com nome correto (minúsculo)
            // no banco — o EF detectou a divergência no snapshot mas o índice real não
            // tinha o nome com maiúsculas. Manter o RenameIndex causaria falha em produção.

            migrationBuilder.AddColumn<string>(
                name: "onda",
                schema: "public",
                table: "migracao_jobs",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_migracao_jobs_estab_onda_status",
                schema: "public",
                table: "migracao_jobs",
                columns: new[] { "estabelecimento_id", "onda", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_migracao_jobs_estab_onda_status",
                schema: "public",
                table: "migracao_jobs");

            migrationBuilder.DropColumn(
                name: "onda",
                schema: "public",
                table: "migracao_jobs");
        }
    }
}

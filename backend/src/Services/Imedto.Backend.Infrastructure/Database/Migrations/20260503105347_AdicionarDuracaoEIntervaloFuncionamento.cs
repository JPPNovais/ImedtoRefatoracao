using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDuracaoEIntervaloFuncionamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duracao_consulta_padrao_minutos",
                schema: "public",
                table: "estabelecimentos",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "intervalo_entre_consultas_minutos",
                schema: "public",
                table: "estabelecimentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "duracao_consulta_padrao_minutos",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.DropColumn(
                name: "intervalo_entre_consultas_minutos",
                schema: "public",
                table: "estabelecimentos");
        }
    }
}

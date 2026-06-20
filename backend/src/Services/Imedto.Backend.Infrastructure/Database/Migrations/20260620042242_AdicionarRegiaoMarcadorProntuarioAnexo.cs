using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRegiaoMarcadorProntuarioAnexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "marcador",
                schema: "public",
                table: "prontuario_anexos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "regiao_anatomica",
                schema: "public",
                table: "prontuario_anexos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "marcador",
                schema: "public",
                table: "prontuario_anexos");

            migrationBuilder.DropColumn(
                name: "regiao_anatomica",
                schema: "public",
                table: "prontuario_anexos");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVisualEActionLevelEmModelosEPacientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "alertas",
                schema: "public",
                table: "pacientes",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string[]>(
                name: "tags",
                schema: "public",
                table: "pacientes",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string>(
                name: "cor",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descricao",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icone",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alertas",
                schema: "public",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "tags",
                schema: "public",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "cor",
                schema: "public",
                table: "modelo_permissao_estabelecimento");

            migrationBuilder.DropColumn(
                name: "descricao",
                schema: "public",
                table: "modelo_permissao_estabelecimento");

            migrationBuilder.DropColumn(
                name: "icone",
                schema: "public",
                table: "modelo_permissao_estabelecimento");
        }
    }
}

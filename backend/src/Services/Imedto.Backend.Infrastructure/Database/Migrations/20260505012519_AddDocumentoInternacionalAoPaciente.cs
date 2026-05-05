using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentoInternacionalAoPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "documento_internacional",
                schema: "public",
                table: "pacientes",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_pacientes_estabelecimento_doc_internacional",
                schema: "public",
                table: "pacientes",
                columns: new[] { "estabelecimento_id", "documento_internacional" },
                unique: true,
                filter: "documento_internacional IS NOT NULL AND deletado_em IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_pacientes_estabelecimento_doc_internacional",
                schema: "public",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "documento_internacional",
                schema: "public",
                table: "pacientes");
        }
    }
}

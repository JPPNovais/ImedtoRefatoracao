using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAssinaturaDigitalStatusEmReceitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assinatura_digital_status",
                schema: "public",
                table: "receitas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NaoAssinada");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "assinatura_digital_status",
                schema: "public",
                table: "receitas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarProfissaoConvidadaEmVinculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "profissao_convidada_id",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profissao_convidada_id",
                schema: "public",
                table: "vinculo_profissional_estabelecimento");
        }
    }
}

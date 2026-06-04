using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarUltimoEstabelecimentoIdUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ultimo_estabelecimento_id",
                schema: "public",
                table: "usuarios",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_ultimo_estabelecimento_id",
                schema: "public",
                table: "usuarios",
                column: "ultimo_estabelecimento_id");

            migrationBuilder.AddForeignKey(
                name: "fk_usuarios_ultimo_estabelecimento",
                schema: "public",
                table: "usuarios",
                column: "ultimo_estabelecimento_id",
                principalSchema: "public",
                principalTable: "estabelecimentos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_ultimo_estabelecimento",
                schema: "public",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_ultimo_estabelecimento_id",
                schema: "public",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ultimo_estabelecimento_id",
                schema: "public",
                table: "usuarios");
        }
    }
}

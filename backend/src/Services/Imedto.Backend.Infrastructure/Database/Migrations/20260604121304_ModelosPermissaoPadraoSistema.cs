using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ModelosPermissaoPadraoSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "estabelecimento_id",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "estabelecimento_id",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}

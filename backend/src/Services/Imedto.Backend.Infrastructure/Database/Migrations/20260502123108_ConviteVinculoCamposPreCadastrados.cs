using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConviteVinculoCamposPreCadastrados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "modelo_permissao_id",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "especialidade_convidada",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nome_convidado",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telefone_convidado",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "especialidade_convidada",
                schema: "public",
                table: "vinculo_profissional_estabelecimento");

            migrationBuilder.DropColumn(
                name: "nome_convidado",
                schema: "public",
                table: "vinculo_profissional_estabelecimento");

            migrationBuilder.DropColumn(
                name: "telefone_convidado",
                schema: "public",
                table: "vinculo_profissional_estabelecimento");

            migrationBuilder.AlterColumn<long>(
                name: "modelo_permissao_id",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}

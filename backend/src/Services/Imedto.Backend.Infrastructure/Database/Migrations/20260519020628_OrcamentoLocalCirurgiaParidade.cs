using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class OrcamentoLocalCirurgiaParidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento_internacao",
                schema: "public");

            migrationBuilder.RenameColumn(
                name: "tipo_internacao",
                schema: "public",
                table: "orcamento_configuracao_local_cirurgia",
                newName: "tipo_local");

            migrationBuilder.AddColumn<long>(
                name: "agendamento_id",
                schema: "public",
                table: "orcamentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tempo_local_minutos",
                schema: "public",
                table: "orcamentos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_local",
                schema: "public",
                table: "orcamentos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "titulo",
                schema: "public",
                table: "orcamentos",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_local",
                schema: "public",
                table: "orcamentos",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_agendamento",
                schema: "public",
                table: "orcamentos",
                column: "agendamento_id",
                filter: "agendamento_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_orcamento_agendamento",
                schema: "public",
                table: "orcamentos",
                column: "agendamento_id",
                principalSchema: "public",
                principalTable: "agendamentos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orcamento_agendamento",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropIndex(
                name: "ix_orcamento_agendamento",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "agendamento_id",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "tempo_local_minutos",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "tipo_local",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "titulo",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "valor_local",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.RenameColumn(
                name: "tipo_local",
                schema: "public",
                table: "orcamento_configuracao_local_cirurgia",
                newName: "tipo_internacao");

            migrationBuilder.CreateTable(
                name: "orcamento_internacao",
                schema: "public",
                columns: table => new
                {
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    dias = table.Column<int>(type: "integer", nullable: false),
                    tipo_internacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_internacao", x => x.orcamento_id);
                    table.ForeignKey(
                        name: "fk_orcamento_internacao_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}

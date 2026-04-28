using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarAutomacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "lembrete_por_email_enviado",
                schema: "public",
                table: "agendamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "configuracoes_automacao",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    lembretes_habilitados = table.Column<bool>(type: "boolean", nullable: false),
                    horas_antecedencia_lembrete = table.Column<int>(type: "integer", nullable: false),
                    expiracao_orcamentos_habilitada = table.Column<bool>(type: "boolean", nullable: false),
                    email_remetente = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_automacao", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuracao_automacao_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_configuracoes_automacao_estabelecimento",
                schema: "public",
                table: "configuracoes_automacao",
                column: "estabelecimento_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes_automacao",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "lembrete_por_email_enviado",
                schema: "public",
                table: "agendamentos");
        }
    }
}

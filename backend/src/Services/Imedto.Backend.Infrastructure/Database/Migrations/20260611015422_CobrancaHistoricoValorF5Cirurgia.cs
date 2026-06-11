using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CobrancaHistoricoValorF5Cirurgia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cobranca_historico_valor",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranca_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_anterior = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    valor_novo = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    alterado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alterado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranca_historico_valor", x => x.id);
                    table.ForeignKey(
                        name: "fk_cobranca_historico_valor_cobranca",
                        column: x => x.cobranca_id,
                        principalSchema: "public",
                        principalTable: "cobrancas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_cobrancas_orcamento_cirurgia",
                schema: "public",
                table: "cobrancas",
                column: "orcamento_id",
                unique: true,
                filter: "origem = 'Cirurgia' AND orcamento_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cobranca_historico_valor_cobranca_id",
                schema: "public",
                table: "cobranca_historico_valor",
                column: "cobranca_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cobranca_historico_valor",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ux_cobrancas_orcamento_cirurgia",
                schema: "public",
                table: "cobrancas");
        }
    }
}

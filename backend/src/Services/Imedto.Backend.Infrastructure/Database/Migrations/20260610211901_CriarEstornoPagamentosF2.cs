using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarEstornoPagamentosF2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estorno_pagamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    cobranca_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    lancamento_estorno_id = table.Column<long>(type: "bigint", nullable: true),
                    estornado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_estorno = table.Column<DateOnly>(type: "date", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estorno_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_estorno_pagamentos_cobranca",
                        column: x => x.cobranca_id,
                        principalSchema: "public",
                        principalTable: "cobrancas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_estorno_pagamentos_pagamento",
                        column: x => x.pagamento_id,
                        principalSchema: "public",
                        principalTable: "pagamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_estorno_pagamentos_cobranca_id",
                schema: "public",
                table: "estorno_pagamentos",
                column: "cobranca_id");

            migrationBuilder.CreateIndex(
                name: "ix_estorno_pagamentos_estab_cobranca",
                schema: "public",
                table: "estorno_pagamentos",
                columns: new[] { "estabelecimento_id", "cobranca_id" });

            migrationBuilder.CreateIndex(
                name: "uq_estorno_pagamentos_pagamento_id",
                schema: "public",
                table: "estorno_pagamentos",
                column: "pagamento_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estorno_pagamentos",
                schema: "public");
        }
    }
}

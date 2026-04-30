using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase6_1bProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_produto",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    valor_referencia = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    uso_unico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_produto", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_produto_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_cirurgia_produto",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    catalogo_cirurgia_id = table.Column<long>(type: "bigint", nullable: false),
                    catalogo_produto_id = table.Column<long>(type: "bigint", nullable: false),
                    quantidade_padrao = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    obrigatorio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_cirurgia_produto", x => x.id);
                    table.ForeignKey(
                        name: "fk_cirurgia_produto_cirurgia",
                        column: x => x.catalogo_cirurgia_id,
                        principalSchema: "public",
                        principalTable: "orcamento_catalogo_cirurgia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cirurgia_produto_produto",
                        column: x => x.catalogo_produto_id,
                        principalSchema: "public",
                        principalTable: "orcamento_catalogo_produto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_catalogo_cirurgia_produto_catalogo_produto_id",
                schema: "public",
                table: "orcamento_catalogo_cirurgia_produto",
                column: "catalogo_produto_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalogo_cirurgia_produto",
                schema: "public",
                table: "orcamento_catalogo_cirurgia_produto",
                columns: new[] { "catalogo_cirurgia_id", "catalogo_produto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_produto_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_produto",
                columns: new[] { "estabelecimento_id", "ativo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento_catalogo_cirurgia_produto",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_produto",
                schema: "public");
        }
    }
}

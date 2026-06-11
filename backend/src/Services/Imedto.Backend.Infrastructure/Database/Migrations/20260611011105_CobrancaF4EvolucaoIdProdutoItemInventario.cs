using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CobrancaF4EvolucaoIdProdutoItemInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "item_inventario_id",
                schema: "public",
                table: "orcamento_catalogo_produto",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "evolucao_id",
                schema: "public",
                table: "cobrancas",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_catalogo_produto_item_inventario_id",
                schema: "public",
                table: "orcamento_catalogo_produto",
                column: "item_inventario_id");

            migrationBuilder.AddForeignKey(
                name: "fk_catalogo_produto_item_inventario",
                schema: "public",
                table: "orcamento_catalogo_produto",
                column: "item_inventario_id",
                principalSchema: "public",
                principalTable: "itens_inventario",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Índice UNIQUE parcial — idempotência F4 (R7/CA77/CA78).
            // HasFilter com expressão mista não é emitido pelo gerador EF/Npgsql;
            // adicionado via Sql() para garantir o DDL correto no banco.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_cobrancas_evolucao_procedimento " +
                "ON public.cobrancas (evolucao_id) " +
                "WHERE origem = 'Procedimento' AND evolucao_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS public.ux_cobrancas_evolucao_procedimento;");

            migrationBuilder.DropForeignKey(
                name: "fk_catalogo_produto_item_inventario",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropIndex(
                name: "IX_orcamento_catalogo_produto_item_inventario_id",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "item_inventario_id",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "evolucao_id",
                schema: "public",
                table: "cobrancas");
        }
    }
}

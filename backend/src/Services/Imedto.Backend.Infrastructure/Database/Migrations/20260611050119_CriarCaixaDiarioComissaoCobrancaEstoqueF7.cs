using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarCaixaDiarioComissaoCobrancaEstoqueF7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "cobranca_id",
                schema: "public",
                table: "movimentacoes_estoque",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "caixa_diario",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aberto_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aberto_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fechado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fechado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reaberto_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reaberto_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caixa_diario", x => x.id);
                    table.ForeignKey(
                        name: "fk_caixa_diario_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "config_comissao_profissional",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_comissao_profissional", x => x.id);
                    table.ForeignKey(
                        name: "fk_config_comissao_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_cobranca_id",
                schema: "public",
                table: "movimentacoes_estoque",
                column: "cobranca_id");

            // F7: índice geral para extrato/KPIs que incluem todos os status (Pendente + Pago).
            // Adicionado via Sql() porque o EF não suporta dois índices fluentes nas mesmas colunas.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_lancamentos_estab_data_pagamento " +
                "ON public.lancamentos (estabelecimento_id, data_pagamento);");

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_estab_data_pagamento_pago",
                schema: "public",
                table: "lancamentos",
                columns: new[] { "estabelecimento_id", "data_pagamento" },
                filter: "status = 'Pago'");

            // F7: índice em data_pagamento de pagamentos para a query de comissões (regime caixa — ObterComissoes).
            // pagamentos não tem estabelecimento_id no domínio EF (join via cobranca_id), então apenas data_pagamento.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_pagamentos_data_pagamento " +
                "ON public.pagamentos (data_pagamento);");

            migrationBuilder.CreateIndex(
                name: "uq_caixa_diario_estab_data",
                schema: "public",
                table: "caixa_diario",
                columns: new[] { "estabelecimento_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_config_comissao_estab_prof_tipo",
                schema: "public",
                table: "config_comissao_profissional",
                columns: new[] { "estabelecimento_id", "profissional_usuario_id", "tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "caixa_diario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "config_comissao_profissional",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_movimentacao_cobranca_id",
                schema: "public",
                table: "movimentacoes_estoque");

            migrationBuilder.DropIndex(
                name: "ix_lancamentos_estab_data_pagamento_pago",
                schema: "public",
                table: "lancamentos");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS public.ix_lancamentos_estab_data_pagamento;");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS public.ix_pagamentos_data_pagamento;");

            migrationBuilder.DropColumn(
                name: "cobranca_id",
                schema: "public",
                table: "movimentacoes_estoque");
        }
    }
}

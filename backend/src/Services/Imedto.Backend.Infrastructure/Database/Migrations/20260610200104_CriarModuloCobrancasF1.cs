using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloCobrancasF1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "cobranca_id",
                schema: "public",
                table: "lancamentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "pagamento_id",
                schema: "public",
                table: "lancamentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cobrancas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    origem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    agendamento_id = table.Column<long>(type: "bigint", nullable: true),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo_atendimento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    convenio_id = table.Column<long>(type: "bigint", nullable: true),
                    valor_cobrado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobrancas", x => x.id);
                    table.ForeignKey(
                        name: "fk_cobrancas_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "config_taxa_forma_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    forma_pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    taxa_percentual = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false, defaultValue: 0m),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_taxa_forma_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_config_taxa_forma_pagamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_config_taxa_forma_pagamento_forma",
                        column: x => x.forma_pagamento_id,
                        principalSchema: "public",
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tabela_preco_consulta",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valor_sugerido = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tabela_preco_consulta", x => x.id);
                    table.ForeignKey(
                        name: "fk_tabela_preco_consulta_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranca_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    forma_pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    parcelas = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    juros = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    taxa = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    registrado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lancamento_id = table.Column<long>(type: "bigint", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagamentos_cobranca",
                        column: x => x.cobranca_id,
                        principalSchema: "public",
                        principalTable: "cobrancas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamentos_forma_pagamento",
                        column: x => x.forma_pagamento_id,
                        principalSchema: "public",
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_cobranca_id",
                schema: "public",
                table: "lancamentos",
                column: "cobranca_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_agendamento_id",
                schema: "public",
                table: "cobrancas",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_estab_paciente",
                schema: "public",
                table: "cobrancas",
                columns: new[] { "estabelecimento_id", "paciente_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_estab_status",
                schema: "public",
                table: "cobrancas",
                columns: new[] { "estabelecimento_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_config_taxa_forma_pagamento_forma_id",
                schema: "public",
                table: "config_taxa_forma_pagamento",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "uq_config_taxa_forma_pagamento_estab_forma",
                schema: "public",
                table: "config_taxa_forma_pagamento",
                columns: new[] { "estabelecimento_id", "forma_pagamento_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_cobranca_id",
                schema: "public",
                table: "pagamentos",
                column: "cobranca_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_forma_pagamento_id",
                schema: "public",
                table: "pagamentos",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_tabela_preco_consulta_estab_profissional",
                schema: "public",
                table: "tabela_preco_consulta",
                columns: new[] { "estabelecimento_id", "profissional_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "config_taxa_forma_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "pagamentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tabela_preco_consulta",
                schema: "public");

            migrationBuilder.DropTable(
                name: "cobrancas",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_lancamentos_cobranca_id",
                schema: "public",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "cobranca_id",
                schema: "public",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "pagamento_id",
                schema: "public",
                table: "lancamentos");
        }
    }
}

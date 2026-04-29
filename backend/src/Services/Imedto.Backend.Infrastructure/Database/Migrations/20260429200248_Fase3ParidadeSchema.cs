using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase3ParidadeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "emitida_em",
                schema: "public",
                table: "receitas",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "tipo_notificacao",
                schema: "public",
                table: "receitas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "concentracao",
                schema: "public",
                table: "receita_itens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "duracao",
                schema: "public",
                table: "receita_itens",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forma_farmaceutica",
                schema: "public",
                table: "receita_itens",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "acrescimo_percentual",
                schema: "public",
                table: "orcamento_formas_pagamento",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "entrada_percentual",
                schema: "public",
                table: "orcamento_formas_pagamento",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "orcamento_anestesia",
                schema: "public",
                columns: table => new
                {
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_anestesia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_anestesia", x => x.orcamento_id);
                    table.ForeignKey(
                        name: "fk_orcamento_anestesia_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_cirurgias",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    procedimento_cirurgico_id = table.Column<long>(type: "bigint", nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    duracao_minutos = table.Column<int>(type: "integer", nullable: true),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_cirurgias", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_cirurgia_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_orcamento_cirurgia_procedimento_cirurgico",
                        column: x => x.procedimento_cirurgico_id,
                        principalSchema: "public",
                        principalTable: "procedimentos_cirurgicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_internacao",
                schema: "public",
                columns: table => new
                {
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_internacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dias = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "regioes_anatomicas_catalogo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    pai_codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    nivel = table.Column<short>(type: "smallint", nullable: false),
                    vista = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    template_texto = table.Column<string>(type: "text", nullable: true),
                    svg_coords = table.Column<string>(type: "jsonb", nullable: true),
                    ordem = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    lateralidade = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regioes_anatomicas_catalogo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_cirurgia_orcamento",
                schema: "public",
                table: "orcamento_cirurgias",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_cirurgias_procedimento_cirurgico_id",
                schema: "public",
                table: "orcamento_cirurgias",
                column: "procedimento_cirurgico_id");

            migrationBuilder.CreateIndex(
                name: "ix_regioes_anatomicas_catalogo_ativo_vista",
                schema: "public",
                table: "regioes_anatomicas_catalogo",
                columns: new[] { "ativo", "vista" });

            migrationBuilder.CreateIndex(
                name: "ix_regioes_anatomicas_catalogo_vista",
                schema: "public",
                table: "regioes_anatomicas_catalogo",
                column: "vista");

            migrationBuilder.CreateIndex(
                name: "uq_regioes_anatomicas_catalogo_codigo",
                schema: "public",
                table: "regioes_anatomicas_catalogo",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento_anestesia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_cirurgias",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_internacao",
                schema: "public");

            migrationBuilder.DropTable(
                name: "regioes_anatomicas_catalogo",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "tipo_notificacao",
                schema: "public",
                table: "receitas");

            migrationBuilder.DropColumn(
                name: "concentracao",
                schema: "public",
                table: "receita_itens");

            migrationBuilder.DropColumn(
                name: "duracao",
                schema: "public",
                table: "receita_itens");

            migrationBuilder.DropColumn(
                name: "forma_farmaceutica",
                schema: "public",
                table: "receita_itens");

            migrationBuilder.DropColumn(
                name: "acrescimo_percentual",
                schema: "public",
                table: "orcamento_formas_pagamento");

            migrationBuilder.DropColumn(
                name: "entrada_percentual",
                schema: "public",
                table: "orcamento_formas_pagamento");

            migrationBuilder.AlterColumn<DateTime>(
                name: "emitida_em",
                schema: "public",
                table: "receitas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}

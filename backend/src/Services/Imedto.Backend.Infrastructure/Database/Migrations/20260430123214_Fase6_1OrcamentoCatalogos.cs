using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase6_1OrcamentoCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_cirurgia",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    valor_base = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    duracao_padrao_minutos = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_cirurgia", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_cirurgia_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_equipe",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    valor_padrao = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_equipe", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_equipe_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_implante",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    item_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_implante", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_implante_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalogo_implante_item_inventario",
                        column: x => x.item_inventario_id,
                        principalSchema: "public",
                        principalTable: "itens_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_configuracao_local_cirurgia",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_internacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tempo_base_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_base = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tempo_adicional_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_adicional = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_configuracao_local_cirurgia", x => x.id);
                    table.ForeignKey(
                        name: "fk_config_local_cirurgia_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_configuracao_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    forma_pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    acrescimo_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    entrada_percentual_padrao = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    taxa_parcela = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    parcelas_maximas = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_configuracao_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_config_pgto_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_config_pgto_forma_pagamento",
                        column: x => x.forma_pagamento_id,
                        principalSchema: "public",
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_valor_profissional",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    funcao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    tempo_base_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_tempo_base = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tempo_adicional_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_adicional = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_plus = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_valor_profissional", x => x.id);
                    table.ForeignKey(
                        name: "fk_valor_prof_orc_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_cirurgia_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_cirurgia",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_equipe_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_equipe",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_implante_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_implante",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_catalogo_implante_item_inventario_id",
                schema: "public",
                table: "orcamento_catalogo_implante",
                column: "item_inventario_id");

            migrationBuilder.CreateIndex(
                name: "uq_config_local_estab_tipo",
                schema: "public",
                table: "orcamento_configuracao_local_cirurgia",
                columns: new[] { "estabelecimento_id", "tipo_internacao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_configuracao_pagamento_forma_pagamento_id",
                schema: "public",
                table: "orcamento_configuracao_pagamento",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "uq_config_pgto_estab_forma",
                schema: "public",
                table: "orcamento_configuracao_pagamento",
                columns: new[] { "estabelecimento_id", "forma_pagamento_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valor_prof_orc_estab_prof_funcao",
                schema: "public",
                table: "orcamento_valor_profissional",
                columns: new[] { "estabelecimento_id", "profissional_usuario_id", "funcao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento_catalogo_cirurgia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_equipe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_implante",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_configuracao_local_cirurgia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_configuracao_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_valor_profissional",
                schema: "public");
        }
    }
}

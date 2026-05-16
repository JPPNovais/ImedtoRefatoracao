using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConfigOrcamentoAbas2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codigo_sku",
                schema: "public",
                table: "orcamento_catalogo_produto",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fornecedor_nome",
                schema: "public",
                table: "orcamento_catalogo_produto",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "marca",
                schema: "public",
                table: "orcamento_catalogo_produto",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                schema: "public",
                table: "orcamento_catalogo_produto",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Outros");

            migrationBuilder.AddColumn<string>(
                name: "unidade",
                schema: "public",
                table: "orcamento_catalogo_produto",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "un");

            migrationBuilder.AddColumn<bool>(
                name: "incluido",
                schema: "public",
                table: "orcamento_catalogo_cirurgia_produto",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "categoria",
                schema: "public",
                table: "orcamento_catalogo_cirurgia",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_interno",
                schema: "public",
                table: "orcamento_catalogo_cirurgia",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_tuss",
                schema: "public",
                table: "orcamento_catalogo_cirurgia",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "orcamento_anestesista",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    crm = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    especialidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    telefone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tabela_honorarios = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_anestesista", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_anestesista_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_team_role",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    papel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nome_padrao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tipo_honorario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    base_calculo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_team_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_team_role_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_anestesista_faixa",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    anestesista_id = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_anestesista_faixa", x => x.id);
                    table.ForeignKey(
                        name: "fk_anestesista_faixa_anestesista",
                        column: x => x.anestesista_id,
                        principalSchema: "public",
                        principalTable: "orcamento_anestesista",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_pacote",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    anestesista_id = table.Column<long>(type: "bigint", nullable: true),
                    valor_total_sugerido = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_pacote", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_pacote_anestesista",
                        column: x => x.anestesista_id,
                        principalSchema: "public",
                        principalTable: "orcamento_anestesista",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamento_pacote_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_pacote_procedimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacote_id = table.Column<long>(type: "bigint", nullable: false),
                    catalogo_cirurgia_id = table.Column<long>(type: "bigint", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_pacote_procedimento", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacote_procedimento_cirurgia",
                        column: x => x.catalogo_cirurgia_id,
                        principalSchema: "public",
                        principalTable: "orcamento_catalogo_cirurgia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pacote_procedimento_pacote",
                        column: x => x.pacote_id,
                        principalSchema: "public",
                        principalTable: "orcamento_pacote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_pacote_produto",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacote_id = table.Column<long>(type: "bigint", nullable: false),
                    catalogo_produto_id = table.Column<long>(type: "bigint", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_pacote_produto", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacote_produto_pacote",
                        column: x => x.pacote_id,
                        principalSchema: "public",
                        principalTable: "orcamento_pacote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pacote_produto_produto",
                        column: x => x.catalogo_produto_id,
                        principalSchema: "public",
                        principalTable: "orcamento_catalogo_produto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_pacote_team_role",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacote_id = table.Column<long>(type: "bigint", nullable: false),
                    team_role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_pacote_team_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacote_team_role_pacote",
                        column: x => x.pacote_id,
                        principalSchema: "public",
                        principalTable: "orcamento_pacote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pacote_team_role_team_role",
                        column: x => x.team_role_id,
                        principalSchema: "public",
                        principalTable: "orcamento_team_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_anestesista_estab_ativo",
                schema: "public",
                table: "orcamento_anestesista",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_anestesista_faixa_descricao",
                schema: "public",
                table: "orcamento_anestesista_faixa",
                columns: new[] { "anestesista_id", "descricao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_pacote_anestesista_id",
                schema: "public",
                table: "orcamento_pacote",
                column: "anestesista_id");

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_pacote_estab_ativo",
                schema: "public",
                table: "orcamento_pacote",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_pacote_procedimento_catalogo_cirurgia_id",
                schema: "public",
                table: "orcamento_pacote_procedimento",
                column: "catalogo_cirurgia_id");

            migrationBuilder.CreateIndex(
                name: "uq_pacote_procedimento",
                schema: "public",
                table: "orcamento_pacote_procedimento",
                columns: new[] { "pacote_id", "catalogo_cirurgia_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_pacote_produto_catalogo_produto_id",
                schema: "public",
                table: "orcamento_pacote_produto",
                column: "catalogo_produto_id");

            migrationBuilder.CreateIndex(
                name: "uq_pacote_produto",
                schema: "public",
                table: "orcamento_pacote_produto",
                columns: new[] { "pacote_id", "catalogo_produto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_pacote_team_role_team_role_id",
                schema: "public",
                table: "orcamento_pacote_team_role",
                column: "team_role_id");

            migrationBuilder.CreateIndex(
                name: "uq_pacote_team_role",
                schema: "public",
                table: "orcamento_pacote_team_role",
                columns: new[] { "pacote_id", "team_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_team_role_estab_ativo",
                schema: "public",
                table: "orcamento_team_role",
                columns: new[] { "estabelecimento_id", "ativo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento_anestesista_faixa",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_pacote_procedimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_pacote_produto",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_pacote_team_role",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_pacote",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_team_role",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_anestesista",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "codigo_sku",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "fornecedor_nome",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "marca",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "unidade",
                schema: "public",
                table: "orcamento_catalogo_produto");

            migrationBuilder.DropColumn(
                name: "incluido",
                schema: "public",
                table: "orcamento_catalogo_cirurgia_produto");

            migrationBuilder.DropColumn(
                name: "categoria",
                schema: "public",
                table: "orcamento_catalogo_cirurgia");

            migrationBuilder.DropColumn(
                name: "codigo_interno",
                schema: "public",
                table: "orcamento_catalogo_cirurgia");

            migrationBuilder.DropColumn(
                name: "codigo_tuss",
                schema: "public",
                table: "orcamento_catalogo_cirurgia");
        }
    }
}

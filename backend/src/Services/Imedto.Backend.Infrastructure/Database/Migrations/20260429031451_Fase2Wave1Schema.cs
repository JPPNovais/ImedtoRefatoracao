using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase2Wave1Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deletado_em",
                schema: "public",
                table: "profissionais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "profissionais",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorias_financeiras",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    padrao = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_financeiras", x => x.id);
                    table.ForeignKey(
                        name: "fk_categoria_financeira_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formas_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    padrao = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formas_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_forma_pagamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "public",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    hash_payload = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    response_json = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "profissoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    conselho_sigla = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profissoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "especialidades",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissao_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especialidades", x => x.id);
                    table.ForeignKey(
                        name: "fk_especialidades_profissao",
                        column: x => x.profissao_id,
                        principalSchema: "public",
                        principalTable: "profissoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categoria_financeira_estab_tipo_ativo",
                schema: "public",
                table: "categorias_financeiras",
                columns: new[] { "estabelecimento_id", "tipo", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_categoria_financeira_estab_nome",
                schema: "public",
                table: "categorias_financeiras",
                columns: new[] { "estabelecimento_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_especialidades_profissao_ativo",
                schema: "public",
                table: "especialidades",
                columns: new[] { "profissao_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_especialidades_profissao_nome",
                schema: "public",
                table: "especialidades",
                columns: new[] { "profissao_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_forma_pagamento_estab_ativo",
                schema: "public",
                table: "formas_pagamento",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_forma_pagamento_estab_nome",
                schema: "public",
                table: "formas_pagamento",
                columns: new[] { "estabelecimento_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profissoes_ativo",
                schema: "public",
                table: "profissoes",
                column: "ativo");

            migrationBuilder.CreateIndex(
                name: "uq_profissoes_nome",
                schema: "public",
                table: "profissoes",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorias_financeiras",
                schema: "public");

            migrationBuilder.DropTable(
                name: "especialidades",
                schema: "public");

            migrationBuilder.DropTable(
                name: "formas_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "public");

            migrationBuilder.DropTable(
                name: "profissoes",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "deletado_em",
                schema: "public",
                table: "profissionais");

            migrationBuilder.DropColumn(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "profissionais");
        }
    }
}

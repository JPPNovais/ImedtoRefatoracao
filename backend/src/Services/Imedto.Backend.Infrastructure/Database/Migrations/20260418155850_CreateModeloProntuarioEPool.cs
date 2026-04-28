using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateModeloProntuarioEPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modelo_de_prontuario",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estrutura = table.Column<string>(type: "jsonb", nullable: false),
                    eh_padrao_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelo_de_prontuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuario_variaveis_pool",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    eh_padrao_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_variaveis_pool", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_modelo_prontuario_estabelecimento",
                schema: "public",
                table: "modelo_de_prontuario",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_modelo_prontuario_padrao_sistema",
                schema: "public",
                table: "modelo_de_prontuario",
                column: "eh_padrao_sistema");

            migrationBuilder.CreateIndex(
                name: "ix_pool_estabelecimento_tipo",
                schema: "public",
                table: "prontuario_variaveis_pool",
                columns: new[] { "estabelecimento_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_pool_padrao_tipo",
                schema: "public",
                table: "prontuario_variaveis_pool",
                columns: new[] { "eh_padrao_sistema", "tipo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modelo_de_prontuario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuario_variaveis_pool",
                schema: "public");
        }
    }
}

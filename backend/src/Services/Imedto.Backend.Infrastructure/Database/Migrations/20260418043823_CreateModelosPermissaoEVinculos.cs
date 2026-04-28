using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateModelosPermissaoEVinculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modelo_permissao_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    permissoes = table.Column<string>(type: "jsonb", nullable: false),
                    eh_padrao = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelo_permissao_estabelecimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vinculo_profissional_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    modelo_permissao_id = table.Column<long>(type: "bigint", nullable: false),
                    convidado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    convidado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aceito_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inativado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculo_profissional_estabelecimento", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_modelo_permissao_estabelecimento",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "uq_modelo_permissao_nome_por_estabelecimento",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                columns: new[] { "estabelecimento_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vinculo_estabelecimento_status",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                columns: new[] { "estabelecimento_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_vinculo_profissional_status",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                columns: new[] { "profissional_usuario_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modelo_permissao_estabelecimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "vinculo_profissional_estabelecimento",
                schema: "public");
        }
    }
}

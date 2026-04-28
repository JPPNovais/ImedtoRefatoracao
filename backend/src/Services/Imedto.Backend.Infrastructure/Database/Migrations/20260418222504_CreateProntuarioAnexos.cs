using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateProntuarioAnexos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prontuario_anexos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    evolucao_id = table.Column<long>(type: "bigint", nullable: true),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    nome_original = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    arquivado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arquivado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_anexos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_anexos_evolucao",
                schema: "public",
                table: "prontuario_anexos",
                column: "evolucao_id");

            migrationBuilder.CreateIndex(
                name: "ix_anexos_prontuario",
                schema: "public",
                table: "prontuario_anexos",
                columns: new[] { "prontuario_id", "arquivado_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prontuario_anexos",
                schema: "public");
        }
    }
}

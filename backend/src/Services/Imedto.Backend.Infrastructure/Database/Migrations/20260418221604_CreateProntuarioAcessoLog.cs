using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateProntuarioAcessoLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prontuario_acesso_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_acesso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocorrido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_acesso_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_acesso_log_prontuario_data",
                schema: "public",
                table: "prontuario_acesso_log",
                columns: new[] { "prontuario_id", "ocorrido_em" });

            migrationBuilder.CreateIndex(
                name: "ix_acesso_log_usuario",
                schema: "public",
                table: "prontuario_acesso_log",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prontuario_acesso_log",
                schema: "public");
        }
    }
}

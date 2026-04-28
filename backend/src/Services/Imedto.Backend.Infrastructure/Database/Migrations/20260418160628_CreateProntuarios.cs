using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateProntuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prontuario_evolucoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    autor_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    modelo_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    modelo_de_prontuario_id_origem = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_evolucoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuarios",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    modelo_de_prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuarios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evolucoes_prontuario_data",
                schema: "public",
                table: "prontuario_evolucoes",
                columns: new[] { "prontuario_id", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "uq_prontuario_paciente_estabelecimento",
                schema: "public",
                table: "prontuarios",
                columns: new[] { "paciente_id", "estabelecimento_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prontuario_evolucoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuarios",
                schema: "public");
        }
    }
}

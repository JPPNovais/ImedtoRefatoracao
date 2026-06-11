using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarPendenciasAtendimentoF3B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pendencias_atendimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    evolucao_id = table.Column<long>(type: "bigint", nullable: false),
                    agendamento_id = table.Column<long>(type: "bigint", nullable: true),
                    acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    referencia_id = table.Column<long>(type: "bigint", nullable: true),
                    concluida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pendencias_atendimento", x => x.id);
                    table.ForeignKey(
                        name: "fk_pendencias_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pendencias_evolucao",
                        column: x => x.evolucao_id,
                        principalSchema: "public",
                        principalTable: "prontuario_evolucoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pendencias_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pendencias_atendimento_paciente_id",
                schema: "public",
                table: "pendencias_atendimento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pendencias_estab_paciente_status",
                schema: "public",
                table: "pendencias_atendimento",
                columns: new[] { "estabelecimento_id", "paciente_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_pendencias_evolucao_acao",
                schema: "public",
                table: "pendencias_atendimento",
                columns: new[] { "evolucao_id", "acao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pendencias_atendimento",
                schema: "public");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateAgendamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agendamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio_previsto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fim_previsto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_servico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_agendamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agendamento_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_estab_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "estabelecimento_id", "inicio_previsto" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_paciente_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "paciente_id", "inicio_previsto" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_prof_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "profissional_usuario_id", "inicio_previsto" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agendamentos",
                schema: "public");
        }
    }
}

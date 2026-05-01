using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddListaEsperaAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lista_espera_agendamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profissional_preferido_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atendido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atendido_por_agendamento_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lista_espera_agendamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_lista_espera_agendamento",
                        column: x => x.atendido_por_agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lista_espera_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lista_espera_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lista_espera_agendamento_atendido_por_agendamento_id",
                schema: "public",
                table: "lista_espera_agendamento",
                column: "atendido_por_agendamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_lista_espera_agendamento_paciente_id",
                schema: "public",
                table: "lista_espera_agendamento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_lista_espera_estab_atendido",
                schema: "public",
                table: "lista_espera_agendamento",
                columns: new[] { "estabelecimento_id", "atendido_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lista_espera_agendamento",
                schema: "public");
        }
    }
}

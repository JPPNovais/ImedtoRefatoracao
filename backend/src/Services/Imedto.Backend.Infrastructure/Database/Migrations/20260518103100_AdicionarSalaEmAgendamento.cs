using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSalaEmAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "sala_id",
                schema: "public",
                table: "agendamentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agendamento_sala_audit",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agendamento_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    sala_id_anterior = table.Column<long>(type: "bigint", nullable: true),
                    sala_id_nova = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamento_sala_audit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_sala",
                schema: "public",
                table: "agendamentos",
                column: "sala_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_sala_audit_agendamento",
                schema: "public",
                table: "agendamento_sala_audit",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_sala_audit_estab",
                schema: "public",
                table: "agendamento_sala_audit",
                column: "estabelecimento_id");

            migrationBuilder.AddForeignKey(
                name: "fk_agendamento_sala",
                schema: "public",
                table: "agendamentos",
                column: "sala_id",
                principalSchema: "public",
                principalTable: "sala_atendimento",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_agendamento_sala",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropTable(
                name: "agendamento_sala_audit",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_agendamentos_sala",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "sala_id",
                schema: "public",
                table: "agendamentos");
        }
    }
}

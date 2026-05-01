using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ListaEsperaPrioridadePeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "preferencia_periodo",
                schema: "public",
                table: "lista_espera_agendamento",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Qualquer");

            migrationBuilder.AddColumn<string>(
                name: "prioridade",
                schema: "public",
                table: "lista_espera_agendamento",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Rotina");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferencia_periodo",
                schema: "public",
                table: "lista_espera_agendamento");

            migrationBuilder.DropColumn(
                name: "prioridade",
                schema: "public",
                table: "lista_espera_agendamento");
        }
    }
}

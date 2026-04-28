using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFuncionamentoEstabelecimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "datas_bloqueadas",
                schema: "public",
                table: "estabelecimentos",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "dias_semana_funcionamento",
                schema: "public",
                table: "estabelecimentos",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[1,2,3,4,5]'::jsonb");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "horario_fim",
                schema: "public",
                table: "estabelecimentos",
                type: "time",
                nullable: false,
                defaultValueSql: "'18:00'::time");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "horario_inicio",
                schema: "public",
                table: "estabelecimentos",
                type: "time",
                nullable: false,
                defaultValueSql: "'08:00'::time");

            migrationBuilder.AddColumn<string>(
                name: "horarios_bloqueados",
                schema: "public",
                table: "estabelecimentos",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "datas_bloqueadas",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.DropColumn(
                name: "dias_semana_funcionamento",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.DropColumn(
                name: "horario_fim",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.DropColumn(
                name: "horario_inicio",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.DropColumn(
                name: "horarios_bloqueados",
                schema: "public",
                table: "estabelecimentos");
        }
    }
}

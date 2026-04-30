using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase6LimpaOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "config_pagamento_json",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "public",
                table: "orcamentos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "config_pagamento_json",
                schema: "public",
                table: "orcamentos",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                schema: "public",
                table: "orcamentos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Simples");
        }
    }
}

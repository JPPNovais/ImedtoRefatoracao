using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateProfissionais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profissionais",
                schema: "public",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conselho = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    numero_registro = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    especialidade = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profissionais", x => x.usuario_id);
                });

            migrationBuilder.CreateIndex(
                name: "uq_profissionais_conselho_uf_numero",
                schema: "public",
                table: "profissionais",
                columns: new[] { "conselho", "uf", "numero_registro" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profissionais",
                schema: "public");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarSalasAtendimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tipo_sala_atendimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_sala_atendimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sala_atendimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    unidade_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_sala_id = table.Column<long>(type: "bigint", nullable: true),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sala_atendimento", x => x.id);
                    table.ForeignKey(
                        name: "fk_salas_estab",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_salas_tipo",
                        column: x => x.tipo_sala_id,
                        principalSchema: "public",
                        principalTable: "tipo_sala_atendimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_salas_unidade",
                        column: x => x.unidade_id,
                        principalSchema: "public",
                        principalTable: "unidades_estabelecimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sala_atendimento_tipo_sala_id",
                schema: "public",
                table: "sala_atendimento",
                column: "tipo_sala_id");

            migrationBuilder.CreateIndex(
                name: "ix_salas_estab",
                schema: "public",
                table: "sala_atendimento",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_salas_unidade",
                schema: "public",
                table: "sala_atendimento",
                column: "unidade_id");

            migrationBuilder.CreateIndex(
                name: "uq_tipo_sala_nome",
                schema: "public",
                table: "tipo_sala_atendimento",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sala_atendimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tipo_sala_atendimento",
                schema: "public");
        }
    }
}

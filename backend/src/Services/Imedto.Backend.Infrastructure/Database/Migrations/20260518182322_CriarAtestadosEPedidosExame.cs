using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarAtestadosEPedidosExame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atestados",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dias_afastamento = table.Column<int>(type: "integer", nullable: true),
                    cid10 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    conteudo = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atestados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modelos_atestado",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelos_atestado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_exame",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    exames = table.Column<string>(type: "jsonb", nullable: true),
                    indicacao_clinica = table.Column<string>(type: "text", nullable: false),
                    cid10 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos_exame", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_atestados_estab_criado",
                schema: "public",
                table: "atestados",
                columns: new[] { "estabelecimento_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_atestados_paciente_criado",
                schema: "public",
                table: "atestados",
                columns: new[] { "paciente_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_modelos_atestado_estab_nome",
                schema: "public",
                table: "modelos_atestado",
                columns: new[] { "estabelecimento_id", "nome" });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_exame_estab_criado",
                schema: "public",
                table: "pedidos_exame",
                columns: new[] { "estabelecimento_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_exame_paciente_criado",
                schema: "public",
                table: "pedidos_exame",
                columns: new[] { "paciente_id", "criado_em" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atestados",
                schema: "public");

            migrationBuilder.DropTable(
                name: "modelos_atestado",
                schema: "public");

            migrationBuilder.DropTable(
                name: "pedidos_exame",
                schema: "public");
        }
    }
}

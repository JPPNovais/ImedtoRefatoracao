using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class HardeningFase1Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deletado_em",
                schema: "public",
                table: "prontuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "prontuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deletado_em",
                schema: "public",
                table: "prontuario_evolucoes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "prontuario_evolucoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deletado_em",
                schema: "public",
                table: "prontuario_anexos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "prontuario_anexos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "custo_total",
                schema: "public",
                table: "movimentacoes_estoque",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "custo_unitario",
                schema: "public",
                table: "movimentacoes_estoque",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "deletado_em",
                schema: "public",
                table: "movimentacoes_estoque",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "movimentacoes_estoque",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "custo_medio",
                schema: "public",
                table: "itens_inventario",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ai_audit_logs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    prompt_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tokens_in = table.Column<int>(type: "integer", nullable: true),
                    tokens_out = table.Column<int>(type: "integer", nullable: true),
                    modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    duracao_ms = table.Column<int>(type: "integer", nullable: true),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    erro_mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_delete_attempts",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tabela = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    registro_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tentado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_delete_attempts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_audit_estab_data",
                schema: "public",
                table: "ai_audit_logs",
                columns: new[] { "estabelecimento_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ai_audit_usuario_data",
                schema: "public",
                table: "ai_audit_logs",
                columns: new[] { "usuario_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_delete_estab_data",
                schema: "public",
                table: "audit_delete_attempts",
                columns: new[] { "estabelecimento_id", "tentado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_delete_tabela_data",
                schema: "public",
                table: "audit_delete_attempts",
                columns: new[] { "tabela", "tentado_em" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_audit_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "audit_delete_attempts",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "deletado_em",
                schema: "public",
                table: "prontuarios");

            migrationBuilder.DropColumn(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "prontuarios");

            migrationBuilder.DropColumn(
                name: "deletado_em",
                schema: "public",
                table: "prontuario_evolucoes");

            migrationBuilder.DropColumn(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "prontuario_evolucoes");

            migrationBuilder.DropColumn(
                name: "deletado_em",
                schema: "public",
                table: "prontuario_anexos");

            migrationBuilder.DropColumn(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "prontuario_anexos");

            migrationBuilder.DropColumn(
                name: "custo_total",
                schema: "public",
                table: "movimentacoes_estoque");

            migrationBuilder.DropColumn(
                name: "custo_unitario",
                schema: "public",
                table: "movimentacoes_estoque");

            migrationBuilder.DropColumn(
                name: "deletado_em",
                schema: "public",
                table: "movimentacoes_estoque");

            migrationBuilder.DropColumn(
                name: "deletado_por_usuario_id",
                schema: "public",
                table: "movimentacoes_estoque");

            migrationBuilder.DropColumn(
                name: "custo_medio",
                schema: "public",
                table: "itens_inventario");
        }
    }
}

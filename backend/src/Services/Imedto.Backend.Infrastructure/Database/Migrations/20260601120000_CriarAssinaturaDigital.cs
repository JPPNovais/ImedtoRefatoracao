using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarAssinaturaDigital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Novas colunas em receitas ---

            migrationBuilder.AddColumn<string>(
                name: "pdf_assinado_s3_key",
                schema: "public",
                table: "receitas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "assinatura_solicitada_em",
                schema: "public",
                table: "receitas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "assinada_em",
                schema: "public",
                table: "receitas",
                type: "timestamp with time zone",
                nullable: true);

            // --- Nova tabela: assinatura_certificados ---

            migrationBuilder.CreateTable(
                name: "assinatura_certificados",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    medico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provedor = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinatura_certificados", x => x.id);
                });

            // --- Nova tabela: assinatura_audit_log ---

            migrationBuilder.CreateTable(
                name: "assinatura_audit_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    receita_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<string>(type: "text", nullable: false),
                    status_anterior = table.Column<string>(type: "text", nullable: true),
                    status_novo = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinatura_audit_log", x => x.id);
                });

            // --- Índices não-concurrent (criados dentro da transação) ---
            // Índices CONCURRENTLY ficam em arquivo separado: 20260601120001_indices_assinatura_digital_concurrently.sql

            migrationBuilder.CreateIndex(
                name: "uq_assinatura_certificados_medico_provedor",
                schema: "public",
                table: "assinatura_certificados",
                columns: new[] { "medico_id", "provedor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_certificados_medico",
                schema: "public",
                table: "assinatura_certificados",
                column: "medico_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_audit_log_receita",
                schema: "public",
                table: "assinatura_audit_log",
                column: "receita_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_audit_log_estab_criado",
                schema: "public",
                table: "assinatura_audit_log",
                columns: new[] { "estabelecimento_id", "criado_em" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assinatura_audit_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "assinatura_certificados",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "assinada_em",
                schema: "public",
                table: "receitas");

            migrationBuilder.DropColumn(
                name: "assinatura_solicitada_em",
                schema: "public",
                table: "receitas");

            migrationBuilder.DropColumn(
                name: "pdf_assinado_s3_key",
                schema: "public",
                table: "receitas");
        }
    }
}

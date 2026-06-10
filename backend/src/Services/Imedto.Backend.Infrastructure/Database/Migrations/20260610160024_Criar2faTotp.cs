using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Criar2faTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTA: tipo_prazo_entrega em fornecedores_estoque já foi aplicado em
            // 20260604180000_TipoPrazoEntregaFornecedor (SQL idempotente em db/migrations/).
            // Removida desta migration para evitar duplicidade — o SQL idempotente abaixo
            // usa a guarda __ef_migrations_history que protege contra re-execução.

            migrationBuilder.AddColumn<bool>(
                name: "exigir_dono_2fa",
                schema: "public",
                table: "estabelecimentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "usuario_2fa",
                schema: "public",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segredo_cifrado = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_2fa", x => x.usuario_id);
                });

            migrationBuilder.CreateTable(
                name: "usuario_2fa_codigo_recuperacao",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_hash = table.Column<string>(type: "text", nullable: false),
                    usado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_2fa_codigo_recuperacao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario_seguranca_audit",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<string>(type: "text", nullable: false),
                    ocorrido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_seguranca_audit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_usuario_2fa_codigo_recuperacao_usuario",
                schema: "public",
                table: "usuario_2fa_codigo_recuperacao",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_seguranca_audit_usuario_data",
                schema: "public",
                table: "usuario_seguranca_audit",
                columns: new[] { "usuario_id", "ocorrido_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario_2fa",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuario_2fa_codigo_recuperacao",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuario_seguranca_audit",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "exigir_dono_2fa",
                schema: "public",
                table: "estabelecimentos");
        }
    }
}

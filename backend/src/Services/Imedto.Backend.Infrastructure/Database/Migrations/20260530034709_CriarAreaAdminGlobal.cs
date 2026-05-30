using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarAreaAdminGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "imedto_admins",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    senha_hash = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    force_password_reset = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultimo_login_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    desativado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    desativado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_admins", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_admins_imedto_admins_criado_por_admin_id",
                        column: x => x.criado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_imedto_admins_imedto_admins_desativado_por_admin_id",
                        column: x => x.desativado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "imedto_admin_audit_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acao = table.Column<string>(type: "text", nullable: false),
                    recurso_tipo = table.Column<string>(type: "text", nullable: true),
                    recurso_id = table.Column<string>(type: "text", nullable: true),
                    tenant_afetado_id = table.Column<long>(type: "bigint", nullable: true),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_admin_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_admin_audit_log_imedto_admins_admin_id",
                        column: x => x.admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "imedto_admin_refresh_tokens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revogado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_origem = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_admin_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_admin_refresh_tokens_imedto_admins_admin_id",
                        column: x => x.admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "imedto_config",
                schema: "public",
                columns: table => new
                {
                    chave = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<string>(type: "jsonb", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_config", x => x.chave);
                    table.ForeignKey(
                        name: "FK_imedto_config_imedto_admins_atualizado_por_admin_id",
                        column: x => x.atualizado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "imedto_planos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    descricao_curta = table.Column<string>(type: "text", nullable: true),
                    preco_mensal_centavos = table.Column<int>(type: "integer", nullable: true),
                    gratuito = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    limites_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_planos", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_planos_imedto_admins_criado_por_admin_id",
                        column: x => x.criado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "imedto_assinaturas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    plano_id = table.Column<Guid>(type: "uuid", nullable: false),
                    iniciada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fim_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    gratuita = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criada_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_assinaturas", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_assinaturas_estabelecimentos_estabelecimento_id",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_imedto_assinaturas_imedto_admins_criada_por_admin_id",
                        column: x => x.criada_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_imedto_assinaturas_imedto_planos_plano_id",
                        column: x => x.plano_id,
                        principalSchema: "public",
                        principalTable: "imedto_planos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_admin_audit_log_acao_criado",
                schema: "public",
                table: "imedto_admin_audit_log",
                columns: new[] { "acao", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_admin_audit_log_admin_criado",
                schema: "public",
                table: "imedto_admin_audit_log",
                columns: new[] { "admin_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_admin_audit_log_criado_em",
                schema: "public",
                table: "imedto_admin_audit_log",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "ix_imedto_admin_audit_log_tenant_criado",
                schema: "public",
                table: "imedto_admin_audit_log",
                columns: new[] { "tenant_afetado_id", "criado_em" },
                filter: "tenant_afetado_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_imedto_admin_refresh_tokens_admin_expira",
                schema: "public",
                table: "imedto_admin_refresh_tokens",
                columns: new[] { "admin_id", "expira_em" });

            migrationBuilder.CreateIndex(
                name: "uq_imedto_admin_refresh_tokens_hash",
                schema: "public",
                table: "imedto_admin_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_imedto_admins_ativo",
                schema: "public",
                table: "imedto_admins",
                column: "ativo");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_admins_criado_por_admin_id",
                schema: "public",
                table: "imedto_admins",
                column: "criado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_admins_desativado_por_admin_id",
                schema: "public",
                table: "imedto_admins",
                column: "desativado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "uq_imedto_admins_email",
                schema: "public",
                table: "imedto_admins",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_imedto_assinaturas_criada_por_admin_id",
                schema: "public",
                table: "imedto_assinaturas",
                column: "criada_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_imedto_assinaturas_estabelecimento_fim",
                schema: "public",
                table: "imedto_assinaturas",
                columns: new[] { "estabelecimento_id", "fim_em" });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_assinaturas_plano",
                schema: "public",
                table: "imedto_assinaturas",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_config_atualizado_por_admin_id",
                schema: "public",
                table: "imedto_config",
                column: "atualizado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_imedto_planos_ativo",
                schema: "public",
                table: "imedto_planos",
                column: "ativo");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_planos_criado_por_admin_id",
                schema: "public",
                table: "imedto_planos",
                column: "criado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "uq_imedto_planos_nome",
                schema: "public",
                table: "imedto_planos",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imedto_admin_audit_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_admin_refresh_tokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_assinaturas",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_config",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_planos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_admins",
                schema: "public");
        }
    }
}

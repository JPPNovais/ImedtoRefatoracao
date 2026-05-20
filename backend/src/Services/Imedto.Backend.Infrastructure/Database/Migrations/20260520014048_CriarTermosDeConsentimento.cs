using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarTermosDeConsentimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cidade",
                schema: "public",
                table: "estabelecimentos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                schema: "public",
                table: "estabelecimentos",
                type: "character(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "termo_audit_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entidade_id = table.Column<long>(type: "bigint", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_termo_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "termo_emitido",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    termo_modelo_id = table.Column<long>(type: "bigint", nullable: false),
                    versao_modelo = table.Column<int>(type: "integer", nullable: false),
                    conteudo_snapshot_html = table.Column<string>(type: "text", nullable: false),
                    conteudo_snapshot_texto = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assinatura_tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assinado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_assinatura = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent_assinatura = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hash_integridade = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    pdf_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pdf_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    token_aceite = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    token_expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revogado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revogado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revogado_motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    emitido_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_termo_emitido", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "termo_emitido_acesso_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    termo_emitido_id = table.Column<long>(type: "bigint", nullable: false),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    acao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_termo_emitido_acesso_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "termo_modelo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    conteudo_html = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    versao_atual = table.Column<int>(type: "integer", nullable: false),
                    padrao_clonado_de = table.Column<long>(type: "bigint", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_termo_modelo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "termo_modelo_versao",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    termo_modelo_id = table.Column<long>(type: "bigint", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    conteudo_html = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_termo_modelo_versao", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_termo_audit_log_entidade_criado",
                schema: "public",
                table: "termo_audit_log",
                columns: new[] { "entidade", "entidade_id", "criado_em" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_termo_audit_log_estab_criado",
                schema: "public",
                table: "termo_audit_log",
                columns: new[] { "estabelecimento_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_termo_emitido_estab_status",
                schema: "public",
                table: "termo_emitido",
                columns: new[] { "estabelecimento_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_termo_emitido_paciente_estab_criado",
                schema: "public",
                table: "termo_emitido",
                columns: new[] { "paciente_id", "estabelecimento_id", "criado_em" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "uq_termo_emitido_token",
                schema: "public",
                table: "termo_emitido",
                column: "token_aceite",
                unique: true,
                filter: "token_aceite IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_termo_emitido_acesso_log_termo_criado",
                schema: "public",
                table: "termo_emitido_acesso_log",
                columns: new[] { "termo_emitido_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_termo_modelo_estab_cat_ativo",
                schema: "public",
                table: "termo_modelo",
                columns: new[] { "estabelecimento_id", "categoria", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_termo_modelo_estab_deletado",
                schema: "public",
                table: "termo_modelo",
                columns: new[] { "estabelecimento_id", "deletado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_termo_modelo_padrao_categoria",
                schema: "public",
                table: "termo_modelo",
                column: "categoria",
                filter: "estabelecimento_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_termo_modelo_versao",
                schema: "public",
                table: "termo_modelo_versao",
                columns: new[] { "termo_modelo_id", "versao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "termo_audit_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "termo_emitido",
                schema: "public");

            migrationBuilder.DropTable(
                name: "termo_emitido_acesso_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "termo_modelo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "termo_modelo_versao",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "cidade",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.DropColumn(
                name: "estado",
                schema: "public",
                table: "estabelecimentos");
        }
    }
}

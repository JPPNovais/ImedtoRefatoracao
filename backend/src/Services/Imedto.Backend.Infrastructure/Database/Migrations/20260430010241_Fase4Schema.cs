using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase4Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requer_retencao",
                schema: "public",
                table: "receitas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "anonimizado_em",
                schema: "public",
                table: "pacientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "anonimizado_por_usuario_id",
                schema: "public",
                table: "pacientes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalogo_procedimentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capitulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogo_procedimentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lgpd_anonimizacoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tabela = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    registro_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    anonimizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    executado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lgpd_anonimizacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lgpd_consentimentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    versao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aceito_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lgpd_consentimentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "solicitacoes_vinculo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    respondida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    respondida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_recusa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitacoes_vinculo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_procedimentos_ativo_origem",
                schema: "public",
                table: "catalogo_procedimentos",
                columns: new[] { "ativo", "origem" });

            migrationBuilder.CreateIndex(
                name: "uq_catalogo_procedimentos_codigo",
                schema: "public",
                table: "catalogo_procedimentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lgpd_anonimizacoes_motivo_data",
                schema: "public",
                table: "lgpd_anonimizacoes",
                columns: new[] { "motivo", "anonimizado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_lgpd_anonimizacoes_tabela_registro",
                schema: "public",
                table: "lgpd_anonimizacoes",
                columns: new[] { "tabela", "registro_id" });

            migrationBuilder.CreateIndex(
                name: "ix_lgpd_consentimentos_usuario_tipo_data",
                schema: "public",
                table: "lgpd_consentimentos",
                columns: new[] { "usuario_id", "tipo", "aceito_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_estab_status_data",
                schema: "public",
                table: "solicitacoes_vinculo",
                columns: new[] { "estabelecimento_id", "status", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_profissional_status",
                schema: "public",
                table: "solicitacoes_vinculo",
                columns: new[] { "profissional_usuario_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_solicitacoes_vinculo_pendente",
                schema: "public",
                table: "solicitacoes_vinculo",
                columns: new[] { "profissional_usuario_id", "estabelecimento_id" },
                unique: true,
                filter: "status = 'Pendente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogo_procedimentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lgpd_anonimizacoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lgpd_consentimentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "solicitacoes_vinculo",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "requer_retencao",
                schema: "public",
                table: "receitas");

            migrationBuilder.DropColumn(
                name: "anonimizado_em",
                schema: "public",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "anonimizado_por_usuario_id",
                schema: "public",
                table: "pacientes");
        }
    }
}

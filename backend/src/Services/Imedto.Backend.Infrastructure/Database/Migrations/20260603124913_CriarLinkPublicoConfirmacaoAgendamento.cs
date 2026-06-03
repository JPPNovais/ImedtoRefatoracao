using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarLinkPublicoConfirmacaoAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Contexto: as tabelas imedto_*_global foram dropadas via SQL direto em
            // 20260530200000_drop_catalogos_globais_wave2.sql e não existem no banco.
            // As tabelas assinatura_certificados e assinatura_audit_log foram criadas via SQL
            // direto em 20260601120000_criar_assinatura_digital.sql e já existem no banco.
            // Esta migration adiciona APENAS o schema da Fase 2 (link público de confirmação).

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmado_por_link_em",
                schema: "public",
                table: "agendamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "token_confirmacao",
                schema: "public",
                table: "agendamentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "token_confirmacao_expira_em",
                schema: "public",
                table: "agendamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agendamento_confirmacao_acesso_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agendamento_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    acao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    acessado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamento_confirmacao_acesso_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_agendamento_confirmacao_acesso_log_agendamento",
                        column: x => x.agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_agendamento_confirmacao_acesso_log_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_agendamentos_token_confirmacao",
                schema: "public",
                table: "agendamentos",
                column: "token_confirmacao",
                unique: true,
                filter: "token_confirmacao IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_confirmacao_acesso_log_agendamento_acessado",
                schema: "public",
                table: "agendamento_confirmacao_acesso_log",
                columns: new[] { "agendamento_id", "acessado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_confirmacao_acesso_log_estabelecimento",
                schema: "public",
                table: "agendamento_confirmacao_acesso_log",
                column: "estabelecimento_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agendamento_confirmacao_acesso_log",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "uq_agendamentos_token_confirmacao",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "confirmado_por_link_em",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "token_confirmacao",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "token_confirmacao_expira_em",
                schema: "public",
                table: "agendamentos");
        }
    }
}

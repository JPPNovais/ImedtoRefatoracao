using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase2Wave234Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "evolucao_id",
                schema: "public",
                table: "ai_audit_logs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "paciente_id",
                schema: "public",
                table: "ai_audit_logs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "prontuario_id",
                schema: "public",
                table: "ai_audit_logs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "automation_rules",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    evento_gatilho = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    condicoes_json = table.Column<string>(type: "jsonb", nullable: false),
                    acoes_json = table.Column<string>(type: "jsonb", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "establishment_ai_settings",
                schema: "public",
                columns: table => new
                {
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    ai_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ai_provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "anthropic"),
                    ai_model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "claude-sonnet-4-6"),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    rate_limit_per_day = table.Column<int>(type: "integer", nullable: false, defaultValue: 200),
                    data_minimization_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "standard"),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishment_ai_settings", x => x.estabelecimento_id);
                    table.ForeignKey(
                        name: "FK_establishment_ai_settings_estabelecimentos_estabelecimento_~",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jobs_agendados",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    proximo_run_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ultimo_run_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    intervalo_seg = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ultima_falha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs_agendados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notificacoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    link_acao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lida = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    preco_mensal = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    limite_profissionais = table.Column<int>(type: "integer", nullable: true),
                    limite_pacientes = table.Column<int>(type: "integer", nullable: true),
                    features_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_events",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    regra_id = table.Column<long>(type: "bigint", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativa_n = table.Column<int>(type: "integer", nullable: false),
                    executar_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    executado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultima_falha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_automation_events_regra",
                        column: x => x.regra_id,
                        principalSchema: "public",
                        principalTable: "automation_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assinaturas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    plano_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    iniciada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    renovada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinaturas", x => x.id);
                    table.ForeignKey(
                        name: "FK_assinaturas_estabelecimentos_estabelecimento_id",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assinaturas_planos_plano_id",
                        column: x => x.plano_id,
                        principalSchema: "public",
                        principalTable: "planos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assinaturas_plano_id",
                schema: "public",
                table: "assinaturas",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_status_expira",
                schema: "public",
                table: "assinaturas",
                columns: new[] { "status", "expira_em" });

            migrationBuilder.CreateIndex(
                name: "uq_assinaturas_estabelecimento",
                schema: "public",
                table: "assinaturas",
                column: "estabelecimento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_automation_events_regra_id",
                schema: "public",
                table: "automation_events",
                column: "regra_id");

            migrationBuilder.CreateIndex(
                name: "ix_automation_events_status_executar_em",
                schema: "public",
                table: "automation_events",
                columns: new[] { "status", "executar_em" });

            migrationBuilder.CreateIndex(
                name: "ix_automation_rules_estab_evento_ativa",
                schema: "public",
                table: "automation_rules",
                columns: new[] { "estabelecimento_id", "evento_gatilho", "ativa" });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_agendados_status_proximo_run",
                schema: "public",
                table: "jobs_agendados",
                columns: new[] { "status", "proximo_run_em" });

            migrationBuilder.CreateIndex(
                name: "uq_jobs_agendados_nome",
                schema: "public",
                table: "jobs_agendados",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notificacoes_estabelecimento_criada",
                schema: "public",
                table: "notificacoes",
                columns: new[] { "estabelecimento_id", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_notificacoes_usuario_lida_criada",
                schema: "public",
                table: "notificacoes",
                columns: new[] { "usuario_id", "lida", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "uq_planos_nome",
                schema: "public",
                table: "planos",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assinaturas",
                schema: "public");

            migrationBuilder.DropTable(
                name: "automation_events",
                schema: "public");

            migrationBuilder.DropTable(
                name: "establishment_ai_settings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "jobs_agendados",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notificacoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "planos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "automation_rules",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "evolucao_id",
                schema: "public",
                table: "ai_audit_logs");

            migrationBuilder.DropColumn(
                name: "paciente_id",
                schema: "public",
                table: "ai_audit_logs");

            migrationBuilder.DropColumn(
                name: "prontuario_id",
                schema: "public",
                table: "ai_audit_logs");
        }
    }
}

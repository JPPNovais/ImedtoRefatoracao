using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AssinaturasF1VigenciaSuspensaoConfigTrial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "features_json",
                schema: "public",
                table: "imedto_planos",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expira_em",
                schema: "public",
                table: "imedto_assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origem",
                schema: "public",
                table: "imedto_assinaturas",
                type: "text",
                nullable: false,
                defaultValue: "admin_manual");

            migrationBuilder.AddColumn<string>(
                name: "referencia_externa",
                schema: "public",
                table: "imedto_assinaturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_cobranca",
                schema: "public",
                table: "imedto_assinaturas",
                type: "text",
                nullable: false,
                defaultValue: "nao_aplicavel");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "suspensa_em",
                schema: "public",
                table: "imedto_assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "imedto_config_trial",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_trial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duracao_trial_dias = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    trial_habilitado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_config_trial", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_config_trial_imedto_planos_plano_trial_id",
                        column: x => x.plano_trial_id,
                        principalSchema: "public",
                        principalTable: "imedto_planos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_assinaturas_expira_em",
                schema: "public",
                table: "imedto_assinaturas",
                column: "expira_em");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_config_trial_plano_trial_id",
                schema: "public",
                table: "imedto_config_trial",
                column: "plano_trial_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imedto_config_trial",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_imedto_assinaturas_expira_em",
                schema: "public",
                table: "imedto_assinaturas");

            migrationBuilder.DropColumn(
                name: "features_json",
                schema: "public",
                table: "imedto_planos");

            migrationBuilder.DropColumn(
                name: "expira_em",
                schema: "public",
                table: "imedto_assinaturas");

            migrationBuilder.DropColumn(
                name: "origem",
                schema: "public",
                table: "imedto_assinaturas");

            migrationBuilder.DropColumn(
                name: "referencia_externa",
                schema: "public",
                table: "imedto_assinaturas");

            migrationBuilder.DropColumn(
                name: "status_cobranca",
                schema: "public",
                table: "imedto_assinaturas");

            migrationBuilder.DropColumn(
                name: "suspensa_em",
                schema: "public",
                table: "imedto_assinaturas");
        }
    }
}

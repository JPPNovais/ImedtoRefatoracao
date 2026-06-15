using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelasMigracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "migracao_templates",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "varchar(200)", nullable: false),
                    entidade = table.Column<string>(type: "varchar(100)", nullable: false),
                    mapa_json = table.Column<string>(type: "jsonb", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migracao_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "migracao_jobs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", nullable: false),
                    origem = table.Column<string>(type: "varchar(200)", nullable: true),
                    arquivo_s3_key = table.Column<string>(type: "varchar(500)", nullable: true),
                    arquivo_expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arquivo_expirado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    termo_aceito_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    template_origem_id = table.Column<long>(type: "bigint", nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    disparado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migracao_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_migracao_jobs_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_migracao_jobs_template_origem",
                        column: x => x.template_origem_id,
                        principalSchema: "public",
                        principalTable: "migracao_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "migracao_mapas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    migracao_job_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    entidade = table.Column<string>(type: "varchar(100)", nullable: false),
                    mapa_json = table.Column<string>(type: "jsonb", nullable: false),
                    revisado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revisado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migracao_mapas", x => x.id);
                    table.ForeignKey(
                        name: "fk_migracao_mapas_job",
                        column: x => x.migracao_job_id,
                        principalSchema: "public",
                        principalTable: "migracao_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "migracao_registros",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    migracao_job_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    entidade = table.Column<string>(type: "varchar(100)", nullable: false),
                    payload_bruto = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "pendente"),
                    motivo_rejeicao = table.Column<string>(type: "text", nullable: true),
                    entidade_alvo_id = table.Column<long>(type: "bigint", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migracao_registros", x => x.id);
                    table.ForeignKey(
                        name: "fk_migracao_registros_job",
                        column: x => x.migracao_job_id,
                        principalSchema: "public",
                        principalTable: "migracao_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_migracao_jobs_arquivo_expira_em",
                schema: "public",
                table: "migracao_jobs",
                column: "arquivo_expira_em");

            migrationBuilder.CreateIndex(
                name: "ix_migracao_jobs_criado_por_usuario_id",
                schema: "public",
                table: "migracao_jobs",
                column: "criado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_migracao_jobs_estab_status",
                schema: "public",
                table: "migracao_jobs",
                columns: new[] { "estabelecimento_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_migracao_jobs_template_origem_id",
                schema: "public",
                table: "migracao_jobs",
                column: "template_origem_id");

            migrationBuilder.CreateIndex(
                name: "uq_migracao_mapas_job_entidade",
                schema: "public",
                table: "migracao_mapas",
                columns: new[] { "migracao_job_id", "entidade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_migracao_registros_estab_entidade",
                schema: "public",
                table: "migracao_registros",
                columns: new[] { "estabelecimento_id", "entidade" });

            migrationBuilder.CreateIndex(
                name: "ix_migracao_registros_job_id",
                schema: "public",
                table: "migracao_registros",
                column: "migracao_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_migracao_registros_job_status",
                schema: "public",
                table: "migracao_registros",
                columns: new[] { "migracao_job_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_migracao_templates_nome_entidade",
                schema: "public",
                table: "migracao_templates",
                columns: new[] { "nome", "entidade" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "migracao_mapas",
                schema: "public");

            migrationBuilder.DropTable(
                name: "migracao_registros",
                schema: "public");

            migrationBuilder.DropTable(
                name: "migracao_jobs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "migracao_templates",
                schema: "public");
        }
    }
}

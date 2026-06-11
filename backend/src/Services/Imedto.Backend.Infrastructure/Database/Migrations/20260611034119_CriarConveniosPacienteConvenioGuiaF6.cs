using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarConveniosPacienteConvenioGuiaF6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "guia_autorizada_em",
                schema: "public",
                table: "cobrancas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guia_numero",
                schema: "public",
                table: "cobrancas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guia_senha",
                schema: "public",
                table: "cobrancas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "convenios",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    registro_ans = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convenios", x => x.id);
                    table.ForeignKey(
                        name: "fk_convenios_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "convenio_planos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    convenio_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convenio_planos", x => x.id);
                    table.ForeignKey(
                        name: "fk_convenio_planos_convenio",
                        column: x => x.convenio_id,
                        principalSchema: "public",
                        principalTable: "convenios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_convenio_planos_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paciente_convenios",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    convenio_id = table.Column<long>(type: "bigint", nullable: false),
                    plano_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_carteirinha = table.Column<string>(type: "text", nullable: false),
                    validade = table.Column<DateOnly>(type: "date", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente_convenios", x => x.id);
                    table.ForeignKey(
                        name: "fk_paciente_convenios_convenio",
                        column: x => x.convenio_id,
                        principalSchema: "public",
                        principalTable: "convenios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paciente_convenios_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paciente_convenios_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_convenio_id",
                schema: "public",
                table: "cobrancas",
                column: "convenio_id",
                filter: "convenio_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_convenio_planos_convenio_ativo",
                schema: "public",
                table: "convenio_planos",
                columns: new[] { "convenio_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_convenio_planos_estabelecimento_id",
                schema: "public",
                table: "convenio_planos",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_convenios_estab_ativo",
                schema: "public",
                table: "convenios",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_paciente_convenios_convenio_id",
                schema: "public",
                table: "paciente_convenios",
                column: "convenio_id");

            migrationBuilder.CreateIndex(
                name: "ix_paciente_convenios_estabelecimento_id",
                schema: "public",
                table: "paciente_convenios",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_paciente_convenios_paciente_estab_ativo",
                schema: "public",
                table: "paciente_convenios",
                columns: new[] { "paciente_id", "estabelecimento_id", "ativo" });

            migrationBuilder.AddForeignKey(
                name: "fk_cobrancas_convenio",
                schema: "public",
                table: "cobrancas",
                column: "convenio_id",
                principalSchema: "public",
                principalTable: "convenios",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cobrancas_convenio",
                schema: "public",
                table: "cobrancas");

            migrationBuilder.DropTable(
                name: "convenio_planos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "paciente_convenios",
                schema: "public");

            migrationBuilder.DropTable(
                name: "convenios",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_cobrancas_convenio_id",
                schema: "public",
                table: "cobrancas");

            migrationBuilder.DropColumn(
                name: "guia_autorizada_em",
                schema: "public",
                table: "cobrancas");

            migrationBuilder.DropColumn(
                name: "guia_numero",
                schema: "public",
                table: "cobrancas");

            migrationBuilder.DropColumn(
                name: "guia_senha",
                schema: "public",
                table: "cobrancas");
        }
    }
}

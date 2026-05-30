using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarCatalogosGlobaisWave2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "secao",
                schema: "public",
                table: "imedto_config",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                schema: "public",
                table: "imedto_config",
                type: "text",
                nullable: false,
                defaultValue: "texto");

            migrationBuilder.CreateTable(
                name: "imedto_modelo_prontuario_global",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    conteudo_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_modelo_prontuario_global", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_modelo_prontuario_global_imedto_admins_atualizado_po~",
                        column: x => x.atualizado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_imedto_modelo_prontuario_global_imedto_admins_criado_por_ad~",
                        column: x => x.criado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "imedto_regiao_anatomica_global",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    sinonimos = table.Column<string[]>(type: "text[]", nullable: true),
                    sistema_corporal = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_regiao_anatomica_global", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "imedto_variavel_pool_global",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    valores_json = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb"),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_por_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imedto_variavel_pool_global", x => x.id);
                    table.ForeignKey(
                        name: "FK_imedto_variavel_pool_global_imedto_admins_atualizado_por_ad~",
                        column: x => x.atualizado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_imedto_variavel_pool_global_imedto_admins_criado_por_admin_~",
                        column: x => x.criado_por_admin_id,
                        principalSchema: "public",
                        principalTable: "imedto_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_config_secao_chave",
                schema: "public",
                table: "imedto_config",
                columns: new[] { "secao", "chave" });

            migrationBuilder.CreateIndex(
                name: "ix_imedto_modelo_prontuario_global_ativo_nome",
                schema: "public",
                table: "imedto_modelo_prontuario_global",
                columns: new[] { "ativo", "nome" });

            migrationBuilder.CreateIndex(
                name: "IX_imedto_modelo_prontuario_global_atualizado_por_admin_id",
                schema: "public",
                table: "imedto_modelo_prontuario_global",
                column: "atualizado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_modelo_prontuario_global_criado_por_admin_id",
                schema: "public",
                table: "imedto_modelo_prontuario_global",
                column: "criado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "uq_imedto_modelo_prontuario_global_nome_lower",
                schema: "public",
                table: "imedto_modelo_prontuario_global",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_imedto_regiao_anatomica_global_ativo_sistema_nome",
                schema: "public",
                table: "imedto_regiao_anatomica_global",
                columns: new[] { "ativo", "sistema_corporal", "nome" });

            migrationBuilder.CreateIndex(
                name: "uq_imedto_regiao_anatomica_global_nome_lower",
                schema: "public",
                table: "imedto_regiao_anatomica_global",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_imedto_variavel_pool_global_ativo_tipo_nome",
                schema: "public",
                table: "imedto_variavel_pool_global",
                columns: new[] { "ativo", "tipo", "nome" });

            migrationBuilder.CreateIndex(
                name: "IX_imedto_variavel_pool_global_atualizado_por_admin_id",
                schema: "public",
                table: "imedto_variavel_pool_global",
                column: "atualizado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_imedto_variavel_pool_global_criado_por_admin_id",
                schema: "public",
                table: "imedto_variavel_pool_global",
                column: "criado_por_admin_id");

            migrationBuilder.CreateIndex(
                name: "uq_imedto_variavel_pool_global_nome_lower",
                schema: "public",
                table: "imedto_variavel_pool_global",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imedto_modelo_prontuario_global",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_regiao_anatomica_global",
                schema: "public");

            migrationBuilder.DropTable(
                name: "imedto_variavel_pool_global",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_imedto_config_secao_chave",
                schema: "public",
                table: "imedto_config");

            migrationBuilder.DropColumn(
                name: "secao",
                schema: "public",
                table: "imedto_config");

            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "public",
                table: "imedto_config");
        }
    }
}

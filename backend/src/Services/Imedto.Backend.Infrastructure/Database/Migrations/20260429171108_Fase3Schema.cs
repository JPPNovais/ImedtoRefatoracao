using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fase3Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "config_pagamento_json",
                schema: "public",
                table: "orcamentos",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "custo_implantes_total",
                schema: "public",
                table: "orcamentos",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "procedimento_cirurgico_id",
                schema: "public",
                table: "orcamentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                schema: "public",
                table: "orcamentos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Simples");

            // Item 3.4 — permissoes_extras adicionado manualmente: o snapshot anterior
            // ja referenciava a propriedade no modelo, entao o EF nao detectou o diff.
            // Sem este ALTER a migration nao cria a coluna em ambientes novos.
            migrationBuilder.AddColumn<string>(
                name: "permissoes_extras",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateTable(
                name: "exame_fisico",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    evolucao_id = table.Column<long>(type: "bigint", nullable: false),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    realizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    realizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dados_gerais_json = table.Column<string>(type: "jsonb", nullable: true),
                    observacoes_gerais = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exame_fisico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medicamentos_favoritos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    medicamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posologia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    via_administracao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    uso_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ultimo_uso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicamentos_favoritos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_equipe",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    papel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_equipe", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_equipe_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_formas_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    forma_pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    parcelas = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_formas_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_forma_pagamento_forma",
                        column: x => x.forma_pagamento_id,
                        principalSchema: "public",
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamento_forma_pagamento_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_implantes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    item_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    custo_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_implantes", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_implante_item_inventario",
                        column: x => x.item_inventario_id,
                        principalSchema: "public",
                        principalTable: "itens_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_orcamento_implante_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedimentos_cirurgicos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    agendamento_id = table.Column<long>(type: "bigint", nullable: true),
                    data_agendada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_realizada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cirurgia_principal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cirurgia_codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    descricao_cirurgica = table.Column<string>(type: "text", nullable: true),
                    ficha_anestesica = table.Column<string>(type: "jsonb", nullable: true),
                    evolucao_pos_op = table.Column<string>(type: "text", nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cancelado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedimentos_cirurgicos", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedimento_agendamento",
                        column: x => x.agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_procedimento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedimento_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedimento_prontuario",
                        column: x => x.prontuario_id,
                        principalSchema: "public",
                        principalTable: "prontuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receitas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    emitida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    validade_ate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cancelada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receitas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receitas_configuracao_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    cabecalho_html = table.Column<string>(type: "text", nullable: true),
                    rodape_html = table.Column<string>(type: "text", nullable: true),
                    modelo_padrao_id = table.Column<long>(type: "bigint", nullable: true),
                    emissor_padrao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receitas_configuracao_estabelecimento", x => x.estabelecimento_id);
                });

            migrationBuilder.CreateTable(
                name: "exame_fisico_regioes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exame_fisico_id = table.Column<long>(type: "bigint", nullable: false),
                    regiao_codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    regiao_pai_codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    lateralidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    achados = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    severidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exame_fisico_regioes", x => x.id);
                    table.ForeignKey(
                        name: "FK_exame_fisico_regioes_exame_fisico_exame_fisico_id",
                        column: x => x.exame_fisico_id,
                        principalSchema: "public",
                        principalTable: "exame_fisico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "equipe_cirurgica",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    procedimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    papel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipe_cirurgica", x => x.id);
                    table.ForeignKey(
                        name: "fk_membro_equipe_cirurgica_procedimento",
                        column: x => x.procedimento_id,
                        principalSchema: "public",
                        principalTable: "procedimentos_cirurgicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receita_itens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receita_id = table.Column<long>(type: "bigint", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    medicamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posologia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    via_administracao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receita_itens", x => x.id);
                    table.ForeignKey(
                        name: "FK_receita_itens_receitas_receita_id",
                        column: x => x.receita_id,
                        principalSchema: "public",
                        principalTable: "receitas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_procedimento_cirurgico",
                schema: "public",
                table: "orcamentos",
                column: "procedimento_cirurgico_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipe_cirurgica_procedimento_papel",
                schema: "public",
                table: "equipe_cirurgica",
                columns: new[] { "procedimento_id", "papel" });

            migrationBuilder.CreateIndex(
                name: "uq_equipe_cirurgica_procedimento_profissional_papel",
                schema: "public",
                table: "equipe_cirurgica",
                columns: new[] { "procedimento_id", "profissional_usuario_id", "papel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_estabelecimento",
                schema: "public",
                table: "exame_fisico",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_evolucao",
                schema: "public",
                table: "exame_fisico",
                column: "evolucao_id");

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_paciente_realizado",
                schema: "public",
                table: "exame_fisico",
                columns: new[] { "paciente_id", "realizado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_prontuario_realizado",
                schema: "public",
                table: "exame_fisico",
                columns: new[] { "prontuario_id", "realizado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_exame_fisico_regiao_codigo",
                schema: "public",
                table: "exame_fisico_regioes",
                columns: new[] { "exame_fisico_id", "regiao_codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medicamentos_favoritos_ranking",
                schema: "public",
                table: "medicamentos_favoritos",
                columns: new[] { "profissional_usuario_id", "estabelecimento_id", "uso_count" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "uq_medicamentos_favoritos_chave",
                schema: "public",
                table: "medicamentos_favoritos",
                columns: new[] { "profissional_usuario_id", "estabelecimento_id", "medicamento", "posologia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_orcamento_equipe_orcamento_profissional_papel",
                schema: "public",
                table: "orcamento_equipe",
                columns: new[] { "orcamento_id", "profissional_usuario_id", "papel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_forma_pagamento_orcamento",
                schema: "public",
                table: "orcamento_formas_pagamento",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_formas_pagamento_forma_pagamento_id",
                schema: "public",
                table: "orcamento_formas_pagamento",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_implante_orcamento",
                schema: "public",
                table: "orcamento_implantes",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_implantes_item_inventario_id",
                schema: "public",
                table: "orcamento_implantes",
                column: "item_inventario_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedimento_estab_data_agendada",
                schema: "public",
                table: "procedimentos_cirurgicos",
                columns: new[] { "estabelecimento_id", "data_agendada" });

            migrationBuilder.CreateIndex(
                name: "ix_procedimento_paciente_data_realizada",
                schema: "public",
                table: "procedimentos_cirurgicos",
                columns: new[] { "paciente_id", "data_realizada" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_procedimentos_cirurgicos_agendamento_id",
                schema: "public",
                table: "procedimentos_cirurgicos",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_procedimentos_cirurgicos_prontuario_id",
                schema: "public",
                table: "procedimentos_cirurgicos",
                column: "prontuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_receita_itens_receita_ordem",
                schema: "public",
                table: "receita_itens",
                columns: new[] { "receita_id", "ordem" });

            migrationBuilder.CreateIndex(
                name: "ix_receitas_estab_prof_emitida",
                schema: "public",
                table: "receitas",
                columns: new[] { "estabelecimento_id", "profissional_usuario_id", "emitida_em" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_receitas_paciente_emitida",
                schema: "public",
                table: "receitas",
                columns: new[] { "paciente_id", "emitida_em" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "fk_orcamento_procedimento_cirurgico",
                schema: "public",
                table: "orcamentos",
                column: "procedimento_cirurgico_id",
                principalSchema: "public",
                principalTable: "procedimentos_cirurgicos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orcamento_procedimento_cirurgico",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropTable(
                name: "equipe_cirurgica",
                schema: "public");

            migrationBuilder.DropTable(
                name: "exame_fisico_regioes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "medicamentos_favoritos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_equipe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_formas_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_implantes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receita_itens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receitas_configuracao_estabelecimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "procedimentos_cirurgicos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "exame_fisico",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receitas",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_orcamento_procedimento_cirurgico",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "config_pagamento_json",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "custo_implantes_total",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "procedimento_cirurgico_id",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "public",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "permissoes_extras",
                schema: "public",
                table: "modelo_permissao_estabelecimento");
        }
    }
}

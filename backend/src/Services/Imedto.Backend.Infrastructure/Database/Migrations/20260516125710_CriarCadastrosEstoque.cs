using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CriarCadastrosEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Tabelas mestre.
            migrationBuilder.CreateTable(
                name: "categorias_estoque",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    icone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_categorias_estoque_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fabricantes_estoque",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    pais = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fabricantes_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_fabricantes_estoque_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fornecedores_estoque",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    razao_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    contato_nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    contato_telefone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    contato_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    prazo_entrega_dias = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fornecedores_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_fornecedores_estoque_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locais_estoque",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    andar_setor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    responsavel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locais_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_locais_estoque_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 2) Colunas novas em itens_inventario — categoria_id começa NULLABLE
            //    para que a data migration popule antes do NOT NULL final.
            migrationBuilder.AddColumn<long>(
                name: "categoria_id",
                schema: "public",
                table: "itens_inventario",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "fabricante_id",
                schema: "public",
                table: "itens_inventario",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "fornecedor_padrao_id",
                schema: "public",
                table: "itens_inventario",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "local_padrao_id",
                schema: "public",
                table: "itens_inventario",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "custo_unitario",
                schema: "public",
                table: "itens_inventario",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            // 3) Data migration: cria 1 CategoriaEstoque por (estab, categoria text) distinto
            //    em itens existentes, com cor azul default e ícone fa-tag.
            //    Itens com categoria vazia ou nula viram "Sem categoria".
            migrationBuilder.Sql(@"
                UPDATE public.itens_inventario
                   SET categoria = 'Sem categoria'
                 WHERE COALESCE(NULLIF(trim(categoria), ''), '') = '';

                INSERT INTO public.categorias_estoque (estabelecimento_id, nome, cor, icone, ativo, criado_em)
                SELECT DISTINCT i.estabelecimento_id, i.categoria, 'hsl(218 70% 50%)', 'fa-tag', true, now()
                  FROM public.itens_inventario i
                 WHERE NOT EXISTS (
                        SELECT 1 FROM public.categorias_estoque c
                         WHERE c.estabelecimento_id = i.estabelecimento_id
                           AND lower(c.nome) = lower(i.categoria)
                       );

                UPDATE public.itens_inventario i
                   SET categoria_id = c.id
                  FROM public.categorias_estoque c
                 WHERE c.estabelecimento_id = i.estabelecimento_id
                   AND lower(c.nome) = lower(i.categoria)
                   AND i.categoria_id IS NULL;
            ");

            // 4) Aplica NOT NULL agora que categoria_id está preenchido em todos os itens.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public.itens_inventario WHERE categoria_id IS NULL) THEN
                        RAISE EXCEPTION 'Migration falhou: existem itens_inventario com categoria_id NULL após data migration.';
                    END IF;
                END $$;
            ");

            migrationBuilder.AlterColumn<long>(
                name: "categoria_id",
                schema: "public",
                table: "itens_inventario",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            // 5) Índices e FKs.
            migrationBuilder.CreateIndex(
                name: "ix_inventario_estab_categoria",
                schema: "public",
                table: "itens_inventario",
                columns: new[] { "estabelecimento_id", "categoria_id" });

            migrationBuilder.CreateIndex(
                name: "IX_itens_inventario_categoria_id",
                schema: "public",
                table: "itens_inventario",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_inventario_fabricante_id",
                schema: "public",
                table: "itens_inventario",
                column: "fabricante_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_inventario_fornecedor_padrao_id",
                schema: "public",
                table: "itens_inventario",
                column: "fornecedor_padrao_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_inventario_local_padrao_id",
                schema: "public",
                table: "itens_inventario",
                column: "local_padrao_id");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_estoque_estab_ativo",
                schema: "public",
                table: "categorias_estoque",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_fabricantes_estoque_estab_ativo",
                schema: "public",
                table: "fabricantes_estoque",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_fornecedores_estoque_estab_ativo",
                schema: "public",
                table: "fornecedores_estoque",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_locais_estoque_estab_ativo",
                schema: "public",
                table: "locais_estoque",
                columns: new[] { "estabelecimento_id", "ativo" });

            // Unicidade case-insensitive por estabelecimento (índices em lower(nome)).
            // EF não tem suporte nativo a expression index — feito via Sql.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS uq_categorias_estoque_estab_nome    ON public.categorias_estoque   (estabelecimento_id, lower(nome));
                CREATE UNIQUE INDEX IF NOT EXISTS uq_fabricantes_estoque_estab_nome   ON public.fabricantes_estoque  (estabelecimento_id, lower(nome));
                CREATE UNIQUE INDEX IF NOT EXISTS uq_fornecedores_estoque_estab_razao ON public.fornecedores_estoque (estabelecimento_id, lower(razao_social));
                CREATE UNIQUE INDEX IF NOT EXISTS uq_locais_estoque_estab_nome        ON public.locais_estoque       (estabelecimento_id, lower(nome));
                CREATE UNIQUE INDEX IF NOT EXISTS uq_fornecedores_estoque_estab_cnpj  ON public.fornecedores_estoque (estabelecimento_id, cnpj) WHERE cnpj IS NOT NULL;
            ");

            migrationBuilder.AddForeignKey(
                name: "fk_inventario_categoria",
                schema: "public",
                table: "itens_inventario",
                column: "categoria_id",
                principalSchema: "public",
                principalTable: "categorias_estoque",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventario_fabricante",
                schema: "public",
                table: "itens_inventario",
                column: "fabricante_id",
                principalSchema: "public",
                principalTable: "fabricantes_estoque",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventario_fornecedor_padrao",
                schema: "public",
                table: "itens_inventario",
                column: "fornecedor_padrao_id",
                principalSchema: "public",
                principalTable: "fornecedores_estoque",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventario_local_padrao",
                schema: "public",
                table: "itens_inventario",
                column: "local_padrao_id",
                principalSchema: "public",
                principalTable: "locais_estoque",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventario_categoria",
                schema: "public",
                table: "itens_inventario");

            migrationBuilder.DropForeignKey(
                name: "fk_inventario_fabricante",
                schema: "public",
                table: "itens_inventario");

            migrationBuilder.DropForeignKey(
                name: "fk_inventario_fornecedor_padrao",
                schema: "public",
                table: "itens_inventario");

            migrationBuilder.DropForeignKey(
                name: "fk_inventario_local_padrao",
                schema: "public",
                table: "itens_inventario");

            migrationBuilder.DropTable(name: "categorias_estoque", schema: "public");
            migrationBuilder.DropTable(name: "fabricantes_estoque", schema: "public");
            migrationBuilder.DropTable(name: "fornecedores_estoque", schema: "public");
            migrationBuilder.DropTable(name: "locais_estoque", schema: "public");

            migrationBuilder.DropIndex(name: "ix_inventario_estab_categoria",                  schema: "public", table: "itens_inventario");
            migrationBuilder.DropIndex(name: "IX_itens_inventario_categoria_id",               schema: "public", table: "itens_inventario");
            migrationBuilder.DropIndex(name: "IX_itens_inventario_fabricante_id",              schema: "public", table: "itens_inventario");
            migrationBuilder.DropIndex(name: "IX_itens_inventario_fornecedor_padrao_id",       schema: "public", table: "itens_inventario");
            migrationBuilder.DropIndex(name: "IX_itens_inventario_local_padrao_id",            schema: "public", table: "itens_inventario");

            migrationBuilder.DropColumn(name: "categoria_id",         schema: "public", table: "itens_inventario");
            migrationBuilder.DropColumn(name: "custo_unitario",       schema: "public", table: "itens_inventario");
            migrationBuilder.DropColumn(name: "fabricante_id",        schema: "public", table: "itens_inventario");
            migrationBuilder.DropColumn(name: "fornecedor_padrao_id", schema: "public", table: "itens_inventario");
            migrationBuilder.DropColumn(name: "local_padrao_id",      schema: "public", table: "itens_inventario");
        }
    }
}

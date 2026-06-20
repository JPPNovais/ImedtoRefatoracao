using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRegiaoMarcadorProntuarioAnexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DDL idempotente via SQL raw (gotcha: AddColumn cru não é idempotente —
            // o QA cria schema no mesmo Postgres da EC2 sem gravar __EFMigrationsHistory).
            migrationBuilder.Sql("""
                ALTER TABLE public.prontuario_anexos
                    ADD COLUMN IF NOT EXISTS marcador character varying(50) NULL,
                    ADD COLUMN IF NOT EXISTS regiao_anatomica character varying(200) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.prontuario_anexos
                    DROP COLUMN IF EXISTS regiao_anatomica,
                    DROP COLUMN IF EXISTS marcador;
                """);
        }
    }
}

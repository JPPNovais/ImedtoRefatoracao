using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AumentarTipoAcessoLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente via SQL raw — ALTER COLUMN TYPE é seguro de reexecutar (gotcha:
            // o QA aplica o schema no Postgres da EC2 fora do __EFMigrationsHistory).
            migrationBuilder.Sql("""
                ALTER TABLE public.paciente_acesso_log
                    ALTER COLUMN tipo_acesso TYPE character varying(50);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.paciente_acesso_log
                    ALTER COLUMN tipo_acesso TYPE character varying(20);
                """);
        }
    }
}

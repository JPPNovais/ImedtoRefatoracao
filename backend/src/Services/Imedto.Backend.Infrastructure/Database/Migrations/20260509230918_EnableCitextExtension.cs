using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnableCitextExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // citext está declarada como Npgsql:PostgresExtension no InitialCreate, mas o
            // CREATE EXTENSION IF NOT EXISTS citext ficou de fora da SQL gerada.
            // auth_credenciais.email usa citext → sem a extensão, todo login quebra com
            // "data type 'citext' could not be found in the types loaded by Npgsql".
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não derrubamos a extensão: outros objetos podem estar dependendo dela.
        }
    }
}

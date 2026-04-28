using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UniqueDonoEstabelecimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_estabelecimentos_dono",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.CreateIndex(
                name: "uq_estabelecimentos_dono",
                schema: "public",
                table: "estabelecimentos",
                column: "dono_usuario_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_estabelecimentos_dono",
                schema: "public",
                table: "estabelecimentos");

            migrationBuilder.CreateIndex(
                name: "ix_estabelecimentos_dono",
                schema: "public",
                table: "estabelecimentos",
                column: "dono_usuario_id");
        }
    }
}

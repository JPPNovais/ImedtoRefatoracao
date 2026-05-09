using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedProfissoesAdministrativas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Profissoes administrativas (sem conselho regulamentado).
            // Idempotente: ON CONFLICT DO NOTHING — uq_profissoes_nome impede duplicatas.
            migrationBuilder.Sql(@"
INSERT INTO public.profissoes (nome, conselho_sigla, ativo) VALUES
    ('Secretária',                    '', true),
    ('Auxiliar Administrativo',       '', true),
    ('Gerente / Coordenador(a)',      '', true),
    ('Financeiro',                    '', true)
ON CONFLICT (nome) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only: desfazer um seed pode apagar profissoes ja vinculadas a profissionais
            // ativos. Em rollback, manter as linhas — seguro pois nao ha mudanca de schema.
        }
    }
}

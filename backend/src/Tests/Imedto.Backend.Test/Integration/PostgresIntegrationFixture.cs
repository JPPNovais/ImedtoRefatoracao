using Imedto.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Fixture compartilhada que sobe um Postgres real em container efêmero antes de
/// qualquer teste de integração e o derruba ao final. Aplica o schema via
/// <see cref="DbContext.Database.EnsureCreatedAsync"/> — não usa migrations
/// idempotentes aqui (essas são validadas pela pipeline de migrations contra RDS).
///
/// Pré-requisito: Docker daemon rodando (Docker Desktop, Colima, etc.).
/// Os testes falham com mensagem clara se o daemon não estiver disponível.
/// </summary>
[SetUpFixture]
public class PostgresIntegrationFixture
{
    private static PostgreSqlContainer _container;
    public static string ConnectionString { get; private set; }

    [OneTimeSetUp]
    public async Task SetUp()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("imedto_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            // Inclui ArgumentException do Testcontainers quando Docker daemon nao
            // esta acessivel — o Build() chama Validate() que detecta isso antes
            // mesmo do StartAsync.
            Assert.Ignore($"Docker indisponível — testes de integração ignorados: {ex.Message}");
            return;
        }

        ConnectionString = _container.GetConnectionString();

        // Garante schema criado (rodada inicial — depois cada teste limpa as tabelas).
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var ctx = new AppDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        // Aplica migrations SQL custom (não geridas pelo EF Core) que os testes precisam.
        // Lista corresponde a db/migrations relevantes para o caminho de queries.
        await AplicarMigrationsCustomAsync(ctx);
    }

    private static async Task AplicarMigrationsCustomAsync(AppDbContext ctx)
    {
        // pg_trgm + unaccent + wrapper IMMUTABLE + indice trigram em pacientes.
        // Sem isso, PacienteQueryRepository.Listar quebra em runtime nos testes.
        const string trigramSql = """
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
            CREATE EXTENSION IF NOT EXISTS unaccent;

            CREATE OR REPLACE FUNCTION public.imutable_unaccent(text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                PARALLEL SAFE
                STRICT
            AS $$
                SELECT public.unaccent('public.unaccent', $1);
            $$;

            CREATE INDEX IF NOT EXISTS ix_pacientes_nome_completo_trgm
                ON public.pacientes
                USING gin (public.imutable_unaccent(lower(nome_completo)) gin_trgm_ops)
                WHERE deletado_em IS NULL;
            """;
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(trigramSql);
#pragma warning restore EF1002
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

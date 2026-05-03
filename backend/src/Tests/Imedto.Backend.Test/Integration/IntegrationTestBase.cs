using Imedto.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Base de testes de integração que reutiliza o Postgres efêmero do
/// <see cref="PostgresIntegrationFixture"/>. Cada teste deve sobrescrever
/// <see cref="TabelasParaTruncar"/> com a lista de tabelas que pertencem
/// ao seu cenário — evita TRUNCATE global desnecessário.
///
/// Pula automaticamente se Docker não estiver disponível (Assert.Ignore).
/// </summary>
public abstract class IntegrationTestBase
{
    protected DbContextOptions<AppDbContext> Options { get; private set; }

    /// <summary>
    /// Lista de tabelas (em ordem-livre) que serão TRUNCATE-adas antes de cada teste.
    /// Use CASCADE-friendly: o TRUNCATE já roda com CASCADE, então só liste as raízes
    /// que são realmente tocadas pelo teste.
    /// </summary>
    protected abstract string[] TabelasParaTruncar { get; }

    [SetUp]
    public async Task BaseSetUp()
    {
        if (string.IsNullOrEmpty(PostgresIntegrationFixture.ConnectionString))
            Assert.Ignore("Container Postgres nao subiu (Docker indisponivel).");

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresIntegrationFixture.ConnectionString)
            .Options;

        await using var ctx = new AppDbContext(Options);
        // Tabelas vem da subclasse (codigo confiavel), entao ExecuteSqlRawAsync com
        // interpolacao eh seguro neste contexto. EF1002 silenciado.
        var tabelas = string.Join(", ", TabelasParaTruncar);
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            $"TRUNCATE {tabelas} RESTART IDENTITY CASCADE;");
#pragma warning restore EF1002
    }

    /// <summary>Cria uma nova instância de <see cref="AppDbContext"/> ligada ao container.</summary>
    protected AppDbContext NewContext() => new(Options);
}

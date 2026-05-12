using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using NUnit.Framework;

namespace Imedto.Backend.IntegrationTest.Auth;

/// <summary>
/// Garante que <see cref="EfAuthEmailTokenRepository.ObterValidoPorHashAsync"/>
/// rejeita tokens já expirados ou consumidos. Bug histórico: o método retornava
/// qualquer token pelo hash, permitindo que tokens expirados de reset/confirmação
/// fossem reusados — burlando o TTL de 1h em RedefinirSenha (descoberto em QA
/// 2026-05-11, achado N6 do qa/REPORT-V2.md).
/// </summary>
[TestFixture]
public class AuthEmailTokenRepositoryTests
{
    private AppDbContext CriarDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"auth-tok-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(opts);
    }

    [Test]
    public async Task ObterValidoPorHashAsync_RetornaToken_QuandoValido()
    {
        await using var db = CriarDb();
        var repo = new EfAuthEmailTokenRepository(db);
        var token = AuthEmailToken.Emitir(
            Guid.NewGuid(), AuthEmailTokenTipo.ResetSenha,
            "HASH_VALIDO", DateTime.UtcNow.AddHours(1));
        await repo.AdicionarAsync(token);
        await db.SaveChangesAsync();

        var encontrado = await repo.ObterValidoPorHashAsync("HASH_VALIDO", AuthEmailTokenTipo.ResetSenha);

        Assert.That(encontrado, Is.Not.Null);
        Assert.That(encontrado.TokenHash, Is.EqualTo("HASH_VALIDO"));
    }

    [Test]
    public async Task ObterValidoPorHashAsync_RetornaNull_QuandoExpirado()
    {
        await using var db = CriarDb();
        var repo = new EfAuthEmailTokenRepository(db);

        // Cria com expira_em futuro (validação do construtor exige), depois empurra pro passado via reflexão.
        var token = AuthEmailToken.Emitir(
            Guid.NewGuid(), AuthEmailTokenTipo.ResetSenha,
            "HASH_EXPIRADO", DateTime.UtcNow.AddHours(1));
        typeof(AuthEmailToken)
            .GetProperty(nameof(AuthEmailToken.ExpiraEm))!
            .SetValue(token, DateTime.UtcNow.AddHours(-1));
        await repo.AdicionarAsync(token);
        await db.SaveChangesAsync();

        var encontrado = await repo.ObterValidoPorHashAsync("HASH_EXPIRADO", AuthEmailTokenTipo.ResetSenha);

        Assert.That(encontrado, Is.Null,
            "Token expirado deve ser tratado como ausente — sem isso, RedefinirSenha aceita tokens fora do TTL.");
    }

    [Test]
    public async Task ObterValidoPorHashAsync_RetornaNull_QuandoConsumido()
    {
        await using var db = CriarDb();
        var repo = new EfAuthEmailTokenRepository(db);

        var token = AuthEmailToken.Emitir(
            Guid.NewGuid(), AuthEmailTokenTipo.ResetSenha,
            "HASH_CONSUMIDO", DateTime.UtcNow.AddHours(1));
        token.MarcarComoConsumido();
        await repo.AdicionarAsync(token);
        await db.SaveChangesAsync();

        var encontrado = await repo.ObterValidoPorHashAsync("HASH_CONSUMIDO", AuthEmailTokenTipo.ResetSenha);

        Assert.That(encontrado, Is.Null,
            "Token já consumido não deve ser retornado — defesa em profundidade contra reuso.");
    }

    [Test]
    public async Task ObterValidoPorHashAsync_RetornaNull_QuandoTipoDiferente()
    {
        await using var db = CriarDb();
        var repo = new EfAuthEmailTokenRepository(db);

        var token = AuthEmailToken.Emitir(
            Guid.NewGuid(), AuthEmailTokenTipo.ResetSenha,
            "HASH_X", DateTime.UtcNow.AddHours(1));
        await repo.AdicionarAsync(token);
        await db.SaveChangesAsync();

        // Mesmo hash mas pedindo tipo Convite — não deve casar.
        var encontrado = await repo.ObterValidoPorHashAsync("HASH_X", AuthEmailTokenTipo.Convite);

        Assert.That(encontrado, Is.Null);
    }
}

using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Teste end-to-end do <see cref="CriarEstabelecimentoCommandHandler"/>:
/// command → repositorios reais (EF Core) → Postgres real (Testcontainers).
/// Valida que o aggregate eh persistido corretamente, com FK do dono e constraints
/// (CNPJ unique, dono unique) cumpridas pelo schema real — algo impossivel de
/// pegar com mock.
/// </summary>
[TestFixture]
public class CriarEstabelecimentoIntegrationTests
{
    private DbContextOptions<AppDbContext> _options;

    [SetUp]
    public async Task SetUp()
    {
        if (string.IsNullOrEmpty(PostgresIntegrationFixture.ConnectionString))
            Assert.Ignore("Container Postgres nao subiu (Docker indisponivel).");

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresIntegrationFixture.ConnectionString)
            .Options;

        // Limpeza de tabelas usadas por este teste — garante isolamento entre runs.
        await using var ctx = new AppDbContext(_options);
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE estabelecimentos, usuarios RESTART IDENTITY CASCADE;");
    }

    [Test]
    public async Task Handle_FluxoCompleto_PersisteEstabelecimentoVinculadoAoDono()
    {
        var donoId = Guid.NewGuid();

        // Arrange: cria usuario onboarded direto no banco.
        await using (var ctx = new AppDbContext(_options))
        {
            var u = Usuario.Criar(donoId, "dono@imedto.com");
            u.CompletarOnboarding("João Dono", "12345678909", "11999998888");
            ctx.Usuarios.Add(u);
            await ctx.SaveChangesAsync();
        }

        // Act: roda o handler com repositorios reais.
        await using (var ctx = new AppDbContext(_options))
        {
            var estabRepo = new EstabelecimentoRepository(ctx);
            var usuarioRepo = new UsuarioRepository(ctx);
            var eventBus = new Mock<IEventBus>();
            var sut = new CriarEstabelecimentoCommandHandler(estabRepo, usuarioRepo, eventBus.Object);

            await sut.Handle(new CriarEstabelecimentoCommand
            {
                DonoUsuarioId = donoId,
                NomeFantasia = "Clinica Integration",
                Cnpj = "12.345.678/0001-95",
                Telefone = "11988887777",
                Endereco = "Rua A, 1",
            });
        }

        // Assert: confirma persistencia em uma nova session (sem cache).
        await using (var ctx = new AppDbContext(_options))
        {
            var estab = await ctx.Estabelecimentos.SingleAsync();
            Assert.That(estab.DonoUsuarioId, Is.EqualTo(donoId));
            Assert.That(estab.NomeFantasia, Is.EqualTo("Clinica Integration"));
            Assert.That(estab.Cnpj, Is.EqualTo("12345678000195"));
            Assert.That(estab.Status, Is.EqualTo(EstabelecimentoStatus.Ativo));
            Assert.That(estab.Id, Is.GreaterThan(0), "EF deve popular Id auto-gerado.");
        }
    }

    [Test]
    public void Handle_DonoTentaCriarSegundoEstabelecimento_LancaBusinessException()
    {
        var donoId = Guid.NewGuid();

        // Arrange: usuario + estabelecimento ja persistidos.
        using (var ctx = new AppDbContext(_options))
        {
            var u = Usuario.Criar(donoId, "dono@imedto.com");
            u.CompletarOnboarding("João", "12345678909", null);
            ctx.Usuarios.Add(u);

            var primeiro = Estabelecimento.Criar(donoId, "Primeiro", null, null, null, null);
            ctx.Estabelecimentos.Add(primeiro);
            ctx.SaveChanges();
        }

        // Act + Assert: tentar criar segundo deve falhar pela regra "1 estabelecimento por dono".
        using (var ctx = new AppDbContext(_options))
        {
            var sut = new CriarEstabelecimentoCommandHandler(
                new EstabelecimentoRepository(ctx),
                new UsuarioRepository(ctx),
                new Mock<IEventBus>().Object);

            var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new CriarEstabelecimentoCommand
            {
                DonoUsuarioId = donoId,
                NomeFantasia = "Segundo",
            }));
            Assert.That(ex.Message, Does.Contain("já é dono"));
        }
    }

    [Test]
    public async Task Handle_CnpjJaUsadoPorOutroEstabelecimento_LancaBusinessException()
    {
        var dono1 = Guid.NewGuid();
        var dono2 = Guid.NewGuid();

        await using (var ctx = new AppDbContext(_options))
        {
            var u1 = Usuario.Criar(dono1, "d1@imedto.com");
            u1.CompletarOnboarding("D1", "11111111111", null);
            ctx.Usuarios.Add(u1);
            ctx.Estabelecimentos.Add(Estabelecimento.Criar(dono1, "Primeiro", null, "12345678000195", null, null));

            var u2 = Usuario.Criar(dono2, "d2@imedto.com");
            u2.CompletarOnboarding("D2", "22222222222", null);
            ctx.Usuarios.Add(u2);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new AppDbContext(_options))
        {
            var sut = new CriarEstabelecimentoCommandHandler(
                new EstabelecimentoRepository(ctx),
                new UsuarioRepository(ctx),
                new Mock<IEventBus>().Object);

            var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new CriarEstabelecimentoCommand
            {
                DonoUsuarioId = dono2,
                NomeFantasia = "Segundo",
                Cnpj = "12.345.678/0001-95",
            }));
            Assert.That(ex.Message, Does.Contain("CNPJ"));
        }
    }
}

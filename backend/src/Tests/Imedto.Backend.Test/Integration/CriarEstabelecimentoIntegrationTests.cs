using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Usuarios;
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
public class CriarEstabelecimentoIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[] { "estabelecimentos", "usuarios" };

    [Test]
    public async Task Handle_FluxoCompleto_PersisteEstabelecimentoVinculadoAoDono()
    {
        var donoId = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            var u = Usuario.Criar(donoId, "dono@imedto.com");
            u.CompletarOnboarding("João Dono", "12345678909", "11999998888");
            ctx.Usuarios.Add(u);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var sut = new CriarEstabelecimentoCommandHandler(
                new EstabelecimentoRepository(ctx),
                new UsuarioRepository(ctx),
                new Mock<IEventBus>().Object);

            await sut.Handle(new CriarEstabelecimentoCommand
            {
                DonoUsuarioId = donoId,
                NomeFantasia = "Clinica Integration",
                Cnpj = "12.345.678/0001-95",
                Telefone = "11988887777",
                Endereco = "Rua A, 1",
            });
        }

        await using (var ctx = NewContext())
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

        using (var ctx = NewContext())
        {
            var u = Usuario.Criar(donoId, "dono@imedto.com");
            u.CompletarOnboarding("João", "12345678909", null);
            ctx.Usuarios.Add(u);
            ctx.Estabelecimentos.Add(Estabelecimento.Criar(donoId, "Primeiro", null, null, null, null));
            ctx.SaveChanges();
        }

        using (var ctx = NewContext())
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

        await using (var ctx = NewContext())
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

        await using (var ctx = NewContext())
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

using Imedto.Backend.Application.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Catalogo;

[TestFixture]
public class BuscarCid10QueryHandlerTests
{
    private static Mock<Cid10QueryRepository> CriarRepo()
        => new Mock<Cid10QueryRepository>(
            new Imedto.Backend.Infrastructure.AppReadConnectionString("Host=localhost;Database=fake"));

    [Test]
    public async Task SemBusca_RetornaListaCompleta()
    {
        var lista = new List<Cid10Dto>
        {
            new() { Codigo = "J00",  Descricao = "Nasofaringite aguda" },
            new() { Codigo = "I10",  Descricao = "Hipertensão essencial" }
        };

        var repo = CriarRepo();
        repo.Setup(r => r.Buscar(null, 20)).ReturnsAsync(lista);

        var sut = new BuscarCid10QueryHandlers(repo.Object);
        var result = await sut.Handle(new BuscarCid10Query { Busca = null, Limite = 20 });

        Assert.That(result, Is.EqualTo(lista));
        repo.Verify(r => r.Buscar(null, 20), Times.Once);
    }

    [Test]
    public async Task ComBusca_RetornaFiltrado()
    {
        var lista = new List<Cid10Dto>
        {
            new() { Codigo = "J00", Descricao = "Nasofaringite aguda" }
        };

        var repo = CriarRepo();
        repo.Setup(r => r.Buscar("J00", 20)).ReturnsAsync(lista);

        var sut = new BuscarCid10QueryHandlers(repo.Object);
        var result = await sut.Handle(new BuscarCid10Query { Busca = "J00", Limite = 20 });

        Assert.That(result, Is.EqualTo(lista));
        repo.Verify(r => r.Buscar("J00", 20), Times.Once);
    }

    [Test]
    public async Task LimiteMaximo_Clampado()
    {
        var repo = CriarRepo();
        repo.Setup(r => r.Buscar(null, 50)).ReturnsAsync(Enumerable.Empty<Cid10Dto>());

        var sut = new BuscarCid10QueryHandlers(repo.Object);
        await sut.Handle(new BuscarCid10Query { Busca = null, Limite = 999 });

        repo.Verify(r => r.Buscar(null, 50), Times.Once);
    }

    [Test]
    public async Task LimiteMinimo_Clampado()
    {
        var repo = CriarRepo();
        repo.Setup(r => r.Buscar(null, 1)).ReturnsAsync(Enumerable.Empty<Cid10Dto>());

        var sut = new BuscarCid10QueryHandlers(repo.Object);
        await sut.Handle(new BuscarCid10Query { Busca = null, Limite = 0 });

        repo.Verify(r => r.Buscar(null, 1), Times.Once);
    }

    [Test]
    public async Task SemResultado_RetornaVazio()
    {
        var repo = CriarRepo();
        repo.Setup(r => r.Buscar("xyz", 20)).ReturnsAsync(Enumerable.Empty<Cid10Dto>());

        var sut = new BuscarCid10QueryHandlers(repo.Object);
        var result = await sut.Handle(new BuscarCid10Query { Busca = "xyz", Limite = 20 });

        Assert.That(result, Is.Empty);
    }
}

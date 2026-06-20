using Imedto.Backend.Application.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Catalogo;

[TestFixture]
public class BuscarExameCatalogoQueryHandlerTests
{
    private static Mock<ExameCatalogoQueryRepository> CriarRepo()
        => new Mock<ExameCatalogoQueryRepository>(
            new Imedto.Backend.Infrastructure.AppReadConnectionString("Host=localhost;Database=fake"));

    [Test]
    public async Task SemBusca_RetornaListaCompleta()
    {
        var lista = new List<ExameCatalogoDto>
        {
            new() { Id = 1, Nome = "Hemograma completo", Tipo = "Laboratorial" },
            new() { Id = 2, Nome = "Raio-X de tórax",   Tipo = "Imagem" }
        };

        var repo = CriarRepo();
        repo.Setup(r => r.Buscar(null, 30)).ReturnsAsync(lista);

        var sut = new BuscarExameCatalogoQueryHandlers(repo.Object);
        var result = await sut.Handle(new BuscarExameCatalogoQuery { Busca = null, Limite = 30 });

        Assert.That(result, Is.EqualTo(lista));
        repo.Verify(r => r.Buscar(null, 30), Times.Once);
    }

    [Test]
    public async Task ComBusca_RetornaFiltrado()
    {
        var lista = new List<ExameCatalogoDto>
        {
            new() { Id = 1, Nome = "Hemograma completo", Tipo = "Laboratorial" }
        };

        var repo = CriarRepo();
        repo.Setup(r => r.Buscar("hemo", 30)).ReturnsAsync(lista);

        var sut = new BuscarExameCatalogoQueryHandlers(repo.Object);
        var result = await sut.Handle(new BuscarExameCatalogoQuery { Busca = "hemo", Limite = 30 });

        Assert.That(result, Is.EqualTo(lista));
        repo.Verify(r => r.Buscar("hemo", 30), Times.Once);
    }

    [Test]
    public async Task LimiteMaximo_Clampado()
    {
        var repo = CriarRepo();
        repo.Setup(r => r.Buscar(null, 50)).ReturnsAsync(Enumerable.Empty<ExameCatalogoDto>());

        var sut = new BuscarExameCatalogoQueryHandlers(repo.Object);
        await sut.Handle(new BuscarExameCatalogoQuery { Busca = null, Limite = 999 });

        repo.Verify(r => r.Buscar(null, 50), Times.Once);
    }

    [Test]
    public async Task LimiteMinimo_Clampado()
    {
        var repo = CriarRepo();
        repo.Setup(r => r.Buscar(null, 1)).ReturnsAsync(Enumerable.Empty<ExameCatalogoDto>());

        var sut = new BuscarExameCatalogoQueryHandlers(repo.Object);
        await sut.Handle(new BuscarExameCatalogoQuery { Busca = null, Limite = 0 });

        repo.Verify(r => r.Buscar(null, 1), Times.Once);
    }

    [Test]
    public async Task SemResultado_RetornaVazio()
    {
        var repo = CriarRepo();
        repo.Setup(r => r.Buscar("xyz", 30)).ReturnsAsync(Enumerable.Empty<ExameCatalogoDto>());

        var sut = new BuscarExameCatalogoQueryHandlers(repo.Object);
        var result = await sut.Handle(new BuscarExameCatalogoQuery { Busca = "xyz", Limite = 30 });

        Assert.That(result, Is.Empty);
    }
}

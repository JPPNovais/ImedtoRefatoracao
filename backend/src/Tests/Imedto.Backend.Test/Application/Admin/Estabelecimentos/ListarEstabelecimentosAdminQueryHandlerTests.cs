using Imedto.Backend.Application.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Estabelecimentos;

[TestFixture]
public class ListarEstabelecimentosAdminQueryHandlerTests
{
    private Mock<IAdminEstabelecimentosQueryRepository> _repo;
    private ListarEstabelecimentosAdminQueryHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IAdminEstabelecimentosQueryRepository>();
        _sut = new ListarEstabelecimentosAdminQueryHandler(_repo.Object);
    }

    [Test]
    public async Task Handle_QuandoPaginaValida_RetornaResultadoPaginado()
    {
        var itens = new[]
        {
            new EstabelecimentoAdminListaItemDto { Id = 1, NomeFantasia = "Clínica A" },
        };
        _repo.Setup(r => r.ListarAsync(null, null, 1, 25, It.IsAny<CancellationToken>()))
             .ReturnsAsync((itens, 1));

        var query = new ListarEstabelecimentosAdminQuery { Pagina = 1, TamanhoPagina = 25 };
        var resultado = await _sut.Handle(query);

        Assert.That(resultado.Total, Is.EqualTo(1));
        Assert.That(resultado.Pagina, Is.EqualTo(1));
        Assert.That(resultado.TamanhoPagina, Is.EqualTo(25));
        Assert.That(resultado.Itens.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_QuandoTamanhoPaginaZero_NormalizaPara25()
    {
        _repo.Setup(r => r.ListarAsync(null, null, 1, 25, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Array.Empty<EstabelecimentoAdminListaItemDto>(), 0));

        var query = new ListarEstabelecimentosAdminQuery { Pagina = 0, TamanhoPagina = 0 };
        var resultado = await _sut.Handle(query);

        _repo.Verify(r => r.ListarAsync(null, null, 1, 25, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(resultado.TamanhoPagina, Is.EqualTo(25));
    }

    [Test]
    public async Task Handle_ComBuscaEStatus_PassaFiltrosParaRepositorio()
    {
        _repo.Setup(r => r.ListarAsync("clinic", "Ativo", 2, 25, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Array.Empty<EstabelecimentoAdminListaItemDto>(), 0));

        var query = new ListarEstabelecimentosAdminQuery
        {
            Busca = "clinic",
            Status = "Ativo",
            Pagina = 2,
            TamanhoPagina = 25,
        };
        await _sut.Handle(query);

        _repo.Verify(r => r.ListarAsync("clinic", "Ativo", 2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }
}

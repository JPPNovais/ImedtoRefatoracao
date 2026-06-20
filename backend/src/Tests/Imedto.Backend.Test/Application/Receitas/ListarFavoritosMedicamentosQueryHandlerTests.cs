using Imedto.Backend.Application.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

/// <summary>
/// Testes do handler de favoritos de medicamento (Item 13).
/// Garante multi-tenant, limite e lista vazia para Guid.Empty.
/// </summary>
[TestFixture]
public class ListarFavoritosMedicamentosQueryHandlerTests
{
    private Mock<IReceitaQueryRepository> _repo;
    private ListarFavoritosMedicamentosQueryHandler _sut;

    private const long EstabelecimentoId = 1;
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IReceitaQueryRepository>();
        _sut = new ListarFavoritosMedicamentosQueryHandler(_repo.Object);
    }

    private ListarFavoritosMedicamentosQuery Query(Guid? profId = null, int limite = 50) =>
        new()
        {
            ProfissionalUsuarioId = profId ?? ProfissionalId,
            EstabelecimentoId = EstabelecimentoId,
            Limite = limite
        };

    [Test]
    public async Task Handle_RetornaFavoritosOrdenadosPorUso()
    {
        var favoritos = new List<MedicamentoFavoritoDto>
        {
            new() { Id = 1, Medicamento = "Dipirona", UsoCount = 10 },
            new() { Id = 2, Medicamento = "Paracetamol", UsoCount = 5 }
        };

        _repo.Setup(r => r.ListarFavoritos(ProfissionalId, EstabelecimentoId, 50))
            .ReturnsAsync(favoritos);

        var resultado = (await _sut.Handle(Query())).ToList();

        Assert.That(resultado.Count, Is.EqualTo(2));
        Assert.That(resultado[0].Medicamento, Is.EqualTo("Dipirona"));
    }

    [Test]
    public async Task Handle_ProfissionalGuidEmpty_RetornaListaVaziaSequerConsultandoBanco()
    {
        var resultado = await _sut.Handle(Query(profId: Guid.Empty));

        Assert.That(resultado, Is.Empty);
        _repo.Verify(r => r.ListarFavoritos(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<int>()),
            Times.Never, "Guid.Empty: sem consulta ao banco.");
    }

    [Test]
    public async Task Handle_LimiteExcessivo_ClampaPara100()
    {
        _repo.Setup(r => r.ListarFavoritos(ProfissionalId, EstabelecimentoId, 100))
            .ReturnsAsync(new List<MedicamentoFavoritoDto>());

        await _sut.Handle(Query(limite: 500));

        _repo.Verify(r => r.ListarFavoritos(ProfissionalId, EstabelecimentoId, 100), Times.Once,
            "Limite clampado a 100 independente do input.");
    }

    [Test]
    public async Task Handle_MultiTenant_ConsultaApenasEstabelecimentoCorreto()
    {
        const long outroTenant = 99;
        var queryOutroTenant = new ListarFavoritosMedicamentosQuery
        {
            ProfissionalUsuarioId = ProfissionalId,
            EstabelecimentoId = outroTenant,
            Limite = 50
        };

        _repo.Setup(r => r.ListarFavoritos(ProfissionalId, outroTenant, 50))
            .ReturnsAsync(new List<MedicamentoFavoritoDto>());

        await _sut.Handle(queryOutroTenant);

        // Garante que o repo foi chamado com o estabelecimento correto,
        // não com EstabelecimentoId=1 do contexto "padrão" dos outros testes.
        _repo.Verify(r => r.ListarFavoritos(ProfissionalId, outroTenant, 50), Times.Once);
        _repo.Verify(r => r.ListarFavoritos(ProfissionalId, EstabelecimentoId, It.IsAny<int>()), Times.Never);
    }
}

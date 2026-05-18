using Imedto.Backend.Application.Salas.Commands;
using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Salas;

[TestFixture]
public class CriarSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salas;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IUnidadeRepository> _unidades;
    private CriarSalaCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long UnidadeId = 10;
    private const long OutroEstabId = 2;

    [SetUp]
    public void SetUp()
    {
        _salas = new Mock<ISalaRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _unidades = new Mock<IUnidadeRepository>();
        _sut = new CriarSalaCommandHandler(_salas.Object, _estabRepo.Object, _unidades.Object);
    }

    private Estabelecimento Estab(long id = EstabelecimentoId, Guid? dono = null)
    {
        var e = Estabelecimento.Criar(dono ?? _donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, id);
        return e;
    }

    private UnidadeEstabelecimento UnidadeNoEstab(long estabId)
    {
        var u = UnidadeEstabelecimento.Criar(estabId, "Matriz", true,
            new EnderecoUnidadeInput("", "", "", "", "", "", ""), null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(u, UnidadeId);
        return u;
    }

    private CriarSalaCommand Cmd(Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        UnidadeId = UnidadeId,
        Nome = "Sala 1",
    };

    [Test]
    public async Task Handle_DonoComUnidadeDoMesmoEstab_CriaSala()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync(UnidadeNoEstab(EstabelecimentoId));
        _salas.Setup(r => r.ExisteOutraComMesmoNomeNaUnidade(EstabelecimentoId, UnidadeId, "Sala 1", 0)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _salas.Verify(r => r.Salvar(It.IsAny<Sala>()), Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _salas.Verify(r => r.Salvar(It.IsAny<Sala>()), Times.Never);
    }

    [Test]
    public void Handle_UnidadeDeOutroEstab_LancaBusinessException()
    {
        // Repo filtra por tenant: unidade de OutroEstabId não é retornada para EstabelecimentoId.
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync((UnidadeEstabelecimento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Unidade"));
    }

    [Test]
    public void Handle_NomeDuplicadoNaMesmaUnidade_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync(UnidadeNoEstab(EstabelecimentoId));
        _salas.Setup(r => r.ExisteOutraComMesmoNomeNaUnidade(EstabelecimentoId, UnidadeId, "Sala 1", 0)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("nome").And.Contain("unidade"));
    }

    [Test]
    public async Task Handle_MesmoNomeEmUnidadeDiferente_CriaSala()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync(UnidadeNoEstab(EstabelecimentoId));
        _salas.Setup(r => r.ExisteOutraComMesmoNomeNaUnidade(EstabelecimentoId, UnidadeId, "Sala 1", 0)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _salas.Verify(r => r.Salvar(It.IsAny<Sala>()), Times.Once);
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Handle_UnidadeInexistente_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync((UnidadeEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Unidade"));
    }
}

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
public class AtualizarSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salas;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IUnidadeRepository> _unidades;
    private AtualizarSalaCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long UnidadeId = 10;
    private const long SalaId = 50;

    [SetUp]
    public void SetUp()
    {
        _salas = new Mock<ISalaRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _unidades = new Mock<IUnidadeRepository>();
        _sut = new AtualizarSalaCommandHandler(_salas.Object, _estabRepo.Object, _unidades.Object);
    }

    private Estabelecimento Estab()
    {
        var e = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, EstabelecimentoId);
        return e;
    }

    private Sala SalaExistente()
    {
        var s = Sala.Criar(EstabelecimentoId, UnidadeId, null, "Original", null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(s, SalaId);
        return s;
    }

    private UnidadeEstabelecimento Unidade()
    {
        var u = UnidadeEstabelecimento.Criar(EstabelecimentoId, "Matriz", true,
            new EnderecoUnidadeInput("", "", "", "", "", "", ""), null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(u, UnidadeId);
        return u;
    }

    private AtualizarSalaCommand Cmd(Guid? solicitante = null) => new()
    {
        SalaId = SalaId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        UnidadeId = UnidadeId,
        Nome = "Atualizada",
    };

    [Test]
    public async Task Handle_DonoAtualiza_PersisteAlteracoes()
    {
        var sala = SalaExistente();
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId)).ReturnsAsync(sala);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId)).ReturnsAsync(Unidade());
        _salas.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, "Atualizada", SalaId)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(sala.Nome, Is.EqualTo("Atualizada"));
        _salas.Verify(r => r.Salvar(sala), Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId)).ReturnsAsync(SalaExistente());
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
    }

    [Test]
    public void Handle_SalaInexistente_LancaBusinessException()
    {
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId)).ReturnsAsync((Sala)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Repartição"));
    }
}

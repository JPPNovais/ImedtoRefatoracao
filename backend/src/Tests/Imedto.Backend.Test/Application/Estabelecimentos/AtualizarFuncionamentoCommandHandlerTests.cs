using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

[TestFixture]
public class AtualizarFuncionamentoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _repo;
    private AtualizarFuncionamentoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEstabelecimentoRepository>();
        _sut = new AtualizarFuncionamentoCommandHandler(_repo.Object);
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private AtualizarFuncionamentoCommand Cmd(Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        HorarioInicio = new TimeOnly(7, 0),
        HorarioFim = new TimeOnly(20, 0),
        DiasSemana = new[] { 1, 2, 3, 4, 5, 6 },
        HorariosBloqueados = new[]
        {
            new HorarioBloqueadoInput(null, new TimeOnly(12, 0), new TimeOnly(13, 0), "Almoço"),
        },
        DatasBloqueadas = new[]
        {
            new DataBloqueadaInput(null, new DateOnly(2026, 12, 25), "Natal"),
        },
    };

    [Test]
    public async Task Handle_DonoAtualiza_PersisteFuncionamento()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        await _sut.Handle(Cmd());

        Assert.That(estab.HorarioInicio, Is.EqualTo(new TimeOnly(7, 0)));
        Assert.That(estab.HorarioFim, Is.EqualTo(new TimeOnly(20, 0)));
        Assert.That(estab.DiasSemanaFuncionamento, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(estab.HorariosBloqueados, Has.Count.EqualTo(1));
        Assert.That(estab.DatasBloqueadas, Has.Count.EqualTo(1));
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}

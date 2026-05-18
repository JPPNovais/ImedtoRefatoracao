using Imedto.Backend.Application.Salas.Commands;
using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Salas;

[TestFixture]
public class DesativarSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salas;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private DesativarSalaCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long SalaId = 50;

    [SetUp]
    public void SetUp()
    {
        _salas = new Mock<ISalaRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new DesativarSalaCommandHandler(_salas.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab()
    {
        var e = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, EstabelecimentoId);
        return e;
    }

    private Sala SalaExistente()
    {
        var s = Sala.Criar(EstabelecimentoId, 10L, null, "Sala 1", null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(s, SalaId);
        return s;
    }

    [Test]
    public async Task Handle_DonoDesativa_MarcaInativaESalva()
    {
        var sala = SalaExistente();
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(sala);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(new DesativarSalaCommand
        {
            SalaId = SalaId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _donoId,
        });

        Assert.That(sala.Ativo, Is.False);
        _salas.Verify(r => r.Salvar(sala), Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(SalaExistente());
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new DesativarSalaCommand
        {
            SalaId = SalaId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _outroId,
        }));
        Assert.That(ex.Message, Does.Contain("dono"));
    }

    [Test]
    public void Handle_SalaInexistente_LancaBusinessException()
    {
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync((Sala?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new DesativarSalaCommand
        {
            SalaId = SalaId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _donoId,
        }));
        Assert.That(ex.Message, Does.Contain("Repartição"));
    }
}

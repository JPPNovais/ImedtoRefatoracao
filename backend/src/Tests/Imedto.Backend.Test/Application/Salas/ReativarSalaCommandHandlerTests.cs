using Imedto.Backend.Application.Salas.Commands;
using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Salas;

[TestFixture]
public class ReativarSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salas;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private ReativarSalaCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long SalaId = 50;

    [SetUp]
    public void SetUp()
    {
        _salas = new Mock<ISalaRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new ReativarSalaCommandHandler(_salas.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab()
    {
        var e = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, EstabelecimentoId);
        return e;
    }

    private Sala SalaInativa()
    {
        var s = Sala.Criar(EstabelecimentoId, 10L, null, "Sala 1", null);
        s.Desativar();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(s, SalaId);
        return s;
    }

    [Test]
    public async Task Handle_DonoReativa_MarcaAtivaESalva()
    {
        var sala = SalaInativa();
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(sala);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(new ReativarSalaCommand
        {
            SalaId = SalaId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _donoId,
        });

        Assert.That(sala.Ativo, Is.True);
        _salas.Verify(r => r.Salvar(sala), Times.Once);
    }

    [Test]
    public void Handle_JaAtiva_LancaBusinessException()
    {
        var sala = Sala.Criar(EstabelecimentoId, 10L, null, "Sala 1", null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(sala, SalaId);
        _salas.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(sala);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReativarSalaCommand
        {
            SalaId = SalaId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _donoId,
        }));
        Assert.That(ex.Message, Does.Contain("ativa"));
    }
}

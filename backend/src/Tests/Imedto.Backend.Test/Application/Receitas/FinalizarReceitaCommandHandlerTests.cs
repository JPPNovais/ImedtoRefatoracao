using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.Domain.Receitas.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

[TestFixture]
public class FinalizarReceitaCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo;
    private Mock<IMedicamentoFavoritoRepository> _favRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private Mock<IEventBus> _eventBus;
    private FinalizarReceitaCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ReceitaId = 99;
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroProfissional = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _favRepo = new Mock<IMedicamentoFavoritoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new FinalizarReceitaCommandHandler(
            _receitaRepo.Object, _favRepo.Object, _acessoLog.Object, _eventBus.Object);
    }

    private static Receita Rascunho(long estabId, Guid prof)
    {
        var r = Receita.IniciarRascunho(
            prontuarioId: 200L,
            pacienteId: 100L,
            profissionalUsuarioId: prof,
            estabelecimentoId: estabId,
            tipo: TipoReceita.Comum,
            tipoNotificacao: null,
            observacoes: null,
            validadeAte: null,
            itens: new[] { new Receita.ItemReceitaInput("Dipirona 500mg", "1 cp 6/6h", null, null, null) }!);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(r, ReceitaId);
        return r;
    }

    [Test]
    public async Task Handle_ProfissionalResponsavelFinaliza_TransicionaPersisteFavoritoEvento()
    {
        var receita = Rascunho(EstabelecimentoId, _profissionalId);
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);

        await _sut.Handle(new FinalizarReceitaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _profissionalId,
        });

        Assert.That(receita.Status, Is.EqualTo(StatusReceita.Emitida));
        _favRepo.Verify(f => f.RegistrarUso(
            _profissionalId, EstabelecimentoId, "Dipirona 500mg", "1 cp 6/6h", null), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _profissionalId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is ReceitaEmitidaEvent)), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync((Receita?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new FinalizarReceitaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _profissionalId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
        _favRepo.Verify(f => f.RegistrarUso(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ViaAdministracao?>()),
            Times.Never);
    }

    [Test]
    public void Handle_OutroProfissionalTentaFinalizar_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(Rascunho(EstabelecimentoId, _profissionalId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new FinalizarReceitaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _outroProfissional,
        }));
        Assert.That(ex.Message, Does.Contain("profissional responsável"));
        _favRepo.Verify(f => f.RegistrarUso(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ViaAdministracao?>()),
            Times.Never);
    }
}

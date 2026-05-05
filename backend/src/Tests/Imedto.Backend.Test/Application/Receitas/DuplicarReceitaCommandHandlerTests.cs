using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

[TestFixture]
public class DuplicarReceitaCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo;
    private Mock<IMedicamentoFavoritoRepository> _favRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private Mock<IEventBus> _eventBus;
    private DuplicarReceitaCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ReceitaId = 99;
    private readonly Guid _profissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _favRepo = new Mock<IMedicamentoFavoritoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new DuplicarReceitaCommandHandler(
            _receitaRepo.Object, _favRepo.Object, _acessoLog.Object, _eventBus.Object);
    }

    private static Receita ReceitaEmitida(long estabId, Guid prof) =>
        Receita.Emitir(
            prontuarioId: 200L, pacienteId: 100L,
            profissionalUsuarioId: prof, estabelecimentoId: estabId,
            tipo: TipoReceita.Comum, observacoes: null, validadeAte: null,
            itens: new[] { ("Med", "Pos", (string)null, (ViaAdministracao?)null, (string)null) });

    private DuplicarReceitaCommand Cmd() => new()
    {
        ReceitaIdOrigem = ReceitaId,
        EstabelecimentoId = EstabelecimentoId,
        ProfissionalUsuarioId = _profissionalId,
    };

    [Test]
    public async Task Handle_DoMesmoTenant_DuplicaReceitaEAudita()
    {
        var origem = ReceitaEmitida(EstabelecimentoId, _profissionalId);
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(origem);
        _receitaRepo.Setup(r => r.Salvar(It.IsAny<Receita>()))
                    .Callback<Receita>(rc =>
                        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(rc, 1234L))
                    .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.ReceitaIdCriada, Is.EqualTo(1234L));
        _favRepo.Verify(f => f.RegistrarUso(
            _profissionalId, EstabelecimentoId, "Med", "Pos", null), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _profissionalId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId))
                    .ReturnsAsync((Receita?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync((Receita)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }
}

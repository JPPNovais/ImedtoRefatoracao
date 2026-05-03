using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.Domain.Receitas.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

[TestFixture]
public class EmitirReceitaCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo;
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IMedicamentoFavoritoRepository> _favRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private Mock<IEventBus> _eventBus;
    private EmitirReceitaCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private readonly Guid _profissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _favRepo = new Mock<IMedicamentoFavoritoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new EmitirReceitaCommandHandler(
            _receitaRepo.Object, _prontuarioRepo.Object, _pacienteRepo.Object,
            _favRepo.Object, _acessoLog.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private static Prontuario ProntuarioJaIniciado()
    {
        var p = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, ProntuarioId);
        return p;
    }

    private EmitirReceitaCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        ProfissionalUsuarioId = _profissionalId,
        Tipo = "Comum",
        Itens = new() { new() { Medicamento = "Dipirona 500mg", Posologia = "1 cp 6/6h" } },
    };

    [Test]
    public async Task Handle_TudoValido_EmitePersisteFavoritoEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());
        _receitaRepo.Setup(r => r.Salvar(It.IsAny<Receita>()))
                    .Callback<Receita>(rc =>
                        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(rc, 999L))
                    .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.ReceitaIdCriada, Is.EqualTo(999L));
        _favRepo.Verify(f => f.RegistrarUso(
            _profissionalId, EstabelecimentoId, "Dipirona 500mg", "1 cp 6/6h", null), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            ProntuarioId, _profissionalId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is ReceitaEmitidaEvent)), Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _favRepo.Verify(f => f.RegistrarUso(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ViaAdministracao?>()),
            Times.Never);
    }

    [Test]
    public void Handle_PacienteDeletado_LancaBusinessException()
    {
        var p = PacienteAtivo();
        p.MarcarComoDeletado(Guid.NewGuid());
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(p);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("deletado"));
    }

    [Test]
    public void Handle_SemProntuario_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não foi iniciado"));
    }
}

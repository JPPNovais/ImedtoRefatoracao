using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class RegistrarEvolucaoCommandHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioEvolucaoRepository> _evolucaoRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IModeloDeProntuarioRepository> _modeloRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private Mock<IEventBus> _eventBus;
    private RegistrarEvolucaoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioModeloId = 200;
    private readonly Guid _autorId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _evolucaoRepo = new Mock<IProntuarioEvolucaoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _modeloRepo = new Mock<IModeloDeProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new RegistrarEvolucaoCommandHandler(
            _prontuarioRepo.Object, _evolucaoRepo.Object, _pacienteRepo.Object,
            _modeloRepo.Object, _acessoLog.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo() =>
        Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);

    private Prontuario ProntuarioJaIniciado()
    {
        var p = Prontuario.Iniciar(PacienteId, EstabelecimentoId, ProntuarioModeloId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 500L);
        return p;
    }

    private static ModeloDeProntuario ModeloDoEstab(long estabId, bool ativo = true)
    {
        var m = ModeloDeProntuario.CriarDoEstabelecimento(estabId, "Modelo", null, "{\"campos\":[]}");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(m, ProntuarioModeloId);
        if (!ativo)
        {
            typeof(ModeloDeProntuario).GetProperty(nameof(ModeloDeProntuario.Ativo))!
                .SetValue(m, false);
        }
        return m;
    }

    private RegistrarEvolucaoCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        AutorUsuarioId = _autorId,
        ConteudoJson = "{\"queixa\":\"dor\"}",
    };

    [Test]
    public async Task Handle_TudoValido_RegistraEvolucaoEPersisteAuditEEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());
        _modeloRepo.Setup(r => r.ObterPorIdOuNulo(ProntuarioModeloId))
                   .ReturnsAsync(ModeloDoEstab(EstabelecimentoId));
        _evolucaoRepo.Setup(r => r.Salvar(It.IsAny<ProntuarioEvolucao>()))
                     .Callback<ProntuarioEvolucao>(e =>
                         typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, 700L))
                     .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _evolucaoRepo.Verify(r => r.Salvar(It.IsAny<ProntuarioEvolucao>()), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            500L, _autorId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is EvolucaoRegistradaEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _acessoLog.Verify(a => a.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(),
            It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()), Times.Never);
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
    public void Handle_ProntuarioAindaNaoIniciado_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não foi iniciado"));
    }

    [Test]
    public void Handle_ModeloInativo_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());
        _modeloRepo.Setup(r => r.ObterPorIdOuNulo(ProntuarioModeloId))
                   .ReturnsAsync(ModeloDoEstab(EstabelecimentoId, ativo: false));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("inativo"));
    }
}

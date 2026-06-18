using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Pacientes.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

[TestFixture]
public class CadastrarPacienteCommandHandlerTests
{
    private Mock<IPacienteRepository> _repo;
    private Mock<IEventBus> _eventBus;
    private Mock<IAssinaturaService> _assinatura;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private CadastrarPacienteCommandHandler _sut;

    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPacienteRepository>();
        _eventBus = new Mock<IEventBus>();
        _assinatura = new Mock<IAssinaturaService>();
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new CadastrarPacienteCommandHandler(_repo.Object, _eventBus.Object, _assinatura.Object, _acessoLog.Object);

        // Default: limite NAO atingido (caminho feliz).
        _assinatura.Setup(s => s.LimiteAtingidoAsync(EstabelecimentoId, "pacientes", default))
                   .ReturnsAsync(false);
    }

    private static CadastrarPacienteCommand Cmd() => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        NomeCompleto = "Paciente Teste",
        Cpf = "123.456.789-09",
        DataNascimento = new DateTime(1990, 5, 15),
        Genero = "Masculino",
        Telefone = "11999998888",
        Email = "p@test.com",
        Endereco = "Rua A",
        Observacoes = null,
    };

    [Test]
    public async Task Handle_TudoValido_SalvaEPublicaEvento()
    {
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento("12345678909", EstabelecimentoId, 0))
             .ReturnsAsync(false);
        _repo.Setup(r => r.Salvar(It.IsAny<Paciente>()))
             .Callback<Paciente>(p =>
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 99L))
             .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is PacienteCadastradoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_LimiteDoPlanoAtingido_LancaBusinessException()
    {
        _assinatura.Setup(s => s.LimiteAtingidoAsync(EstabelecimentoId, "pacientes", default))
                   .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Plano"));
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public void Handle_CpfDuplicadoNoMesmoEstabelecimento_LancaBusinessException()
    {
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento("12345678909", EstabelecimentoId, 0))
             .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("CPF"));
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public async Task Handle_SemCpf_NaoChecaDuplicidade()
    {
        var cmd = Cmd();
        cmd.Cpf = null;
        _repo.Setup(r => r.Salvar(It.IsAny<Paciente>()))
             .Callback<Paciente>(p =>
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 99L))
             .Returns(Task.CompletedTask);

        await _sut.Handle(cmd);

        _repo.Verify(r => r.ExisteCpfNoEstabelecimento(It.IsAny<string>(),
            It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task Handle_GeneroInvalido_AdotaNaoInformado()
    {
        var cmd = Cmd();
        cmd.Genero = "Outro_Invalido";
        Paciente capturado = null;
        _repo.Setup(r => r.Salvar(It.IsAny<Paciente>()))
             .Callback<Paciente>(p =>
             {
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 99L);
                 capturado = p;
             })
             .Returns(Task.CompletedTask);

        await _sut.Handle(cmd);

        Assert.That(capturado, Is.Not.Null);
        Assert.That(capturado.Genero, Is.EqualTo(GeneroPaciente.NaoInformado));
    }

    [Test]
    public void Handle_DocInternacionalDuplicadoNoMesmoEstabelecimento_LancaBusinessException()
    {
        var cmd = Cmd();
        cmd.Cpf = null;
        cmd.DocumentoInternacional = "PASSAPORTE-AB123456";

        _repo.Setup(r => r.ExisteDocumentoInternacionalNoEstabelecimento(
                "PASSAPORTE-AB123456", EstabelecimentoId, 0))
             .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("documento").IgnoreCase);
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public async Task Handle_ComDocInternacional_SalvaSemChecarCpf()
    {
        var cmd = Cmd();
        cmd.Cpf = null;
        cmd.DocumentoInternacional = "PASSAPORTE-AB123456";

        Paciente capturado = null;
        _repo.Setup(r => r.ExisteDocumentoInternacionalNoEstabelecimento(
                "PASSAPORTE-AB123456", EstabelecimentoId, 0))
             .ReturnsAsync(false);
        _repo.Setup(r => r.Salvar(It.IsAny<Paciente>()))
             .Callback<Paciente>(p =>
             {
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 99L);
                 capturado = p;
             })
             .Returns(Task.CompletedTask);

        await _sut.Handle(cmd);

        _repo.Verify(r => r.ExisteCpfNoEstabelecimento(It.IsAny<string>(),
            It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        Assert.That(capturado.DocumentoInternacional, Is.EqualTo("PASSAPORTE-AB123456"));
        Assert.That(capturado.Cpf, Is.Null);
    }

    [Test]
    public void Handle_CpfInvalido_LancaBusinessExceptionDoDominio()
    {
        // Backend eh fonte da verdade — nao confia no front. CPF com DV errado
        // deve ser rejeitado no aggregate (BusinessException → 422 no controller).
        var cmd = Cmd();
        cmd.Cpf = "99999999999"; // sequencia repetida — invalido

        _repo.Setup(r => r.ExisteCpfNoEstabelecimento(It.IsAny<string>(), EstabelecimentoId, 0))
             .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("CPF").IgnoreCase);
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public async Task Handle_RespeitaTenancyDoCommand_SalvaComMesmoEstabelecimento()
    {
        Paciente capturado = null;
        _repo.Setup(r => r.Salvar(It.IsAny<Paciente>()))
             .Callback<Paciente>(p =>
             {
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 99L);
                 capturado = p;
             })
             .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        Assert.That(capturado.EstabelecimentoId, Is.EqualTo(EstabelecimentoId),
            "EstabelecimentoId do command (vindo do tenancy filter) deve ser preservado.");
    }
}

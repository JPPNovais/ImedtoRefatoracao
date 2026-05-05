using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

[TestFixture]
public class AtualizarPacienteCommandHandlerTests
{
    private Mock<IPacienteRepository> _repo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private AtualizarPacienteCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 99;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPacienteRepository>();
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new AtualizarPacienteCommandHandler(_repo.Object, _acessoLog.Object);
    }

    private static Paciente CriarPaciente() =>
        Paciente.Cadastrar(EstabelecimentoId, "Original", "12345678909",
            new DateTime(1990, 1, 1), GeneroPaciente.Masculino,
            null, null, null, null);

    private AtualizarPacienteCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId,
        NomeCompleto = "Atualizado",
        Cpf = "987.654.321-00",
        DataNascimento = new DateTime(1991, 2, 2),
        Genero = "Feminino",
        Telefone = "11888887777",
        Email = "novo@test.com",
        Endereco = "Rua B",
        Observacoes = "obs",
    };

    [Test]
    public async Task Handle_TudoValido_AtualizaEPersisteAuditoria()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento("98765432100", EstabelecimentoId, PacienteId))
             .ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(paciente.NomeCompleto, Is.EqualTo("Atualizado"));
        Assert.That(paciente.Cpf, Is.EqualTo("98765432100"));
        Assert.That(paciente.Email, Is.EqualTo("novo@test.com"));
        _repo.Verify(r => r.Salvar(paciente), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            PacienteId, _solicitanteId, EstabelecimentoId, TipoAcessoPaciente.Edicao),
            Times.Once);
    }

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>()),
            Times.Never);
    }

    [Test]
    public void Handle_CpfDuplicadoEmOutroPacienteDoMesmoEstab_LancaBusinessException()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        // Outro paciente do mesmo estab usa esse CPF (passou ignorarPacienteId = self).
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento("98765432100", EstabelecimentoId, PacienteId))
             .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("CPF"));
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public async Task Handle_SemAlterarCpf_PassaIgnorarPacienteIdNaConsultaParaPermitirOMesmoCpf()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento("12345678909", EstabelecimentoId, PacienteId))
             .ReturnsAsync(false);

        var cmd = Cmd();
        cmd.Cpf = "123.456.789-09"; // mesmo CPF original
        await _sut.Handle(cmd);

        _repo.Verify(r => r.ExisteCpfNoEstabelecimento("12345678909", EstabelecimentoId, PacienteId),
            Times.Once,
            "ignorarPacienteId deve ser o proprio paciente para nao colidir consigo mesmo.");
    }

    [Test]
    public void Handle_DocInternacionalDuplicadoEmOutroPaciente_LancaBusinessException()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        _repo.Setup(r => r.ExisteDocumentoInternacionalNoEstabelecimento(
                "PASSAPORTE-X", EstabelecimentoId, PacienteId))
             .ReturnsAsync(true);

        var cmd = Cmd();
        cmd.Cpf = null;
        cmd.DocumentoInternacional = "PASSAPORTE-X";

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("documento").IgnoreCase);
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public async Task Handle_TrocarCpfPorDocInternacional_AtualizaCorretamente()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        _repo.Setup(r => r.ExisteDocumentoInternacionalNoEstabelecimento(
                "PASSAPORTE-Z", EstabelecimentoId, PacienteId))
             .ReturnsAsync(false);

        var cmd = Cmd();
        cmd.Cpf = null;
        cmd.DocumentoInternacional = "PASSAPORTE-Z";

        await _sut.Handle(cmd);

        Assert.That(paciente.Cpf, Is.Null);
        Assert.That(paciente.DocumentoInternacional, Is.EqualTo("PASSAPORTE-Z"));
    }

    [Test]
    public async Task Handle_GeneroInvalido_AdotaNaoInformado()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>()))
             .ReturnsAsync(false);

        var cmd = Cmd();
        cmd.Genero = "Inexistente";
        await _sut.Handle(cmd);

        Assert.That(paciente.Genero, Is.EqualTo(GeneroPaciente.NaoInformado));
    }
}

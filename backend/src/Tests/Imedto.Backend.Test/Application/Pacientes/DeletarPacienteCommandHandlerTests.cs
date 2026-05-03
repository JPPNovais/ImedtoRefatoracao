using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

[TestFixture]
public class DeletarPacienteCommandHandlerTests
{
    private Mock<IPacienteRepository> _repo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private DeletarPacienteCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 99;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPacienteRepository>();
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new DeletarPacienteCommandHandler(_repo.Object, _acessoLog.Object);
    }

    private static Paciente CriarPaciente() =>
        Paciente.Cadastrar(EstabelecimentoId, "Paciente Teste", "12345678909",
            new DateTime(1990, 1, 1), GeneroPaciente.Masculino,
            null, null, null, null);

    private DeletarPacienteCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId,
    };

    [Test]
    public async Task Handle_PacienteValido_SoftDeleteEPersisteAuditoria()
    {
        var paciente = CriarPaciente();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        await _sut.Handle(Cmd());

        Assert.That(paciente.DeletadoEm, Is.Not.Null);
        Assert.That(paciente.DeletadoPorUsuarioId, Is.EqualTo(_solicitanteId));
        _repo.Verify(r => r.Salvar(paciente), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            PacienteId, _solicitanteId, EstabelecimentoId, TipoAcessoPaciente.Exclusao),
            Times.Once,
            "Audit LGPD obrigatorio para exclusao de paciente.");
    }

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaMensagemGenericaENaoPersisteAuditoria()
    {
        // Repositorio com filtro de tenant retorna null para paciente de outro estab.
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."),
            "Mensagem identica para inexistente OU cross-tenant — nao vaza existencia.");
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>()),
            Times.Never,
            "Auditoria nao deve ser registrada para tentativa cross-tenant — caso contrario, atacante poderia inferir existencia.");
    }
}

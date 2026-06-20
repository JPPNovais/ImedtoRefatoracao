using Imedto.Backend.Application.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

/// <summary>
/// Garante que o endpoint de revelação de dados sensíveis (Item 7):
/// — audita o acesso com motivo RevelacaoDadosSensiveis;
/// — respeita o isolamento multi-tenant (retorna null para paciente de outro tenant);
/// — não audita quando o paciente não é encontrado (sem acesso, sem log).
/// </summary>
[TestFixture]
public class ObterDadosSensiveisPacienteQueryHandlerTests
{
    private Mock<PacienteQueryRepository> _repo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private ObterDadosSensiveisPacienteQueryHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 42;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<PacienteQueryRepository>(new AppReadConnectionString("Host=ignored"));
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new ObterDadosSensiveisPacienteQueryHandler(_repo.Object, _acessoLog.Object);
    }

    private ObterDadosSensiveisPacienteQuery Query() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId
    };

    [Test]
    public async Task Handle_PacienteEncontrado_RetornasCpfETelefoneERegistraAudit()
    {
        _repo.Setup(r => r.ObterDadosSensiveis(PacienteId, EstabelecimentoId))
            .ReturnsAsync(("123.456.789-09", "(11) 99999-8888"));

        var dto = await _sut.Handle(Query());

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Cpf, Is.EqualTo("123.456.789-09"));
        Assert.That(dto.Telefone, Is.EqualTo("(11) 99999-8888"));

        _acessoLog.Verify(a => a.RegistrarAsync(
                PacienteId,
                _solicitanteId,
                EstabelecimentoId,
                TipoAcessoPaciente.RevelacaoDadosSensiveis),
            Times.Once,
            "Audit deve ser registrado com motivo RevelacaoDadosSensiveis.");
    }

    [Test]
    public async Task Handle_PacienteNaoEncontrado_RetornaNullSemAudit()
    {
        // Paciente de outro tenant retorna null no repo (falha-fechada).
        _repo.Setup(r => r.ObterDadosSensiveis(PacienteId, EstabelecimentoId))
            .ReturnsAsync(((string?, string?)?)null);

        var dto = await _sut.Handle(Query());

        Assert.That(dto, Is.Null, "Paciente de outro tenant deve retornar null (404 genérico no controller).");

        _acessoLog.Verify(a => a.RegistrarAsync(
                It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>()),
            Times.Never,
            "Sem acesso real, sem audit — evita log de tentativas frustradas.");
    }

    [Test]
    public async Task Handle_PacienteComCamposNulos_RetornaDtoComNulos()
    {
        // Paciente sem CPF e sem telefone cadastrado.
        _repo.Setup(r => r.ObterDadosSensiveis(PacienteId, EstabelecimentoId))
            .ReturnsAsync(((string?)null, (string?)null));

        var dto = await _sut.Handle(Query());

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Cpf, Is.Null);
        Assert.That(dto.Telefone, Is.Null);

        _acessoLog.Verify(a => a.RegistrarAsync(
                PacienteId, _solicitanteId, EstabelecimentoId, TipoAcessoPaciente.RevelacaoDadosSensiveis),
            Times.Once,
            "Mesmo sem dados preenchidos, o acesso ao registro é auditado.");
    }

    [Test]
    public async Task Handle_TenantDiferente_RetornaNullSemAudit()
    {
        // Isolamento multi-tenant: outro estabelecimento não vê os dados.
        const long outroTenant = 999;
        var queryOutroTenant = new ObterDadosSensiveisPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = outroTenant,
            SolicitanteUsuarioId = _solicitanteId
        };

        _repo.Setup(r => r.ObterDadosSensiveis(PacienteId, outroTenant))
            .ReturnsAsync(((string?, string?)?)null);

        var dto = await _sut.Handle(queryOutroTenant);

        Assert.That(dto, Is.Null, "Multi-tenant: paciente de outro estabelecimento retorna null.");
        _acessoLog.Verify(a => a.RegistrarAsync(
                It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>()),
            Times.Never);
    }
}

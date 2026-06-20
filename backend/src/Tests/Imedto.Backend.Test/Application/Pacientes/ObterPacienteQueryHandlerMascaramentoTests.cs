using Imedto.Backend.Application.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

/// <summary>
/// Valida o mascaramento opt-in de CPF/telefone em GET /api/paciente/{id}?contato=mascarado.
/// Premissa: SEM o param (MascararContato=false) o comportamento é idêntico ao atual —
/// PII completa retornada (web safe). COM o param, PII é ofuscada na borda.
/// </summary>
[TestFixture]
public class ObterPacienteQueryHandlerMascaramentoTests
{
    private Mock<PacienteQueryRepository> _repo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private ObterPacienteQueryHandlers _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 42;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<PacienteQueryRepository>(new AppReadConnectionString("Host=ignored"));
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new ObterPacienteQueryHandlers(_repo.Object, _acessoLog.Object);
    }

    private PacienteDto DtoCompleto() => new()
    {
        Id = PacienteId,
        NomeCompleto = "João Silva",
        Cpf = "123.456.789-09",
        Telefone = "(11) 99999-8888",
        Email = "joao@exemplo.com"
    };

    // ── SEM mascaramento (default web) ──────────────────────────────────────────

    [Test]
    public async Task Handle_SemMascararContato_RetornaCpfETelefoneCompletos()
    {
        _repo.Setup(r => r.ObterPorId(PacienteId, EstabelecimentoId))
            .ReturnsAsync(DtoCompleto());

        var dto = await _sut.Handle(new ObterPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            MascararContato = false  // default
        });

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Cpf, Is.EqualTo("123.456.789-09"), "PII completa sem o param — web não muda.");
        Assert.That(dto.Telefone, Is.EqualTo("(11) 99999-8888"), "Telefone completo sem o param.");
    }

    // ── COM mascaramento (mobile opt-in) ────────────────────────────────────────

    [Test]
    public async Task Handle_ComMascararContato_RetornaCpfMascaradoComUltimos2Digitos()
    {
        _repo.Setup(r => r.ObterPorId(PacienteId, EstabelecimentoId))
            .ReturnsAsync(DtoCompleto());

        var dto = await _sut.Handle(new ObterPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            MascararContato = true
        });

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Cpf, Is.EqualTo("•••.•••.•••-09"), "CPF mascarado: apenas últimos 2 dígitos visíveis.");
        Assert.That(dto.Telefone, Is.EqualTo("(••) •••••-8888"), "Celular mascarado: apenas últimos 4 dígitos visíveis.");
    }

    [Test]
    public async Task Handle_ComMascararContato_AuditaAcessoNormalmente()
    {
        // Mascaramento não suprime audit — o acesso ainda ocorreu.
        _repo.Setup(r => r.ObterPorId(PacienteId, EstabelecimentoId))
            .ReturnsAsync(DtoCompleto());

        await _sut.Handle(new ObterPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            MascararContato = true
        });

        _acessoLog.Verify(a => a.RegistrarAsync(
                PacienteId, _solicitanteId, EstabelecimentoId, TipoAcessoPaciente.Leitura),
            Times.Once,
            "Audit deve ser registrado mesmo no modo mascarado — o acesso ao registro ocorreu.");
    }

    [Test]
    public async Task Handle_ComMascararContato_PacienteNaoEncontrado_RetornaNullSemAudit()
    {
        _repo.Setup(r => r.ObterPorId(PacienteId, EstabelecimentoId))
            .ReturnsAsync((PacienteDto?)null);

        var dto = await _sut.Handle(new ObterPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            MascararContato = true
        });

        Assert.That(dto, Is.Null);
        _acessoLog.Verify(a => a.RegistrarAsync(
                It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ComMascararContato_DadosNulos_RetornaComoEstao()
    {
        // Paciente sem CPF/telefone cadastrado: não explode, retorna null.
        _repo.Setup(r => r.ObterPorId(PacienteId, EstabelecimentoId))
            .ReturnsAsync(new PacienteDto { Id = PacienteId, NomeCompleto = "Sem Contato" });

        var dto = await _sut.Handle(new ObterPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            MascararContato = true
        });

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Cpf, Is.Null, "CPF null permanece null — sem explodir.");
        Assert.That(dto.Telefone, Is.Null, "Telefone null permanece null.");
    }

    // ── Helpers de mascaramento (testes unitários das funções puras) ────────────

    [TestCase("123.456.789-09", "•••.•••.•••-09")]
    [TestCase("000.000.000-00", "•••.•••.•••-00")]
    [TestCase("98765432100", "•••.•••.•••-00")]   // dígitos crus sem formatação
    public void MascararCpf_FormatosVariados_MascaraCorretamente(string cpf, string esperado)
    {
        var resultado = ObterPacienteQueryHandlers.MascararCpf(cpf);
        Assert.That(resultado, Is.EqualTo(esperado));
    }

    [TestCase(null, null)]
    [TestCase("", "")]
    [TestCase("   ", "   ")]
    public void MascararCpf_ValoresVazios_RetornaComoEsta(string? cpf, string? esperado)
    {
        var resultado = ObterPacienteQueryHandlers.MascararCpf(cpf);
        Assert.That(resultado, Is.EqualTo(esperado));
    }

    [TestCase("(11) 99999-8888", "(••) •••••-8888")]   // celular (11 dígitos)
    [TestCase("(21) 3333-4444", "(••) ••••-4444")]       // fixo (10 dígitos)
    [TestCase("11999998888", "(••) •••••-8888")]          // cru 11 dígitos
    [TestCase("2133334444", "(••) ••••-4444")]            // cru 10 dígitos
    public void MascararTelefone_FormatosVariados_MascaraCorretamente(string telefone, string esperado)
    {
        var resultado = ObterPacienteQueryHandlers.MascararTelefone(telefone);
        Assert.That(resultado, Is.EqualTo(esperado));
    }

    [TestCase(null, null)]
    [TestCase("", "")]
    public void MascararTelefone_ValoresVazios_RetornaComoEsta(string? telefone, string? esperado)
    {
        var resultado = ObterPacienteQueryHandlers.MascararTelefone(telefone);
        Assert.That(resultado, Is.EqualTo(esperado));
    }
}

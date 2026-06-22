using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de unidade para AtualizarAlertasProntuarioCommandHandler (briefing 2026-06-22_002).
/// Cobre: gating por papel (R2/R3), multi-tenant (CA13), LGPD audit (R8), domínio.
/// </summary>
[TestFixture]
public class AtualizarAlertasProntuarioCommandHandlerTests
{
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<ProntuarioQueryRepository> _queryRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private AtualizarAlertasProntuarioCommandHandler _sut;

    private const long EstabelecimentoId = 1L;
    private const long OutroEstabId = 2L;
    private const long PacienteId = 100L;
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();

    // Connection inerte — apenas para satisfazer o construtor; o mock intercepta os métodos virtual.
    private static readonly AppReadConnectionString _connInerte = new("Host=localhost;Database=test_inerte");

    [SetUp]
    public void SetUp()
    {
        _pacienteRepo = new Mock<IPacienteRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        // ProntuarioQueryRepository é concreto — mock via subclasse (VerificarVinculoAtendimento é virtual).
        _queryRepo = new Mock<ProntuarioQueryRepository>(_connInerte);
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new AtualizarAlertasProntuarioCommandHandler(
            _pacienteRepo.Object, _prontuarioRepo.Object, _queryRepo.Object, _acessoLog.Object);
    }

    private static Paciente PacienteAtivo() =>
        Paciente.Cadastrar(EstabelecimentoId, "Ana Silva", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);

    private static Prontuario ProntuarioDoEstab() =>
        Prontuario.Iniciar(PacienteId, EstabelecimentoId, 0L);

    private AtualizarAlertasProntuarioCommand Cmd(TenantPapel papel, Guid? usuarioId = null) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = usuarioId ?? (papel == TenantPapel.Dono ? _donoId : _profissionalId),
        SolicitantePapel = papel,
        Alertas = new[] { "Alergia a penicilina" },
    };

    [Test]
    public async Task Handle_Dono_SalvaAlertasSemVerificarVinculo()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioDoEstab());

        await _sut.Handle(Cmd(TenantPapel.Dono));

        _pacienteRepo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Once);
        // Dono: nunca deve chamar a verificação de vínculo
        _queryRepo.Verify(q => q.VerificarVinculoAtendimento(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Handle_ProfissionalComVinculo_SalvaAlertas()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _queryRepo.Setup(q => q.VerificarVinculoAtendimento(PacienteId, EstabelecimentoId, _profissionalId))
                  .ReturnsAsync(true);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioDoEstab());

        await _sut.Handle(Cmd(TenantPapel.Profissional));

        _pacienteRepo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Once);
    }

    [Test]
    public void Handle_ProfissionalSemVinculo_LancaForbidden()
    {
        // CA12: Profissional sem vínculo de atendimento → 403 ForbiddenException.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _queryRepo.Setup(q => q.VerificarVinculoAtendimento(PacienteId, EstabelecimentoId, _profissionalId))
                  .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(Cmd(TenantPapel.Profissional)));
        Assert.That(ex.Message, Is.EqualTo("Sem permissão."));
        _pacienteRepo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public void Handle_Recepcionista_LancaForbidden()
    {
        // CA12: Recepcionista nunca gerencia alertas → 403 ForbiddenException.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(Cmd(TenantPapel.Recepcionista)));
        Assert.That(ex.Message, Is.EqualTo("Sem permissão."));
        // Recepcionista: nunca deve chamar VerificarVinculoAtendimento
        _queryRepo.Verify(q => q.VerificarVinculoAtendimento(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void Handle_SemAcesso_LancaForbidden()
    {
        // CA12: Papel desconhecido → 403 ForbiddenException.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(Cmd(TenantPapel.SemAcesso)));
        Assert.That(ex.Message, Is.EqualTo("Sem permissão."));
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        // CA13: paciente de outro estabelecimento retorna nulo → mensagem genérica.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(TenantPapel.Dono)));
        Assert.That(ex.Message, Is.EqualTo("Não encontrado."),
            "Mensagem genérica — não deve revelar existência de paciente de outro tenant.");
        _pacienteRepo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    [Test]
    public async Task Handle_Dono_AuditaEscritaNoProntuario()
    {
        // R8 / CA15: escrita de alertas deve gerar linha de audit no prontuário.
        var prontuario = ProntuarioDoEstab();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(prontuario, 999L);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(prontuario);

        await _sut.Handle(Cmd(TenantPapel.Dono));

        _acessoLog.Verify(a => a.RegistrarAsync(
            999L, _donoId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once,
            "Audit de escrita obrigatório.");
    }

    [Test]
    public async Task Handle_SemProntuarioIniciado_SalvaAlertasSemAudit()
    {
        // Prontuário pode não ter sido iniciado ainda — nesse caso audit é ignorado (best-effort).
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        await _sut.Handle(Cmd(TenantPapel.Dono));

        _pacienteRepo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Once);
        // Sem prontuário iniciado, audit não é possível — mas a operação não deve falhar.
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_AuditFalha_OperacaoContinuaExecutando()
    {
        // R8: falha no audit não deve bloquear a operação clínica.
        var prontuario = ProntuarioDoEstab();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(prontuario, 999L);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(prontuario);
        _acessoLog.Setup(a => a.RegistrarAsync(
                It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()))
            .ThrowsAsync(new Exception("Falha no audit"));

        // Não deve propagar a exceção do audit
        Assert.DoesNotThrowAsync(() => _sut.Handle(Cmd(TenantPapel.Dono)));
        _pacienteRepo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Once);
    }
}

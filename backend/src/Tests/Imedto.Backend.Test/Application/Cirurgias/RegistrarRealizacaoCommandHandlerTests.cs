using Imedto.Backend.Application.Cirurgias.Commands;
using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cirurgias;

[TestFixture]
public class RegistrarRealizacaoCommandHandlerTests
{
    private Mock<IProcedimentoCirurgicoRepository> _repo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private RegistrarRealizacaoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ProcedimentoId = 99;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProcedimentoCirurgicoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new RegistrarRealizacaoCommandHandler(_repo.Object, _acessoLog.Object);
    }

    private static ProcedimentoCirurgico Planejado(long estabId)
    {
        return ProcedimentoCirurgico.Planejar(
            pacienteId: 100L, prontuarioId: 200L,
            estabelecimentoId: estabId, agendamentoId: null,
            cirurgiaPrincipal: "Cirurgia X", cirurgiaCodigo: null,
            dataAgendada: DateTime.UtcNow.AddDays(-1),
            equipeInicial: new[] {
                new ProcedimentoCirurgico.EquipeInicialPayload(Guid.NewGuid(), PapelCirurgia.Cirurgiao)
            });
    }

    private RegistrarRealizacaoCommand Cmd() => new()
    {
        ProcedimentoId = ProcedimentoId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId,
        DataRealizada = DateTime.UtcNow.AddHours(-2),
        DescricaoCirurgica = "Realizada sem intercorrencias.",
    };

    [Test]
    public async Task Handle_DoMesmoTenant_RegistraEAudita()
    {
        var proc = Planejado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ProcedimentoId, EstabelecimentoId)).ReturnsAsync(proc);

        await _sut.Handle(Cmd());

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Realizado));
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessException()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _repo.Setup(r => r.ObterPorIdOuNulo(ProcedimentoId, EstabelecimentoId))
            .ReturnsAsync((ProcedimentoCirurgico?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    [Test]
    public void Handle_DataNoFuturo_LancaBusinessExceptionDoAggregate()
    {
        var proc = Planejado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ProcedimentoId, EstabelecimentoId)).ReturnsAsync(proc);

        var cmd = Cmd();
        cmd.DataRealizada = DateTime.UtcNow.AddDays(7);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("futuro").Or.Contain("data"));
    }
}

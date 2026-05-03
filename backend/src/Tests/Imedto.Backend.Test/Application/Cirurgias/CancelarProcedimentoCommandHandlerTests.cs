using Imedto.Backend.Application.Cirurgias.Commands;
using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cirurgias;

[TestFixture]
public class CancelarProcedimentoCommandHandlerTests
{
    private Mock<IProcedimentoCirurgicoRepository> _repo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private CancelarProcedimentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ProcedimentoId = 99;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProcedimentoCirurgicoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new CancelarProcedimentoCommandHandler(_repo.Object, _acessoLog.Object);
    }

    private static ProcedimentoCirurgico Planejado(long estabId)
    {
        return ProcedimentoCirurgico.Planejar(
            pacienteId: 100L, prontuarioId: 200L,
            estabelecimentoId: estabId, agendamentoId: null,
            cirurgiaPrincipal: "Cirurgia X", cirurgiaCodigo: null,
            dataAgendada: DateTime.UtcNow.AddDays(7),
            equipeInicial: new[] {
                new ProcedimentoCirurgico.EquipeInicialPayload(Guid.NewGuid(), PapelCirurgia.Cirurgiao)
            });
    }

    [Test]
    public async Task Handle_DoMesmoTenant_CancelaEAudita()
    {
        var proc = Planejado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorId(ProcedimentoId)).ReturnsAsync(proc);

        await _sut.Handle(new CancelarProcedimentoCommand
        {
            ProcedimentoId = ProcedimentoId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            Motivo = "Paciente desistiu",
        });

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Cancelado));
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorId(ProcedimentoId)).ReturnsAsync(Planejado(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarProcedimentoCommand
        {
            ProcedimentoId = ProcedimentoId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            Motivo = "tentativa cross-tenant",
        }));
        Assert.That(ex.Message, Does.Contain("não pertence"));
        _repo.Verify(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()), Times.Never);
    }

    [Test]
    public void Handle_MotivoVazio_LancaBusinessExceptionDoAggregate()
    {
        var proc = Planejado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorId(ProcedimentoId)).ReturnsAsync(proc);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarProcedimentoCommand
        {
            ProcedimentoId = ProcedimentoId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            Motivo = " ",
        }));
        Assert.That(ex.Message, Does.Contain("Motivo"));
    }
}

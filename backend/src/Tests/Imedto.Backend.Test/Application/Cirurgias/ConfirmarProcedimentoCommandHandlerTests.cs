using Imedto.Backend.Application.Cirurgias.Commands;
using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Cirurgias.Events;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cirurgias;

[TestFixture]
public class ConfirmarProcedimentoCommandHandlerTests
{
    private Mock<IProcedimentoCirurgicoRepository> _repo;
    private Mock<IEventBus> _events;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private ConfirmarProcedimentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ProcedimentoId = 99;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProcedimentoCirurgicoRepository>();
        _events = new Mock<IEventBus>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new ConfirmarProcedimentoCommandHandler(_repo.Object, _events.Object, _acessoLog.Object);
    }

    private static ProcedimentoCirurgico Planejado(long estabId)
    {
        return ProcedimentoCirurgico.Planejar(
            pacienteId: 100L, prontuarioId: 200L,
            estabelecimentoId: estabId, agendamentoId: null,
            cirurgiaPrincipal: "Cirurgia X", cirurgiaCodigo: "001",
            dataAgendada: DateTime.UtcNow.AddDays(7),
            equipeInicial: new[] {
                new ProcedimentoCirurgico.EquipeInicialPayload(Guid.NewGuid(), PapelCirurgia.Cirurgiao)
            });
    }

    [Test]
    public async Task Handle_DoMesmoTenant_ConfirmaPersisteAuditEEvento()
    {
        var proc = Planejado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ProcedimentoId, EstabelecimentoId)).ReturnsAsync(proc);

        await _sut.Handle(new ConfirmarProcedimentoCommand
        {
            ProcedimentoId = ProcedimentoId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
        });

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Confirmado));
        _repo.Verify(r => r.Salvar(proc), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
        _events.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is ProcedimentoConfirmadoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessExceptionENaoAudita()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null (de outro tenant).
        _repo.Setup(r => r.ObterPorIdOuNulo(ProcedimentoId, EstabelecimentoId))
            .ReturnsAsync((ProcedimentoCirurgico?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ConfirmarProcedimentoCommand
        {
            ProcedimentoId = ProcedimentoId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _repo.Verify(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()), Times.Never);
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_SolicitanteEmpty_NaoChamaAudit()
    {
        var proc = Planejado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ProcedimentoId, EstabelecimentoId)).ReturnsAsync(proc);

        await _sut.Handle(new ConfirmarProcedimentoCommand
        {
            ProcedimentoId = ProcedimentoId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = Guid.Empty,
        });

        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never,
            "Audit nao deve ser registrado quando solicitante eh anonimo (jobs/sistema).");
    }
}

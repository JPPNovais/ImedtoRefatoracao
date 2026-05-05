using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

[TestFixture]
public class ConverterOrcamentoEmCirurgiaCommandHandlerTests
{
    private Mock<IOrcamentoRepository> _orcRepo;
    private Mock<IProcedimentoCirurgicoRepository> _procRepo;
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IEventBus> _events;
    private ConverterOrcamentoEmCirurgiaCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long OrcamentoId = 99;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;

    [SetUp]
    public void SetUp()
    {
        _orcRepo = new Mock<IOrcamentoRepository>();
        _procRepo = new Mock<IProcedimentoCirurgicoRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _events = new Mock<IEventBus>();
        _sut = new ConverterOrcamentoEmCirurgiaCommandHandler(
            _orcRepo.Object, _procRepo.Object, _prontuarioRepo.Object, _events.Object);
    }

    private static Orcamento OrcamentoAprovado(long estabId, bool comCirurgias = true)
    {
        var orc = Orcamento.Criar(
            estabId, PacienteId,
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            null, Guid.NewGuid(), null,
            itens: new[] { new Orcamento.ItemPayload("X", 1, 100m, 0m) },
            equipe: new[] { new Orcamento.EquipePayload(Guid.NewGuid(), "cirurgiao", 100m) },
            cirurgias: comCirurgias
                ? new[] { new Orcamento.CirurgiaPayload(null, "Cirurgia X", 1, 60, 1000m) }
                : Array.Empty<Orcamento.CirurgiaPayload>());
        orc.Enviar();
        orc.Aprovar();
        return orc;
    }

    private static Prontuario ProntuarioJaIniciado()
    {
        var p = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, ProntuarioId);
        return p;
    }

    private ConverterOrcamentoEmCirurgiaCommand Cmd() => new()
    {
        OrcamentoId = OrcamentoId,
        EstabelecimentoId = EstabelecimentoId,
        DataAgendada = DateTime.UtcNow.AddDays(7),
    };

    [Test]
    public async Task Handle_OrcamentoAprovado_CriaProcedimentoEVincula()
    {
        _orcRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabelecimentoId)).ReturnsAsync(OrcamentoAprovado(EstabelecimentoId));
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());
        _procRepo.Setup(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()))
                 .Callback<ProcedimentoCirurgico>(p =>
                     typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 555L))
                 .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.ProcedimentoCirurgicoIdCriado, Is.EqualTo(555L));
        _procRepo.Verify(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()), Times.Once);
        _orcRepo.Verify(r => r.Salvar(It.IsAny<Orcamento>()), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _orcRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabelecimentoId))
            .ReturnsAsync((Orcamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Orçamento não encontrado."));
        _procRepo.Verify(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()), Times.Never);
    }

    [Test]
    public void Handle_OrcamentoNaoAprovado_LancaBusinessException()
    {
        var orc = Orcamento.Criar(
            EstabelecimentoId, PacienteId,
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            null, Guid.NewGuid(), null,
            itens: new[] { new Orcamento.ItemPayload("X", 1, 100m, 0m) }); // Rascunho
        _orcRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabelecimentoId)).ReturnsAsync(orc);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("aprovados"));
    }

    [Test]
    public void Handle_OrcamentoSemCirurgias_LancaBusinessException()
    {
        _orcRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabelecimentoId))
                .ReturnsAsync(OrcamentoAprovado(EstabelecimentoId, comCirurgias: false));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Orçamento sem cirurgias"));
    }
}

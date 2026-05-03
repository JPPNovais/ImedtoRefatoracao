using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

[TestFixture]
public class AprovarOrcamentoCommandHandlerTests
{
    private Mock<IOrcamentoRepository> _repo;
    private Mock<IEventBus> _events;
    private AprovarOrcamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long OrcamentoId = 99;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IOrcamentoRepository>();
        _events = new Mock<IEventBus>();
        _sut = new AprovarOrcamentoCommandHandler(_repo.Object, _events.Object);
    }

    private static Orcamento OrcamentoEnviado(long estabId)
    {
        var orc = Orcamento.Criar(
            estabId, 100L,
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            null, Guid.NewGuid(), null,
            itens: new[] { new Orcamento.ItemPayload("Consulta", 1, 100m, 0m) });
        orc.Enviar();
        return orc;
    }

    [Test]
    public async Task Handle_OrcamentoEnviadoDoMesmoTenant_AprovaEPublicaEvento()
    {
        var orc = OrcamentoEnviado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdCompleto(OrcamentoId)).ReturnsAsync(orc);

        await _sut.Handle(new AprovarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Aprovado));
        _repo.Verify(r => r.Salvar(orc), Times.Once);
        _events.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is OrcamentoAprovadoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenericaENaoSalva()
    {
        _repo.Setup(r => r.ObterPorIdCompleto(OrcamentoId)).ReturnsAsync(OrcamentoEnviado(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AprovarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Orçamento não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<Orcamento>()), Times.Never);
    }
}

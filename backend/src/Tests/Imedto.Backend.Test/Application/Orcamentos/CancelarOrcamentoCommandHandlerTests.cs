using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

[TestFixture]
public class CancelarOrcamentoCommandHandlerTests
{
    private Mock<IOrcamentoRepository> _repo;
    private CancelarOrcamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long OrcamentoId = 99;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IOrcamentoRepository>();
        _sut = new CancelarOrcamentoCommandHandler(_repo.Object);
    }

    private static Orcamento OrcamentoRascunho(long estabId) =>
        Orcamento.Criar(
            estabId, 100L,
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            null, Guid.NewGuid(), null,
            itens: new[] { new Orcamento.ItemPayload("Consulta", 1, 100m, 0m) });

    [Test]
    public async Task Handle_DoMesmoTenant_Cancela()
    {
        var orc = OrcamentoRascunho(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(OrcamentoId)).ReturnsAsync(orc);

        await _sut.Handle(new CancelarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Cancelado));
        _repo.Verify(r => r.Salvar(orc), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(OrcamentoId)).ReturnsAsync(OrcamentoRascunho(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Orçamento não encontrado."));
    }
}

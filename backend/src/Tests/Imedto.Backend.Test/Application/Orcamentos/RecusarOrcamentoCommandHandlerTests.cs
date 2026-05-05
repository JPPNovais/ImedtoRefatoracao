using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

[TestFixture]
public class RecusarOrcamentoCommandHandlerTests
{
    private Mock<IOrcamentoRepository> _repo;
    private RecusarOrcamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long OrcamentoId = 99;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IOrcamentoRepository>();
        _sut = new RecusarOrcamentoCommandHandler(_repo.Object);
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
    public async Task Handle_OrcamentoEnviadoDoMesmoTenant_Recusa()
    {
        var orc = OrcamentoEnviado(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(OrcamentoId, EstabelecimentoId)).ReturnsAsync(orc);

        await _sut.Handle(new RecusarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Recusado));
        _repo.Verify(r => r.Salvar(orc), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _repo.Setup(r => r.ObterPorIdOuNulo(OrcamentoId, EstabelecimentoId))
            .ReturnsAsync((Orcamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RecusarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Orçamento não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<Orcamento>()), Times.Never);
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(OrcamentoId, EstabelecimentoId)).ReturnsAsync((Orcamento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RecusarOrcamentoCommand
        {
            OrcamentoId = OrcamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}

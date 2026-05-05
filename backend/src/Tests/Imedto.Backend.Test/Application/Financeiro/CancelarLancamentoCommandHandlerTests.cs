using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class CancelarLancamentoCommandHandlerTests
{
    private Mock<ILancamentoRepository> _repo;
    private CancelarLancamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long LancamentoId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ILancamentoRepository>();
        _sut = new CancelarLancamentoCommandHandler(_repo.Object);
    }

    private static Lancamento LancamentoNoEstab(long estabId) =>
        Lancamento.Criar(estabId, TipoLancamento.Receita, "Consulta", 100m,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            "Atendimento", Guid.NewGuid());

    [Test]
    public async Task Handle_DoMesmoTenant_Cancela()
    {
        var l = LancamentoNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(LancamentoId, EstabelecimentoId)).ReturnsAsync(l);

        await _sut.Handle(new CancelarLancamentoCommand
        {
            LancamentoId = LancamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(l.Status, Is.EqualTo(StatusLancamento.Cancelado));
        _repo.Verify(r => r.Salvar(l), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(LancamentoId, EstabelecimentoId)).ReturnsAsync((Lancamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarLancamentoCommand
        {
            LancamentoId = LancamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Lançamento não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<Lancamento>()), Times.Never);
    }
}

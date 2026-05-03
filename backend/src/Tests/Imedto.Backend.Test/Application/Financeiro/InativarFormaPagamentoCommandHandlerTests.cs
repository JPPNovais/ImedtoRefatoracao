using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class InativarFormaPagamentoCommandHandlerTests
{
    private Mock<IFormaPagamentoRepository> _repo;
    private InativarFormaPagamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long FormaId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IFormaPagamentoRepository>();
        _sut = new InativarFormaPagamentoCommandHandler(_repo.Object);
    }

    private static FormaPagamento FormaNoEstab(long estabId) =>
        FormaPagamento.Criar(estabId, "Forma");

    [Test]
    public async Task Handle_DoMesmoTenant_Inativa()
    {
        var f = FormaNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(FormaId)).ReturnsAsync(f);

        await _sut.Handle(new InativarFormaPagamentoCommand
        {
            FormaPagamentoId = FormaId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(f.Ativo, Is.False);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(FormaId)).ReturnsAsync(FormaNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarFormaPagamentoCommand
        {
            FormaPagamentoId = FormaId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Forma de pagamento não encontrada."));
    }
}

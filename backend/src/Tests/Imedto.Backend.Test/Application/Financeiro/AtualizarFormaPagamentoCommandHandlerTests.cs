using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class AtualizarFormaPagamentoCommandHandlerTests
{
    private Mock<IFormaPagamentoRepository> _repo;
    private AtualizarFormaPagamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long FormaId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IFormaPagamentoRepository>();
        _sut = new AtualizarFormaPagamentoCommandHandler(_repo.Object);
    }

    private static FormaPagamento FormaNoEstab(long estabId, bool padrao = false) =>
        padrao
            ? FormaPagamento.CriarPadrao(estabId, "Padrao")
            : FormaPagamento.Criar(estabId, "Original");

    private static AtualizarFormaPagamentoCommand Cmd() => new()
    {
        FormaPagamentoId = FormaId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizada",
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaForma()
    {
        var f = FormaNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(FormaId)).ReturnsAsync(f);

        await _sut.Handle(Cmd());

        Assert.That(f.Nome, Is.EqualTo("Atualizada"));
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(FormaId)).ReturnsAsync(FormaNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Forma de pagamento não encontrada."));
    }

    [Test]
    public void Handle_FormaPadrao_LancaBusinessExceptionDoAggregate()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(FormaId)).ReturnsAsync(FormaNoEstab(EstabelecimentoId, padrao: true));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }
}

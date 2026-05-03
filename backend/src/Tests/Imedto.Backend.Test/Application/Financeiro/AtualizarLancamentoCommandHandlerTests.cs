using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class AtualizarLancamentoCommandHandlerTests
{
    private Mock<ILancamentoRepository> _repo;
    private AtualizarLancamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long LancamentoId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ILancamentoRepository>();
        _sut = new AtualizarLancamentoCommandHandler(_repo.Object);
    }

    private static Lancamento LancamentoNoEstab(long estabId) =>
        Lancamento.Criar(estabId, TipoLancamento.Receita, "Original", 100m,
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            "Atendimento", Guid.NewGuid());

    private static AtualizarLancamentoCommand Cmd() => new()
    {
        LancamentoId = LancamentoId,
        EstabelecimentoId = EstabelecimentoId,
        Descricao = "Atualizado",
        Valor = 200m,
        DataVencimento = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
        Categoria = "Nova",
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaCampos()
    {
        var l = LancamentoNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(LancamentoId)).ReturnsAsync(l);

        await _sut.Handle(Cmd());

        Assert.That(l.Descricao, Is.EqualTo("Atualizado"));
        Assert.That(l.Valor, Is.EqualTo(200m));
        _repo.Verify(r => r.Salvar(l), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(LancamentoId)).ReturnsAsync(LancamentoNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Lançamento não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<Lancamento>()), Times.Never);
    }
}

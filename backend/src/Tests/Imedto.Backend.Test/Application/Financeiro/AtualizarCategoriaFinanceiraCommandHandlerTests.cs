using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class AtualizarCategoriaFinanceiraCommandHandlerTests
{
    private Mock<ICategoriaFinanceiraRepository> _repo;
    private AtualizarCategoriaFinanceiraCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long CategoriaId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoriaFinanceiraRepository>();
        _sut = new AtualizarCategoriaFinanceiraCommandHandler(_repo.Object);
    }

    private static CategoriaFinanceira CategoriaNoEstab(long estabId, bool padrao = false) =>
        padrao
            ? CategoriaFinanceira.CriarPadrao(estabId, "Padrao", TipoCategoria.Receita)
            : CategoriaFinanceira.Criar(estabId, "Original", TipoCategoria.Receita);

    private static AtualizarCategoriaFinanceiraCommand Cmd() => new()
    {
        CategoriaId = CategoriaId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizada",
        Tipo = "Despesa",
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaCategoria()
    {
        var c = CategoriaNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId)).ReturnsAsync(c);

        await _sut.Handle(Cmd());

        Assert.That(c.Nome, Is.EqualTo("Atualizada"));
        Assert.That(c.Tipo, Is.EqualTo(TipoCategoria.Despesa));
    }

    [Test]
    public void Handle_TipoInvalido_LancaBusinessException()
    {
        var cmd = Cmd();
        cmd.Tipo = "Bug";
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("Tipo"));
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId)).ReturnsAsync(CategoriaNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Categoria não encontrada."));
    }

    [Test]
    public void Handle_CategoriaPadrao_LancaBusinessExceptionDoAggregate()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId)).ReturnsAsync(CategoriaNoEstab(EstabelecimentoId, padrao: true));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }
}

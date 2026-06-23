using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testes do ReativarCategoriaFinanceiraCommandHandler.
/// CA19 (briefing 2026-06-22_003): estabelecimento pode reativar categoria que inativou,
/// inclusive padrão (M5/R8 — reativar padrão é permitido).
/// </summary>
[TestFixture]
public class ReativarCategoriaFinanceiraCommandHandlerTests
{
    private Mock<ICategoriaFinanceiraRepository> _repo;
    private ReativarCategoriaFinanceiraCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long CategoriaId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoriaFinanceiraRepository>();
        _sut = new ReativarCategoriaFinanceiraCommandHandler(_repo.Object);
    }

    private static CategoriaFinanceira CategoriaCustomizadaInativa(long estabId)
    {
        var c = CategoriaFinanceira.Criar(estabId, "Cat", TipoCategoria.Receita);
        c.Inativar();
        return c;
    }

    private static CategoriaFinanceira CategoriaPadraoInativa(long estabId)
    {
        var c = CategoriaFinanceira.CriarPadrao(estabId, "Consultas", TipoCategoria.Receita);
        c.Inativar();
        return c;
    }

    private ReativarCategoriaFinanceiraCommand Cmd(long estabId = EstabelecimentoId) => new()
    {
        CategoriaId = CategoriaId,
        EstabelecimentoId = estabId,
    };

    // --- caminho feliz: customizada inativa ---

    [Test]
    public async Task Handle_CategoriaInativa_ReativaComSucesso()
    {
        var c = CategoriaCustomizadaInativa(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(c);

        await _sut.Handle(Cmd());

        Assert.That(c.Ativo, Is.True);
    }

    // --- M5: padrão inativada pelo estabelecimento pode ser reativada (R8) ---

    [Test]
    public async Task Handle_CategoriaPadrao_ReativaComSucesso()
    {
        var c = CategoriaPadraoInativa(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(c);

        await _sut.Handle(Cmd());

        Assert.That(c.Ativo, Is.True);
        Assert.That(c.Padrao, Is.True, "Padrao deve permanecer true após reativar.");
    }

    // --- multi-tenant: mensagem genérica, não vaza existência ---

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync((CategoriaFinanceira?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Categoria não encontrada."));
    }
}

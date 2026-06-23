using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testes do InativarCategoriaFinanceiraCommandHandler.
/// M5 (briefing 2026-06-22_003): inativar categoria Padrao=true agora é PERMITIDO (R8).
/// Renomear e excluir padrão continuam BLOQUEADOS (CA18).
/// </summary>
[TestFixture]
public class InativarCategoriaFinanceiraCommandHandlerTests
{
    private Mock<ICategoriaFinanceiraRepository> _repo;
    private InativarCategoriaFinanceiraCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long CategoriaId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoriaFinanceiraRepository>();
        _sut = new InativarCategoriaFinanceiraCommandHandler(_repo.Object);
    }

    private static CategoriaFinanceira CategoriaCustomizada(long estabId) =>
        CategoriaFinanceira.Criar(estabId, "Cat", TipoCategoria.Receita);

    private static CategoriaFinanceira CategoriaPadrao(long estabId) =>
        CategoriaFinanceira.CriarPadrao(estabId, "Consultas", TipoCategoria.Receita);

    private InativarCategoriaFinanceiraCommand Cmd(long estabId = EstabelecimentoId) => new()
    {
        CategoriaId = CategoriaId,
        EstabelecimentoId = estabId,
    };

    // --- caminho feliz: customizada ---

    [Test]
    public async Task Handle_CategoriaCustomizada_InativaComSucesso()
    {
        var c = CategoriaCustomizada(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(c);

        await _sut.Handle(Cmd());

        Assert.That(c.Ativo, Is.False);
    }

    // --- M5: padrão pode ser inativada (R8) ---

    [Test]
    public async Task Handle_CategoriaPadrao_InativaComSucesso_M5()
    {
        var c = CategoriaPadrao(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(c);

        // Antes do M5 isso lançava BusinessException; agora deve completar sem erro.
        await _sut.Handle(Cmd());

        Assert.That(c.Ativo, Is.False);
        Assert.That(c.Padrao, Is.True, "Padrao deve permanecer true após inativar.");
    }

    // --- proteções remanescentes de padrão ---

    [Test]
    public void Handle_CategoriaPadrao_RenomearSegueBloqueado_CA18()
    {
        var c = CategoriaPadrao(EstabelecimentoId);

        var ex = Assert.Throws<BusinessException>(() => c.Atualizar("Novo Nome", TipoCategoria.Receita));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }

    // --- multi-tenant ---

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync((CategoriaFinanceira?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Categoria não encontrada."));
    }

    // --- domínio: inativar idempotente ---

    [Test]
    public async Task Handle_JaInativa_NaoRepeteSalvar()
    {
        var c = CategoriaCustomizada(EstabelecimentoId);
        c.Inativar(); // já inativa
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(c);

        await _sut.Handle(Cmd());

        // Salvar é chamado mas Ativo permanece false e sem exceção
        Assert.That(c.Ativo, Is.False);
    }
}

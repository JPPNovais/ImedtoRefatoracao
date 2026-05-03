using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class InativarCategoriaFinanceiraCommandHandlerTests
{
    private Mock<ICategoriaFinanceiraRepository> _repo;
    private InativarCategoriaFinanceiraCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long CategoriaId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoriaFinanceiraRepository>();
        _sut = new InativarCategoriaFinanceiraCommandHandler(_repo.Object);
    }

    private static CategoriaFinanceira CategoriaNoEstab(long estabId) =>
        CategoriaFinanceira.Criar(estabId, "Cat", TipoCategoria.Receita);

    [Test]
    public async Task Handle_DoMesmoTenant_Inativa()
    {
        var c = CategoriaNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId)).ReturnsAsync(c);

        await _sut.Handle(new InativarCategoriaFinanceiraCommand
        {
            CategoriaId = CategoriaId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(c.Ativo, Is.False);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CategoriaId)).ReturnsAsync(CategoriaNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarCategoriaFinanceiraCommand
        {
            CategoriaId = CategoriaId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Categoria não encontrada."));
    }
}

using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class ExcluirVariavelPoolCommandHandlerTests
{
    private Mock<IProntuarioVariavelPoolRepository> _repo;
    private ExcluirVariavelPoolCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ItemId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProntuarioVariavelPoolRepository>();
        _sut = new ExcluirVariavelPoolCommandHandler(_repo.Object);
    }

    private static ProntuarioVariavelPool ItemDoEstab(long estabId) =>
        ProntuarioVariavelPool.CriarDoEstabelecimento(estabId, TipoVariavelPool.Alergia, "Item");

    [Test]
    public async Task Handle_DoMesmoTenant_Exclui()
    {
        var item = ItemDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(item);

        await _sut.Handle(new ExcluirVariavelPoolCommand
        {
            ItemId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
        });

        _repo.Verify(r => r.Excluir(item), Times.Once);
    }

    [Test]
    public void Handle_PadraoSistema_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId))
             .ReturnsAsync(ProntuarioVariavelPool.CriarPadraoSistema(TipoVariavelPool.Alergia, "Padrao"));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ExcluirVariavelPoolCommand
        {
            ItemId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(ItemDoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ExcluirVariavelPoolCommand
        {
            ItemId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Opção não encontrada."));
    }
}

using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class AtualizarVariavelPoolCommandHandlerTests
{
    private Mock<IProntuarioVariavelPoolRepository> _repo;
    private AtualizarVariavelPoolCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ItemId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProntuarioVariavelPoolRepository>();
        _sut = new AtualizarVariavelPoolCommandHandler(_repo.Object);
    }

    private static ProntuarioVariavelPool ItemDoEstab(long estabId) =>
        ProntuarioVariavelPool.CriarDoEstabelecimento(estabId, TipoVariavelPool.Alergia, "Original");

    private static ProntuarioVariavelPool ItemPadraoSistema() =>
        ProntuarioVariavelPool.CriarPadraoSistema(TipoVariavelPool.Alergia, "Padrao");

    private AtualizarVariavelPoolCommand Cmd() => new()
    {
        ItemId = ItemId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizado",
    };

    [Test]
    public async Task Handle_DoMesmoTenant_Renomeia()
    {
        var item = ItemDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync(item);
        _repo.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, TipoVariavelPool.Alergia, "Atualizado", item.Id))
             .ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(item.Nome, Is.EqualTo("Atualizado"));
        _repo.Verify(r => r.Salvar(item), Times.Once);
    }

    [Test]
    public void Handle_PadraoSistema_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync(ItemPadraoSistema());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync((ProntuarioVariavelPool?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Opção não encontrada."));
    }

    [Test]
    public void Handle_NomeDuplicado_LancaBusinessException()
    {
        var item = ItemDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync(item);
        _repo.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, TipoVariavelPool.Alergia, "Atualizado", item.Id))
             .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("nome"));
    }
}

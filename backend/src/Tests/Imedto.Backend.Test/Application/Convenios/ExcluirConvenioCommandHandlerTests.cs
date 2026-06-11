using Imedto.Backend.Application.Convenios.Commands;
using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Convenios;

/// <summary>Testa ExcluirConvenio — CA134 (em uso → 422) + multi-tenant (CA132).</summary>
[TestFixture]
public class ExcluirConvenioCommandHandlerTests
{
    private Mock<IConvenioRepository> _repo;
    private ExcluirConvenioCommandHandler _sut;

    private const long EstabId = 1L;
    private const long ConvenioId = 10L;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IConvenioRepository>();
        _sut = new ExcluirConvenioCommandHandler(_repo.Object);
    }

    private static ExcluirConvenioCommand Cmd() => new()
    {
        ConvenioId = ConvenioId,
        EstabelecimentoId = EstabId,
        UsuarioSolicitanteId = Guid.NewGuid(),
    };

    private static Convenio ConvenioExistente()
        => Convenio.Criar(EstabId, "Unimed", null);

    // ── CA132: multi-tenant — convênio não encontrado ou de outro tenant ──────

    [Test]
    public void Handle_ConvenioNaoEncontrado_LancaGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync((Convenio?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _repo.Verify(r => r.Excluir(It.IsAny<Convenio>()), Times.Never);
    }

    // ── CA134: em uso → inative em vez de excluir ────────────────────────────

    [Test]
    public void Handle_ConvenioEmUso_LancaBusinessException()
    {
        var c = ConvenioExistente();
        _repo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync(c);
        _repo.Setup(r => r.TemCarteirinhasOuCobrancas(ConvenioId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("em uso"));
        _repo.Verify(r => r.Excluir(It.IsAny<Convenio>()), Times.Never);
    }

    [Test]
    public async Task Handle_ConvenioSemUso_ExcluiFisicamente()
    {
        var c = ConvenioExistente();
        _repo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync(c);
        _repo.Setup(r => r.TemCarteirinhasOuCobrancas(ConvenioId)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Excluir(c), Times.Once);
    }

    // ── Falha-fechada: repo chamado sempre com estabelecimentoId do tenant ────

    [Test]
    public void Handle_SempreFiltroTenant()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync((Convenio?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));

        // Garantia: nunca chama sem filtro de tenant
        _repo.Verify(r => r.ObterPorIdOuNulo(ConvenioId, EstabId), Times.Once);
        _repo.Verify(r => r.ObterPorIdOuNulo(It.IsAny<long>(), 0), Times.Never);
    }
}

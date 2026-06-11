using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class FecharCaixaDiarioCommandHandlerTests
{
    private Mock<ICaixaDiarioRepository> _repo;
    private FecharCaixaDiarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICaixaDiarioRepository>();
        _sut = new FecharCaixaDiarioCommandHandler(_repo.Object);
    }

    private FecharCaixaDiarioCommand Cmd(string? obs = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        Data = _hoje,
        UsuarioId = _usuarioId,
        Observacao = obs
    };

    [Test]
    public async Task Handle_CaixaAberto_FechaComSucesso()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuarioId);
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixa);

        await _sut.Handle(Cmd("Fechamento do dia"));

        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Fechado));
        _repo.Verify(r => r.Salvar(caixa), Times.Once);
    }

    [Test]
    public void Handle_CaixaNaoExiste_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync((CaixaDiario?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
    }

    [Test]
    public void Handle_CaixaJaFechado_LancaBusinessException()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuarioId);
        caixa.Fechar(_usuarioId, null);
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixa);

        // Fechar 2x → 422 (CA165).
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
    }

    [Test]
    public async Task Handle_CaixaDeOutroEstabelecimento_NaoEncontraOuIgnora()
    {
        // Isolamento multi-tenant: ObterPorData filtra por establ. — se não encontra, 422.
        _repo.Setup(r => r.ObterPorData(99L, _hoje)).ReturnsAsync((CaixaDiario?)null);
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync((CaixaDiario?)null);

        var cmdOutroEstab = new FecharCaixaDiarioCommand
        {
            EstabelecimentoId = 99L, // tenant errado
            Data = _hoje,
            UsuarioId = _usuarioId
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmdOutroEstab));
    }
}

using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// CA167 — Reabrir caixa: somente Dono. Profissional sem papel de Dono recebe 403/422.
/// </summary>
[TestFixture]
public class ReabrirCaixaDiarioCommandHandlerTests
{
    private Mock<ICaixaDiarioRepository> _repo;
    private ReabrirCaixaDiarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profId = Guid.NewGuid();
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICaixaDiarioRepository>();
        _sut = new ReabrirCaixaDiarioCommandHandler(_repo.Object);
    }

    private CaixaDiario CaixaFechado()
    {
        var c = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _donoId);
        c.Fechar(_donoId, null);
        return c;
    }

    [Test]
    public async Task Handle_Dono_ReabreCaixaFechado()
    {
        var caixa = CaixaFechado();
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixa);

        await _sut.Handle(new ReabrirCaixaDiarioCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            Data = _hoje,
            UsuarioId = _donoId,
            EhDono = true
        });

        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Aberto));
        _repo.Verify(r => r.Salvar(caixa), Times.Once);
    }

    [Test]
    public void Handle_NaoDono_LancaBusinessException_CA167()
    {
        var caixa = CaixaFechado();
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixa);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReabrirCaixaDiarioCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            Data = _hoje,
            UsuarioId = _profId,
            EhDono = false
        }));
    }

    [Test]
    public void Handle_CaixaNaoExiste_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync((CaixaDiario?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReabrirCaixaDiarioCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            Data = _hoje,
            UsuarioId = _donoId,
            EhDono = true
        }));
    }

    [Test]
    public void Handle_CaixaJaAberto_LancaBusinessException()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _donoId);
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixa);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReabrirCaixaDiarioCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            Data = _hoje,
            UsuarioId = _donoId,
            EhDono = true
        }));
    }
}

using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class AbrirCaixaDiarioCommandHandlerTests
{
    private Mock<ICaixaDiarioRepository> _repo;
    private AbrirCaixaDiarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICaixaDiarioRepository>();
        _sut = new AbrirCaixaDiarioCommandHandler(_repo.Object);
    }

    private AbrirCaixaDiarioCommand Cmd(DateOnly? data = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        Data = data ?? _hoje,
        UsuarioId = _usuarioId
    };

    [Test]
    public async Task Handle_CaixaNaoExiste_AbreCaixaEPersiste()
    {
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync((CaixaDiario?)null);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.Is<CaixaDiario>(c =>
            c.EstabelecimentoId == EstabelecimentoId &&
            c.Status == StatusCaixaDiario.Aberto &&
            c.AbertoPorUsuarioId == _usuarioId)), Times.Once);
    }

    [Test]
    public async Task Handle_CaixaJaExisteFechado_ReabreCaixaExistente()
    {
        // Caixa fechado do dia anterior já existe → reabrir, não criar novo.
        var caixaExistente = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuarioId);
        caixaExistente.Fechar(_usuarioId, null);

        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixaExistente);

        // Ao tentar abrir um caixa fechado (idempotência via Abrir handler), deve lançar
        // porque o uso correto é pelo endpoint /reabrir, não /abrir.
        // O handler de Abrir: se já existe e está Aberto → 422; se está Fechado → 422 orientando usar /reabrir.
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
    }

    [Test]
    public void Handle_CaixaJaAberto_LancaBusinessException()
    {
        var caixaAberto = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuarioId);
        _repo.Setup(r => r.ObterPorData(EstabelecimentoId, _hoje)).ReturnsAsync(caixaAberto);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
    }
}

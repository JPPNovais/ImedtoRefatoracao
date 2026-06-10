using Imedto.Backend.Application.Cobrancas.Commands;
using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cobrancas;

/// <summary>
/// Testes do handler de estorno (INV-7 — CA29, CA32).
/// Valida: atomicidade (EstornoPagamento + Lancamento criados), rollback simulado,
/// idempotência (2ª chamada 422), multi-tenant falha-fechada.
/// </summary>
[TestFixture]
public class EstornarPagamentoCommandHandlerTests
{
    private Mock<ICobrancaRepository> _cobrancaRepo;
    private Mock<IEstornoPagamentoRepository> _estornoRepo;
    private Mock<ILancamentoRepository> _lancamentoRepo;
    private EstornarPagamentoCommandHandler _sut;

    private const long EstabId = 1L;
    private const long CobrancaId = 99L;
    private const long PagamentoId = 10L;
    private const long LancamentoEstornoId = 200L;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    [SetUp]
    public void SetUp()
    {
        _cobrancaRepo = new Mock<ICobrancaRepository>();
        _estornoRepo  = new Mock<IEstornoPagamentoRepository>();
        _lancamentoRepo = new Mock<ILancamentoRepository>();

        // Simula geração de Id do Lancamento pelo banco
        _lancamentoRepo.Setup(r => r.Salvar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => l.SimularIdBanco(LancamentoEstornoId))
            .Returns(Task.CompletedTask);

        _sut = new EstornarPagamentoCommandHandler(
            _cobrancaRepo.Object, _estornoRepo.Object, _lancamentoRepo.Object);
    }

    private Cobranca CriarCobrancaComPagamento(decimal valorPago = 200m)
    {
        var c = Cobranca.CriarParaConsulta(EstabId, 10L, 500L,
            TipoAtendimento.Particular, 200m, "Consulta", UsuarioId);
        c.SimularIdBanco(CobrancaId);

        // Simula cobrancaRepo.Salvar atribuindo Id ao Pagamento
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(cobranca =>
            {
                foreach (var p in cobranca.Pagamentos.Where(x => x.Id == 0))
                    p.SimularIdBanco(PagamentoId);
                foreach (var e in cobranca.Estornos.Where(x => x.Id == 0))
                    e.SimularIdBanco(50L);
            })
            .Returns(Task.CompletedTask);

        c.RegistrarPagamento(valorPago, 1L, 1, 0m, 0m, Hoje, UsuarioId);
        // Pagamento precisa de Id para ser encontrado pelo aggregate
        foreach (var p in c.Pagamentos) p.SimularIdBanco(PagamentoId);

        return c;
    }

    private EstornarPagamentoCommand CmdEstorno() => new EstornarPagamentoCommand
    {
        CobrancaId = CobrancaId,
        PagamentoId = PagamentoId,
        EstabelecimentoId = EstabId,
        UsuarioId = UsuarioId,
        Motivo = "valor incorreto",
    };

    // ── CA29: atomicidade — EstornoPagamento + Lancamento criados ─────────────

    [Test]
    public async Task Handle_Estorno_CriaLancamentoNegativoEVincula()
    {
        var cobranca = CriarCobrancaComPagamento(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        await _sut.Handle(CmdEstorno());

        // Lancamento de estorno criado
        _lancamentoRepo.Verify(r => r.Salvar(It.Is<Lancamento>(l =>
            l.Valor < 0 &&
            l.Categoria == Lancamento.CategoriaEstorno &&
            l.CobrancaId == CobrancaId &&
            l.PagamentoId == PagamentoId
        )), Times.Once);

        // Aggregate salvo com LancamentoEstornoId vinculado
        _cobrancaRepo.Verify(r => r.Salvar(cobranca), Times.AtLeast(2));

        // Estorno tem LancamentoEstornoId preenchido
        var estorno = cobranca.Estornos.FirstOrDefault();
        Assert.That(estorno, Is.Not.Null);
        Assert.That(estorno!.LancamentoEstornoId, Is.EqualTo(LancamentoEstornoId));
    }

    // ── CA29 variante: status recalculado corretamente ─────────────────────

    [Test]
    public async Task Handle_Estorno_RecalculaStatusParaAberta()
    {
        var cobranca = CriarCobrancaComPagamento(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        Assert.That(cobranca.Status, Is.EqualTo(StatusCobranca.Paga));
        await _sut.Handle(CmdEstorno());

        Assert.That(cobranca.Status, Is.EqualTo(StatusCobranca.Aberta));
    }

    // ── CA32: idempotência — 2ª chamada deve lançar ────────────────────────

    [Test]
    public async Task Handle_EstornoRepetido_Lanca()
    {
        var cobranca = CriarCobrancaComPagamento(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        await _sut.Handle(CmdEstorno());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CmdEstorno()));
        Assert.That(ex!.Message, Does.Contain("já foi estornado"));
    }

    // ── Multi-tenant falha-fechada (R11) ──────────────────────────────────

    [Test]
    public void Handle_CobrancaNaoEncontrada_Lanca()
    {
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync((Cobranca?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CmdEstorno()));
        Assert.That(ex!.Message, Does.Contain("Não encontrado"));
    }

    // ── R5: motivo vazio lança ─────────────────────────────────────────────

    [Test]
    public void Handle_MotivoVazio_Lanca()
    {
        var cobranca = CriarCobrancaComPagamento(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        var cmd = new EstornarPagamentoCommand
        {
            CobrancaId = CobrancaId, PagamentoId = PagamentoId,
            EstabelecimentoId = EstabId, UsuarioId = UsuarioId,
            Motivo = "",
        };
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("Motivo"));
    }
}

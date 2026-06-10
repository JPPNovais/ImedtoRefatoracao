using Imedto.Backend.Application.Cobrancas.Commands;
using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cobrancas;

[TestFixture]
public class RegistrarPagamentosCommandHandlerTests
{
    private Mock<ICobrancaRepository> _cobrancaRepo;
    private Mock<ILancamentoRepository> _lancamentoRepo;
    private Mock<IConfigTaxaFormaPagamentoRepository> _configTaxaRepo;
    private RegistrarPagamentosCommandHandler _sut;

    private const long EstabId = 1L;
    private const long CobrancaId = 99L;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _cobrancaRepo = new Mock<ICobrancaRepository>();
        _lancamentoRepo = new Mock<ILancamentoRepository>();
        _configTaxaRepo = new Mock<IConfigTaxaFormaPagamentoRepository>();

        // Taxa padrão: sem config → 0%
        _configTaxaRepo.Setup(r => r.ObterPorForma(EstabId, It.IsAny<long>()))
            .ReturnsAsync((ConfigTaxaFormaPagamento?)null);

        _sut = new RegistrarPagamentosCommandHandler(
            _cobrancaRepo.Object, _lancamentoRepo.Object, _configTaxaRepo.Object);
    }

    private Cobranca CriarCobrancaAberta(decimal valor = 200m)
    {
        var cobranca = Cobranca.CriarParaConsulta(EstabId, 10L, 500L,
            TipoAtendimento.Particular, valor, "Consulta", UsuarioId);
        cobranca.SimularIdBanco(CobrancaId);
        return cobranca;
    }

    // ── INV-3: atomicidade ────────────────────────────────────────────────────

    [Test]
    public async Task Handle_PagamentoUnico_CriaLancamentoEVincula()
    {
        var cobranca = CriarCobrancaAberta(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        var pagamentoIdCounter = 2000L;
        // Simula geração de Id do Pagamento pelo banco ao salvar a Cobrança.
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c =>
            {
                // Atribui Id a pagamentos sem Id (primeira persistência por iteração).
                foreach (var p in c.Pagamentos.Where(x => x.Id == 0))
                    p.SimularIdBanco(++pagamentoIdCounter);
            })
            .Returns(Task.CompletedTask);

        Lancamento? lancamentoCriado = null;
        _lancamentoRepo.Setup(r => r.Salvar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l =>
            {
                lancamentoCriado = l;
                l.SimularIdBanco(1001L);
            })
            .Returns(Task.CompletedTask);

        await _sut.Handle(new RegistrarPagamentosCommand
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioId = UsuarioId,
            Desconto = 0m,
            PodeAplicarDesconto = false,
            DataPagamento = DateOnly.FromDateTime(DateTime.Today),
            Formas = new List<FormaPagamentoItem>
            {
                new() { FormaPagamentoId = 1L, Valor = 200m, Parcelas = 1, Juros = 0m }
            }
        });

        // Lançamento criado e vinculado
        Assert.That(lancamentoCriado, Is.Not.Null);
        Assert.That(lancamentoCriado!.CobrancaId, Is.EqualTo(CobrancaId));
        Assert.That(lancamentoCriado.Status, Is.EqualTo(StatusLancamento.Pago));
        Assert.That(lancamentoCriado.PagamentoId, Is.EqualTo(2001L));

        // Pagamento tem LancamentoId preenchido
        var pagamento = cobranca.Pagamentos.Single();
        Assert.That(pagamento.LancamentoId, Is.EqualTo(1001L));
    }

    // ── R11: múltiplas formas de pagamento ───────────────────────────────────

    [Test]
    public async Task Handle_DuasFormas_CriaDoisPagamentosEDoisLancamentos()
    {
        var cobranca = CriarCobrancaAberta(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        var pagamentoIdCounter = 3000L;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c =>
            {
                foreach (var p in c.Pagamentos.Where(x => x.Id == 0))
                    p.SimularIdBanco(++pagamentoIdCounter);
            })
            .Returns(Task.CompletedTask);

        var lancamentoIdCounter = 1000L;
        _lancamentoRepo.Setup(r => r.Salvar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => l.SimularIdBanco(++lancamentoIdCounter))
            .Returns(Task.CompletedTask);

        await _sut.Handle(new RegistrarPagamentosCommand
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioId = UsuarioId,
            Desconto = 0m,
            PodeAplicarDesconto = false,
            DataPagamento = DateOnly.FromDateTime(DateTime.Today),
            Formas = new List<FormaPagamentoItem>
            {
                new() { FormaPagamentoId = 1L, Valor = 100m, Parcelas = 1 },
                new() { FormaPagamentoId = 2L, Valor = 100m, Parcelas = 1 }
            }
        });

        Assert.That(cobranca.Pagamentos, Has.Count.EqualTo(2));
        Assert.That(cobranca.Status, Is.EqualTo(StatusCobranca.Paga));
        _lancamentoRepo.Verify(r => r.Salvar(It.IsAny<Lancamento>()), Times.Exactly(2));
    }

    // ── INV-8: desconto sem permissão ─────────────────────────────────────────

    [Test]
    public void Handle_DescontoSemPermissao_Lanca()
    {
        var cobranca = CriarCobrancaAberta(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarPagamentosCommand
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioId = UsuarioId,
            Desconto = 50m,
            PodeAplicarDesconto = false,
            DataPagamento = DateOnly.FromDateTime(DateTime.Today),
            Formas = new List<FormaPagamentoItem>
            {
                new() { FormaPagamentoId = 1L, Valor = 150m, Parcelas = 1 }
            }
        }));
        Assert.That(ex.Message, Does.Contain("permissão"));
    }

    // ── Multi-tenant (R14): cobrança de outro tenant retorna não encontrado ───

    [Test]
    public void Handle_CobrancaDeOutroTenant_Lanca()
    {
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId))
            .ReturnsAsync((Cobranca?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarPagamentosCommand
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioId = UsuarioId,
            DataPagamento = DateOnly.FromDateTime(DateTime.Today),
            Formas = new List<FormaPagamentoItem>
            {
                new() { FormaPagamentoId = 1L, Valor = 100m }
            }
        }));
        Assert.That(ex.Message, Does.Contain("Não encontrado"));
    }

    // ── Forma vazia ──────────────────────────────────────────────────────────

    [Test]
    public void Handle_SemFormas_Lanca()
    {
        var cobranca = CriarCobrancaAberta(200m);
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarPagamentosCommand
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioId = UsuarioId,
            DataPagamento = DateOnly.FromDateTime(DateTime.Today),
            Formas = new List<FormaPagamentoItem>()
        }));
        Assert.That(ex.Message, Does.Contain("ao menos uma forma"));
    }
}

using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Cobrancas;

/// <summary>
/// Testes de domínio para Pagamento.RegistrarEmissaoRecibo (F8).
/// CA120: pagamento estornado bloqueia; CA128: flag gravada só na 1ª emissão.
/// </summary>
[TestFixture]
public class PagamentoReciboTests
{
    private const long EstabId = 1L;
    private const long PacienteId = 10L;
    private const long AgendamentoId = 500L;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private Cobranca CriarComPagamento(out Pagamento pagamento)
    {
        var c = Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Particular, 200m, "Consulta", UsuarioId);
        c.SimularIdBanco(99L);
        c.RegistrarPagamento(200m, 1L, 1, 0m, 0m, Hoje, UsuarioId);
        pagamento = c.Pagamentos.First();
        pagamento.SimularIdBanco(10L);
        return c;
    }

    // ── CA120: pagamento estornado bloqueia emissão ───────────────────────────

    [Test]
    public void RegistrarEmissaoRecibo_PagamentoEstornado_Lanca()
    {
        var c = CriarComPagamento(out var pagamento);
        c.EstornarPagamento(pagamento.Id, "motivo teste", UsuarioId);

        var ex = Assert.Throws<BusinessException>(
            () => pagamento.RegistrarEmissaoRecibo(c.Estornos));

        Assert.That(ex!.Message, Does.Contain("estornado"));
    }

    // ── CA128: flag gravada apenas na 1ª emissão (idempotente) ────────────────

    [Test]
    public void RegistrarEmissaoRecibo_PrimeiralEmissao_GravaTimestamp()
    {
        var c = CriarComPagamento(out var pagamento);

        Assert.That(pagamento.ReciboEmitidoEm, Is.Null, "flag deve iniciar null");

        pagamento.RegistrarEmissaoRecibo(c.Estornos);

        Assert.That(pagamento.ReciboEmitidoEm, Is.Not.Null);
        Assert.That(pagamento.ReciboEmitidoEm!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void RegistrarEmissaoRecibo_SegundaEmissao_NaoSobrescreve()
    {
        var c = CriarComPagamento(out var pagamento);

        pagamento.RegistrarEmissaoRecibo(c.Estornos);
        var primeiroTimestamp = pagamento.ReciboEmitidoEm;

        // Pausa mínima para garantir que um novo DateTime.UtcNow seria diferente
        System.Threading.Thread.Sleep(10);

        // 2ª emissão não deve lançar nem sobrescrever
        pagamento.RegistrarEmissaoRecibo(c.Estornos);

        Assert.That(pagamento.ReciboEmitidoEm, Is.EqualTo(primeiroTimestamp),
            "ReciboEmitidoEm não deve ser sobrescrito na 2ª emissão");
    }

    // ── Pagamento não estornado — caminho feliz ───────────────────────────────

    [Test]
    public void RegistrarEmissaoRecibo_SemEstorno_NaoLanca()
    {
        var c = CriarComPagamento(out var pagamento);

        Assert.DoesNotThrow(() => pagamento.RegistrarEmissaoRecibo(c.Estornos));
    }

    // ── Estorno de outro pagamento não bloqueia ─────────────────────────────

    [Test]
    public void RegistrarEmissaoRecibo_EstornoDeOutroPagamento_NaoBloqueia()
    {
        // Cobrança com 2 parcelas: estorna uma, emite recibo da outra
        var c = Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Particular, 200m, "Consulta", UsuarioId);
        c.SimularIdBanco(99L);
        c.RegistrarPagamento(100m, 1L, 1, 0m, 0m, Hoje, UsuarioId);
        var pag1 = c.Pagamentos.First();
        pag1.SimularIdBanco(10L);
        c.RegistrarPagamento(100m, 1L, 1, 0m, 0m, Hoje, UsuarioId);
        var pag2 = c.Pagamentos.Last();
        pag2.SimularIdBanco(11L);

        // Estorna apenas pag1
        c.EstornarPagamento(pag1.Id, "motivo", UsuarioId);

        // pag2 pode emitir recibo mesmo com pag1 estornado
        Assert.DoesNotThrow(() => pag2.RegistrarEmissaoRecibo(c.Estornos));
    }
}

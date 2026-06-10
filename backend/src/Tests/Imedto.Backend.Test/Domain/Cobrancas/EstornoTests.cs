using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Cobrancas;

/// <summary>
/// Testes de domínio para INV-7 (estorno atômico), R5, R7, R8, R12 e soma líquida.
/// CA29, CA30, CA31, CA32, status após estorno.
/// </summary>
[TestFixture]
public class EstornoTests
{
    private const long EstabId = 1L;
    private const long PacienteId = 10L;
    private const long AgendamentoId = 500L;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private static Cobranca CriarParticular(decimal valor = 200m)
        => Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Particular, valor, "Consulta", UsuarioId);

    private static Cobranca CriarConvenio()
        => Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Convenio, 0m, "Convênio", UsuarioId);

    private static Cobranca CriarComPagamento(decimal valorCobranca = 200m, decimal valorPago = 200m)
    {
        var c = CriarParticular(valorCobranca);
        // Simula Id gerado pelo banco (necessário para o pagamento ser localizado)
        typeof(Cobranca).BaseType!.GetProperty("Id")!
            .SetValue(c, 1L);
        c.RegistrarPagamento(valorPago, 1L, 1, 0m, 0m, Hoje, UsuarioId);
        return c;
    }

    // ── Helpers para acessar pagamento privado ───────────────────────────────
    private static Pagamento PrimeiroPagamento(Cobranca c)
        => c.Pagamentos.First();

    private static void SetPagamentoId(Pagamento p, long id)
        => typeof(Pagamento).BaseType!.GetProperty("Id")!.SetValue(p, id);

    // ── R5: motivo obrigatório ───────────────────────────────────────────────

    [Test]
    public void EstornarPagamento_MotivoVazio_Lanca()
    {
        var c = CriarComPagamento();
        var p = PrimeiroPagamento(c);
        SetPagamentoId(p, 1L);

        var ex = Assert.Throws<BusinessException>(() =>
            c.EstornarPagamento(1L, "  ", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Motivo"));
    }

    // ── R8: não estornar duas vezes (CA32) ───────────────────────────────────

    [Test]
    public void EstornarPagamento_JaEstornado_Lanca()
    {
        var c = CriarComPagamento();
        var p = PrimeiroPagamento(c);
        SetPagamentoId(p, 2L);

        c.EstornarPagamento(2L, "primeira vez", UsuarioId);

        var ex = Assert.Throws<BusinessException>(() =>
            c.EstornarPagamento(2L, "segunda vez", UsuarioId));
        Assert.That(ex.Message, Does.Contain("já foi estornado"));
    }

    // ── R7: status recalcula para Aberta após estorno total (CA30) ───────────

    [Test]
    public void EstornarPagamento_TotalPago_StatusVoltaAberta()
    {
        var c = CriarComPagamento(200m, 200m);
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Paga));
        var p = PrimeiroPagamento(c);
        SetPagamentoId(p, 3L);

        c.EstornarPagamento(3L, "valor incorreto", UsuarioId);

        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Aberta));
        Assert.That(c.SaldoDevedor(), Is.EqualTo(200m));
        Assert.That(c.TotalPagoLiquido(), Is.EqualTo(0m));
    }

    // ── R7: status ParcialmentePaga → Aberta após estorno (CA30 variante) ───

    [Test]
    public void EstornarPagamento_Parcial_StatusVoltaAberta()
    {
        var c = CriarComPagamento(200m, 80m);
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.ParcialmentePaga));
        var p = PrimeiroPagamento(c);
        SetPagamentoId(p, 4L);

        c.EstornarPagamento(4L, "erro", UsuarioId);

        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Aberta));
        Assert.That(c.TotalPagoLiquido(), Is.EqualTo(0m));
        Assert.That(c.SaldoDevedor(), Is.EqualTo(200m));
    }

    // ── R12: convênio não aceita estorno ─────────────────────────────────────

    [Test]
    public void EstornarPagamento_Convenio_Lanca()
    {
        var c = CriarConvenio();

        var ex = Assert.Throws<BusinessException>(() =>
            c.EstornarPagamento(99L, "motivo", UsuarioId));
        Assert.That(ex.Message, Does.Contain("convênio"));
    }

    // ── Pagamento não encontrado retorna "Não encontrado" ────────────────────

    [Test]
    public void EstornarPagamento_PagamentoInexistente_Lanca()
    {
        var c = CriarComPagamento();
        // Não seta o Id do pagamento — padrão é 0; busca por Id=999 falha
        var ex = Assert.Throws<BusinessException>(() =>
            c.EstornarPagamento(999L, "motivo", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Não encontrado"));
    }

    // ── TotalPagoLiquido = pagamentos − estornos ─────────────────────────────

    [Test]
    public void TotalPagoLiquido_SemEstornos_IgualTotalPago()
    {
        var c = CriarComPagamento(200m, 80m);
        Assert.That(c.TotalPagoLiquido(), Is.EqualTo(80m));
    }

    [Test]
    public void TotalEstornado_SemEstornos_Zero()
    {
        var c = CriarParticular();
        Assert.That(c.TotalEstornado(), Is.EqualTo(0m));
    }

    // ── Cobrança cancelada não aceita estorno ────────────────────────────────

    [Test]
    public void EstornarPagamento_CobrancaCancelada_Lanca()
    {
        var c = CriarComPagamento();
        var p = PrimeiroPagamento(c);
        SetPagamentoId(p, 5L);
        c.Cancelar();

        var ex = Assert.Throws<BusinessException>(() =>
            c.EstornarPagamento(5L, "motivo", UsuarioId));
        Assert.That(ex.Message, Does.Contain("cancelada"));
    }
}

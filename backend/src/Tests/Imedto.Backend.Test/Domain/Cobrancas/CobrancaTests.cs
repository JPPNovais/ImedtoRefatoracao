using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Cobrancas;

[TestFixture]
public class CobrancaTests
{
    private const long EstabId = 1L;
    private const long PacienteId = 10L;
    private const long AgendamentoId = 500L;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static Cobranca CriarParticular(decimal valor = 200m)
        => Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Particular, valor, "Consulta", UsuarioId);

    private static Cobranca CriarConvenio()
        => Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Convenio, 0m, "Consulta Convênio", UsuarioId);

    // ── INV-6: tenant + paciente + agendamento obrigatórios ──────────────────

    [Test]
    public void CriarParaConsulta_EstabelecimentoZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaConsulta(0, PacienteId, AgendamentoId,
                TipoAtendimento.Particular, 200m, "Consulta", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void CriarParaConsulta_PacienteZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaConsulta(EstabId, 0, AgendamentoId,
                TipoAtendimento.Particular, 200m, "Consulta", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Paciente"));
    }

    [Test]
    public void CriarParaConsulta_AgendamentoZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaConsulta(EstabId, PacienteId, 0,
                TipoAtendimento.Particular, 200m, "Consulta", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Agendamento"));
    }

    // ── R12: Convênio nasce com valor zero ───────────────────────────────────

    [Test]
    public void CriarParaConsulta_Convenio_NasceSemValor()
    {
        var c = CriarConvenio();
        Assert.That(c.ValorCobrado, Is.EqualTo(0m));
        Assert.That(c.TipoAtendimento, Is.EqualTo(TipoAtendimento.Convenio));
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Aberta));
    }

    [Test]
    public void CriarParaConsulta_Particular_ValorZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
                TipoAtendimento.Particular, 0m, "Consulta", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Valor cobrado"));
    }

    // ── INV-2: status derivado ───────────────────────────────────────────────

    [Test]
    public void Status_NasceAberta()
    {
        var c = CriarParticular(300m);
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Aberta));
    }

    // ── INV-4: desconto não pode exceder valor cobrado ───────────────────────

    [Test]
    public void AplicarDesconto_ExcededValorCobrado_Lanca()
    {
        var c = CriarParticular(100m);
        var ex = Assert.Throws<BusinessException>(() => c.AplicarDesconto(150m, true));
        Assert.That(ex.Message, Does.Contain("não pode ser maior"));
    }

    [Test]
    public void AplicarDesconto_Negativo_Lanca()
    {
        var c = CriarParticular(100m);
        var ex = Assert.Throws<BusinessException>(() => c.AplicarDesconto(-1m, true));
        Assert.That(ex.Message, Does.Contain("negativo"));
    }

    // ── INV-8: RBAC de desconto ──────────────────────────────────────────────

    [Test]
    public void AplicarDesconto_SemPermissao_Lanca()
    {
        var c = CriarParticular(100m);
        var ex = Assert.Throws<BusinessException>(() => c.AplicarDesconto(20m, podeAplicarDesconto: false));
        Assert.That(ex.Message, Does.Contain("permissão"));
    }

    [Test]
    public void AplicarDesconto_ComPermissao_Aplica()
    {
        var c = CriarParticular(100m);
        c.AplicarDesconto(20m, podeAplicarDesconto: true);
        Assert.That(c.Desconto, Is.EqualTo(20m));
        Assert.That(c.TotalLiquido(), Is.EqualTo(80m));
    }

    // ── CA18: arredondamento monetário ───────────────────────────────────────

    [Test]
    public void ArredondamentoMonetario_MidpointArredondaParaCima()
    {
        Assert.That(ArredondamentoMonetario.Arredondar(0.125m), Is.EqualTo(0.13m));
        Assert.That(ArredondamentoMonetario.Arredondar(0.115m), Is.EqualTo(0.12m));
        Assert.That(ArredondamentoMonetario.Arredondar(1.005m), Is.EqualTo(1.01m));
    }

    [Test]
    public void ArredondamentoMonetario_ValorExato_SemAlteracao()
    {
        Assert.That(ArredondamentoMonetario.Arredondar(100.00m), Is.EqualTo(100.00m));
        Assert.That(ArredondamentoMonetario.Arredondar(99.99m), Is.EqualTo(99.99m));
    }

    // ── INV-5: pagamento com valor > saldo lança ─────────────────────────────

    [Test]
    public void RegistrarPagamento_ValorExcedeSaldo_Lanca()
    {
        var c = CriarParticular(100m);
        var ex = Assert.Throws<BusinessException>(() =>
            c.RegistrarPagamento(150m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId));
        Assert.That(ex.Message, Does.Contain("excede o saldo"));
    }

    [Test]
    public void RegistrarPagamento_ValorZero_Lanca()
    {
        var c = CriarParticular(100m);
        var ex = Assert.Throws<BusinessException>(() =>
            c.RegistrarPagamento(0m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId));
        Assert.That(ex.Message, Does.Contain("maior que zero"));
    }

    // ── INV-2: status derivado após pagamentos ───────────────────────────────

    [Test]
    public void Status_PagamentoParcial_RetornaParcialmentePaga()
    {
        var c = CriarParticular(100m);
        c.RegistrarPagamento(50m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId);
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.ParcialmentePaga));
        Assert.That(c.SaldoDevedor(), Is.EqualTo(50m));
    }

    [Test]
    public void Status_PagamentoTotal_RetornaPaga()
    {
        var c = CriarParticular(100m);
        c.RegistrarPagamento(100m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId);
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Paga));
        Assert.That(c.SaldoDevedor(), Is.EqualTo(0m));
    }

    [Test]
    public void Status_PagamentoAposDesconto_StatusCorreto()
    {
        var c = CriarParticular(100m);
        c.AplicarDesconto(20m, true); // liquido = 80m
        c.RegistrarPagamento(80m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId);
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Paga));
    }

    // ── R12: Convênio não aceita pagamento de balcão ─────────────────────────

    [Test]
    public void RegistrarPagamento_Convenio_Lanca()
    {
        var c = CriarConvenio();
        var ex = Assert.Throws<BusinessException>(() =>
            c.RegistrarPagamento(100m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId));
        Assert.That(ex.Message, Does.Contain("convênio"));
    }

    // ── Cancelamento ──────────────────────────────────────────────────────────

    [Test]
    public void Cancelar_CobrancaAberta_Cancela()
    {
        var c = CriarParticular();
        c.Cancelar();
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Cancelada));
    }

    [Test]
    public void Cancelar_JaCancelada_Lanca()
    {
        var c = CriarParticular();
        c.Cancelar();
        var ex = Assert.Throws<BusinessException>(() => c.Cancelar());
        Assert.That(ex.Message, Does.Contain("já está cancelada"));
    }

    [Test]
    public void RegistrarPagamento_CobrancaCancelada_Lanca()
    {
        var c = CriarParticular(100m);
        c.Cancelar();
        var ex = Assert.Throws<BusinessException>(() =>
            c.RegistrarPagamento(50m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId));
        Assert.That(ex.Message, Does.Contain("cancelada"));
    }

    // ── F5/R5: CriarParaCirurgia — invariantes ───────────────────────────────

    private static Cobranca CriarParaCirurgia(decimal valor = 1500m)
        => Cobranca.CriarParaCirurgia(EstabId, PacienteId, 50L, valor, "Cirurgia — orçamento #50", UsuarioId);

    [Test]
    public void CriarParaCirurgia_NasceComOrigem()
    {
        var c = CriarParaCirurgia();
        Assert.That(c.Origem, Is.EqualTo("Cirurgia"));
        Assert.That(c.TipoAtendimento, Is.EqualTo(TipoAtendimento.Particular));
        Assert.That(c.Status, Is.EqualTo(StatusCobranca.Aberta));
        Assert.That(c.AgendamentoId, Is.Null);
        Assert.That(c.EvolucaoId, Is.Null);
        Assert.That(c.OrcamentoId, Is.EqualTo(50L));
    }

    [Test]
    public void CriarParaCirurgia_OrcamentoZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaCirurgia(EstabId, PacienteId, 0, 1500m, "desc", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Orçamento"));
    }

    [Test]
    public void CriarParaCirurgia_ValorZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaCirurgia(EstabId, PacienteId, 50L, 0m, "desc", UsuarioId));
        Assert.That(ex.Message, Does.Contain("Valor cobrado"));
    }

    [Test]
    public void CriarParaCirurgia_UsuarioVazio_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Cobranca.CriarParaCirurgia(EstabId, PacienteId, 50L, 1500m, "desc", Guid.Empty));
        Assert.That(ex.Message, Does.Contain("Usuário"));
    }

    // ── F5/R8: SincronizarValorCobrado ──────────────────────────────────────

    [Test]
    public void SincronizarValorCobrado_MesmoValor_NoOp()
    {
        var c = CriarParaCirurgia(1500m);
        c.SincronizarValorCobrado(1500m, UsuarioId);
        Assert.That(c.HistoricoValor.Count, Is.Zero); // nenhum histórico (CA103)
        Assert.That(c.ValorCobrado, Is.EqualTo(1500m));
    }

    [Test]
    public void SincronizarValorCobrado_ValorDiferente_GravaHistorico()
    {
        var c = CriarParaCirurgia(1500m);
        c.SincronizarValorCobrado(2000m, UsuarioId);

        Assert.That(c.ValorCobrado, Is.EqualTo(2000m));
        Assert.That(c.HistoricoValor.Count, Is.EqualTo(1));
        var h = c.HistoricoValor.First();
        Assert.That(h.ValorAnterior, Is.EqualTo(1500m));
        Assert.That(h.ValorNovo, Is.EqualTo(2000m));
        Assert.That(h.AlteradoPorUsuarioId, Is.EqualTo(UsuarioId));
    }

    [Test]
    public void SincronizarValorCobrado_MultiplasAlteracoes_GravaHistoricoCorreto()
    {
        var c = CriarParaCirurgia(1000m);
        c.SincronizarValorCobrado(1500m, UsuarioId);
        c.SincronizarValorCobrado(2000m, UsuarioId);

        Assert.That(c.ValorCobrado, Is.EqualTo(2000m));
        Assert.That(c.HistoricoValor.Count, Is.EqualTo(2));
    }

    [Test]
    public void SincronizarValorCobrado_ReducaoAbaixoPagoLiquido_Lanca()
    {
        // Cobrança de 1500, com pagamento de 1200. Redução para 1000 < 1200 → R9.
        var c = CriarParaCirurgia(1500m);
        c.RegistrarPagamento(1200m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId);

        var ex = Assert.Throws<BusinessException>(() =>
            c.SincronizarValorCobrado(1000m, UsuarioId));
        Assert.That(ex.Message, Does.Contain("menor que o total já pago"));
    }

    [Test]
    public void SincronizarValorCobrado_ReducaoAcimaPagoLiquido_Permite()
    {
        // Cobrança de 1500, com pagamento de 800. Redução para 1000 > 800 → permitido.
        var c = CriarParaCirurgia(1500m);
        c.RegistrarPagamento(800m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId);

        Assert.DoesNotThrow(() => c.SincronizarValorCobrado(1000m, UsuarioId));
        Assert.That(c.ValorCobrado, Is.EqualTo(1000m));
    }
}

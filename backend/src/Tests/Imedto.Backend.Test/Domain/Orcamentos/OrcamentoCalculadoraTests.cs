using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class OrcamentoCalculadoraTests
{
    // ──────── CalcularValorProfissional ────────

    [Test]
    public void ValorProfissional_TempoZero_RetornaZero()
    {
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 0,
            tempoBaseMinutos: 60, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 0m);
        Assert.That(v, Is.EqualTo(0m));
    }

    [Test]
    public void ValorProfissional_TempoIgualBase_RetornaValorBaseMaisPlus()
    {
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 60,
            tempoBaseMinutos: 60, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 50m);
        Assert.That(v, Is.EqualTo(1050m));
    }

    [Test]
    public void ValorProfissional_TempoAbaixoBase_RetornaValorBase()
    {
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 30,
            tempoBaseMinutos: 60, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 0m);
        Assert.That(v, Is.EqualTo(1000m));
    }

    [Test]
    public void ValorProfissional_UmBlocoAdicionalCompleto_SomaUmAdicional()
    {
        // 60 base + 30 = 90 → 1 bloco adicional
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 90,
            tempoBaseMinutos: 60, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 0m);
        Assert.That(v, Is.EqualTo(1200m));
    }

    [Test]
    public void ValorProfissional_BlocoParcial_ArrendondaParaCima()
    {
        // 60 base + 15 minutos de excedente: 15/30 = 0.5 → ceil = 1 bloco
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 75,
            tempoBaseMinutos: 60, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 0m);
        Assert.That(v, Is.EqualTo(1200m));
    }

    [Test]
    public void ValorProfissional_TresBlocos_SomaTresAdicionais()
    {
        // 60 base + 90 minutos = 3 blocos de 30
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 150,
            tempoBaseMinutos: 60, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 100m);
        Assert.That(v, Is.EqualTo(1700m)); // 1000 + 3*200 + 100
    }

    // ──────── CalcularValorLocal ────────

    [Test]
    public void ValorLocal_AbaixoBase_RetornaBase()
    {
        var v = OrcamentoCalculadora.CalcularValorLocal(
            tempoCirurgiaMinutos: 50,
            tempoBaseMinutos: 60, valorBase: 500m,
            tempoAdicionalMinutos: 30, valorAdicional: 100m);
        Assert.That(v, Is.EqualTo(500m));
    }

    [Test]
    public void ValorLocal_DoisBlocosAdicionais_Soma()
    {
        // 60 base + 60 (= 2 blocos de 30) = 500 + 200 = 700
        var v = OrcamentoCalculadora.CalcularValorLocal(
            tempoCirurgiaMinutos: 120,
            tempoBaseMinutos: 60, valorBase: 500m,
            tempoAdicionalMinutos: 30, valorAdicional: 100m);
        Assert.That(v, Is.EqualTo(700m));
    }

    // CA-3: ValorProfissional(tempoBase=120, valorBase=1000, tempoAd=30, valorAd=200, plus=0)
    // com tempo=180min → 1400 (1 bloco adicional cheio).
    [Test]
    public void CA3_ValorProfissional_180minComBase120e30Adicional_Retorna1400()
    {
        var v = OrcamentoCalculadora.CalcularValorProfissional(
            tempoCirurgiaMinutos: 180,
            tempoBaseMinutos: 120, valorTempoBase: 1000m,
            tempoAdicionalMinutos: 30, valorAdicional: 200m, valorPlus: 0m);
        Assert.That(v, Is.EqualTo(1400m));
    }

    // CA-4: CalcularValorLocal(int_geral, base=2000, tempoBase=120, tempoAd=30, valorAd=300)
    // com tempo=180min → 2600.
    [Test]
    public void CA4_ValorLocal_IntGeral_180minComBase120e300Adicional_Retorna2600()
    {
        var config = ConfiguracaoLocalCirurgia.Criar(
            estabelecimentoId: 1, tipo: Imedto.Backend.Domain.Orcamentos.TipoLocalCirurgia.IntGeral,
            tempoBaseMinutos: 120, valorBase: 2000m,
            tempoAdicionalMinutos: 30, valorAdicional: 300m);

        var v = OrcamentoCalculadora.CalcularValorLocal(
            Imedto.Backend.Domain.Orcamentos.TipoLocalCirurgia.IntGeral,
            tempoCirurgiaMinutos: 180,
            config);
        Assert.That(v, Is.EqualTo(2600m));
    }

    [Test]
    public void ValorLocal_TipoFixo_RetornaValorBaseIgnorandoTempo()
    {
        var config = ConfiguracaoLocalCirurgia.Criar(
            estabelecimentoId: 1,
            tipo: Imedto.Backend.Domain.Orcamentos.TipoLocalCirurgia.SemInternacao,
            tempoBaseMinutos: 1, valorBase: 350m,
            tempoAdicionalMinutos: 1, valorAdicional: 999m);

        var v = OrcamentoCalculadora.CalcularValorLocal(
            Imedto.Backend.Domain.Orcamentos.TipoLocalCirurgia.SemInternacao,
            tempoCirurgiaMinutos: 600, config);
        Assert.That(v, Is.EqualTo(350m), "tipo fixo (SemInternacao/Ambulatorio) ignora tempo");
    }

    [Test]
    public void ValorLocal_ConfigNull_RetornaZero()
    {
        var v = OrcamentoCalculadora.CalcularValorLocal(
            Imedto.Backend.Domain.Orcamentos.TipoLocalCirurgia.IntLocal,
            tempoCirurgiaMinutos: 60, config: null);
        Assert.That(v, Is.EqualTo(0m));
    }

    // ──────── CalcularFormaPagamento ────────

    [Test]
    public void FormaPagamento_SemAcrescimoSemEntrada_DistribuiEmParcelas()
    {
        var r = OrcamentoCalculadora.CalcularFormaPagamento(
            subtotal: 1200m, acrescimoPercentual: 0m, entradaPercentual: 0m, parcelas: 12);
        Assert.That(r.TotalBruto, Is.EqualTo(1200m));
        Assert.That(r.Entrada, Is.EqualTo(0m));
        Assert.That(r.ValorParcela, Is.EqualTo(100m));
    }

    [Test]
    public void FormaPagamento_ComAcrescimo10Pct_AumentaTotal()
    {
        var r = OrcamentoCalculadora.CalcularFormaPagamento(
            subtotal: 1000m, acrescimoPercentual: 10m, entradaPercentual: 0m, parcelas: 1);
        Assert.That(r.TotalBruto, Is.EqualTo(1100m));
        Assert.That(r.ValorParcela, Is.EqualTo(1100m));
    }

    [Test]
    public void FormaPagamento_ComEntrada30Pct_RestoEmParcelas()
    {
        // 1000 sem acréscimo, entrada 30% = 300, restante 700 / 7x = 100
        var r = OrcamentoCalculadora.CalcularFormaPagamento(
            subtotal: 1000m, acrescimoPercentual: 0m, entradaPercentual: 30m, parcelas: 7);
        Assert.That(r.TotalBruto, Is.EqualTo(1000m));
        Assert.That(r.Entrada, Is.EqualTo(300m));
        Assert.That(r.ValorParcela, Is.EqualTo(100m));
    }

    [Test]
    public void FormaPagamento_ParcelasZero_ValorParcelaZero()
    {
        var r = OrcamentoCalculadora.CalcularFormaPagamento(
            subtotal: 500m, acrescimoPercentual: 0m, entradaPercentual: 0m, parcelas: 0);
        Assert.That(r.ValorParcela, Is.EqualTo(0m));
    }

    [Test]
    public void FormaPagamento_AcrescimoEEntradaCombinados()
    {
        // 1000 + 5% acréscimo = 1050; entrada 20% de 1050 = 210; restante 840 / 6x = 140
        var r = OrcamentoCalculadora.CalcularFormaPagamento(
            subtotal: 1000m, acrescimoPercentual: 5m, entradaPercentual: 20m, parcelas: 6);
        Assert.That(r.TotalBruto, Is.EqualTo(1050m));
        Assert.That(r.Entrada, Is.EqualTo(210m));
        Assert.That(r.ValorParcela, Is.EqualTo(140m));
    }
}

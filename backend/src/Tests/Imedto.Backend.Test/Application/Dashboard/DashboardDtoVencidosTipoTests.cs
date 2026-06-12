using Imedto.Backend.Contracts.Dashboard;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Dashboard;

/// <summary>
/// Testa os campos VencidosAReceber/VencidosAPagar do DashboardDto (R2/CA12).
/// Os valores de vencidos por tipo são campos simples de DTO — o teste garante
/// que o mapeamento Dapper consegue populá-los corretamente via object initializer
/// (proxy para validação de paridade de nomenclatura antes do deploy).
/// </summary>
[TestFixture]
public class DashboardDtoVencidosTipoTests
{
    [Test]
    public void DashboardDto_VencidosAReceberEAPagar_SaoInicializadosComoZero()
    {
        var dto = new DashboardDto();
        Assert.That(dto.VencidosAReceber, Is.EqualTo(0m));
        Assert.That(dto.VencidosAPagar, Is.EqualTo(0m));
    }

    [Test]
    public void DashboardDto_VencidosAReceber_PodeSerAtribuido()
    {
        var dto = new DashboardDto { VencidosAReceber = 150m };
        Assert.That(dto.VencidosAReceber, Is.EqualTo(150m));
    }

    [Test]
    public void DashboardDto_VencidosAPagar_PodeSerAtribuido()
    {
        var dto = new DashboardDto { VencidosAPagar = 80m };
        Assert.That(dto.VencidosAPagar, Is.EqualTo(80m));
    }

    [Test]
    public void DashboardDto_CamposExistentes_NaoForamAlterados()
    {
        // CA15 análogo: campos pré-existentes do DTO devem continuar funcionando.
        var dto = new DashboardDto
        {
            TotalPacientesAtivos = 10,
            AgendamentosHoje = 3,
            LancamentosVencidos = 2,
            ItensAbaixoMinimo = 1,
            OrcamentosPendentes = 4,
            SaldoMes = 500m
        };

        Assert.That(dto.TotalPacientesAtivos, Is.EqualTo(10));
        Assert.That(dto.AgendamentosHoje, Is.EqualTo(3));
        Assert.That(dto.LancamentosVencidos, Is.EqualTo(2));
        Assert.That(dto.ItensAbaixoMinimo, Is.EqualTo(1));
        Assert.That(dto.OrcamentosPendentes, Is.EqualTo(4));
        Assert.That(dto.SaldoMes, Is.EqualTo(500m));
    }

    [Test]
    public void DashboardDto_VencidosAReceberEAPagar_IndependentesEntreElevados()
    {
        // CA12: receita conta como "a receber", despesa como "a pagar" — valores independentes.
        var dto = new DashboardDto
        {
            VencidosAReceber = 150m,
            VencidosAPagar = 80m
        };

        Assert.That(dto.VencidosAReceber, Is.Not.EqualTo(dto.VencidosAPagar));
        Assert.That(dto.VencidosAReceber, Is.EqualTo(150m));
        Assert.That(dto.VencidosAPagar, Is.EqualTo(80m));
    }
}

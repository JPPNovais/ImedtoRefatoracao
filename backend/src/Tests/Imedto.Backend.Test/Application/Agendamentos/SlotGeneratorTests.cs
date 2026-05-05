using Imedto.Backend.Application.Agendamentos.Queries;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class SlotGeneratorTests
{
    [Test]
    public void Gerar_DuracaoPadrao30SemIntervalo_GridDeMeiaEmMeiaHora()
    {
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(10, 0), 30, 0);

        Assert.That(slots, Is.EqualTo(new[]
        {
            new TimeOnly(8, 0), new TimeOnly(8, 30),
            new TimeOnly(9, 0), new TimeOnly(9, 30),
        }));
    }

    [Test]
    public void Gerar_Duracao60SemIntervalo_PassoDe60()
    {
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(11, 0), 60, 0);

        Assert.That(slots, Is.EqualTo(new[]
        {
            new TimeOnly(8, 0), new TimeOnly(9, 0), new TimeOnly(10, 0),
        }));
    }

    [Test]
    public void Gerar_Duracao60ComIntervalo15_PassoDe75()
    {
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(11, 0), 60, 15);

        // 8:00 (cabe ate 9:00) → +75 → 9:15 (cabe ate 10:15) → +75 → 10:30 (10:30+60=11:30 NAO cabe).
        Assert.That(slots, Is.EqualTo(new[]
        {
            new TimeOnly(8, 0), new TimeOnly(9, 15),
        }));
    }

    [Test]
    public void Gerar_UltimoSlotPrecisaCaberInteiro_NaoEmiteSlotQueExcedeOFim()
    {
        // Janela 8:00–9:30 com duracao 60: 8:00 OK, 9:00 → 10:00 nao cabe.
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(9, 30), 60, 0);

        Assert.That(slots, Is.EqualTo(new[] { new TimeOnly(8, 0) }));
    }

    [Test]
    public void Gerar_Duracao120ComIntervalo0_DoisSlotsEm4Horas()
    {
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(12, 0), 120, 0);

        Assert.That(slots, Is.EqualTo(new[]
        {
            new TimeOnly(8, 0), new TimeOnly(10, 0),
        }));
    }

    [TestCase(0, 0)]
    [TestCase(-30, 0)]
    public void Gerar_DuracaoNaoPositiva_RetornaListaVazia(int duracao, int intervalo)
    {
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(18, 0), duracao, intervalo);

        Assert.That(slots, Is.Empty);
    }

    [Test]
    public void Gerar_FimAntesDoInicio_RetornaListaVazia()
    {
        var slots = SlotGenerator.Gerar(new TimeOnly(18, 0), new TimeOnly(8, 0), 30, 0);

        Assert.That(slots, Is.Empty);
    }

    [Test]
    public void Gerar_DuracaoEncaixaJustoAteOFim_IncluiUltimoSlot()
    {
        // 8:00–9:00 com duracao 30: ultimo slot 8:30 termina em 9:00 (==), deve ser incluido.
        var slots = SlotGenerator.Gerar(new TimeOnly(8, 0), new TimeOnly(9, 0), 30, 0);

        Assert.That(slots, Is.EqualTo(new[]
        {
            new TimeOnly(8, 0), new TimeOnly(8, 30),
        }));
    }
}

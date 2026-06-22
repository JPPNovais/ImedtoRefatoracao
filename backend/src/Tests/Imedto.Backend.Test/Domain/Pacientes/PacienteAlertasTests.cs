using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Pacientes;

/// <summary>
/// Testes de unidade para Paciente.AtualizarSomenteAlertas (briefing 2026-06-22_002).
/// </summary>
[TestFixture]
public class PacienteAlertasTests
{
    private static Paciente PacienteAtivo() =>
        Paciente.Cadastrar(1L, "João Silva", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);

    [Test]
    public void AtualizarSomenteAlertas_ListaValida_PersisteAlertas()
    {
        var paciente = PacienteAtivo();
        var alertas = new[] { "Alergia a penicilina", "Hipertensão" };

        paciente.AtualizarSomenteAlertas(alertas);

        Assert.That(paciente.Alertas, Is.EquivalentTo(alertas));
    }

    [Test]
    public void AtualizarSomenteAlertas_ListaVazia_LimpaAlertas()
    {
        var paciente = PacienteAtivo();
        // Primeiro adiciona alertas
        paciente.AtualizarSomenteAlertas(new[] { "Alerta A" });
        // Depois limpa
        paciente.AtualizarSomenteAlertas(Array.Empty<string>());

        Assert.That(paciente.Alertas, Is.Empty);
    }

    [Test]
    public void AtualizarSomenteAlertas_ListaNull_LimpaAlertas()
    {
        var paciente = PacienteAtivo();
        paciente.AtualizarSomenteAlertas(new[] { "Alerta A" });

        // null equivale a lista vazia — NormalizarLista retorna Array.Empty<string>()
        paciente.AtualizarSomenteAlertas(null);

        Assert.That(paciente.Alertas, Is.Empty);
    }

    [Test]
    public void AtualizarSomenteAlertas_MaisDe10Alertas_LancaBusinessException()
    {
        var paciente = PacienteAtivo();
        var alertas = Enumerable.Range(1, 11).Select(i => $"Alerta {i}").ToArray();

        var ex = Assert.Throws<BusinessException>(() => paciente.AtualizarSomenteAlertas(alertas));
        Assert.That(ex.Message, Does.Contain("Alertas"));
    }

    [Test]
    public void AtualizarSomenteAlertas_AlertaComMaisDe200Chars_LancaBusinessException()
    {
        var paciente = PacienteAtivo();
        var alertaLongo = new string('x', 201);

        var ex = Assert.Throws<BusinessException>(() => paciente.AtualizarSomenteAlertas(new[] { alertaLongo }));
        Assert.That(ex.Message, Does.Contain("Alertas"));
    }

    [Test]
    public void AtualizarSomenteAlertas_Exatos10Alertas_Permitido()
    {
        var paciente = PacienteAtivo();
        var alertas = Enumerable.Range(1, 10).Select(i => $"Alerta {i}").ToArray();

        Assert.DoesNotThrow(() => paciente.AtualizarSomenteAlertas(alertas));
        Assert.That(paciente.Alertas, Has.Count.EqualTo(10));
    }

    [Test]
    public void AtualizarSomenteAlertas_PacienteDeletado_LancaBusinessException()
    {
        var paciente = PacienteAtivo();
        paciente.MarcarComoDeletado(Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() => paciente.AtualizarSomenteAlertas(new[] { "Alerta" }));
        Assert.That(ex.Message, Does.Contain("deletado"));
    }

    [Test]
    public void AtualizarSomenteAlertas_NaoAlteraOutrosCampos()
    {
        var paciente = PacienteAtivo();
        var nomeBefore = paciente.NomeCompleto;
        var estabBefore = paciente.EstabelecimentoId;

        paciente.AtualizarSomenteAlertas(new[] { "Alergia" });

        Assert.That(paciente.NomeCompleto, Is.EqualTo(nomeBefore));
        Assert.That(paciente.EstabelecimentoId, Is.EqualTo(estabBefore));
    }
}

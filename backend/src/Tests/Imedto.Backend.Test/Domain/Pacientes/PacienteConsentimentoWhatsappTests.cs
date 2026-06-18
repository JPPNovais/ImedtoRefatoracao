using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Pacientes;

/// <summary>
/// Testes de unidade do aggregate Paciente — consentimento WhatsApp (CA8/R3/R4).
/// </summary>
[TestFixture]
public class PacienteConsentimentoWhatsappTests
{
    private static Paciente CriarPaciente() =>
        Paciente.Cadastrar(1, "Maria Silva", "12345678909",
            new DateTime(1990, 1, 1), GeneroPaciente.Feminino,
            "11999998888", "maria@test.com", null, null);

    private readonly Guid _usuarioId = Guid.NewGuid();

    [Test]
    public void AtualizarConsentimentoWhatsapp_OptInTrue_GravaConsentimentoComAudit()
    {
        var paciente = CriarPaciente();

        paciente.AtualizarConsentimentoWhatsapp(true, _usuarioId);

        Assert.That(paciente.WhatsappLembreteOptIn, Is.True);
        Assert.That(paciente.WhatsappLembreteOptInEm, Is.Not.Null);
        Assert.That(paciente.WhatsappLembreteOptInEm, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-5)));
        Assert.That(paciente.WhatsappLembreteOptInPorUsuarioId, Is.EqualTo(_usuarioId));
    }

    [Test]
    public void AtualizarConsentimentoWhatsapp_OptInFalse_RevogaConsentimento()
    {
        var paciente = CriarPaciente();
        paciente.AtualizarConsentimentoWhatsapp(true, _usuarioId);

        paciente.AtualizarConsentimentoWhatsapp(false, _usuarioId);

        Assert.That(paciente.WhatsappLembreteOptIn, Is.False);
        // Registra quem revogou e quando (audit de revogação — R4)
        Assert.That(paciente.WhatsappLembreteOptInEm, Is.Not.Null);
        Assert.That(paciente.WhatsappLembreteOptInPorUsuarioId, Is.EqualTo(_usuarioId));
    }

    [Test]
    public void AtualizarConsentimentoWhatsapp_PacienteDeletado_LancaBusinessException()
    {
        var paciente = CriarPaciente();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(paciente, 99L);
        paciente.MarcarComoDeletado(_usuarioId);

        var ex = Assert.Throws<BusinessException>(
            () => paciente.AtualizarConsentimentoWhatsapp(true, _usuarioId));
        Assert.That(ex.Message, Does.Contain("Paciente deletado"));
    }

    [Test]
    public void NovoPaciente_WhatsappOptIn_EhFalsoPorPadrao()
    {
        var paciente = CriarPaciente();

        Assert.That(paciente.WhatsappLembreteOptIn, Is.False);
        Assert.That(paciente.WhatsappLembreteOptInEm, Is.Null);
        Assert.That(paciente.WhatsappLembreteOptInPorUsuarioId, Is.Null);
    }
}

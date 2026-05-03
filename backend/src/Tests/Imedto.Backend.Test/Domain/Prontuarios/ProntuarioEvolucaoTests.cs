using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Prontuarios;

[TestFixture]
public class ProntuarioEvolucaoTests
{
    private static ProntuarioEvolucao CriarValida() =>
        ProntuarioEvolucao.Registrar(
            prontuarioId: 1L,
            autorUsuarioId: Guid.NewGuid(),
            modeloDeProntuarioIdOrigem: 1L,
            modeloSnapshotJson: "{\"campos\":[]}",
            conteudoJson: "{\"queixa\":\"dor\"}");

    // ----- Registrar -----

    [Test]
    public void Registrar_Valido_StateOk()
    {
        var e = CriarValida();
        Assert.That(e.ProntuarioId, Is.EqualTo(1L));
        Assert.That(e.ConteudoJson, Is.EqualTo("{\"queixa\":\"dor\"}"));
        Assert.That(e.ModeloSnapshotJson, Is.EqualTo("{\"campos\":[]}"));
        Assert.That(e.CriadaEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(e.DeletadoEm, Is.Null);
    }

    [Test]
    public void Registrar_ProntuarioIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioEvolucao.Registrar(0L, Guid.NewGuid(), 1L, "{}", "{}"));
        Assert.That(ex.Message, Does.Contain("Prontuário"));
    }

    [Test]
    public void Registrar_AutorEmpty_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioEvolucao.Registrar(1L, Guid.Empty, 1L, "{}", "{}"));
        Assert.That(ex.Message, Does.Contain("Autor"));
    }

    [Test]
    public void Registrar_ModeloOrigemZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioEvolucao.Registrar(1L, Guid.NewGuid(), 0L, "{}", "{}"));
        Assert.That(ex.Message, Does.Contain("Modelo"));
    }

    [Test]
    public void Registrar_SnapshotVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioEvolucao.Registrar(1L, Guid.NewGuid(), 1L, " ", "{}"));
        Assert.That(ex.Message, Does.Contain("Snapshot"));
    }

    [Test]
    public void Registrar_ConteudoVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioEvolucao.Registrar(1L, Guid.NewGuid(), 1L, "{}", ""));
        Assert.That(ex.Message, Does.Contain("Conteúdo"));
    }

    // ----- MarcarComoRegistrada -----

    [Test]
    public void MarcarComoRegistrada_IdZero_LancaInvalidOperation()
    {
        var e = CriarValida();
        Assert.Throws<InvalidOperationException>(() => e.MarcarComoRegistrada());
    }

    [Test]
    public void MarcarComoRegistrada_IdValido_PublicaEvento()
    {
        var e = CriarValida();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, 99L);

        e.MarcarComoRegistrada();

        Assert.That(e.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(e.DomainEvents.First(), Is.TypeOf<EvolucaoRegistradaEvent>());
    }

    // ----- MarcarComoDeletado (LGPD soft delete) -----

    [Test]
    public void MarcarComoDeletado_Valido_SetaCamposDeAuditoria()
    {
        var e = CriarValida();
        var quemDeletou = Guid.NewGuid();
        e.MarcarComoDeletado(quemDeletou);

        Assert.That(e.DeletadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(e.DeletadoPorUsuarioId, Is.EqualTo(quemDeletou));
    }

    [Test]
    public void MarcarComoDeletado_UsuarioEmpty_LancaBusinessException()
    {
        var e = CriarValida();
        var ex = Assert.Throws<BusinessException>(() => e.MarcarComoDeletado(Guid.Empty));
        Assert.That(ex.Message, Does.Contain("Usuário"));
    }

    [Test]
    public void MarcarComoDeletado_JaDeletado_LancaBusinessException()
    {
        var e = CriarValida();
        e.MarcarComoDeletado(Guid.NewGuid());
        var ex = Assert.Throws<BusinessException>(() => e.MarcarComoDeletado(Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("já está deletada"));
    }
}

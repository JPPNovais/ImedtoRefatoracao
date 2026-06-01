using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;
using System.Collections.Generic;

namespace Imedto.Backend.Test.Domain;

/// <summary>
/// Testes de unidade da máquina de estados de assinatura digital no aggregate Receita.
/// Cobre: IniciarAssinatura, ConfirmarAssinatura, RegistrarFalhaAssinatura, ExpirarAssinaturaPendente.
/// </summary>
[TestFixture]
public class ReceitaAssinaturaTests
{
    private static Receita ReceitaEmitida()
    {
        var itens = new List<(string, string, string?, ViaAdministracao?, string?)>
        {
            ("Dipirona", "1 cp 8h", null, null, null)
        };
        return Receita.Emitir(
            prontuarioId: 1,
            pacienteId: 2,
            profissionalUsuarioId: Guid.NewGuid(),
            estabelecimentoId: 3,
            tipo: TipoReceita.Comum,
            observacoes: null,
            validadeAte: null,
            itens: itens);
    }

    // ── IniciarAssinatura ──────────────────────────────────────────────────────

    [Test]
    public void IniciarAssinatura_ReceitaEmitida_TransicionaParaPendente()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaPendente));
        Assert.That(r.AssinaturaSolicitadaEm, Is.Not.Null);
    }

    [Test]
    public void IniciarAssinatura_JaAssinada_LancaBusinessException()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.ConfirmarAssinatura("s3/key/receita.pdf");

        var ex = Assert.Throws<BusinessException>(() => r.IniciarAssinatura());
        Assert.That(ex!.Message, Does.Contain("já está assinada digitalmente"));
    }

    [Test]
    public void IniciarAssinatura_FalhaAssinatura_PermiteReDisparo()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.RegistrarFalhaAssinatura();

        // CA-11: redisparo permitido de FalhaAssinatura.
        Assert.DoesNotThrow(() => r.IniciarAssinatura());
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaPendente));
    }

    [Test]
    public void IniciarAssinatura_AssinaturaExpirada_PermiteReDisparo()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.ExpirarAssinaturaPendente();

        // CA-11: redisparo permitido de AssinaturaExpirada.
        Assert.DoesNotThrow(() => r.IniciarAssinatura());
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaPendente));
    }

    [Test]
    public void IniciarAssinatura_ReceitaRascunho_LancaBusinessException()
    {
        var r = Receita.IniciarRascunho(1, 2, Guid.NewGuid(), 3, TipoReceita.Comum, null, null, null);
        Assert.Throws<BusinessException>(() => r.IniciarAssinatura());
    }

    // ── ConfirmarAssinatura ────────────────────────────────────────────────────

    [Test]
    public void ConfirmarAssinatura_PendentePdfValido_TransicionaParaAssinadaIcp()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.ConfirmarAssinatura("receitas-assinadas/3/1/receita.pdf");

        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinadaIcp));
        Assert.That(r.PdfAssinadoS3Key, Is.EqualTo("receitas-assinadas/3/1/receita.pdf"));
        Assert.That(r.AssinadaEm, Is.Not.Null);
    }

    [Test]
    public void ConfirmarAssinatura_NaoPendente_LancaBusinessException()
    {
        var r = ReceitaEmitida();
        Assert.Throws<BusinessException>(() => r.ConfirmarAssinatura("key"));
    }

    [Test]
    public void ConfirmarAssinatura_S3KeyVazia_LancaBusinessException()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        Assert.Throws<BusinessException>(() => r.ConfirmarAssinatura(""));
    }

    // ── RegistrarFalhaAssinatura ───────────────────────────────────────────────

    [Test]
    public void RegistrarFalhaAssinatura_Pendente_TransicionaParaFalha()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.RegistrarFalhaAssinatura();
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.FalhaAssinatura));
    }

    [Test]
    public void RegistrarFalhaAssinatura_NaoPendente_Idempotente()
    {
        // CA-09: se já resolvido de outra forma, ignora.
        var r = ReceitaEmitida();
        Assert.DoesNotThrow(() => r.RegistrarFalhaAssinatura()); // NaoAssinada → ignora
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.NaoAssinada));
    }

    // ── ExpirarAssinaturaPendente ──────────────────────────────────────────────

    [Test]
    public void ExpirarAssinaturaPendente_Pendente_TransicionaParaExpirada()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.ExpirarAssinaturaPendente();
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaExpirada));
    }

    [Test]
    public void ExpirarAssinaturaPendente_NaoPendente_Idempotente()
    {
        var r = ReceitaEmitida();
        Assert.DoesNotThrow(() => r.ExpirarAssinaturaPendente()); // NaoAssinada → ignora
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.NaoAssinada));
    }

    [Test]
    public void ExpirarAssinaturaPendente_JaAssinada_Idempotente()
    {
        var r = ReceitaEmitida();
        r.IniciarAssinatura();
        r.ConfirmarAssinatura("s3key");
        // Webhook chegou depois do job — não deve reverter.
        Assert.DoesNotThrow(() => r.ExpirarAssinaturaPendente());
        Assert.That(r.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinadaIcp));
    }
}

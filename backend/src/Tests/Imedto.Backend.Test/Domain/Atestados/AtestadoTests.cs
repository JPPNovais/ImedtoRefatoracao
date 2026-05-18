using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Atestados;

[TestFixture]
public class AtestadoTests
{
    [Test]
    public void Emitir_Comparecimento_RetornaAtestadoSemDias()
    {
        var atestado = Atestado.Emitir(
            estabelecimentoId: 1, pacienteId: 10, profissionalUsuarioId: Guid.NewGuid(),
            tipo: TipoAtestado.Comparecimento, diasAfastamento: null, cid10: null,
            conteudo: "Compareceu à consulta no dia 18/05/2026.");

        Assert.That(atestado.Tipo, Is.EqualTo(TipoAtestado.Comparecimento));
        Assert.That(atestado.DiasAfastamento, Is.Null);
    }

    [Test]
    public void Emitir_AfastamentoSemDias_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Afastamento,
            diasAfastamento: null, cid10: null, conteudo: "Afastar."));
        Assert.That(ex.Message, Does.Contain("dias"));
    }

    [Test]
    public void Emitir_AfastamentoComDiasZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Afastamento, 0, null, "Afastar."));
    }

    [Test]
    public void Emitir_AfastamentoComDiasNegativos_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Afastamento, -3, null, "Afastar."));
    }

    [Test]
    public void Emitir_AfastamentoComDiasValidos_DefineDias()
    {
        var a = Atestado.Emitir(1, 10, Guid.NewGuid(), TipoAtestado.Afastamento, 5, null, "Afastar.");
        Assert.That(a.DiasAfastamento, Is.EqualTo(5));
    }

    [Test]
    public void Emitir_DiasMaiorQue365_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Afastamento, 400, null, "Longo afastamento."));
    }

    [Test]
    public void Emitir_DiasIgnorado_SeTipoForNaoAfastamento()
    {
        var a = Atestado.Emitir(1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, 5, null, "Compareceu.");
        Assert.That(a.DiasAfastamento, Is.Null);
    }

    [Test]
    public void Emitir_ConteudoVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, null, null, ""));
    }

    [Test]
    public void Emitir_ConteudoSomenteEspacos_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, null, null, "   "));
    }

    [Test]
    public void Emitir_Cid10ComFormatoErrado_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => Atestado.Emitir(
            1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, null, "U99", "Conteúdo."));
        Assert.That(ex.Message, Does.Contain("CID"));
    }

    [Test]
    public void Emitir_Cid10Formatado_Aceita()
    {
        var a = Atestado.Emitir(1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, null, "M54.5", "Conteúdo.");
        Assert.That(a.Cid10, Is.EqualTo("M54.5"));
    }

    [Test]
    public void Emitir_Cid10MinusculoEEspacos_Normaliza()
    {
        var a = Atestado.Emitir(1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, null, " j06.9 ", "Conteúdo.");
        Assert.That(a.Cid10, Is.EqualTo("J06.9"));
    }

    [Test]
    public void Emitir_SemCid_DeixaNull()
    {
        var a = Atestado.Emitir(1, 10, Guid.NewGuid(), TipoAtestado.Comparecimento, null, null, "Conteúdo.");
        Assert.That(a.Cid10, Is.Null);
    }
}

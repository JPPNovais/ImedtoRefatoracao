using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Termos;

[TestFixture]
public class TermoEmitidoTests
{
    private const long PacienteId = 10;
    private const long EstabId = 1;
    private const long ModeloId = 99;

    private static TermoEmitido EmitirSimples(AssinaturaTipo tipo = AssinaturaTipo.PdfAnexado) =>
        TermoEmitido.Emitir(
            PacienteId, EstabId, ModeloId, versaoModelo: 1,
            conteudoResolvidoHtml: "<p>Hello</p>",
            conteudoResolvidoTexto: "Hello",
            assinaturaTipo: tipo,
            emitidoPorUsuarioId: Guid.NewGuid(),
            ttlLinkPublico: TimeSpan.FromDays(7));

    [Test]
    public void Emitir_TipoPdfAnexado_NaoGeraToken()
    {
        var t = EmitirSimples(AssinaturaTipo.PdfAnexado);
        Assert.That(t.TokenAceite, Is.Null);
        Assert.That(t.TokenExpiraEm, Is.Null);
        Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Pendente));
        Assert.That(t.HashIntegridade, Has.Length.EqualTo(64));
    }

    [Test]
    public void Emitir_TipoAceiteLink_GeraTokenComExpiracao()
    {
        var t = EmitirSimples(AssinaturaTipo.AceiteLink);
        Assert.That(t.TokenAceite, Is.Not.Null);
        Assert.That(t.TokenAceite, Has.Length.GreaterThan(40)); // 32 bytes base64url ~ 43 chars
        Assert.That(t.TokenExpiraEm, Is.Not.Null);
        Assert.That(t.TokenExpiraEm.Value, Is.GreaterThan(DateTime.UtcNow.AddDays(6)));
    }

    [Test]
    public void Emitir_ConteudosNormalizadosComCrlf_GeramMesmoHashQueLf()
    {
        var t1 = TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1,
            "<p>Linha1</p>\n<p>Linha2</p>", "txt", AssinaturaTipo.PdfAnexado, Guid.NewGuid(), TimeSpan.FromDays(7));
        var t2 = TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1,
            "<p>Linha1</p>\r\n<p>Linha2</p>", "txt", AssinaturaTipo.PdfAnexado, Guid.NewGuid(), TimeSpan.FromDays(7));

        Assert.That(t1.HashIntegridade, Is.EqualTo(t2.HashIntegridade));
    }

    [Test]
    public void Emitir_ConteudoVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1, "", "tx",
                AssinaturaTipo.PdfAnexado, Guid.NewGuid(), TimeSpan.FromDays(7)));
    }

    [Test]
    public void AnexarPdf_TipoAceiteLink_LancaBusinessException()
    {
        var t = EmitirSimples(AssinaturaTipo.AceiteLink);
        Assert.Throws<BusinessException>(() => t.AnexarPdf("path/x.pdf", new string('a', 64)));
    }

    [Test]
    public void AnexarPdf_HashInvalido_LancaBusinessException()
    {
        var t = EmitirSimples();
        Assert.Throws<BusinessException>(() => t.AnexarPdf("path/x.pdf", "abc"));
    }

    [Test]
    public void AnexarPdf_Pendente_MarcaAssinado()
    {
        var t = EmitirSimples();
        t.AnexarPdf("termos/1/2_a.pdf", new string('a', 64));

        Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Assinado));
        Assert.That(t.AssinadoEm, Is.Not.Null);
        Assert.That(t.PdfUrl, Is.EqualTo("termos/1/2_a.pdf"));
    }

    [Test]
    public void AnexarPdf_JaAssinado_LancaBusinessException()
    {
        var t = EmitirSimples();
        t.AnexarPdf("p.pdf", new string('a', 64));
        Assert.Throws<BusinessException>(() => t.AnexarPdf("outro.pdf", new string('b', 64)));
    }

    [Test]
    public void RegistrarAceitePublico_TipoPdfAnexado_LancaBusinessException()
    {
        var t = EmitirSimples(AssinaturaTipo.PdfAnexado);
        Assert.Throws<BusinessException>(() => t.RegistrarAceitePublico("1.2.3.4", "Mozilla"));
    }

    [Test]
    public void RegistrarAceitePublico_AceiteLinkValido_MarcaAssinado()
    {
        var t = EmitirSimples(AssinaturaTipo.AceiteLink);
        t.RegistrarAceitePublico("1.2.3.4", "Mozilla/5.0");
        Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Assinado));
        Assert.That(t.IpAssinatura, Is.EqualTo("1.2.3.4"));
        Assert.That(t.UserAgentAssinatura, Is.EqualTo("Mozilla/5.0"));
    }

    [Test]
    public void Revogar_NaoAssinado_LancaBusinessException()
    {
        var t = EmitirSimples();
        Assert.Throws<BusinessException>(() => t.Revogar(Guid.NewGuid(), "motivo"));
    }

    [Test]
    public void Revogar_MotivoVazio_LancaBusinessException()
    {
        var t = EmitirSimples();
        t.AnexarPdf("p.pdf", new string('a', 64));
        Assert.Throws<BusinessException>(() => t.Revogar(Guid.NewGuid(), "  "));
    }

    [Test]
    public void Revogar_Assinado_MarcaRevogado()
    {
        var t = EmitirSimples();
        t.AnexarPdf("p.pdf", new string('a', 64));
        var usuario = Guid.NewGuid();
        t.Revogar(usuario, "Erro no consentimento");

        Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Revogado));
        Assert.That(t.RevogadoPorUsuarioId, Is.EqualTo(usuario));
        Assert.That(t.RevogadoMotivo, Is.EqualTo("Erro no consentimento"));
    }

    [Test]
    public void Expirar_AceiteLinkAposPrazo_MudaParaExpirado()
    {
        var t = TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1, "<p>x</p>", "x",
            AssinaturaTipo.AceiteLink, Guid.NewGuid(), TimeSpan.FromMilliseconds(1));
        Thread.Sleep(10);
        t.Expirar();
        Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Expirado));
    }

    [Test]
    public void NormalizarHtml_DiferentesEolsRetornamMesmoString()
    {
        var a = TermoEmitido.NormalizarHtml("<p>a</p>\n<p>b</p>");
        var b = TermoEmitido.NormalizarHtml("<p>a</p>\r\n<p>b</p>");
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void CalcularHashSha256_ConteudoFixo_RetornaHash64HexLowercase()
    {
        var h = TermoEmitido.CalcularHashSha256("teste");
        Assert.That(h, Has.Length.EqualTo(64));
        Assert.That(h, Does.Match("^[a-f0-9]{64}$"));
    }
}

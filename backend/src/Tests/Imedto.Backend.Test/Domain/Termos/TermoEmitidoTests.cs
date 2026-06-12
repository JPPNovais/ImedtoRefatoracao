using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Termos;

/// <summary>
/// Testes do aggregate TermoEmitido pós briefing 2026-06-12_002:
/// - AceiteLink removido; único fluxo é PdfAnexado.
/// - Métodos RegistrarAceitePublico, Expirar, RegistrarRecusaPublica removidos.
/// </summary>
[TestFixture]
public class TermoEmitidoTests
{
    private const long PacienteId = 10;
    private const long EstabId = 1;
    private const long ModeloId = 99;

    private static TermoEmitido EmitirSimples(long? evolucaoId = null) =>
        TermoEmitido.Emitir(
            PacienteId, EstabId, ModeloId, versaoModelo: 1,
            conteudoResolvidoHtml: "<p>Hello</p>",
            conteudoResolvidoTexto: "Hello",
            emitidoPorUsuarioId: Guid.NewGuid(),
            evolucaoId: evolucaoId);

    // ── Emissão ───────────────────────────────────────────────────────────────

    [Test]
    public void Emitir_PdfAnexado_NaoGeraTokenEFicaPendente()
    {
        var t = EmitirSimples();
        Assert.That(t.TokenAceite, Is.Null);
        Assert.That(t.TokenExpiraEm, Is.Null);
        Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Pendente));
        Assert.That(t.AssinaturaTipo, Is.EqualTo(AssinaturaTipo.PdfAnexado));
        Assert.That(t.HashIntegridade, Has.Length.EqualTo(64));
    }

    [Test]
    public void Emitir_ComEvolucaoId_VinculaCorretamente()
    {
        var t = EmitirSimples(evolucaoId: 42L);
        Assert.That(t.EvolucaoId, Is.EqualTo(42L));
    }

    [Test]
    public void Emitir_SemEvolucaoId_EvolucaoIdEhNull()
    {
        var t = EmitirSimples();
        Assert.That(t.EvolucaoId, Is.Null);
    }

    [Test]
    public void Emitir_ConteudosNormalizadosComCrlf_GeramMesmoHashQueLf()
    {
        var t1 = TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1,
            "<p>Linha1</p>\n<p>Linha2</p>", "txt", Guid.NewGuid());
        var t2 = TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1,
            "<p>Linha1</p>\r\n<p>Linha2</p>", "txt", Guid.NewGuid());

        Assert.That(t1.HashIntegridade, Is.EqualTo(t2.HashIntegridade));
    }

    [Test]
    public void Emitir_ConteudoVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1, "", "tx", Guid.NewGuid()));
    }

    [Test]
    public void Emitir_EmissorVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoEmitido.Emitir(PacienteId, EstabId, ModeloId, 1, "<p>x</p>", "x", Guid.Empty));
    }

    // ── AnexarPdf ──────────────────────────────────────────────────────────────

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
        Assert.That(t.PdfHash, Is.EqualTo(new string('a', 64)));
    }

    [Test]
    public void AnexarPdf_JaAssinado_LancaBusinessException()
    {
        var t = EmitirSimples();
        t.AnexarPdf("p.pdf", new string('a', 64));
        Assert.Throws<BusinessException>(() => t.AnexarPdf("outro.pdf", new string('b', 64)));
    }

    [Test]
    public void AnexarPdf_JaTemPdfUrl_LancaBusinessException()
    {
        // Segunda chamada (mesma instância já tem PdfUrl preenchida após primeira).
        var t = EmitirSimples();
        t.AnexarPdf("p.pdf", new string('a', 64));
        Assert.Throws<BusinessException>(() => t.AnexarPdf("outro.pdf", new string('b', 64)));
    }

    // ── Revogar ───────────────────────────────────────────────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────────

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

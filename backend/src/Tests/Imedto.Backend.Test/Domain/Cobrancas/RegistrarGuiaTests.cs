using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Cobrancas;

/// <summary>Testes da invariante RegistrarGuia (F6/R10/CA148).</summary>
[TestFixture]
public class RegistrarGuiaTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const long EstabId = 1L;
    private const long PacienteId = 10L;
    private const long AgendamentoId = 500L;

    private static Cobranca CriarConvenio(long? convenioId = 55L)
        => Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Convenio, 0m, "Consulta Convênio", UsuarioId, convenioId);

    private static Cobranca CriarParticular()
        => Cobranca.CriarParaConsulta(EstabId, PacienteId, AgendamentoId,
            TipoAtendimento.Particular, 200m, "Consulta Particular", UsuarioId);

    // ── CA148: guia só em convênio ────────────────────────────────────────────

    [Test]
    public void RegistrarGuia_TipoParticular_Lanca422()
    {
        var c = CriarParticular();
        var ex = Assert.Throws<BusinessException>(() =>
            c.RegistrarGuia("123", null, null));
        Assert.That(ex.Message, Does.Contain("convênio"));
    }

    [Test]
    public void RegistrarGuia_NumeroVazio_Lanca422()
    {
        var c = CriarConvenio();
        var ex = Assert.Throws<BusinessException>(() =>
            c.RegistrarGuia("  ", null, null));
        Assert.That(ex.Message, Does.Contain("guia"));
    }

    [Test]
    public void RegistrarGuia_DadosValidos_PreenchemPropriedades()
    {
        var c = CriarConvenio();
        var autorizadaEm = DateOnly.FromDateTime(DateTime.Today);
        c.RegistrarGuia(" G-001 ", "senha123", autorizadaEm);

        Assert.Multiple(() =>
        {
            Assert.That(c.GuiaNumero, Is.EqualTo("G-001")); // trim
            Assert.That(c.GuiaSenha, Is.EqualTo("senha123"));
            Assert.That(c.GuiaAutorizadaEm, Is.EqualTo(autorizadaEm));
            Assert.That(c.AtualizadoEm, Is.Not.Null);
        });
    }

    [Test]
    public void RegistrarGuia_SemSenha_SenhaNula()
    {
        var c = CriarConvenio();
        c.RegistrarGuia("G-002", "  ", null);
        Assert.That(c.GuiaSenha, Is.Null);
        Assert.That(c.GuiaAutorizadaEm, Is.Null);
    }

    [Test]
    public void RegistrarGuia_Sobrescreve_GuiaAnterior()
    {
        var c = CriarConvenio();
        c.RegistrarGuia("G-001", "s1", null);
        c.RegistrarGuia("G-002", "s2", DateOnly.FromDateTime(DateTime.Today));
        Assert.That(c.GuiaNumero, Is.EqualTo("G-002"));
        Assert.That(c.GuiaSenha, Is.EqualTo("s2"));
    }

    // ── F6/R7: convenioId gravado no factory ─────────────────────────────────

    [Test]
    public void CriarParaConsulta_TipoConvenio_ConvenioIdGravado()
    {
        var c = CriarConvenio(convenioId: 77L);
        Assert.That(c.ConvenioId, Is.EqualTo(77L));
        Assert.That(c.TipoAtendimento, Is.EqualTo(TipoAtendimento.Convenio));
    }

    [Test]
    public void CriarParaConsulta_TipoParticular_ConvenioIdNulo()
    {
        var c = CriarParticular();
        // CA145 regressão: Particular nunca tem convenioId mesmo que passemos
        Assert.That(c.ConvenioId, Is.Null);
    }
}

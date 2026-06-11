using Imedto.Backend.Domain.PacienteConvenios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Convenios;

[TestFixture]
public class PacienteConvenioTests
{
    private const long PacienteId = 10L;
    private const long EstabId = 1L;
    private const long ConvenioId = 5L;

    // ── Invariantes Criar ─────────────────────────────────────────────────────

    [Test]
    public void Criar_PacienteZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            PacienteConvenio.Criar(0, EstabId, ConvenioId, null, "123", null));
        Assert.That(ex.Message, Does.Contain("Paciente"));
    }

    [Test]
    public void Criar_EstabelecimentoZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            PacienteConvenio.Criar(PacienteId, 0, ConvenioId, null, "123", null));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Criar_ConvenioZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            PacienteConvenio.Criar(PacienteId, EstabId, 0, null, "123", null));
        Assert.That(ex.Message, Does.Contain("Convênio"));
    }

    [Test]
    public void Criar_NumeroCarteirinhaVazio_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            PacienteConvenio.Criar(PacienteId, EstabId, ConvenioId, null, "  ", null));
        Assert.That(ex.Message, Does.Contain("carteirinha"));
    }

    [Test]
    public void Criar_DadosValidos_CriaAtiva()
    {
        var pc = PacienteConvenio.Criar(PacienteId, EstabId, ConvenioId, 2L, " 98765 ", DateOnly.FromDateTime(DateTime.Today.AddYears(1)));
        Assert.Multiple(() =>
        {
            Assert.That(pc.PacienteId, Is.EqualTo(PacienteId));
            Assert.That(pc.EstabelecimentoId, Is.EqualTo(EstabId));
            Assert.That(pc.ConvenioId, Is.EqualTo(ConvenioId));
            Assert.That(pc.PlanoId, Is.EqualTo(2L));
            Assert.That(pc.NumeroCarteirinha, Is.EqualTo("98765")); // trim
            Assert.That(pc.Ativo, Is.True);
        });
    }

    [Test]
    public void Criar_PlanoIdZero_FicaNulo()
    {
        var pc = PacienteConvenio.Criar(PacienteId, EstabId, ConvenioId, 0L, "123", null);
        Assert.That(pc.PlanoId, Is.Null);
    }

    // ── Invariantes Atualizar ─────────────────────────────────────────────────

    [Test]
    public void Atualizar_ConvenioZero_Lanca()
    {
        var pc = PacienteConvenio.Criar(PacienteId, EstabId, ConvenioId, null, "123", null);
        var ex = Assert.Throws<BusinessException>(() => pc.Atualizar(0, null, "123", null, true));
        Assert.That(ex.Message, Does.Contain("Convênio"));
    }

    [Test]
    public void Atualizar_NumeroVazio_Lanca()
    {
        var pc = PacienteConvenio.Criar(PacienteId, EstabId, ConvenioId, null, "123", null);
        var ex = Assert.Throws<BusinessException>(() => pc.Atualizar(ConvenioId, null, "", null, true));
        Assert.That(ex.Message, Does.Contain("carteirinha"));
    }

    [Test]
    public void Atualizar_Inativo_DesativacaoRefletida()
    {
        var pc = PacienteConvenio.Criar(PacienteId, EstabId, ConvenioId, null, "123", null);
        pc.Atualizar(ConvenioId, null, "456", null, false);
        Assert.That(pc.Ativo, Is.False);
        Assert.That(pc.AtualizadoEm, Is.Not.Null);
    }
}

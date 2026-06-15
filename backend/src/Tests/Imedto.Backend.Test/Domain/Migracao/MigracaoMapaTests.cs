using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Migracao;

[TestFixture]
public class MigracaoMapaTests
{
    private const long JobId = 10;
    private const long EstabelecimentoId = 42;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const string EntidadeValida = "paciente";
    private const string MapaJsonValido = "{\"de_para\":{\"nome\":\"nome\"},\"confianca\":0.9,\"duvidas\":[]}";

    // ─── Criar ──────────────────────────────────────────────────────────────────

    [Test]
    public void Criar_Valido_RetornaMapa()
    {
        var mapa = MigracaoMapa.Criar(JobId, EstabelecimentoId, EntidadeValida, MapaJsonValido);

        Assert.That(mapa.MigracaoJobId, Is.EqualTo(JobId));
        Assert.That(mapa.EstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        Assert.That(mapa.Entidade, Is.EqualTo(EntidadeValida));
        Assert.That(mapa.MapaJson, Is.EqualTo(MapaJsonValido));
        Assert.That(mapa.RevisadoPorUsuarioId, Is.Null);
        Assert.That(mapa.RevisadoEm, Is.Null);
    }

    [Test]
    public void Criar_EntidadeComEspacos_Trimada()
    {
        var mapa = MigracaoMapa.Criar(JobId, EstabelecimentoId, "  paciente  ", MapaJsonValido);
        Assert.That(mapa.Entidade, Is.EqualTo("paciente"));
    }

    [Test]
    public void Criar_JobIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MigracaoMapa.Criar(0, EstabelecimentoId, EntidadeValida, MapaJsonValido));
        Assert.That(ex!.Message, Does.Contain("Job"));
    }

    [Test]
    public void Criar_EstabelecimentoIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MigracaoMapa.Criar(JobId, 0, EntidadeValida, MapaJsonValido));
        Assert.That(ex!.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Criar_EntidadeVazia_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MigracaoMapa.Criar(JobId, EstabelecimentoId, "  ", MapaJsonValido));
        Assert.That(ex!.Message, Does.Contain("Entidade"));
    }

    [Test]
    public void Criar_MapaJsonVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MigracaoMapa.Criar(JobId, EstabelecimentoId, EntidadeValida, ""));
        Assert.That(ex!.Message, Does.Contain("Mapa"));
    }

    // ─── Revisar ─────────────────────────────────────────────────────────────────

    [Test]
    public void Revisar_Valido_AtualizaCampos()
    {
        var mapa = MigracaoMapa.Criar(JobId, EstabelecimentoId, EntidadeValida, MapaJsonValido);
        var novoJson = "{\"de_para\":{\"nome_completo\":\"nome\"},\"confianca\":1.0,\"duvidas\":[]}";

        mapa.Revisar(novoJson, UsuarioId);

        Assert.That(mapa.MapaJson, Is.EqualTo(novoJson));
        Assert.That(mapa.RevisadoPorUsuarioId, Is.EqualTo(UsuarioId));
        Assert.That(mapa.RevisadoEm, Is.Not.Null);
        Assert.That(mapa.AtualizadoEm, Is.GreaterThan(mapa.CriadoEm).Or.EqualTo(mapa.CriadoEm));
    }

    [Test]
    public void Revisar_MapaVazio_LancaBusinessException()
    {
        var mapa = MigracaoMapa.Criar(JobId, EstabelecimentoId, EntidadeValida, MapaJsonValido);

        var ex = Assert.Throws<BusinessException>(() => mapa.Revisar("", UsuarioId));
        Assert.That(ex!.Message, Does.Contain("vazio"));
    }
}

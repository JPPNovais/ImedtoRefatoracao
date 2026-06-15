using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Migracao;

[TestFixture]
public class MigracaoJobTransicaoTests
{
    private static MigracaoJob CriarJobAguardandoMapa()
    {
        var job = MigracaoJob.Criar(42L, Guid.NewGuid());
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        return job; // status = aguardando_mapa
    }

    // ─── MarcarMapaEmRevisao ──────────────────────────────────────────────────

    [Test]
    public void MarcarMapaEmRevisao_StatusAguardandoMapa_Transiciona()
    {
        var job = CriarJobAguardandoMapa();

        job.MarcarMapaEmRevisao();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusMapaEmRevisao));
    }

    [Test]
    public void MarcarMapaEmRevisao_StatusErrado_LancaBusinessException()
    {
        // Job em aguardando_arquivo (não passou por RegistrarArquivoRecebido).
        var job = MigracaoJob.Criar(42L, Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() => job.MarcarMapaEmRevisao());
        Assert.That(ex!.Message, Does.Contain("aguardando mapa"));
    }

    [Test]
    public void MarcarMapaEmRevisao_JaEmRevisao_LancaBusinessException()
    {
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao();

        // Tentar transicionar novamente deve falhar.
        var ex = Assert.Throws<BusinessException>(() => job.MarcarMapaEmRevisao());
        Assert.That(ex!.Message, Does.Contain("aguardando mapa"));
    }
}

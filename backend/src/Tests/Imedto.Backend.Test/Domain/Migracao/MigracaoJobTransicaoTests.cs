using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Migracao;

[TestFixture]
public class MigracaoJobTransicaoTests
{
    private static MigracaoJob CriarJobAguardandoMapa()
    {
        // Addendum 003: upload agora vai para aguardando_aprovacao;
        // AprovarAnalise() é necessário para chegar em aguardando_mapa.
        var job = MigracaoJob.Criar(42L, Guid.NewGuid());
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid()); // aguardando_aprovacao → aguardando_mapa
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

    // ─── MarcarDesfeito (CA17, R9) ────────────────────────────────────────────

    private static MigracaoJob CriarJobConcluido()
    {
        var admin = Guid.NewGuid();
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(admin);
        job.MarcarMigrando(admin);
        job.MarcarConcluido();
        return job;
    }

    [Test]
    public void MarcarDesfeito_StatusConcluido_Transiciona()
    {
        var job = CriarJobConcluido();

        job.MarcarDesfeito();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusDesfeito));
    }

    [Test]
    public void MarcarDesfeito_StatusConcluidoComErros_Transiciona()
    {
        var admin = Guid.NewGuid();
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(admin);
        job.MarcarMigrando(admin);
        job.MarcarConcluidoComErros();

        job.MarcarDesfeito();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusDesfeito));
    }

    [Test]
    public void MarcarDesfeito_StatusMigrando_LancaBusinessException()
    {
        var admin = Guid.NewGuid();
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(admin);
        job.MarcarMigrando(admin);
        // Não concluiu — ainda está migrando

        var ex = Assert.Throws<BusinessException>(() => job.MarcarDesfeito());
        Assert.That(ex!.Message, Does.Contain("concluídos"));
    }

    [Test]
    public void MarcarDesfeito_StatusAguardandoArquivo_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(42L, Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() => job.MarcarDesfeito());
        Assert.That(ex!.Message, Does.Contain("concluídos"));
    }

    [Test]
    public void MarcarDesfeito_AtualizaAtualizadoEm()
    {
        var antesDeDesfazer = DateTime.UtcNow.AddSeconds(-1);
        var job = CriarJobConcluido();

        job.MarcarDesfeito();

        Assert.That(job.AtualizadoEm, Is.GreaterThanOrEqualTo(antesDeDesfazer));
    }
}

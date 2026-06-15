using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Migracao;

/// <summary>
/// Testes de invariantes de falha e reprocessamento do aggregate MigracaoJob.
/// Cobre CA25, CA26, CA27, CA28, CA30, CA31 do addendum 2026-06-15_002.
/// </summary>
[TestFixture]
public class MigracaoJobFalhaTests
{
    private static MigracaoJob CriarJobAguardandoMapa()
    {
        // Addendum 003: upload vai para aguardando_aprovacao; AprovarAnalise leva a aguardando_mapa.
        var job = MigracaoJob.Criar(42L, Guid.NewGuid());
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid()); // aguardando_aprovacao → aguardando_mapa
        return job; // status = aguardando_mapa
    }

    private static MigracaoJob CriarJobMigrando()
    {
        var admin = Guid.NewGuid();
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(admin);
        job.MarcarMigrando(admin);
        return job; // status = migrando
    }

    // ─── CA25/CA26 — MarcarFalhou ──────────────────────────────────────────────

    /// <summary>CA25 — Job em aguardando_mapa vai para falhou com motivo.</summary>
    [Test]
    public void MarcarFalhou_EmAguardandoMapa_TransicionaParaFalhou()
    {
        var job = CriarJobAguardandoMapa();

        job.MarcarFalhou("IA não configurada");

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        Assert.That(job.MotivoFalha, Is.EqualTo("IA não configurada"));
        Assert.That(job.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
    }

    /// <summary>CA26 — Job em migrando vai para falhou com motivo.</summary>
    [Test]
    public void MarcarFalhou_EmMigrando_TransicionaParaFalhou()
    {
        var job = CriarJobMigrando();

        job.MarcarFalhou("falha inesperada na carga");

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        Assert.That(job.MotivoFalha, Is.EqualTo("falha inesperada na carga"));
        Assert.That(job.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusMigrando));
    }

    /// <summary>CA25 — StatusAntesFalha guarda o estado correto para o reprocessar saber de onde retomar.</summary>
    [Test]
    public void MarcarFalhou_GuardaStatusAntesFalhaCorreto()
    {
        var jobMapa = CriarJobAguardandoMapa();
        var jobMigrando = CriarJobMigrando();

        jobMapa.MarcarFalhou("falha ao gerar o mapa");
        jobMigrando.MarcarFalhou("falha inesperada na carga");

        Assert.That(jobMapa.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
        Assert.That(jobMigrando.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusMigrando));
    }

    /// <summary>CA28 — Motivo não pode ser vazio (força a usar categoria explícita).</summary>
    [Test]
    public void MarcarFalhou_MotivoVazio_LancaBusinessException()
    {
        var job = CriarJobAguardandoMapa();

        var ex = Assert.Throws<BusinessException>(() => job.MarcarFalhou("   "));

        Assert.That(ex!.Message, Does.Contain("Motivo"));
    }

    /// <summary>CA25 — MarcarFalhou a partir de status inválido (ex.: mapa_em_revisao) lança exceção.</summary>
    [Test]
    public void MarcarFalhou_StatusInvalido_LancaBusinessException()
    {
        var admin = Guid.NewGuid();
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao(); // mapa_em_revisao — não é status de falha válido

        var ex = Assert.Throws<BusinessException>(() => job.MarcarFalhou("motivo qualquer"));

        Assert.That(ex!.Message, Does.Contain("aguardando_mapa").Or.Contain("migrando"));
    }

    /// <summary>CA28 — AtualizadoEm é atualizado na transição de falha.</summary>
    [Test]
    public void MarcarFalhou_AtualizaAtualizadoEm()
    {
        var antes = DateTime.UtcNow.AddSeconds(-1);
        var job = CriarJobAguardandoMapa();

        job.MarcarFalhou("IA não configurada");

        Assert.That(job.AtualizadoEm, Is.GreaterThanOrEqualTo(antes));
    }

    // ─── CA30/CA31 — Reprocessar ──────────────────────────────────────────────

    /// <summary>CA30 — Reprocessar a partir de falha na inferência retorna para aguardando_mapa.</summary>
    [Test]
    public void Reprocessar_FalhaInferencia_VoltaParaAguardandoMapa()
    {
        var job = CriarJobAguardandoMapa();
        job.MarcarFalhou("IA não configurada");

        job.Reprocessar();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
        Assert.That(job.MotivoFalha, Is.Null);
        Assert.That(job.StatusAntesFalha, Is.Null);
    }

    /// <summary>CA30 — Reprocessar a partir de falha na carga retorna para migrando.</summary>
    [Test]
    public void Reprocessar_FalhaCarga_VoltaParaMigrando()
    {
        var job = CriarJobMigrando();
        job.MarcarFalhou("falha inesperada na carga");

        job.Reprocessar();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusMigrando));
        Assert.That(job.MotivoFalha, Is.Null);
        Assert.That(job.StatusAntesFalha, Is.Null);
    }

    /// <summary>CA31 — Reprocessar job que NÃO está em falhou lança exceção (422).</summary>
    [Test]
    public void Reprocessar_JobNaoFalhou_LancaBusinessException()
    {
        var job = CriarJobAguardandoMapa();
        // Status = aguardando_mapa (não falhou)

        var ex = Assert.Throws<BusinessException>(() => job.Reprocessar());

        Assert.That(ex!.Message, Does.Contain("falharam"));
    }

    /// <summary>CA31 — Reprocessar job concluído (não em falha) lança exceção.</summary>
    [Test]
    public void Reprocessar_JobConcluido_LancaBusinessException()
    {
        var admin = Guid.NewGuid();
        var job = CriarJobMigrando();
        job.MarcarConcluido();

        var ex = Assert.Throws<BusinessException>(() => job.Reprocessar());

        Assert.That(ex!.Message, Does.Contain("falharam"));
    }

    /// <summary>CA30 — Reprocessar limpa motivo e status anterior.</summary>
    [Test]
    public void Reprocessar_LimpaMotivoeStatusAntes()
    {
        var job = CriarJobAguardandoMapa();
        job.MarcarFalhou("arquivo corrompido ou ilegível");

        job.Reprocessar();

        Assert.That(job.MotivoFalha, Is.Null);
        Assert.That(job.StatusAntesFalha, Is.Null);
    }

    /// <summary>CA25 — Constante de status "falhou" tem o valor correto em snake_case.</summary>
    [Test]
    public void StatusFalhou_Constante_ValorCorreto()
    {
        Assert.That(MigracaoJob.StatusFalhou, Is.EqualTo("falhou"));
    }
}

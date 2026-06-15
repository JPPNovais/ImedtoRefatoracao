using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Migracao;

/// <summary>
/// Testes de invariantes do aggregate MigracaoJob — gate de aprovação (addendum 003).
/// Cobrem: CA40, CA41, CA42, CA45 (domínio).
/// </summary>
[TestFixture]
public class MigracaoJobAprovacaoTests
{
    private const long EstabelecimentoId = 42;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid AdminId   = Guid.NewGuid();

    // ─── CA40 — upload → aguardando_aprovacao (não mais aguardando_mapa) ─────────

    [Test]
    public void RegistrarArquivoRecebido_Valido_TransicionaParaAguardandoAprovacao()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        var antes = DateTime.UtcNow;

        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        // CA40 — estado pós-upload é aguardando_aprovacao, NÃO aguardando_mapa.
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoAprovacao));
        // CA40 — ArquivoExpiraEm e TermoAceitoEm continuam preenchidos (R12/CA24 intactos).
        Assert.That(job.ArquivoExpiraEm, Is.GreaterThan(antes.AddDays(29)));
        Assert.That(job.TermoAceitoEm, Is.GreaterThanOrEqualTo(antes));
        Assert.That(job.ArquivoS3Key, Is.EqualTo("migracao/42/1/arquivo.zip"));
    }

    [Test]
    public void RegistrarArquivoRecebido_Valido_NaoTransicionaParaAguardandoMapa()
    {
        // Gate anti-regressão: após o upload, o status NÃO deve ser aguardando_mapa.
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);

        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        Assert.That(job.Status, Is.Not.EqualTo(MigracaoJob.StatusAguardandoMapa),
            "Upload não deve ir direto para aguardando_mapa — gate de aprovação viola regra addendum 003.");
    }

    // ─── CA41 — AprovarAnalise: aguardando_aprovacao → aguardando_mapa ──────────

    [Test]
    public void AprovarAnalise_EmAguardandoAprovacao_TransicionaParaAguardandoMapa()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        job.AprovarAnalise(AdminId);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
    }

    // ─── CA42 — aprovar fora de aguardando_aprovacao → 422 ──────────────────────

    [Test]
    public void AprovarAnalise_EmAguardandoMapa_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        job.AprovarAnalise(AdminId); // já em aguardando_mapa

        var ex = Assert.Throws<BusinessException>(() => job.AprovarAnalise(AdminId));

        Assert.That(ex.Message, Does.Contain("aguardando aprovação"));
    }

    [Test]
    public void AprovarAnalise_EmAguardandoArquivo_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);

        Assert.Throws<BusinessException>(() => job.AprovarAnalise(AdminId));
    }

    [Test]
    public void AprovarAnalise_AdminIdVazio_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        var ex = Assert.Throws<BusinessException>(() => job.AprovarAnalise(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("Admin"));
    }

    // ─── CA45 — Rejeitar aceita aguardando_aprovacao (R-A5/D-A4) ────────────────

    [Test]
    public void Rejeitar_EmAguardandoAprovacao_TransicionaParaRejeitado()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        // Job está em aguardando_aprovacao — rejeitar deve funcionar (D-A4/R-A5).
        job.Rejeitar();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusRejeitado));
    }

    [Test]
    public void Rejeitar_EmAguardandoAprovacao_NaoDispararaIA()
    {
        // CA45 — job rejeitado a partir de aguardando_aprovacao nunca fica em aguardando_mapa.
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        job.Rejeitar();

        Assert.That(job.Status, Is.Not.EqualTo(MigracaoJob.StatusAguardandoMapa),
            "Job rejeitado não deve atingir aguardando_mapa (não pode ser inferido pela IA).");
    }

    [Test]
    public void Rejeitar_EmStatusTerminal_AindaLancaBusinessException()
    {
        // CA45 — a ampliação de guard NÃO afrouxou os terminais: concluido, desfeito, rejeitado.
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.Rejeitar(); // aguardando_arquivo → rejeitado

        Assert.Throws<BusinessException>(() => job.Rejeitar(),
            "Rejeitar a partir de rejeitado (terminal) deve continuar inválido.");
    }

    // ─── Constante do novo estado (CA20 — valores snake_case) ───────────────────

    [Test]
    public void StatusAguardandoAprovacao_EhSnakeCase()
    {
        Assert.That(MigracaoJob.StatusAguardandoAprovacao, Is.EqualTo("aguardando_aprovacao"));
    }

    // ─── CA50 — regressão: Reprocessar volta para aguardando_mapa (não exige re-aprovação) ──

    [Test]
    public void Reprocessar_DeveVoltarParaAguardandoMapa_NaoParaAguardandoAprovacao()
    {
        // CA50 — um job que falhou na inferência (statusAntesFalha = aguardando_mapa)
        // ao reprocessar DEVE voltar para aguardando_mapa — não exige nova aprovação.
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        job.AprovarAnalise(AdminId); // aguardando_aprovacao → aguardando_mapa
        job.MarcarFalhou("IA não configurada"); // aguardando_mapa → falhou

        job.Reprocessar(); // falhou → aguardando_mapa (statusAntesFalha)

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa),
            "Reprocessar de falha de inferência deve voltar para aguardando_mapa — não exige nova aprovação.");
        Assert.That(job.Status, Is.Not.EqualTo(MigracaoJob.StatusAguardandoAprovacao),
            "Reprocessar NÃO deve voltar para aguardando_aprovacao — aprovação é gate de entrada, não de cada tentativa.");
    }
}

using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Migracao;

/// <summary>
/// Testes de invariantes do aggregate MigracaoJob (briefing 2026-06-15_001 — Marco 1).
/// Cobrem: CA19 (tamanho), CA20 (transições de estado), CA2 (multi-tenant), CA4 (sem PII em erros).
/// </summary>
[TestFixture]
public class MigracaoJobTests
{
    private const long EstabelecimentoId = 42;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    // ─── Criar ──────────────────────────────────────────────────────────────────

    [Test]
    public void Criar_Valido_StatusAguardandoArquivo()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoArquivo));
        Assert.That(job.EstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        Assert.That(job.CriadoPorUsuarioId, Is.EqualTo(UsuarioId));
        Assert.That(job.Origem, Is.Null);
        Assert.That(job.ArquivoS3Key, Is.Null);
        Assert.That(job.ArquivoExpirado, Is.False);
    }

    [Test]
    public void Criar_ComOrigem_NormalizeWhitespace()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId, origem: "  iClinic  ");

        Assert.That(job.Origem, Is.EqualTo("iClinic"));
    }

    [Test]
    public void Criar_OrigemVazia_OrigemNula()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId, origem: "   ");

        Assert.That(job.Origem, Is.Null);
    }

    /// <summary>CA2 — estabelecimento_id inválido é recusado (multi-tenant falha-fechada).</summary>
    [Test]
    public void Criar_EstabelecimentoIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MigracaoJob.Criar(estabelecimentoId: 0, UsuarioId));

        // CA4 — mensagem não pode vazar PII nem identificar tenant alheio.
        Assert.That(ex.Message, Does.Not.Contain("0"));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Criar_UsuarioIdVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MigracaoJob.Criar(EstabelecimentoId, criadoPorUsuarioId: Guid.Empty));

        Assert.That(ex.Message, Does.Contain("Usuário"));
    }

    // ─── RegistrarArquivoRecebido ───────────────────────────────────────────────

    [Test]
    public void RegistrarArquivoRecebido_Valido_TransicionaParaAguardandoAprovacao()
    {
        // Addendum 003 — R-A1: upload agora vai para aguardando_aprovacao (não mais aguardando_mapa).
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        var antes = DateTime.UtcNow;

        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoAprovacao));
        Assert.That(job.ArquivoS3Key, Is.EqualTo("migracao/42/1/arquivo.zip"));
        // CA24 — retenção de 30 dias a partir do registro (R12 intacto).
        Assert.That(job.ArquivoExpiraEm, Is.GreaterThan(antes.AddDays(29)));
        // R12 — termo registrado no momento do upload.
        Assert.That(job.TermoAceitoEm, Is.GreaterThanOrEqualTo(antes));
    }

    [Test]
    public void RegistrarArquivoRecebido_S3KeyVazia_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);

        Assert.Throws<BusinessException>(() =>
            job.RegistrarArquivoRecebido(string.Empty));
    }

    [Test]
    public void RegistrarArquivoRecebido_StatusErrado_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip"); // → aguardando_aprovacao (addendum 003)

        // Tentar registrar de novo não é permitido.
        var ex = Assert.Throws<BusinessException>(() =>
            job.RegistrarArquivoRecebido("outro.zip"));

        Assert.That(ex.Message, Does.Contain("aguardando arquivo"));
    }

    // ─── Rejeitar ───────────────────────────────────────────────────────────────

    [Test]
    public void Rejeitar_EmAguardandoArquivo_TransicionaParaRejeitado()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);

        job.Rejeitar();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusRejeitado));
    }

    [Test]
    public void Rejeitar_EmAguardandoMapa_TransicionaParaRejeitado()
    {
        // Para chegar em aguardando_mapa: upload (→ aguardando_aprovacao) + aprovar (→ aguardando_mapa).
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        job.AprovarAnalise(UsuarioId); // → aguardando_mapa

        job.Rejeitar();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusRejeitado));
    }

    [Test]
    public void Rejeitar_EmAguardandoAprovacao_TransicionaParaRejeitado()
    {
        // Addendum 003 — R-A5: rejeitar a partir de aguardando_aprovacao (D-A4).
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        job.Rejeitar();

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusRejeitado));
    }

    [Test]
    public void Rejeitar_EmStatusFinal_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.Rejeitar(); // já está rejeitado

        // Segunda chamada não é permitida.
        Assert.Throws<BusinessException>(() => job.Rejeitar());
    }

    // ─── MarcarArquivoExpirado ──────────────────────────────────────────────────

    [Test]
    public void MarcarArquivoExpirado_MarcaFlag()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        job.MarcarArquivoExpirado();

        Assert.That(job.ArquivoExpirado, Is.True);
    }

    // ─── Checagem de constantes de status (CA20) ────────────────────────────────

    [Test]
    public void StatusConstants_ValoresSnakeCase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MigracaoJob.StatusAguardandoArquivo, Is.EqualTo("aguardando_arquivo"));
            Assert.That(MigracaoJob.StatusAguardandoMapa,    Is.EqualTo("aguardando_mapa"));
            Assert.That(MigracaoJob.StatusRejeitado,         Is.EqualTo("rejeitado"));
            Assert.That(MigracaoJob.StatusConcluido,         Is.EqualTo("concluido"));
        });
    }
}

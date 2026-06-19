using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

/// <summary>
/// Cobre CA1, CA2, CA5 — regras de domínio de ExpirarPorFimDoDia.
/// Briefing 2026-06-19_001.
/// </summary>
[TestFixture]
public class AgendamentoExpiracaoTests
{
    private const string MotivoExpiracao = "Expirado automaticamente — não finalizado até o fim do dia";

    /// <summary>CriarHistorico permite data no passado — necessário para criar cenários D-1.</summary>
    private static Agendamento CriarHistoricoOntem(
        AgendamentoStatus statusInicial = AgendamentoStatus.Agendado,
        long estabelecimentoId = 1)
    {
        var inicio = DateTime.UtcNow.AddDays(-1);
        var a = Agendamento.CriarHistorico(
            estabelecimentoId: estabelecimentoId,
            pacienteId: 1,
            profissionalUsuarioId: Guid.NewGuid(),
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: inicio,
            fimPrevisto: inicio.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);

        // Ajusta status para o cenário desejado via fábrica + método de domínio.
        if (statusInicial == AgendamentoStatus.Confirmado)
            a.Confirmar();

        return a;
    }

    // ─── CA1: expira Agendado e Confirmado de D-1 ─────────────────────────────

    [Test]
    public void ExpirarPorFimDoDia_StatusAgendado_MudaParaExpirado()
    {
        var a = CriarHistoricoOntem(AgendamentoStatus.Agendado);

        a.ExpirarPorFimDoDia(MotivoExpiracao);

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Expirado));
    }

    [Test]
    public void ExpirarPorFimDoDia_StatusConfirmado_MudaParaExpirado()
    {
        var a = CriarHistoricoOntem(AgendamentoStatus.Confirmado);

        a.ExpirarPorFimDoDia(MotivoExpiracao);

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Expirado));
    }

    [Test]
    public void ExpirarPorFimDoDia_GravaMotivoCancelamento()
    {
        var a = CriarHistoricoOntem();

        a.ExpirarPorFimDoDia(MotivoExpiracao);

        Assert.That(a.MotivoCancelamento, Is.EqualTo(MotivoExpiracao));
    }

    [Test]
    public void ExpirarPorFimDoDia_AtualizaAtualizadoEm()
    {
        var a = CriarHistoricoOntem();
        var antes = DateTime.UtcNow.AddSeconds(-1);

        a.ExpirarPorFimDoDia(MotivoExpiracao);

        Assert.That(a.AtualizadoEm, Is.GreaterThan(antes));
    }

    // ─── CA2: não toca terminais (Concluido e Cancelado) ──────────────────────

    [Test]
    public void ExpirarPorFimDoDia_StatusConcluido_NoOp()
    {
        var a = CriarHistoricoOntem();
        a.Concluir();
        var statusAntes = a.Status;
        var motivoAntes = a.MotivoCancelamento;

        // Guard: não deve lançar, apenas ignorar.
        Assert.DoesNotThrow(() => a.ExpirarPorFimDoDia(MotivoExpiracao));

        Assert.That(a.Status, Is.EqualTo(statusAntes));
        Assert.That(a.MotivoCancelamento, Is.EqualTo(motivoAntes));
    }

    [Test]
    public void ExpirarPorFimDoDia_StatusCancelado_NoOp()
    {
        var a = CriarHistoricoOntem();
        a.Cancelar("Paciente desistiu.");
        var motivoOriginal = a.MotivoCancelamento;

        Assert.DoesNotThrow(() => a.ExpirarPorFimDoDia(MotivoExpiracao));

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Cancelado));
        Assert.That(a.MotivoCancelamento, Is.EqualTo(motivoOriginal)); // motivo original preservado
    }

    [Test]
    public void ExpirarPorFimDoDia_StatusJaExpirado_NoOp()
    {
        var a = CriarHistoricoOntem();
        a.ExpirarPorFimDoDia(MotivoExpiracao);

        // Segunda chamada (idempotência — CA10).
        Assert.DoesNotThrow(() => a.ExpirarPorFimDoDia(MotivoExpiracao));

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Expirado));
    }

    // ─── CA5: silencioso — sem evento de domínio ──────────────────────────────

    [Test]
    public void ExpirarPorFimDoDia_NaoPublicaAgendamentoCanceladoEvent()
    {
        var a = CriarHistoricoOntem();

        a.ExpirarPorFimDoDia(MotivoExpiracao);

        Assert.That(a.DomainEvents, Is.Empty,
            "ExpirarPorFimDoDia não deve publicar eventos — varredura silenciosa (R3).");
    }

    [Test]
    public void ExpirarPorFimDoDia_StatusConfirmado_NaoPublicaEvento()
    {
        var a = CriarHistoricoOntem(AgendamentoStatus.Confirmado);

        a.ExpirarPorFimDoDia(MotivoExpiracao);

        Assert.That(a.DomainEvents, Is.Empty);
    }
}

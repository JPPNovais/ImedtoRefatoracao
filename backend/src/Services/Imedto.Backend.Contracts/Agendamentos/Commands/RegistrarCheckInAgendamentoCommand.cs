using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

public class RegistrarCheckInAgendamentoCommand : ICommand
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Opcional — sala onde o paciente vai aguardar/ser atendido.</summary>
    public long? SalaId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }

    // ── Dados de cobrança (F1 — Financeiro) ──────────────────────────────────
    /// <summary>Particular | Convenio</summary>
    public string TipoAtendimento { get; set; } = "Particular";
    /// <summary>Valor cobrado do paciente (R2). Ignorado para Convênio (R12).</summary>
    public decimal ValorCobrado { get; set; }
}

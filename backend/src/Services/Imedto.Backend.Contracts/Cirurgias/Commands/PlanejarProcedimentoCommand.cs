using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cirurgias.Commands;

/// <summary>
/// Cria um procedimento cirúrgico no estado Planejado. A equipe inicial é opcional —
/// pode ser ajustada antes da confirmação. Idempotente via header Idempotency-Key.
/// </summary>
public class PlanejarProcedimentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public long ProntuarioId { get; set; }
    public long? AgendamentoId { get; set; }
    public string CirurgiaPrincipal { get; set; } = string.Empty;
    public string? CirurgiaCodigo { get; set; }
    public DateTime? DataAgendada { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public List<EquipeInicialPayload> EquipeInicial { get; set; } = new();

    /// <summary>Preenchido pelo handler — id do procedimento criado.</summary>
    public long ProcedimentoIdCriado { get; set; }
}

public record EquipeInicialPayload(Guid ProfissionalUsuarioId, string Papel);

using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

public class AdicionarListaEsperaCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public Guid? ProfissionalPreferidoId { get; set; }
    /// <summary>"Rotina" | "Prioritario" | "Urgente". Default Rotina.</summary>
    public string Prioridade { get; set; } = "Rotina";
    /// <summary>"Qualquer" | "Manha" | "Tarde". Default Qualquer.</summary>
    public string PreferenciaPeriodo { get; set; } = "Qualquer";
    public Guid CriadoPorUsuarioId { get; set; }
    public long IdCriado { get; set; }
}

public class RemoverListaEsperaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

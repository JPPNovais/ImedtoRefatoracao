using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

public class CriarAgendamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }
    public DateTime InicioPrevisto { get; set; }
    public DateTime FimPrevisto { get; set; }
    public string TipoServico { get; set; } = string.Empty;
    public string? Observacoes { get; set; }

    // Saída preenchida pelo handler
    public long AgendamentoIdCriado { get; set; }
}

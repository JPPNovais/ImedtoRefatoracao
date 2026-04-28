using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

public class AtualizarAgendamentoCommand : ICommand
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public DateTime InicioPrevisto { get; set; }
    public DateTime FimPrevisto { get; set; }
    public string TipoServico { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}

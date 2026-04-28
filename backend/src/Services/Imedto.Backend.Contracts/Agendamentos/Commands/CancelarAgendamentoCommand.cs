using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

public class CancelarAgendamentoCommand : ICommand
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

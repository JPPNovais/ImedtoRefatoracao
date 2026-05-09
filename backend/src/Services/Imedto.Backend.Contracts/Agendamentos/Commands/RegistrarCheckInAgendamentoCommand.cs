using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

public class RegistrarCheckInAgendamentoCommand : ICommand
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}

using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

/// <summary>
/// Aloca uma sala num agendamento. <c>SalaId == null</c> desaloca.
/// Permissão: Dono / Recepcionista / Profissional vinculado (filter
/// <c>RequiresPapel</c> aplicado no controller).
/// </summary>
public class AlocarSalaAgendamentoCommand : ICommand
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? SalaId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}

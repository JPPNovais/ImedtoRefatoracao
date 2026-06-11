using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Conclui manualmente uma pendência de atendimento pelo painel (R14/CA67).
/// Requer prontuario.editar (CA70).
/// </summary>
public class ConcluirPendenciaManualCommand : ICommand
{
    public long PendenciaId { get; init; }
    public long EstabelecimentoId { get; init; }
}

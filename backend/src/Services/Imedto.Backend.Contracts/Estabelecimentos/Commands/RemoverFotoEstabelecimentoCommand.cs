using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Commands;

/// <summary>
/// Remove a foto/logo do estabelecimento — apaga o blob no storage e zera
/// <c>FotoUrl</c> no aggregate. Apenas o dono pode remover.
/// Idempotente: se já não havia foto, não falha.
/// </summary>
public class RemoverFotoEstabelecimentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}

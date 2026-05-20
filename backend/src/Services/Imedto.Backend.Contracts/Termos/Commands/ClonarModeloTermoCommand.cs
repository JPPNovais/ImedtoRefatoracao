using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Clona um modelo padrão do sistema para o estabelecimento atual. Após a clonagem,
/// o tenant pode editar livremente.
/// </summary>
public class ClonarModeloTermoCommand : ICommand
{
    public long ModeloPadraoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }

    /// <summary>Preenchido pelo handler — id do clone criado.</summary>
    public long ModeloIdClonado { get; set; }
}

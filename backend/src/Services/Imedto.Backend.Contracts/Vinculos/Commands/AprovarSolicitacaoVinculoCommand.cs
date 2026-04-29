using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

public class AprovarSolicitacaoVinculoCommand : ICommand
{
    public long SolicitacaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid AprovadoPorUsuarioId { get; set; }
}

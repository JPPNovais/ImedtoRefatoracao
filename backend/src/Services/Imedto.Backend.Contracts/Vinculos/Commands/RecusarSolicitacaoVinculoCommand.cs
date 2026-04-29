using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

public class RecusarSolicitacaoVinculoCommand : ICommand
{
    public long SolicitacaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid RecusadoPorUsuarioId { get; set; }
    public string Motivo { get; set; }
}

using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

/// <summary>
/// Retorna os termos emitidos vinculados a uma evolução específica — exibidos na
/// timeline da evolução (CA-C2). Multi-tenant garantido via EstabelecimentoId.
/// </summary>
public class ListarTermosDaEvolucaoQuery : IQuery<IReadOnlyList<TermoEmitidoResumoDto>>
{
    public long EvolucaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

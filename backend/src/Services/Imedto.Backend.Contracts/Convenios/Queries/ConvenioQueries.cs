using Imedto.Backend.Contracts.Convenios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Convenios.Queries;

public class ListarConveniosQuery : IQuery<IReadOnlyList<ConvenioListadoDto>>
{
    public long EstabelecimentoId { get; set; }
    /// <summary>Quando true, retorna apenas convênios ativos (para selects do check-in/carteirinha).</summary>
    public bool ApenasAtivos { get; set; }
}

public class ObterConvenioQuery : IQuery<ConvenioDetalheDto?>
{
    public long ConvenioId { get; set; }
    public long EstabelecimentoId { get; set; }
}

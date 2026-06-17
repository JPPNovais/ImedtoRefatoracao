using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class BuscarProcedimentoCatalogoQuery : IQuery<IEnumerable<ProcedimentoCatalogoDto>>
{
    public string? Termo { get; set; }
    public string? Origem { get; set; }
    public bool? Ativo { get; set; } = true;
    public int Limit { get; set; } = 10;
}

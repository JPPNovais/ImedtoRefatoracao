using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class ObterProcedimentoPorCodigoQuery : IQuery<ProcedimentoCatalogoDto?>
{
    public string Codigo { get; set; } = string.Empty;
}

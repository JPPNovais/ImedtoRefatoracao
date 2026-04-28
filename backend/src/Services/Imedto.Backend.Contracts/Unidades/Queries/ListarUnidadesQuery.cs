using Imedto.Backend.Contracts.Unidades.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Unidades.Queries;

public class ListarUnidadesQuery : IQuery<IEnumerable<UnidadeDto>>
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}

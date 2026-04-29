using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class ListarEspecialidadesQuery : IQuery<IEnumerable<EspecialidadeListadaDto>>
{
    public long ProfissaoId { get; set; }
    public bool ApenasAtivas { get; set; } = true;
}

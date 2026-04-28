using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Queries;

public class ListarProfissionaisEstabelecimentoQuery : IQuery<IEnumerable<ProfissionalVinculadoDto>>
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}

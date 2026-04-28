using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Queries;

public class ListarMeusEstabelecimentosQuery : IQuery<IEnumerable<EstabelecimentoDto>>
{
    public Guid UsuarioId { get; set; }
}

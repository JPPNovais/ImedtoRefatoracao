using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Queries;

public class ListarListaEsperaQuery : IQuery<IEnumerable<ListaEsperaItemDto>>
{
    public long EstabelecimentoId { get; set; }
}

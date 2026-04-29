using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cirurgias.Queries;

public class ListarProcedimentosPlanejadosQuery : IQuery<IEnumerable<ProcedimentoCirurgicoResumoDto>>
{
    public long EstabelecimentoId { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
}

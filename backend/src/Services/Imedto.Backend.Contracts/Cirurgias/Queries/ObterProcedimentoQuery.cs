using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cirurgias.Queries;

public class ObterProcedimentoQuery : IQuery<ProcedimentoCirurgicoDto>
{
    public long ProcedimentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

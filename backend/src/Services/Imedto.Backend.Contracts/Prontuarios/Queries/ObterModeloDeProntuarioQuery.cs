using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

public class ObterModeloDeProntuarioQuery : IQuery<ModeloProntuarioDto>
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
}

using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Queries;

public class ListarOrcamentosQuery : IQuery<IEnumerable<OrcamentoResumoDto>>
{
    public long EstabelecimentoId { get; set; }
    public long? PacienteId { get; set; }
    public string? Status { get; set; }
}

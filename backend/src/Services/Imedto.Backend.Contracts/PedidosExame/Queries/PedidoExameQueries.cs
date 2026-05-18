using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.PedidosExame.Queries;

public class ListarPedidosExameDoPacienteQuery : IQuery<IReadOnlyList<PedidoExameDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

public class ObterPedidoExameQuery : IQuery<PedidoExameDto>
{
    public long PedidoExameId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

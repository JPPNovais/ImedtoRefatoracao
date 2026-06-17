using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.PedidosExame.Queries;

public class ListarPedidosExameDoPacienteQuery : IQuery<PaginaPedidosExameDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class ObterPedidoExameQuery : IQuery<PedidoExameDto>
{
    public long PedidoExameId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

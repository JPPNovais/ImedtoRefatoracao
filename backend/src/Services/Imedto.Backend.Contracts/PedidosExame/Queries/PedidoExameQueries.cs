using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.PedidosExame.Queries;

public class ListarPedidosExameDoPacienteQuery : IQuery<PaginaPedidosExameDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class ObterPedidoExameQuery : IQuery<PedidoExameDto>
{
    public long PedidoExameId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
}

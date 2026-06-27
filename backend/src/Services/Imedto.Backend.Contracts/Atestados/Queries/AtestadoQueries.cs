using Imedto.Backend.Contracts.Atestados.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Atestados.Queries;

public class ListarAtestadosDoPacienteQuery : IQuery<PaginaAtestadosDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class ObterAtestadoQuery : IQuery<AtestadoDto>
{
    public long AtestadoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
}

public class ListarModelosAtestadoQuery : IQuery<IReadOnlyList<ModeloAtestadoDto>>
{
    public long EstabelecimentoId { get; set; }
}

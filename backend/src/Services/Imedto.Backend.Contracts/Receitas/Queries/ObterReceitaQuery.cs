using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Receitas.Queries;

public class ObterReceitaQuery : IQuery<ReceitaDto>
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
}

using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

public class ListarTermosDoPacienteQuery : IQuery<IReadOnlyList<TermoEmitidoResumoDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Filtro opcional por status (pendente/assinado/recusado/revogado/expirado).</summary>
    public string Status { get; set; }
}

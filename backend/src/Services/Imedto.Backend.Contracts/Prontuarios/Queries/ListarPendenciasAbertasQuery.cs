using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Retorna as pendências abertas (status=Pendente) de um paciente no tenant.
/// Alimenta o painel persistente em PacienteDetalheView (CA68/CA74).
/// </summary>
public class ListarPendenciasAbertasQuery : IQuery<IReadOnlyList<PendenciaAbertaDto>>
{
    public long PacienteId { get; init; }
    public long EstabelecimentoId { get; init; }
}

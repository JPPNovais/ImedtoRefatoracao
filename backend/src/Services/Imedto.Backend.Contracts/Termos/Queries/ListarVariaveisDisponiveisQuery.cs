using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

/// <summary>
/// Lista o catálogo de variáveis disponíveis para uso em modelos. Não toca DB —
/// é estático no resolver. Usado pelo editor do front pra montar a paleta.
/// </summary>
public class ListarVariaveisDisponiveisQuery : IQuery<IReadOnlyList<VariavelDisponivelDto>>
{
    public long EstabelecimentoId { get; set; }
}

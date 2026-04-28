using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Retorna os modelos que o estabelecimento pode usar ao iniciar um prontuário:
/// padrão-sistema (admin) UNION modelos criados pelo próprio estabelecimento.
/// </summary>
public class ListarModelosDisponiveisQuery : IQuery<IEnumerable<ModeloProntuarioDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool ApenasAtivos { get; set; } = true;
}

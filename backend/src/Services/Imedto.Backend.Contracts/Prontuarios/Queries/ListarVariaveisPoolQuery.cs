using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Lista os itens pré-cadastrados (padrão-sistema + do estabelecimento) de um tipo específico.
/// </summary>
public class ListarVariaveisPoolQuery : IQuery<IEnumerable<VariavelPoolDto>>
{
    public long EstabelecimentoId { get; set; }
    public string Tipo { get; set; }
    public bool ApenasAtivos { get; set; } = true;
}

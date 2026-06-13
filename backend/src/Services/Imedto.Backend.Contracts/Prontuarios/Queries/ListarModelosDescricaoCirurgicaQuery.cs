using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Lista os modelos de descrição cirúrgica visíveis para o tenant
/// (padrão-sistema + do estabelecimento).
/// </summary>
public class ListarModelosDescricaoCirurgicaQuery : IQuery<IEnumerable<ModeloDescricaoCirurgicaDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool ApenasAtivos { get; set; } = true;
}

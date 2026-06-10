using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Queries;

public class ListarTabelaPrecoConsultaQuery : IQuery<IEnumerable<TabelaPrecoConsultaDto>>
{
    public long EstabelecimentoId { get; set; }
    public string? BuscaProfissional { get; set; }
}

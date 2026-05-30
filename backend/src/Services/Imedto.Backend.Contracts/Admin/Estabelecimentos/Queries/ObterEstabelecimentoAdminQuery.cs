using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;

public class ObterEstabelecimentoAdminQuery : IQuery<EstabelecimentoAdminDetalheDto?>
{
    public long EstabelecimentoId { get; set; }
    public Guid AdminId { get; set; }
}

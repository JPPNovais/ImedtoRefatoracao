using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;

public class ListarEstabelecimentosAdminQuery : IQuery<PaginaEstabelecimentosAdminDto>
{
    public string? Busca { get; set; }
    public string? Status { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 25;
}

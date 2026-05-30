using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Admin.Estabelecimentos.Queries;

/// <summary>
/// Retorna lista paginada de estabelecimentos para o admin global.
/// Singleton: sem dependências scoped.
/// </summary>
public class ListarEstabelecimentosAdminQueryHandler
    : IRequestHandler<ListarEstabelecimentosAdminQuery, PaginaEstabelecimentosAdminDto>
{
    private readonly IAdminEstabelecimentosQueryRepository _repo;

    public ListarEstabelecimentosAdminQueryHandler(IAdminEstabelecimentosQueryRepository repo)
    {
        _repo = repo;
    }

    public async Task<PaginaEstabelecimentosAdminDto> Handle(ListarEstabelecimentosAdminQuery query)
    {
        var tamanhoPagina = query.TamanhoPagina is > 0 and <= 100 ? query.TamanhoPagina : 25;
        var pagina = query.Pagina >= 1 ? query.Pagina : 1;

        var (itens, total) = await _repo.ListarAsync(
            query.Busca,
            query.Status,
            pagina,
            tamanhoPagina);

        return new PaginaEstabelecimentosAdminDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }
}

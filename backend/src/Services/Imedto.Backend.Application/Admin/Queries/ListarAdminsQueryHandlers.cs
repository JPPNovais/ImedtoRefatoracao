using Imedto.Backend.Contracts.Admin.Admins.Queries;
using Imedto.Backend.Contracts.Admin.Admins.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Admins.Queries;

public class ListarAdminsQueryHandler
{
    private readonly AdminQueryRepository _repo;

    public ListarAdminsQueryHandler(AdminQueryRepository repo) => _repo = repo;

    public async Task<ListarAdminsResult> Handle(ListarAdminsQuery query, CancellationToken ct = default)
    {
        var tamanho = query.Tamanho is > 0 and <= 100 ? query.Tamanho : 25;
        var pagina = query.Pagina <= 0 ? 1 : query.Pagina;

        var (items, total) = await _repo.ListarAdmins(query.Busca, pagina, tamanho);

        return new ListarAdminsResult(items, total, pagina, tamanho);
    }
}

public class ObterAdminQueryHandler
{
    private readonly AdminQueryRepository _repo;

    public ObterAdminQueryHandler(AdminQueryRepository repo) => _repo = repo;

    public Task<AdminDetalheDto?> Handle(ObterAdminQuery query, CancellationToken ct = default) =>
        _repo.ObterAdmin(query.Id);
}

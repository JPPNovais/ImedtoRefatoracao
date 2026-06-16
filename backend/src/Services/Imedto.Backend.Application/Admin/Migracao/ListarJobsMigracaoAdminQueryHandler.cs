using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Lista jobs de migração para o painel admin.
/// Padrão do projeto: query handlers admin são Singleton com repositório Dapper concreto injetado.
/// </summary>
public sealed class ListarJobsMigracaoAdminQueryHandler
{
    private readonly MigracaoAdminQueryRepository _repo;

    public ListarJobsMigracaoAdminQueryHandler(MigracaoAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public async Task<ListarJobsMigracaoAdminResult> Handle(
        ListarJobsMigracaoAdminQuery query,
        CancellationToken ct = default)
    {
        var tamanho = query.Tamanho is > 0 and <= 100 ? query.Tamanho : 25;
        var pagina  = query.Pagina >= 1 ? query.Pagina : 1;

        var (itens, total) = await _repo.ListarJobsAsync(
            query.EstabelecimentoId,
            query.Status,
            pagina,
            tamanho,
            query.CriadoDe,
            query.CriadoAte,
            query.Onda,
            query.Origem,
            ct);

        return new ListarJobsMigracaoAdminResult
        {
            Itens    = itens,
            Total    = total,
            Pagina   = pagina,
            Tamanho  = tamanho,
        };
    }
}

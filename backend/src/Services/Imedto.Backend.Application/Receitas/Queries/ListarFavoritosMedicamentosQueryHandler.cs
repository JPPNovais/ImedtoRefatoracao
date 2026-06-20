using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Receitas.Queries;

/// <summary>
/// Lista favoritos de medicamento do profissional autenticado no estabelecimento ativo.
///
/// Singleton — sem dependências com estado de request (não audita: favoritos são
/// dados operacionais do profissional, não PII do paciente).
/// Multi-tenant: filtro duplo (profissional_usuario_id + estabelecimento_id) no repo.
/// </summary>
public class ListarFavoritosMedicamentosQueryHandler
    : IRequestHandler<ListarFavoritosMedicamentosQuery, IEnumerable<MedicamentoFavoritoDto>>
{
    private readonly IReceitaQueryRepository _queryRepo;

    public ListarFavoritosMedicamentosQueryHandler(IReceitaQueryRepository queryRepo)
        => _queryRepo = queryRepo;

    public async Task<IEnumerable<MedicamentoFavoritoDto>> Handle(
        ListarFavoritosMedicamentosQuery query)
    {
        if (query.ProfissionalUsuarioId == Guid.Empty)
            return Enumerable.Empty<MedicamentoFavoritoDto>();

        var limite = Math.Clamp(query.Limite, 1, 100);

        return await _queryRepo.ListarFavoritos(
            query.ProfissionalUsuarioId,
            query.EstabelecimentoId,
            limite);
    }
}

using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Receitas.Queries;

/// <summary>
/// Ranking de medicamentos favoritos do profissional naquele estabelecimento.
/// Não passa pelo audit log do prontuário — favoritos são um índice estatístico
/// privado do profissional, não acesso a registro clínico de paciente.
/// </summary>
public class ListarMedicamentosFavoritosQueryHandlers
    : IRequestHandler<ListarMedicamentosFavoritosQuery, IEnumerable<MedicamentoFavoritoDto>>
{
    private readonly IReceitaQueryRepository _queryRepo;

    public ListarMedicamentosFavoritosQueryHandlers(IReceitaQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public async Task<IEnumerable<MedicamentoFavoritoDto>> Handle(ListarMedicamentosFavoritosQuery query)
    {
        var top = query.Top is < 1 or > 100 ? 20 : query.Top;
        return await _queryRepo.ListarFavoritos(query.ProfissionalUsuarioId, query.EstabelecimentoId, top);
    }
}

using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Queries;

/// <summary>
/// Handler da listagem pública/minimizada de profissionais.
///
/// O gate de acesso é o <c>[RequiresEstabelecimento]</c> no controller:
/// somente membros ativos do tenant atravessam. O DTO devolvido
/// (<see cref="ProfissionalPublicoDto"/>) já é o nível mínimo de exposição
/// — não há necessidade de gate adicional aqui.
/// </summary>
public class ListarProfissionaisPublicoQueryHandlers
    : IRequestHandler<ListarProfissionaisPublicoQuery, IEnumerable<ProfissionalPublicoDto>>
{
    private readonly VinculoQueryRepository _queryRepository;

    public ListarProfissionaisPublicoQueryHandlers(VinculoQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<ProfissionalPublicoDto>> Handle(ListarProfissionaisPublicoQuery query) =>
        _queryRepository.ListarProfissionaisPublicoDoEstabelecimento(query.EstabelecimentoId);
}

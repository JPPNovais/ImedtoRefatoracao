using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Queries;

public class ListarProfissionaisEstabelecimentoQueryHandlers
    : IRequestHandler<ListarProfissionaisEstabelecimentoQuery, IEnumerable<ProfissionalVinculadoDto>>
{
    private readonly VinculoQueryRepository _queryRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public ListarProfissionaisEstabelecimentoQueryHandlers(
        VinculoQueryRepository queryRepository,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _queryRepository = queryRepository;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task<IEnumerable<ProfissionalVinculadoDto>> Handle(ListarProfissionaisEstabelecimentoQuery query)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(query.EstabelecimentoId);

        if (estab.DonoUsuarioId != query.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode listar seus profissionais.");

        return await _queryRepository.ListarProfissionaisDoEstabelecimento(query.EstabelecimentoId);
    }
}

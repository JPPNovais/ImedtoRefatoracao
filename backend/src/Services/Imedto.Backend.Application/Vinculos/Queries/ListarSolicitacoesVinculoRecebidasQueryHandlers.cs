using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Queries;

/// <summary>
/// Lista as solicitações recebidas pelo estabelecimento. Apenas o dono pode ver
/// — replica a regra do <see cref="ListarProfissionaisEstabelecimentoQueryHandlers"/>.
/// </summary>
public class ListarSolicitacoesVinculoRecebidasQueryHandlers
    : IRequestHandler<ListarSolicitacoesVinculoRecebidasQuery, IEnumerable<SolicitacaoVinculoDto>>
{
    private readonly SolicitacaoVinculoQueryRepository _queryRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public ListarSolicitacoesVinculoRecebidasQueryHandlers(
        SolicitacaoVinculoQueryRepository queryRepository,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _queryRepository = queryRepository;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task<IEnumerable<SolicitacaoVinculoDto>> Handle(ListarSolicitacoesVinculoRecebidasQuery query)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(query.EstabelecimentoId);
        if (estab.DonoUsuarioId != query.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode listar solicitações recebidas.");

        return await _queryRepository.ListarPorEstabelecimento(query.EstabelecimentoId, query.Status);
    }
}

using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Estabelecimentos.Queries;

/// <summary>
/// Retorna detalhe completo de um estabelecimento para o admin.
/// ABRIR_DETALHE_TENANT não é auditado — alto volume, baixo valor forense (Wave 7).
/// Scoped: singleton-safe (sem dependências scoped após remoção do audit).
/// </summary>
public class ObterEstabelecimentoAdminQueryHandler
    : IRequestHandler<ObterEstabelecimentoAdminQuery, EstabelecimentoAdminDetalheDto?>
{
    private readonly IAdminEstabelecimentosQueryRepository _repo;

    public ObterEstabelecimentoAdminQueryHandler(IAdminEstabelecimentosQueryRepository repo)
    {
        _repo = repo;
    }

    public async Task<EstabelecimentoAdminDetalheDto?> Handle(ObterEstabelecimentoAdminQuery query)
    {
        var detalhe = await _repo.ObterDetalheAsync(query.EstabelecimentoId);

        if (detalhe is null)
            throw new BusinessException("Estabelecimento não encontrado.");

        return detalhe;
    }
}

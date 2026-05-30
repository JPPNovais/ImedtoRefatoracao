using Imedto.Backend.Contracts.Admin.Planos.Queries;
using Imedto.Backend.Contracts.Admin.Planos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Planos;

public class ObterPlanoAdminQueryHandler
{
    private readonly ImedtoPlanoQueryRepository _queryRepo;

    public ObterPlanoAdminQueryHandler(ImedtoPlanoQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public async Task<PlanoAdminDto> Handle(ObterPlanoAdminQuery query, CancellationToken ct = default)
    {
        return await _queryRepo.ObterPorIdAsync(query.PlanoId, ct)
            ?? throw new BusinessException("Plano não encontrado.");
    }
}

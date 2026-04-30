using Imedto.Backend.Contracts.Lgpd.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Lgpd.Queries;

public class ExportarMeusDadosLgpdQueryHandlers : IRequestHandler<ExportarMeusDadosQuery, MeusDadosLgpdDto>
{
    private readonly LgpdQueryRepository _repo;

    public ExportarMeusDadosLgpdQueryHandlers(LgpdQueryRepository repo) => _repo = repo;

    public async Task<MeusDadosLgpdDto> Handle(ExportarMeusDadosQuery query)
    {
        var dados = await _repo.ExportarMeusDados(query.UsuarioId);
        if (dados is null)
            throw new BusinessException("Usuário não encontrado.");

        return dados;
    }
}

using Imedto.Backend.Contracts.Profissionais.Queries;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Profissionais.Queries;

public class ObterProfissionalMeQueryHandlers : IRequestHandler<ObterProfissionalMeQuery, ProfissionalDto>
{
    private readonly ProfissionalQueryRepository _queryRepository;

    public ObterProfissionalMeQueryHandlers(ProfissionalQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<ProfissionalDto> Handle(ObterProfissionalMeQuery query) =>
        _queryRepository.ObterPorUsuario(query.UsuarioId);
}

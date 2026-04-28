using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Queries;

public class ObterAgendamentoQueryHandlers : IRequestHandler<ObterAgendamentoQuery, AgendamentoDto>
{
    private readonly AgendamentoQueryRepository _repo;

    public ObterAgendamentoQueryHandlers(AgendamentoQueryRepository repo)
        => _repo = repo;

    public async Task<AgendamentoDto> Handle(ObterAgendamentoQuery query)
    {
        var dto = await _repo.ObterPorId(query.AgendamentoId);
        if (dto is null || dto.EstabelecimentoId != query.EstabelecimentoId)
            throw new BusinessException("Agendamento não encontrado.");
        return dto;
    }
}

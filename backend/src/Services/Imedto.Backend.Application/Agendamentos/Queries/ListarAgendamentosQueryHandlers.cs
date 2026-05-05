using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Agendamentos.Queries;

public class ListarAgendamentosQueryHandlers : IRequestHandler<ListarAgendamentosQuery, PaginaAgendamentosDto>
{
    private readonly AgendamentoQueryRepository _repo;

    public ListarAgendamentosQueryHandlers(AgendamentoQueryRepository repo)
        => _repo = repo;

    public Task<PaginaAgendamentosDto> Handle(ListarAgendamentosQuery query)
        => _repo.Listar(
            query.EstabelecimentoId,
            query.DataInicio,
            query.DataFim,
            query.ProfissionalUsuarioId,
            query.PacienteId,
            query.Status,
            query.Pagina,
            query.TamanhoPagina);
}

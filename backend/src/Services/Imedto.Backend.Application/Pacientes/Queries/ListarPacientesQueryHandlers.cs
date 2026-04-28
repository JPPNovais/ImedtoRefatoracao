using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

public class ListarPacientesQueryHandlers : IRequestHandler<ListarPacientesQuery, PaginaPacientesDto>
{
    private readonly PacienteQueryRepository _queryRepository;

    public ListarPacientesQueryHandlers(PacienteQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<PaginaPacientesDto> Handle(ListarPacientesQuery query) =>
        _queryRepository.Listar(query.EstabelecimentoId, query.Busca, query.Pagina, query.TamanhoPagina);
}

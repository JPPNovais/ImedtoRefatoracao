using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

public class ListarPacientesQuery : IQuery<PaginaPacientesDto>
{
    public long EstabelecimentoId { get; set; }
    public string Busca { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

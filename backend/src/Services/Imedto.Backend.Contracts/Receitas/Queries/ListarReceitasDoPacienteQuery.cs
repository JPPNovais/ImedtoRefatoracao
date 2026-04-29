using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Queries;

public class ListarReceitasDoPacienteQuery : IQuery<PaginaReceitasDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

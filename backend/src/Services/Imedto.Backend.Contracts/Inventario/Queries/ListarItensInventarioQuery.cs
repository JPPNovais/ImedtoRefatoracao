using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Queries;

public class ListarItensInventarioQuery : IQuery<PaginaItensInventarioDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Categoria { get; set; }
    public bool? ApenasAbaixoMinimo { get; set; }
    public bool ApenasAtivos { get; set; } = true;
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

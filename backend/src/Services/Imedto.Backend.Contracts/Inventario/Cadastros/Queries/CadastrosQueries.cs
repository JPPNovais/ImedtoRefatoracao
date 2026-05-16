using Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Cadastros.Queries;

public class ListarCategoriasEstoqueQuery : IQuery<PaginaCategoriasEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

public class ListarFabricantesEstoqueQuery : IQuery<PaginaFabricantesEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

public class ListarFornecedoresEstoqueQuery : IQuery<PaginaFornecedoresEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

public class ListarLocaisEstoqueQuery : IQuery<PaginaLocaisEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

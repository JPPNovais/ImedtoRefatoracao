using Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Cadastros.Queries;

public class ListarCategoriasEstoqueQuery : IQuery<PaginaCategoriasEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class ListarFabricantesEstoqueQuery : IQuery<PaginaFabricantesEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class ListarFornecedoresEstoqueQuery : IQuery<PaginaFornecedoresEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class ListarLocaisEstoqueQuery : IQuery<PaginaLocaisEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Busca { get; set; }
    public bool? ApenasAtivos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

// ─── Queries leves para popular dropdowns ────────────────────────────────────
// Servem apenas a selects de formulário (ex.: drawer "Novo produto"). Sempre filtram
// `ativo = true`, ordenam por nome e limitam a 500 registros — não substituem
// as listagens paginadas acima.

public class ObterOpcoesCategoriasEstoqueQuery : IQuery<IReadOnlyList<OpcaoCadastroEstoqueDto>>
{
    public long EstabelecimentoId { get; set; }
}

public class ObterOpcoesFabricantesEstoqueQuery : IQuery<IReadOnlyList<OpcaoCadastroEstoqueDto>>
{
    public long EstabelecimentoId { get; set; }
}

public class ObterOpcoesFornecedoresEstoqueQuery : IQuery<IReadOnlyList<OpcaoCadastroEstoqueDto>>
{
    public long EstabelecimentoId { get; set; }
}

public class ObterOpcoesLocaisEstoqueQuery : IQuery<IReadOnlyList<OpcaoCadastroEstoqueDto>>
{
    public long EstabelecimentoId { get; set; }
}

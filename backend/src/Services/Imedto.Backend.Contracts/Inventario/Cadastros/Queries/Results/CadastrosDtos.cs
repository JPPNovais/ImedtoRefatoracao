namespace Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;

/// <summary>DTO minimizado de categoria — só os campos que a tela usa (LGPD §minimização).</summary>
public class CategoriaEstoqueDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    /// <summary>Contagem de itens vinculados — útil pra UI ("12 itens nesta categoria").</summary>
    public int QuantidadeItens { get; set; }
}

public class FabricanteEstoqueDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public bool Ativo { get; set; }
    public int QuantidadeItens { get; set; }
}

public class FornecedorEstoqueDto
{
    public long Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    /// <summary>Apenas 14 dígitos — front formata. Nunca é logado.</summary>
    public string? Cnpj { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoTelefone { get; set; }
    public string? ContatoEmail { get; set; }
    public int PrazoEntregaDias { get; set; }
    public bool Ativo { get; set; }
    public int QuantidadeItens { get; set; }
}

public class LocalEstoqueDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? AndarSetor { get; set; }
    public string? Responsavel { get; set; }
    public bool Ativo { get; set; }
    public int QuantidadeItens { get; set; }
}

public class PaginaCategoriasEstoqueDto
{
    public IEnumerable<CategoriaEstoqueDto> Itens { get; set; } = Array.Empty<CategoriaEstoqueDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class PaginaFabricantesEstoqueDto
{
    public IEnumerable<FabricanteEstoqueDto> Itens { get; set; } = Array.Empty<FabricanteEstoqueDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class PaginaFornecedoresEstoqueDto
{
    public IEnumerable<FornecedorEstoqueDto> Itens { get; set; } = Array.Empty<FornecedorEstoqueDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class PaginaLocaisEstoqueDto
{
    public IEnumerable<LocalEstoqueDto> Itens { get; set; } = Array.Empty<LocalEstoqueDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

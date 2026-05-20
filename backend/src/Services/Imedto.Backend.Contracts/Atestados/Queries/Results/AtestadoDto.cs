namespace Imedto.Backend.Contracts.Atestados.Queries.Results;

public class AtestadoDto
{
    public long Id { get; set; }
    public long PacienteId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? DiasAfastamento { get; set; }
    public string? Cid10 { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public class PaginaAtestadosDto
{
    public IEnumerable<AtestadoDto> Itens { get; set; } = Array.Empty<AtestadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class ModeloAtestadoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

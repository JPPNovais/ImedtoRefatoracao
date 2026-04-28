namespace Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;

public class ModeloPermissaoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string TipoAcesso { get; set; } = string.Empty;
    public IReadOnlyList<string> Permissoes { get; set; } = Array.Empty<string>();
    public bool EhPadrao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}
